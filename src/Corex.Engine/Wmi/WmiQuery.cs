using System.Runtime.Versioning;
using Corex.Core.Interfaces;
using Corex.Core.Models;
using Microsoft.Win32;

namespace Corex.Engine.Wmi;

[SupportedOSPlatform("windows")]
public sealed class WmiQuery : IWmiQuery
{
    private readonly IWmiQueryExecutor _executor;

    public WmiQuery(IWmiQueryExecutor executor) => _executor = executor;

    public Task<CpuInfo> DetectCpuAsync(CancellationToken ct = default) =>
        Task.Run(DetectCpu, ct);

    public Task<GpuInfo[]> DetectGpusAsync(CancellationToken ct = default) =>
        Task.Run(DetectGpus, ct);

    public Task<RamInfo> DetectRamAsync(CancellationToken ct = default) =>
        Task.Run(DetectRam, ct);

    public Task<StorageInfo> DetectStorageAsync(CancellationToken ct = default) =>
        Task.Run(DetectStorage, ct);

    private CpuInfo DetectCpu()
    {
        var rows = _executor.Execute(
            "Win32_Processor",
            ["Name", "Manufacturer", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed"]);

        var row = rows.FirstOrDefault();
        if (row is null) return CpuInfo.Unknown;

        var mfr = row["Manufacturer"]?.ToString() ?? "";
        var vendor = mfr.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Intel
                   : mfr.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? CpuVendor.Amd
                   : CpuVendor.Other;

        return new CpuInfo
        {
            Vendor = vendor,
            Name = row["Name"]?.ToString() ?? "Unknown",
            PhysicalCoreCount = Convert.ToInt32(row["NumberOfCores"] ?? 0),
            LogicalCoreCount = Convert.ToInt32(row["NumberOfLogicalProcessors"] ?? 0),
            MaxClockSpeedMHz = Convert.ToDouble(row["MaxClockSpeed"] ?? 0)
        };
    }

    private GpuInfo[] DetectGpus()
    {
        var rows = _executor.Execute(
            "Win32_VideoController",
            ["Name", "AdapterCompatibility", "AdapterRAM"]);

        return rows.Select((row, index) =>
        {
            var compat = row["AdapterCompatibility"]?.ToString() ?? "";
            var vendor = compat.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ? GpuVendor.Nvidia
                       : compat.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? GpuVendor.Amd
                       : compat.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? GpuVendor.Intel
                       : GpuVendor.Other;

            // OQ1: AdapterRAM is UINT32 — saturates at uint.MaxValue for cards > 4GB
            var adapterRam = Convert.ToUInt32(row["AdapterRAM"] ?? 0u);
            var vramBytes = adapterRam == uint.MaxValue
                ? ReadVramFromRegistry(index)
                : (long)adapterRam;

            return new GpuInfo
            {
                Vendor = vendor,
                Name = row["Name"]?.ToString() ?? "Unknown",
                VramBytes = vramBytes
            };
        }).ToArray();
    }

    private static long ReadVramFromRegistry(int gpuIndex)
    {
        // Fallback for AdapterRAM > 4GB: QWORD in GPU display class registry key
        var subKey = $@"SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\{gpuIndex:D4}";
        using var key = Registry.LocalMachine.OpenSubKey(subKey);
        return key?.GetValue("HardwareInformation.qwMemorySize") is long bytes ? bytes : 0;
    }

    private RamInfo DetectRam()
    {
        var rows = _executor.Execute(
            "Win32_PhysicalMemory",
            ["Capacity", "SMBIOSMemoryType"]);

        long totalBytes = 0;
        var generation = RamGeneration.Unknown;

        foreach (var row in rows)
        {
            totalBytes += Convert.ToInt64(row["Capacity"] ?? 0L);
            // SMBIOSMemoryType 26 = DDR4, 34 = DDR5
            if (generation == RamGeneration.Unknown)
            {
                var memType = Convert.ToInt32(row["SMBIOSMemoryType"] ?? 0);
                generation = memType switch
                {
                    26 => RamGeneration.Ddr4,
                    34 => RamGeneration.Ddr5,
                    _ => RamGeneration.Unknown
                };
            }
        }

        return totalBytes == 0 ? RamInfo.Unknown : new RamInfo
        {
            TotalBytes = totalBytes,
            Generation = generation
        };
    }

    private StorageInfo DetectStorage()
    {
        // OQ2: MSFT_PhysicalDisk — portable on Win10 22H2+, supports NVMe/SSD/HDD
        var rows = _executor.Execute(
            "MSFT_PhysicalDisk",
            ["FriendlyName", "HealthStatus", "MediaType", "BusType", "Size"],
            namespacePath: @"root\microsoft\windows\storage");

        // Primary = largest disk (typically system drive)
        var row = rows.OrderByDescending(r => Convert.ToInt64(r["Size"] ?? 0L)).FirstOrDefault();
        if (row is null) return StorageInfo.Unknown;

        var busType = Convert.ToInt32(row["BusType"] ?? 0);
        var mediaType = Convert.ToInt32(row["MediaType"] ?? 0);
        var storageType = busType == 11 ? StorageType.NvmeSsd    // BusType NVMe
                        : mediaType == 4 ? StorageType.Ssd        // MediaType SSD
                        : mediaType == 3 ? StorageType.Hdd        // MediaType HDD
                        : StorageType.Unknown;

        // HealthStatus: 0=Healthy, 1=Warning, 2=Unhealthy
        var health = Convert.ToInt32(row["HealthStatus"] ?? -1);
        var smartStatus = health switch
        {
            0 => SmartStatus.Healthy,
            1 => SmartStatus.Warning,
            2 => SmartStatus.Unhealthy,
            _ => SmartStatus.Unknown
        };

        return new StorageInfo
        {
            Type = storageType,
            SmartStatus = smartStatus,
            SizeBytes = Convert.ToInt64(row["Size"] ?? 0L),
            Model = row["FriendlyName"]?.ToString() ?? "Unknown"
        };
    }
}
