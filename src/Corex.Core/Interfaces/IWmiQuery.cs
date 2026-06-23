namespace Corex.Core.Interfaces;

public interface IWmiQuery
{
    Task<Models.CpuInfo> DetectCpuAsync(CancellationToken ct = default);
    Task<Models.GpuInfo[]> DetectGpusAsync(CancellationToken ct = default);
    Task<Models.RamInfo> DetectRamAsync(CancellationToken ct = default);
    Task<Models.StorageInfo> DetectStorageAsync(CancellationToken ct = default);
}
