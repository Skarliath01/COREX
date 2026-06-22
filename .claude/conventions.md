# Corex — Conventions

## Git Flow

### Les 3 branches permanentes

```
main        ← Production — code déployé aux utilisateurs finaux
staging     ← Recette — validation avant mise en prod
dev         ← Intégration — toutes les features mergées ici en premier
```

**Règle absolue : aucun push direct sur `main`, `staging` ou `dev`. Tout passe par PR.**

| Branche | Rôle | Protection | Environnement |
|---------|------|-----------|--------------|
| `main` | Production — release stable | PR obligatoire depuis staging uniquement, 0 push direct | Prod (users finaux) |
| `staging` | Recette — validation QA | PR obligatoire depuis dev uniquement | Staging (tests) |
| `dev` | Intégration continue | PR obligatoire depuis feature/fix/hotfix | Dev (CI) |

---

### Flow de développement — ordre obligatoire

```
1. Créer la branche depuis dev
         │
         ▼
   feature/* ou fix/* ou hotfix/*
         │
         ▼ (PR + squash merge)
        dev   ←── toutes les features arrivent ici
         │
         ▼ (PR + merge commit quand dev est stable)
      staging  ←── recette, tests manuels, validation QA
         │
         ▼ (PR + merge commit quand staging est validé)
        main   ←── production, déclenchement release CI/CD
```

**Chaque étape est une PR distincte. On ne saute jamais une étape.**

---

### Création de branche — toujours depuis `dev`

```bash
# Se positionner sur dev à jour
git checkout dev
git pull origin dev

# Créer la branche de travail
git checkout -b feature/m1-hardware-detection
git checkout -b fix/snapshot-restore-crash
git checkout -b hotfix/license-validation-null-ref
```

**Jamais depuis `staging` ou `main`.** Si une branche est créée depuis le mauvais endroit, la supprimer et recommencer.

---

### Nommage des branches

```
feature/m1-hardware-detection       ← nouvelle fonctionnalité
feature/m2-privacy-tweaks
feature/m4-uninstaller-core

fix/snapshot-restore-crash          ← correction bug non urgent
fix/wmi-timeout-handling

hotfix/license-null-crash           ← correction bug critique en prod
hotfix/smartscreen-false-positive
```

---

### Merge Rules

#### feature/* ou fix/* → dev
- **Squash merge obligatoire** — tous les commits de travail condensés en 1 commit propre
- Message du squash commit = Conventional Commit (`feat(m1): add GPU detection via WMI`)
- CI doit être vert avant merge
- Branche supprimée après merge

```bash
# Sur GitHub : "Squash and merge"
# Résultat dans dev : 1 commit propre par feature
```

#### dev → staging
- **Merge commit** (pas de squash — on veut voir les features individuelles dans staging)
- Déclenché manuellement quand dev est jugée stable (pas de merge auto)
- Déclenche le CI staging (build + tests d'intégration)
- Message : `chore: promote dev to staging — sprint X`

```bash
git checkout staging
git merge --no-ff dev -m "chore: promote dev to staging — sprint 1"
git push origin staging
```

#### staging → main
- **Merge commit** (traçabilité complète)
- Uniquement après validation QA manuelle complète sur staging
- Déclenche automatiquement le workflow release CI/CD (build signé + installeur + GitHub Release)
- Message : `chore: release v1.x.x to production`

```bash
git checkout main
git merge --no-ff staging -m "chore: release v1.0.0 to production"
git tag v1.0.0
git push origin main --tags
```

---

### Cas particulier — Hotfix (bug critique en prod)

Un hotfix corrige un bug bloquant découvert en production. Il doit bypasser le cycle normal tout en restant tracé.

```
hotfix/nom-du-bug  (créée depuis dev)
         │
         ▼ squash merge
        dev
         │
         ▼ merge commit immédiat (pas d'attente)
      staging  ←── test rapide du fix uniquement
         │
         ▼ merge commit immédiat si staging ok
        main   ←── patch release (v1.0.1)
```

**Même un hotfix part de `dev`.** La différence c'est la vitesse de promotion : on ne fait pas de retests complets sur staging, uniquement le scénario du bug fixé.

---

### Protection des branches — configuration GitHub

À configurer dans **Settings → Branches → Branch protection rules** pour chaque branche :

**`main` :**
- [x] Require a pull request before merging
- [x] Require status checks to pass (CI workflow)
- [x] Do not allow bypassing the above settings
- [x] Restrict who can push → personne (même toi)
- [x] Require linear history

**`staging` :**
- [x] Require a pull request before merging
- [x] Require status checks to pass
- [x] Do not allow bypassing

**`dev` :**
- [x] Require a pull request before merging
- [x] Require status checks to pass (CI build + tests)

---

### Résumé visuel complet

```
[feature/m1]  →  squash merge  →  [dev]
[fix/crash]   →  squash merge  →  [dev]
                                    │
                              (stable ?)
                                    │ merge commit
                                    ▼
                                [staging]
                                    │
                              (QA validé ?)
                                    │ merge commit + tag
                                    ▼
                                 [main]  →  CI release → installeur signé → GitHub Release
```

### Messages de commit — Conventional Commits

```
<type>(<scope>): <description courte>

[body optionnel]

[footer optionnel]
```

**Types autorisés :**

| Type | Quand |
|------|-------|
| `feat` | Nouvelle fonctionnalité |
| `fix` | Correction de bug |
| `refactor` | Refactoring sans changement de comportement |
| `test` | Ajout ou modification de tests |
| `docs` | Documentation uniquement |
| `ci` | Modification CI/CD |
| `chore` | Maintenance (deps, config) |
| `perf` | Amélioration de performance |

**Scopes :** `m1` `m2` `m3` `m4` `m5` `m6` `m7` `m8` `m9` `m10` `m11` `infra` `ui` `backend` `native`

**Exemples corrects :**

```
feat(m1): add GPU VRAM detection via WMI
fix(m2): disable SysMain tweak not persisting after reboot
feat(m4): implement orphan registry key cleanup post-uninstall
test(m1): add unit tests for AMD GPU detection path
ci: add EV signing step to release workflow
fix(m10): snapshot restore failing when registry key missing
```

**Interdits :**
```
# Trop vague
fix: bug fix
update: stuff
wip: working on it

# Pas de scope
feat: added hardware detection
```

---

## Conventions C#

### Naming

```csharp
// Namespaces
namespace Corex.Core.Services
namespace Corex.Engine.Wmi
namespace Corex.App.ViewModels

// Classes, interfaces, enums
public class HardwareDetectionService { }
public interface IHardwareDetector { }
public enum StorageType { NvmeSsd, SataSsd, Hdd }

// Méthodes : PascalCase, verbe d'action
public async Task<HardwareProfile> DetectAsync()
public bool IsApplicable(HardwareProfile profile)
public void ApplyTweak(TweakDefinition tweak)

// Propriétés
public string ManufacturerName { get; init; }
public int CoreCount { get; private set; }

// Champs privés : underscore + camelCase
private readonly ILogger<HardwareDetectionService> _logger;
private HardwareProfile? _cachedProfile;

// Paramètres et variables locales : camelCase
var hardwareProfile = await _detector.DetectAsync();
string registryKey = tweak.RegistryPath;

// Constantes
private const int WmiTimeoutSeconds = 30;
public const string AppName = "Corex";
```

### Async/Await

```csharp
// Toujours async jusqu'au bout — jamais .Result ou .Wait()
public async Task<TweakResult> ApplyAsync(TweakDefinition tweak, CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    // ...
}

// ConfigureAwait(false) dans les services (pas dans les ViewModels)
var result = await _tweakEngine.ApplyAsync(tweak, ct).ConfigureAwait(false);

// Naming : méthodes async suffixées Async
public async Task<bool> ValidateLicenseAsync(string key)
```

### Gestion d'erreurs

```csharp
// Exceptions custom pour les cas métier
public class TweakApplicationException : Exception
{
    public TweakDefinition Tweak { get; }
    public TweakApplicationException(TweakDefinition tweak, string message, Exception? inner = null)
        : base(message, inner) => Tweak = tweak;
}

// Pattern Result pour éviter les exceptions de contrôle de flux
public record TweakResult(bool Success, string? ErrorMessage = null, Exception? Exception = null)
{
    public static TweakResult Ok() => new(true);
    public static TweakResult Fail(string error) => new(false, error);
}

// Logger structuré — toujours avec contexte
_logger.LogInformation("Tweak applied: {TweakId} on {Hardware}", tweak.Id, hw.GpuModel);
_logger.LogError(ex, "Snapshot restore failed for session {SessionId}", sessionId);
```

### Records et immutabilité

```csharp
// Modèles hardware : records immuables (détectés une fois, jamais modifiés)
public record HardwareProfile
{
    public required CpuInfo Cpu { get; init; }
    public required GpuInfo Gpu { get; init; }
    public required RamInfo Ram { get; init; }
    public required StorageInfo[] Storages { get; init; }
    public required NetworkInfo Network { get; init; }
    public required OsInfo Os { get; init; }
}

// TweakDefinition : record avec logique
public abstract record TweakDefinition
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract RiskLevel Risk { get; }
    public abstract bool IsApplicable(HardwareProfile hw);
}
```

### Structure des fichiers

```csharp
// Ordre dans un fichier .cs :
// 1. using statements (System en premier, puis Microsoft, puis tiers)
// 2. namespace
// 3. class/interface/record
// 4. Constantes et champs statiques
// 5. Champs privés
// 6. Constructeur(s)
// 7. Propriétés publiques
// 8. Méthodes publiques
// 9. Méthodes privées

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Corex.Core.Services;

public sealed class HardwareDetectionService : IHardwareDetector
{
    private const int CacheExpirationMinutes = 60;

    private readonly ILogger<HardwareDetectionService> _logger;
    private HardwareProfile? _cached;
    private DateTime _cacheTime;

    public HardwareDetectionService(ILogger<HardwareDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task<HardwareProfile> DetectAsync(CancellationToken ct = default)
    {
        if (IsCacheValid()) return _cached!;
        return await DetectInternalAsync(ct);
    }

    private bool IsCacheValid() =>
        _cached is not null && DateTime.UtcNow - _cacheTime < TimeSpan.FromMinutes(CacheExpirationMinutes);

    private async Task<HardwareProfile> DetectInternalAsync(CancellationToken ct)
    {
        // ...
    }
}
```

---

## Conventions XAML (WinUI 3)

```xml
<!-- Naming des contrôles : type + description en PascalCase -->
<Button x:Name="ApplyTweaksButton" />
<ListView x:Name="TweakListView" />
<TextBlock x:Name="HealthScoreText" />

<!-- Bindings : toujours en mode x:Bind (compile-time, pas Binding runtime) -->
<TextBlock Text="{x:Bind ViewModel.HealthScore, Mode=OneWay}" />
<Button Command="{x:Bind ViewModel.ApplyCommand}" />

<!-- Pas de logique dans le XAML -->
<!-- ❌ Interdit -->
<TextBlock Visibility="{x:Bind ViewModel.IsLoading ? 'Visible' : 'Collapsed'}" />
<!-- ✅ Correct : convertisseur ou propriété dédiée dans le ViewModel -->
<TextBlock Visibility="{x:Bind ViewModel.LoadingVisibility, Mode=OneWay}" />
```

---

## Conventions C++ (Corex.Native)

```cpp
// snake_case pour fonctions et variables
int get_gpu_temperature(int adapter_index);
float current_vram_usage;

// PascalCase pour structs/classes
struct GpuMetrics {
    int temperature_celsius;
    float vram_used_mb;
    float vram_total_mb;
    int usage_percent;
};

// Exports DLL : préfixe COREX_API
extern "C" {
    __declspec(dllexport) GpuMetrics COREX_API GetGpuMetrics(int adapter_index);
    __declspec(dllexport) bool COREX_API SetTimerResolution(UINT resolution_ms);
    __declspec(dllexport) void COREX_API RestoreTimerResolution();
}

// Toujours libérer les ressources — RAII ou cleanup explicite
// Toujours vérifier les handles avant utilisation
```

---

## Pull Request Rules

### Template PR

```markdown
## Description
<!-- Quoi et pourquoi, pas comment -->

## Module concerné
<!-- M1 / M2 / ... / Infra / UI / Backend -->

## Type de changement
- [ ] feat — nouvelle fonctionnalité
- [ ] fix — correction bug
- [ ] refactor — sans changement de comportement
- [ ] test — tests uniquement

## Tests
- [ ] Tests unitaires ajoutés/mis à jour
- [ ] Testé manuellement sur Windows 10
- [ ] Testé manuellement sur Windows 11
- [ ] Snapshot + restauration validé (si tweak modifié)

## Checklist
- [ ] Pas de warning de compilation
- [ ] Pas de régression tests existants
- [ ] CHANGELOG.md mis à jour (si feat ou fix visible)
- [ ] Pas de TODO laissé sans issue créée
```

### Règles de review

- 0 merge sans CI vert
- 0 commentaire non résolu avant merge
- `main` : squash merge uniquement (historique propre)
- `develop` : merge commit (traçabilité des features)

---

## Fichiers à ne jamais commiter

```gitignore
# .gitignore obligatoire
*.pfx
*.p12
ev_cert.*
*.user
.vs/
bin/
obj/
publish/
installer/Output/
*.log
appsettings.local.json
backend/.env
backend/.env.local
node_modules/
```

---

## Checklist qualité avant commit

- [ ] `dotnet format` passé sans changements
- [ ] Zéro warning Roslyn dans le scope modifié
- [ ] Tests unitaires du module concerné au vert
- [ ] Pas de `Console.WriteLine` ou `Debug.WriteLine` laissé en prod
- [ ] Pas de clé hardcodée ou mot de passe en clair
- [ ] Log structuré avec les bons paramètres
