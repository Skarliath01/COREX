# Corex — Architecture

## Vue d'ensemble

```text
┌─────────────────────────────────────────────────────────────┐
│                    COREX DESKTOP APP                        │
│            C# 13 + .NET 10 LTS + WinUI 3 (WASDK)            │
├──────────────┬──────────────┬──────────────┬───────────────┤
│   UI Layer   │ Core Layer   │ Engine Layer │ Native Layer  │
│  (WinUI 3)   │  (C# Logic)  │  (C# + C++)  │  (C++/WinAPI) │
└──────────────┴──────────────┴──────────────┴───────────────┘
         │              │              │              │
         └──────────────┴──────────────┴──────────────┘
                              │
                    ┌─────────────────┐
                    │  Backend API    │
                    │ Node.js/Express │
                    │  PostgreSQL     │
                    └─────────────────┘
```text

---

## Structure des dossiers

```text
Corex/
├── src/
│   ├── Corex.App/                    # Projet WinUI 3 principal
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── Views/                    # Pages XAML
│   │   │   ├── DashboardPage.xaml
│   │   │   ├── OptimizePage.xaml
│   │   │   ├── UninstallerPage.xaml
│   │   │   ├── MonitorPage.xaml
│   │   │   └── SettingsPage.xaml
│   │   ├── ViewModels/               # MVVM ViewModels
│   │   │   ├── DashboardViewModel.cs
│   │   │   ├── OptimizeViewModel.cs
│   │   │   └── ...
│   │   └── Assets/
│   │
│   ├── Corex.Core/                   # Logique métier pure
│   │   ├── Models/
│   │   │   ├── HardwareProfile.cs    # Profil hardware complet détecté
│   │   │   ├── TweakDefinition.cs    # Définition d'un tweak
│   │   │   ├── TweakResult.cs        # Résultat d'application
│   │   │   └── SystemSnapshot.cs     # Snapshot avant modification
│   │   ├── Services/
│   │   │   ├── HardwareDetectionService.cs
│   │   │   ├── TweakEngineService.cs
│   │   │   ├── SnapshotService.cs
│   │   │   ├── UninstallerService.cs
│   │   │   └── LicenseService.cs
│   │   ├── Rules/                    # Moteur de règles conditionnelles
│   │   │   ├── ITweakRule.cs
│   │   │   ├── CpuRules.cs
│   │   │   ├── GpuRules.cs
│   │   │   ├── RamRules.cs
│   │   │   └── StorageRules.cs
│   │   └── Interfaces/
│   │       ├── IHardwareDetector.cs
│   │       ├── ITweakEngine.cs
│   │       └── ISnapshotManager.cs
│   │
│   ├── Corex.Engine/                 # Accès bas niveau
│   │   ├── Wmi/
│   │   │   ├── WmiQuery.cs
│   │   │   └── WmiCache.cs
│   │   ├── Registry/
│   │   │   ├── RegistryTweak.cs
│   │   │   └── RegistrySnapshot.cs
│   │   ├── Services/
│   │   │   ├── WindowsServiceManager.cs
│   │   │   ├── ScheduledTaskManager.cs
│   │   │   └── ProcessManager.cs
│   │   └── Native/                   # P/Invoke + C++ interop
│   │       ├── NativeMethods.cs
│   │       └── GpuApi.cs
│   │
│   ├── Corex.Native/                 # Projet C++ (DLL)
│   │   ├── gpu_monitor.cpp           # APIs GPU NVIDIA/AMD/Intel
│   │   ├── timer_resolution.cpp      # timeBeginPeriod/timeEndPeriod
│   │   ├── smart_reader.cpp          # Lecture SMART stockage
│   │   └── corex_native.h
│   │
│   └── Corex.Tests/                  # Tests unitaires + intégration
│       ├── Unit/
│       │   ├── HardwareDetectionTests.cs
│       │   ├── TweakEngineTests.cs
│       │   └── SnapshotTests.cs
│       └── Integration/
│           ├── TweakApplicationTests.cs
│           └── RestoreTests.cs
│
├── backend/                          # API Node.js
│   ├── src/
│   │   ├── routes/
│   │   │   ├── licenses.ts
│   │   │   ├── updates.ts
│   │   │   └── analytics.ts
│   │   ├── middleware/
│   │   └── db/
│   │       └── schema.sql
│   ├── package.json
│   └── tsconfig.json
│
├── installer/                        # Inno Setup
│   ├── corex_setup.iss
│   └── assets/
│
├── .claude/                          # Guides IA
├── .github/
│   └── workflows/
├── docs/
└── README.md
```csharp

---

## Patterns architecturaux

### MVVM strict

- **View** (XAML) : zéro logique métier, bindings uniquement
- **ViewModel** : logique UI, commandes, état observable
- **Model/Service** : logique métier pure, testable sans UI

### Moteur de règles conditionnelles (cœur du produit)

```csharp
// Chaque tweak déclare ses conditions hardware
public class DisableSysMainTweak : ITweak
{
    public bool IsApplicable(HardwareProfile hw) =>
        hw.PrimaryStorage.Type == StorageType.NvmeSsd &&
        hw.PrimaryStorage.ReadSpeedMbps > 2000;

    public RiskLevel Risk => RiskLevel.Moderate;
    public string RegistryKey => @"SYSTEM\CurrentControlSet\Services\SysMain";
    public string ValueName => "Start";
    public object SafeValue => 4; // Disabled
    public object DefaultValue => 2; // Automatic
}
```

### Snapshot avant toute modification

```csharp
// Pattern obligatoire — jamais bypasser
using var snapshot = await _snapshotService.CreateAsync("pre-tweak-session");
try {
    await _tweakEngine.ApplyAsync(selectedTweaks);
    await snapshot.CommitAsync();
} catch {
    await snapshot.RollbackAsync();
    throw;
}
```

### Indicateurs de risque

```csharp
public enum RiskLevel
{
    Safe,      // 🟢 HKCU, services non-essentiels, fichiers temp
    Moderate,  // 🟡 Services système, réseau, alimentation
    Expert     // 🔴 Timer resolution, HPET, Core Parking — double confirmation
}
```

---

## Couche Native (C++ DLL)

Utilisée uniquement pour ce que C#/WMI ne peut pas atteindre :

- Monitoring GPU temps réel (NVAPI pour NVIDIA, ADL pour AMD, IGCL pour Intel)
- `timeBeginPeriod(1)` / `timeEndPeriod(1)` pour timer resolution
- Lecture SMART disques via DeviceIoControl
- Accès IRQ et affinité CPU bas niveau

Chaque appel natif est wrappé dans un service C# avec fallback gracieux si la DLL n'est pas disponible.

---

## Backend API (Node.js + PostgreSQL)

### Endpoints principaux

```text
POST   /api/v1/licenses/validate      # Validation clé licence
POST   /api/v1/licenses/activate      # Activation machine
GET    /api/v1/updates/check          # Vérification mise à jour
POST   /api/v1/analytics/heartbeat    # Heartbeat anonyme (opt-in)
```sql

### Schéma DB minimal

```sql
CREATE TABLE licenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(64) UNIQUE NOT NULL,
    tier VARCHAR(20) NOT NULL,       -- 'free' | 'pro' | 'ultimate'
    email VARCHAR(255),
    activated_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    machine_hash VARCHAR(64),        -- hash anonyme de la machine
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE update_channels (
    version VARCHAR(20) PRIMARY KEY,
    channel VARCHAR(20) NOT NULL,    -- 'stable' | 'beta'
    download_url TEXT NOT NULL,
    sha256 VARCHAR(64) NOT NULL,
    release_notes TEXT,
    published_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## Dépendances clés

| Dépendance | Usage | Justification |
|-----------|-------|---------------|
| `CommunityToolkit.Mvvm` | MVVM helpers | Officiel Microsoft, léger |
| `Microsoft.Win32.Registry` | Accès Registry | Natif .NET |
| `System.Management` | WMI queries | Natif .NET |
| `NVAPI` (C++) | Monitoring GPU NVIDIA | Officiel NVIDIA |
| `ADL SDK` (C++) | Monitoring GPU AMD | Officiel AMD |
| `Inno Setup 6` | Installeur | Standard industrie |
| `xUnit` | Tests unitaires | Standard .NET |

> Aucune dépendance NuGet non-Microsoft pour le cœur du moteur — réduire la surface d'attaque AV.

---

## Contraintes techniques critiques

1. **Compatibilité** : Windows 10 22H2 minimum, Windows 11 supporté
2. **Architecture** : x64 uniquement en V1 (ARM64 en V2)
3. **Droits** : Elevation UAC requise au lancement (manifest `requireAdministrator`)
4. **Signature** : Tout exécutable et DLL signés avec le même certificat EV
5. **Antivirus** : Zéro obfuscation, zéro packing, zéro hook kernel non documenté
6. **Threading** : Tous les tweaks exécutés sur un thread dédié, jamais sur le UI thread
