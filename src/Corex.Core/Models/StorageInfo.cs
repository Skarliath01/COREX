namespace Corex.Core.Models;

public enum StorageType { Unknown, NvmeSsd, Ssd, Hdd }

public enum SmartStatus { Unknown, Healthy, Warning, Unhealthy }

public sealed record StorageInfo
{
    public static readonly StorageInfo Unknown = new()
    {
        Type = StorageType.Unknown,
        SmartStatus = SmartStatus.Unknown,
        SizeBytes = 0,
        Model = "Unknown"
    };

    public required StorageType Type { get; init; }
    public required SmartStatus SmartStatus { get; init; }
    public required long SizeBytes { get; init; }
    public required string Model { get; init; }
}
