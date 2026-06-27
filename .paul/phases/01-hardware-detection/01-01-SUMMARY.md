---
phase: 01-hardware-detection
plan: 01
subsystem: engine
tags: [wmi, hardware, cpu, gpu, ram, storage, detection, csharp, xunit, moq]

requires: []

provides:
  - IHardwareDetector + IWmiQuery interfaces
  - HardwareProfile record (CpuInfo, GpuInfo, RamInfo, StorageInfo)
  - HardwareDetectionService (thread-safe, cache SemaphoreSlim, TTL 60 min)
  - WmiQueryExecutor (Win32_Processor, Win32_VideoController, Win32_PhysicalMemory, MSFT_PhysicalDisk)
  - 31 tests unitaires sans hardware réel (Moq)

affects: [F02-regles-conditionnelles, F03-tweaks, F04-ui-dashboard]

tech-stack:
  added: [System.Management, Microsoft.Win32.Registry, xUnit, Moq, Microsoft.Extensions.Options]
  patterns:
    - "Interface-first (IHardwareDetector / IWmiQuery) — découplage Engine / Core"
    - "Cache volatile + SemaphoreSlim pour thread-safety"
    - "Records immuables pour modèles hardware"
    - "Fallback gracieux vers Unknown sur toute exception WMI"

key-files:
  created:
    - src/Corex.Core/Interfaces/IHardwareDetector.cs
    - src/Corex.Core/Interfaces/IWmiQuery.cs
    - src/Corex.Core/Models/HardwareProfile.cs
    - src/Corex.Core/Models/CpuInfo.cs
    - src/Corex.Core/Models/GpuInfo.cs
    - src/Corex.Core/Models/RamInfo.cs
    - src/Corex.Core/Models/StorageInfo.cs
    - src/Corex.Core/Services/HardwareDetectionService.cs
    - src/Corex.Core/Services/HardwareDetectionOptions.cs
    - src/Corex.Engine/Wmi/IWmiQueryExecutor.cs
    - src/Corex.Engine/Wmi/WmiQuery.cs
    - src/Corex.Engine/Wmi/WmiQueryExecutor.cs
    - src/Corex.Tests/Fixtures/HardwareFixtures.cs
    - src/Corex.Tests/Unit/HardwareDetectionTests.cs
    - src/Corex.Tests/Unit/WmiQueryTests.cs
  modified: []

key-decisions:
  - "D1: IWmiQuery mocké directement — pas besoin de mocker WmiQueryExecutor en tests unitaires"
  - "D2: PrimaryGpu = GPU non-Intel avec plus de VRAM ; fallback Intel si seul GPU"
  - "D3: AdapterRAM uint.MaxValue → fallback registry HardwareInformation.qwMemorySize (OQ1)"
  - "D4: MSFT_PhysicalDisk root\\microsoft\\windows\\storage — plus fiable que Win32_DiskDrive (OQ2)"
  - "D5: Disque principal = plus grand par taille (pas par index)"
  - "D6: Cache 60 min configurable via HardwareDetectionOptions.CacheTtl"
  - "D7: RefreshAsync retourne HardwareProfile.Unknown si DetectAsync jamais appelé"
  - "D8: GPU AMD dual-GPU → sélection par VRAM max (OQ3)"
  - "D9: HardwareProfile.HasUnknownComponents = true si au moins un composant Unknown"

patterns-established:
  - "Tweak conditionnel obligatoire : vérifier hw.Gpu.Vendor / hw.PrimaryStorage.Type avant tout tweak"
  - "Tests unitaires via Moq sans hardware — profils NvidiaGamingRig, AmdRig, LaptopDualGpu"

duration: ~3 sessions
started: 2026-06-23T20:00:00Z
completed: 2026-06-23T22:30:00Z
---

# Phase 01 Plan 01 : F01 Hardware Detection Summary

**Détection hardware complète via WMI — CPU/GPU/RAM/Stockage avec cache thread-safe, fallback Unknown, et 31 tests Moq sans hardware réel. Squash mergé dans `dev` (commit `aae0bdd`).**

## Performance

| Métrique | Valeur |
|----------|--------|
| Durée | ~3 sessions |
| Démarré | 2026-06-23 |
| Terminé | 2026-06-23 |
| Tâches | 6 complètes |
| Fichiers créés | 13 |
| Tests | 31 unitaires (16 HardwareDetectionTests + 15 WmiQueryTests) |

## Acceptance Criteria Results

| Critère | Statut | Notes |
|---------|--------|-------|
| CPU vendor/cores/fréq détectés via WMI | Pass | Intel GenuineIntel, AMD AuthenticAMD |
| GPU primary sélectionné avec tiebreaker VRAM | Pass | Non-Intel max VRAM ; fallback Intel |
| RAM total + génération DDR4/DDR5 | Pass | SMBIOSMemoryType 26/34 |
| Stockage NVMe/SSD/HDD + SMART status | Pass | BusType 11=NVMe, MediaType 4/3 |
| Cache TTL configurable + thread-safe | Pass | SemaphoreSlim + volatile _cache |
| Fallback Unknown sur toute exception WMI | Pass | HasUnknownComponents = true |
| 0 tests en échec | Pass | 31/31 verts |

## Accomplissements

- Couche détection hardware complète sans dépendance sur hardware réel (testable via Moq)
- Résolution de 3 quirks WMI : AdapterRAM overflow (OQ1), namespace storage (OQ2), GPU AMD dual (OQ3)
- Architecture découplée : `Corex.Core` ne dépend pas de `System.Management` — uniquement `Corex.Engine`

## Task Commits

| Tâche | Commit | Description |
|-------|--------|-------------|
| F01 squash complet | `aae0bdd` | feat(m1): F01 — hardware detection via WMI (#4) |
| Hotfix release CI | `c2cd2a0` | fix(infra): use msbuild /t:Publish for WinUI 3 (#6) |
| Markdown lint | `d326490` | fix(infra): fix markdownlint errors across all docs |

## Fichiers Créés

| Fichier | Rôle |
|---------|------|
| `Corex.Core/Interfaces/IHardwareDetector.cs` | Contrat détection async |
| `Corex.Core/Interfaces/IWmiQuery.cs` | Contrat requêtes WMI mockable |
| `Corex.Core/Models/HardwareProfile.cs` | Record agrégat + HasUnknownComponents |
| `Corex.Core/Models/CpuInfo.cs` | Vendor, cores, fréq |
| `Corex.Core/Models/GpuInfo.cs` | Vendor, VRAM, name |
| `Corex.Core/Models/RamInfo.cs` | TotalBytes, génération DDR |
| `Corex.Core/Models/StorageInfo.cs` | Type NVMe/SSD/HDD, SMART |
| `Corex.Core/Services/HardwareDetectionService.cs` | Cache + SemaphoreSlim + fallback |
| `Corex.Core/Services/HardwareDetectionOptions.cs` | CacheTtl = 60 min par défaut |
| `Corex.Engine/Wmi/IWmiQueryExecutor.cs` | Abstraction bas niveau |
| `Corex.Engine/Wmi/WmiQuery.cs` | Implémentation 4 méthodes Detect* |
| `Corex.Engine/Wmi/WmiQueryExecutor.cs` | ManagementObjectSearcher, timeout 3s |
| `Corex.Tests/Fixtures/HardwareFixtures.cs` | Rows WMI en dict |
| `Corex.Tests/Unit/HardwareDetectionTests.cs` | 16 tests service |
| `Corex.Tests/Unit/WmiQueryTests.cs` | 15 tests WmiQuery |

## Décisions Actées

| Décision | Raison | Impact F02+ |
|----------|--------|-------------|
| D1 : Mock IWmiQuery direct | Isole le service du WMI réel | Tests rapides, pas de VM nécessaire |
| D2 : PrimaryGpu = max VRAM non-Intel | Cas dual-GPU AMD/Nvidia + iGPU | F02 peut lire hw.PrimaryGpu.Vendor sans ambiguïté |
| D3 : Fallback registry VRAM | AdapterRAM sature à 4GB uint32 | Cartes >4GB correctement détectées |
| D4 : MSFT_PhysicalDisk | Win32_DiskDrive pas fiable NVMe | BusType 11 = NVMe garanti Win10 22H2+ |
| D6 : CacheTtl configurable | Flexibilité tests (TTL=0) et prod (60 min) | Injection via IOptions<> |
| D9 : HasUnknownComponents | UI peut afficher bandeau d'avertissement | F04 utilisera cette propriété |

## Déviations

| Type | Nb | Impact |
|------|----|--------|
| Deferré V2 | 1 | WmiCache distribué — non nécessaire V1 |
| Deferré F04 | 1 | Bandeau UI "composant inconnu" |

**WmiCache V2 :** Initialement envisagé un cache partagé entre sessions processus. Retiré — le cache in-memory par instance suffît pour V1. À réévaluer si multi-process.

**Bandeau Unknown F04 :** `HasUnknownComponents` implémenté. L'affichage UI est délibérément déféré à F04 (dashboard).

## Points Ouverts

| Ref | Description | Cible |
|-----|-------------|-------|
| V2-01 | WmiCache distribué inter-sessions | V2 |
| F04-01 | Bandeau UI "composant matériel non détecté" | F04 |
| V1.1-01 | Réseau (adaptateur, débit) | V1.1 |
| V1.1-02 | OS (version, BIOS, Secure Boot) | V1.1 |

## Next Phase Readiness

**Prêt :**
- `hw.PrimaryGpu.Vendor`, `hw.PrimaryStorage.Type`, `hw.Cpu.Vendor` disponibles dans `HardwareProfile`
- Pattern conditionnel établi : `if (hw.Gpu.Vendor == GpuVendor.Nvidia) ApplyNvidiaLatencyTweak()`
- IHardwareDetector injectable via DI — F02 peut consommer directement

**Prochaine session :**
- F02 — Moteur de règles conditionnelles : `TweakDefinition`, `IsApplicableCore`, `ITweakRule`
- Entrée : PAUL Plan F02 → `/plan-eng-review` → `lance Superpowers TDD`

**Blockers :** Aucun

---
*Phase: 01-hardware-detection, Plan: 01*
*Terminé: 2026-06-23*
