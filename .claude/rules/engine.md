---
paths: ["src/Corex.Core/Rules/**", "src/Corex.Core/Services/TweakEngine*", "src/Corex.Core/Services/Snapshot*", "src/Corex.Engine/**"]
---

# Règles — Moteur de tweaks + Snapshot (M2, M10)

## Interface ITweakRule — obligatoire sur chaque tweak

```csharp
public abstract record TweakDefinition
{
    public abstract string Id { get; }           // ex: "disable-sysMain"
    public abstract string DisplayName { get; }  // ex: "Désactiver SysMain"
    public abstract RiskLevel Risk { get; }      // 🟢🟡🔴
    public abstract bool IsApplicable(HardwareProfile hw); // OBLIGATOIRE
}

public enum RiskLevel { Safe, Moderate, Expert }
```

## Pattern applique/restaure

```csharp
public class DisableSysMainTweak : TweakDefinition
{
    public override string Id => "disable-sysmain";
    public override string DisplayName => "Désactiver SysMain / SuperFetch";
    public override RiskLevel Risk => RiskLevel.Moderate;

    // Condition hardware — cœur du produit
    public override bool IsApplicable(HardwareProfile hw) =>
        hw.PrimaryStorage?.Type is StorageType.SataSsd or StorageType.NvmeSsd;

    public string RegistryKey => @"SYSTEM\CurrentControlSet\Services\SysMain";
    public string ValueName => "Start";
    public int SafeValue => 4;    // Disabled
    public int DefaultValue => 2; // Automatic
}
```

## Snapshot — non bypassable avant toute modification

```csharp
// TweakEngineService — pattern obligatoire
public async Task<IReadOnlyList<TweakResult>> ApplyAsync(
    IEnumerable<TweakDefinition> tweaks, CancellationToken ct = default)
{
    // 1. Snapshot d'abord — toujours
    var snapshot = await _snapshotService.CreateAsync($"session-{DateTime.UtcNow:yyyyMMdd-HHmm}");

    var results = new List<TweakResult>();
    try
    {
        foreach (var tweak in tweaks)
        {
            ct.ThrowIfCancellationRequested();

            // 2. Vérification hardware — jamais bypasser
            if (!tweak.IsApplicable(_hardwareProfile))
            {
                results.Add(TweakResult.Skipped(tweak.Id, "Non applicable sur ce hardware"));
                continue;
            }

            // 3. Log valeur avant modification
            await snapshot.RegisterKeyAsync(tweak.RegistryKey, tweak.ValueName);

            // 4. Appliquer
            var result = await ApplyTweakAsync(tweak, ct);
            results.Add(result);
            _logger.LogInformation("Tweak applied: {Id} | Risk: {Risk}", tweak.Id, tweak.Risk);
        }

        await snapshot.CommitAsync();
        return results;
    }
    catch
    {
        await snapshot.RollbackAsync(); // restauration auto en cas d'erreur
        throw;
    }
}
```

## Registry — helpers via Corex.Engine

```csharp
// Ne pas utiliser Microsoft.Win32.Registry directement dans Corex.Core
// Passer par RegistryTweak.cs dans Corex.Engine
public class RegistryTweak
{
    public static void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind);
    public static object? GetCurrentValue(string keyPath, string valueName);
    public static void RestoreValue(string keyPath, string valueName, object? originalValue);
}
```

## Services Windows — via WindowsServiceManager

```csharp
// Ne pas appeler ServiceController directement dans Corex.Core
// Passer par Corex.Engine.Services.WindowsServiceManager
public class WindowsServiceManager
{
    public static ServiceStartMode GetStartMode(string serviceName);
    public static void SetStartMode(string serviceName, ServiceStartMode mode);
    public static void StopService(string serviceName);
}
```
