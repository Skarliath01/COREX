---
paths: ["src/Corex.Core/Hardware/**", "src/Corex.Core/Models/**", "src/Corex.Engine/Wmi/**"]
---

# Règles — Couche Hardware (M1)

## Sources WMI par composant

```
CPU     → Win32_Processor         (Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed)
GPU     → Win32_VideoController   (Name, AdapterRAM, DriverVersion)  +  NVAPI/ADL pour GPU actif
RAM     → Win32_PhysicalMemory    (Capacity, Speed, Manufacturer, MemoryType)
Storage → Win32_DiskDrive         + DeviceIoControl (SMART, TBW)
Réseau  → Win32_NetworkAdapter    (Name, Speed, NetConnectionStatus)
OS      → Win32_OperatingSystem   + Registry BIOS, SecureBoot, TPM
```

## Pattern WMI — toujours disposer les ressources

```csharp
using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
using var results = searcher.Get();
foreach (ManagementBaseObject obj in results)
{
    // Défensive coding — propriétés WMI variables selon constructeur
    var vendor = obj["Manufacturer"]?.ToString() ?? string.Empty;
    var cores  = (uint)(obj["NumberOfCores"] ?? 0);
}
```

## Modèles — records immuables

```csharp
public enum GpuVendor  { Unknown, Nvidia, Amd, Intel }
public enum RamType    { Unknown, Ddr4, Ddr5 }
public enum StorageType { Unknown, Hdd, SataSsd, NvmeSsd }

public sealed record CpuInfo(string Vendor, int PhysicalCores, int LogicalCores, double FrequencyGHz);
public sealed record GpuInfo(GpuVendor Vendor, string Name, long VramBytes, string DriverVersion);
public sealed record RamInfo(long TotalBytes, RamType Type, int SpeedMHz);
public sealed record StorageInfo(string Model, StorageType Type, long CapacityBytes, int SmartHealthPercent);
```

## HardwareProfile — singleton lazy

```csharp
public sealed class HardwareProfile
{
    public static HardwareProfile Current => _lazy.Value;
    private static readonly Lazy<HardwareProfile> _lazy =
        new(Detect, LazyThreadSafetyMode.ExecutionAndPublication);

    public required CpuInfo Cpu { get; init; }
    public required GpuInfo Gpu { get; init; }
    public required RamInfo Ram { get; init; }
    public required StorageInfo[] Storages { get; init; }
    public StorageInfo? PrimaryStorage => Storages.FirstOrDefault();
}
```

## Tests sans hardware réel — Moq obligatoire

```csharp
// IHardwareDetector mockable — jamais WMI directement dans les tests
var mock = new Mock<IHardwareDetector>();
mock.Setup(d => d.DetectGpuAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new GpuInfo(GpuVendor.Nvidia, "RTX 4070", 12L * 1024 * 1024 * 1024, "537.13"));

// Fixtures statiques dans Corex.Tests/Unit/Fixtures/HardwareFixtures.cs
// Couvrir : Nvidia RTX, AMD RX, Intel Arc, GPU intégré Intel, pas de GPU dédié
```

## Règle conditionnelle — chaque Rule implémente IsApplicable

```csharp
public class DisableSysMainTweak : ITweakRule
{
    public bool IsApplicable(HardwareProfile hw) =>
        hw.PrimaryStorage?.Type is StorageType.SataSsd or StorageType.NvmeSsd;

    public RiskLevel Risk => RiskLevel.Moderate;
}
```
