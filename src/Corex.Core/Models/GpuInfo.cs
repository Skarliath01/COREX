namespace Corex.Core.Models;

public enum GpuVendor { Unknown, Nvidia, Amd, Intel, Other }

public sealed record GpuInfo
{
    public static readonly GpuInfo Unknown = new()
    {
        Vendor = GpuVendor.Unknown,
        Name = "Unknown",
        VramBytes = 0
    };

    public required GpuVendor Vendor { get; init; }
    public required string Name { get; init; }
    public required long VramBytes { get; init; }
}
