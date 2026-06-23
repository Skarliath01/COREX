namespace Corex.Core.Services;

public sealed class HardwareDetectionOptions
{
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);
}
