using Corex.Core.Interfaces;
using Corex.Core.Models;
using Microsoft.Extensions.Options;

namespace Corex.Core.Services;

public sealed class HardwareDetectionService : IHardwareDetector
{
    private readonly IWmiQuery _wmiQuery;
    private readonly IOptions<HardwareDetectionOptions> _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private HardwareProfile? _cachedProfile;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public HardwareDetectionService(IWmiQuery wmiQuery, IOptions<HardwareDetectionOptions> options)
    {
        _wmiQuery = wmiQuery;
        _options = options;
    }

    public async Task<HardwareProfile> DetectAsync(CancellationToken ct = default)
    {
        var ttl = _options.Value.CacheTtl;

        if (_cachedProfile is not null && DateTimeOffset.UtcNow - _cachedAt < ttl)
            return _cachedProfile;

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check inside lock
            if (_cachedProfile is not null && DateTimeOffset.UtcNow - _cachedAt < ttl)
                return _cachedProfile;

            var cpuTask = SafeDetectAsync(() => _wmiQuery.DetectCpuAsync(ct), CpuInfo.Unknown);
            var gpusTask = SafeDetectAsync(() => _wmiQuery.DetectGpusAsync(ct), Array.Empty<GpuInfo>());
            var ramTask = SafeDetectAsync(() => _wmiQuery.DetectRamAsync(ct), RamInfo.Unknown);
            var storageTask = SafeDetectAsync(() => _wmiQuery.DetectStorageAsync(ct), StorageInfo.Unknown);

            await Task.WhenAll(cpuTask, gpusTask, ramTask, storageTask).ConfigureAwait(false);

            _cachedAt = DateTimeOffset.UtcNow;
            _cachedProfile = new HardwareProfile
            {
                Cpu = cpuTask.Result,
                Gpus = gpusTask.Result,
                Ram = ramTask.Result,
                PrimaryStorage = storageTask.Result,
                DetectedAt = _cachedAt
            };
            return _cachedProfile;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<HardwareProfile> RefreshAsync(CancellationToken ct = default)
    {
        // V1: no-op — returns cached profile or Unknown, never re-triggers WMI
        return Task.FromResult(_cachedProfile ?? HardwareProfile.Unknown);
    }

    public void ClearCache()
    {
        _cachedProfile = null;
        _cachedAt = DateTimeOffset.MinValue;
    }

    private static async Task<T> SafeDetectAsync<T>(Func<Task<T>> detect, T fallback)
    {
        try
        {
            return await detect().ConfigureAwait(false);
        }
        catch
        {
            return fallback;
        }
    }
}
