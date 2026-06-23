namespace Corex.Core.Models;

public enum RamGeneration { Unknown, Ddr4, Ddr5 }

public sealed record RamInfo
{
    public static readonly RamInfo Unknown = new()
    {
        TotalBytes = 0,
        Generation = RamGeneration.Unknown
    };

    public required long TotalBytes { get; init; }
    public required RamGeneration Generation { get; init; }
}
