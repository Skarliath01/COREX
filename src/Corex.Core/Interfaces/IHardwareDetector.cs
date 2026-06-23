namespace Corex.Core.Interfaces;

public interface IHardwareDetector
{
    Task<Models.HardwareProfile> DetectAsync(CancellationToken ct = default);
    Task<Models.HardwareProfile> RefreshAsync(CancellationToken ct = default);
    void ClearCache();
}
