namespace Corex.Tests.Fixtures;

public static class HardwareFixtures
{
    public static IReadOnlyDictionary<string, object?> IntelCpuRow => new Dictionary<string, object?>
    {
        ["Name"] = "Intel(R) Core(TM) i7-12700K",
        ["Manufacturer"] = "GenuineIntel",
        ["NumberOfCores"] = 12u,
        ["NumberOfLogicalProcessors"] = 20u,
        ["MaxClockSpeed"] = 3600u
    };

    public static IReadOnlyDictionary<string, object?> AmdCpuRow => new Dictionary<string, object?>
    {
        ["Name"] = "AMD Ryzen 9 7900X",
        ["Manufacturer"] = "AuthenticAMD",
        ["NumberOfCores"] = 12u,
        ["NumberOfLogicalProcessors"] = 24u,
        ["MaxClockSpeed"] = 4700u
    };

    public static IReadOnlyDictionary<string, object?> NvidiaGpuRow => new Dictionary<string, object?>
    {
        ["Name"] = "NVIDIA GeForce RTX 3080",
        ["AdapterCompatibility"] = "NVIDIA",
        ["AdapterRAM"] = 2u * 1024u * 1024u * 1024u  // 2GB — fits in uint32
    };

    // OQ1: AdapterRAM == uint.MaxValue → card has > 4GB VRAM (registry fallback path)
    public static IReadOnlyDictionary<string, object?> GpuWmiTimeout => new Dictionary<string, object?>
    {
        ["Name"] = "NVIDIA GeForce RTX 3080",
        ["AdapterCompatibility"] = "NVIDIA",
        ["AdapterRAM"] = uint.MaxValue
    };

    public static IReadOnlyDictionary<string, object?> IntelGpuRow => new Dictionary<string, object?>
    {
        ["Name"] = "Intel(R) UHD Graphics 630",
        ["AdapterCompatibility"] = "Intel Corporation",
        ["AdapterRAM"] = 1u * 1024u * 1024u * 1024u
    };

    public static IReadOnlyDictionary<string, object?> Ddr4RamRow => new Dictionary<string, object?>
    {
        ["Capacity"] = 8L * 1024 * 1024 * 1024,
        ["SMBIOSMemoryType"] = 26u
    };

    public static IReadOnlyDictionary<string, object?> Ddr5RamRow => new Dictionary<string, object?>
    {
        ["Capacity"] = 16L * 1024 * 1024 * 1024,
        ["SMBIOSMemoryType"] = 34u
    };

    public static IReadOnlyDictionary<string, object?> NvmeDiskRow => new Dictionary<string, object?>
    {
        ["FriendlyName"] = "Samsung SSD 970 EVO Plus",
        ["HealthStatus"] = 0u,
        ["MediaType"] = 4u,
        ["BusType"] = 11u,
        ["Size"] = 1L * 1024 * 1024 * 1024 * 1024
    };

    public static IReadOnlyDictionary<string, object?> HddRow => new Dictionary<string, object?>
    {
        ["FriendlyName"] = "WDC WD10EZEX-08WN4A0",
        ["HealthStatus"] = 0u,
        ["MediaType"] = 3u,
        ["BusType"] = 3u,
        ["Size"] = 1L * 1024 * 1024 * 1024 * 1024
    };
}
