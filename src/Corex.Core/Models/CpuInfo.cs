namespace Corex.Core.Models;

public enum CpuVendor { Unknown, Intel, Amd, Other }

public sealed record CpuInfo
{
    public static readonly CpuInfo Unknown = new()
    {
        Vendor = CpuVendor.Unknown,
        Name = "Unknown",
        PhysicalCoreCount = 0,
        LogicalCoreCount = 0,
        MaxClockSpeedMHz = 0
    };

    public required CpuVendor Vendor { get; init; }
    public required string Name { get; init; }
    public required int PhysicalCoreCount { get; init; }
    public required int LogicalCoreCount { get; init; }
    public required double MaxClockSpeedMHz { get; init; }
}
