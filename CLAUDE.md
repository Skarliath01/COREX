# Corex by Altysin — CLAUDE.md

Optimiseur PC Windows nouvelle génération, concurrent FPSDoctor, marché FR gaming 18–30 ans.
Tagline : "Your PC. Your Core. Unleashed." Repo : `Skarliath01/corex`. Ne pas toucher à `pokegenesis`.
Solo dev, MVP V1 en 16 semaines, freemium (Free / Pro 4€/mois / Ultimate 8€/mois).

---

## Commandes

```bash
dotnet build src/Corex.sln --configuration Debug --no-restore
dotnet test src/Corex.Tests/Corex.Tests.csproj --filter "Category=Unit"
dotnet test src/Corex.Tests/Corex.Tests.csproj --filter "Category=Integration"
dotnet format src/Corex.sln --verify-no-changes --severity warn
dotnet run --project src/Corex.App/Corex.App.csproj
```

---

## Règle absolue — Tweaks hardware-adaptatifs

**Vérifier le hardware AVANT tout tweak. Sans condition = interdit.**

```csharp
// ✅ Conditionnel obligatoire
if (hw.Gpu.Vendor == GpuVendor.Nvidia) ApplyNvidiaLatencyTweak();
if (hw.PrimaryStorage.Type == StorageType.Hdd) EnableSysMain(); else DisableSysMain();

// ❌ Jamais aveugle — ni défrag sur SSD, ni tweak NVIDIA sur AMD
ApplyNvidiaLatencyTweak(); RunDefrag();
```

Indicateur risque sur chaque `TweakDefinition` : 🟢 Safe · 🟡 Modéré · 🔴 Expert (double confirmation)

---

## Git — Règles absolues

```
feature/* fix/* hotfix/*   ←  TOUJOURS depuis `dev`
    ↓ squash merge
   dev  →  (stable) merge commit  →  staging  →  merge commit + tag  →  main  →  CI release
```

- Jamais de push direct sur `dev`, `staging`, `main`
- Squash merge uniquement vers `dev`
- Branches : `feature/m1-hardware-detection` · `fix/wmi-timeout` · `hotfix/license-null`
- Commits : `feat(m1): add GPU detection` — scopes : `m1`→`m11` `infra` `ui` `backend` `native`
- Hotfix part quand même de `dev`, promotion rapide

---

## Workflow AI — Séquence par feature

```
gstack /office-hours        →  cadrage, vision, risques (début de projet / sprint)
gstack /plan-ceo-review     →  décision produit (optionnel — si arbitrage scope nécessaire)
gstack /plan-eng-review     →  design technique de la feature (avant chaque feature)
PAUL Plan                   →  ancrage session
Context7                    →  doc APIs
Superpowers TDD             →  Clarify → Plan → Test → Implement → Verify
Headroom (70%)              →  Unify automatique
PAUL Unify + Memory         →  clôture
```

Template PAUL Plan : `État backlog [F01 done / F02 en cours] · Décisions récentes [...] · Objectif session [Feature XX]`

---

## Stack

| Couche | Technologie |
|--------|-------------|
| UI | WinUI 3 (WASDK) + C# 13 / .NET 10 LTS, MVVM — `Corex.App/` |
| Core | C# 13 / .NET 10 LTS, records immuables, services — `Corex.Core/` |
| Engine | C# + C++/P/Invoke, WMI, Registry — `Corex.Engine/` |
| Native | C++ DLL, NVAPI + ADL + IGCL — `Corex.Native/` |
| Backend | Node.js + Express + PostgreSQL — `backend/` |
| Installeur | Inno Setup 6, EV Sectigo, TSA `http://timestamp.sectigo.com` |
| Tests | xUnit + Moq, `[Category=Unit]` + `[Category=Integration]` |

Cible : Windows 10 22H2 min · x64 uniquement V1 · UAC `requireAdministrator` au lancement.

---

## Anti-patterns — Jamais

- ❌ Tweak sans condition hardware · Défrag sur SSD · SysMain sans vérifier le stockage
- ❌ Push direct sur `dev`/`staging`/`main` · Merge commit vers `dev` (squash uniquement)
- ❌ `.Result` / `.Wait()` en async · `Binding` XAML (→ `x:Bind`) · Code métier en `.xaml.cs`
- ❌ API ou NuGet sans Context7 d'abord · Code sans PAUL Plan préalable
- ❌ Tweak 🔴 Expert sans double confirmation · Binaire distribué non signé EV
- ❌ `Console.WriteLine` en prod · Secret dans le code source · Toucher `pokegenesis`

---

## Feature en cours : F01 — Détection hardware

Branch : `feature/m1-hardware-detection` depuis `dev`

```
src/Corex.Core/Interfaces/IHardwareDetector.cs
src/Corex.Core/Models/HardwareProfile.cs + CpuInfo + GpuInfo + RamInfo + StorageInfo
src/Corex.Core/Services/HardwareDetectionService.cs
src/Corex.Engine/Wmi/WmiQuery.cs + WmiCache.cs
Corex.Tests/Unit/HardwareDetectionTests.cs  [Category=Unit]
```

Spec : CPU (vendor, cores, fréq), GPU (Nvidia/AMD/Intel, VRAM), RAM (total, DDR4/5), Stockage (NVMe/SSD/HDD, SMART).
Cache 60 min. `HardwareProfile.Current` complet <3s. Tests sur profils Moq sans hardware réel.
Commit : `feat(m1): implement hardware detection via WMI`

---

## Références .claude/

| Fichier | Contenu |
|---------|---------|
| `architecture.md` | Structure modules, couches, patterns MVVM, schéma DB |
| `conventions.md` | Naming C#/C++/XAML, async, records, PR template, gitignore |
| `modules.md` | 12 modules M1→M12, scope V1/V2/V3 détaillé |
| `backlog.md` | 22 features V1, ordre semaine/semaine, DoD, métriques |
| `security.md` | EV Authenticode, RGPD, snapshot policy, CorexRestore.exe |
| `cicd.md` | 3 workflows GitHub Actions (ci.yml, release.yml, nightly.yml) |
| `workflow.md` | Guide gstack → PAUL → Superpowers avec exemples Corex |
| `rules/hardware.md` | WMI classes, models, Moq fixtures (auto-chargé sur Corex.Core/Hardware) |
| `rules/testing.md` | TDD cycle, naming, Arrange/Act/Assert (auto-chargé sur Corex.Tests) |
| `rules/ui.md` | x:Bind, MVVM, palette Corex (auto-chargé sur Corex.App) |
| `rules/engine.md` | ITweakRule, snapshot pattern (auto-chargé sur Corex.Core/Rules + Corex.Engine) |

