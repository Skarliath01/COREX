namespace Corex.Core.Models;

public sealed record HardwareProfile
{
    public static readonly HardwareProfile Unknown = new()
    {
        Cpu = CpuInfo.Unknown,
        Gpus = [],
        Ram = RamInfo.Unknown,
        PrimaryStorage = StorageInfo.Unknown,
        DetectedAt = DateTimeOffset.MinValue
    };

    public required CpuInfo Cpu { get; init; }
    public required IReadOnlyList<GpuInfo> Gpus { get; init; }
    public required RamInfo Ram { get; init; }
    public required StorageInfo PrimaryStorage { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }

    public GpuInfo PrimaryGpu =>
        Gpus.Where(g => g.Vendor != GpuVendor.Intel)
            .OrderByDescending(g => g.VramBytes)
            .FirstOrDefault()
        ?? Gpus.FirstOrDefault()
        ?? GpuInfo.Unknown;

    public bool HasUnknownComponents =>
        Cpu == CpuInfo.Unknown ||
        Ram == RamInfo.Unknown ||
        PrimaryStorage == StorageInfo.Unknown ||
        PrimaryGpu == GpuInfo.Unknown;
}
