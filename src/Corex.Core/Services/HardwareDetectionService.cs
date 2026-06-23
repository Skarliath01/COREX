using Corex.Core.Interfaces;
using Corex.Core.Models;
using Microsoft.Extensions.Options;

namespace Corex.Core.Services;

public sealed class HardwareDetectionService : IHardwareDetector, IDisposable
{
    private readonly IWmiQuery _wmiQuery;
    private readonly IOptions<HardwareDetectionOptions> _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    // volatile: ensures the outer fast-path null check is a fresh read across
    // CPU cores, preventing torn reads with concurrent ClearCache() calls.
    private volatile HardwareProfile? _cachedProfile;
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
        return Task.FromResult(_cachedProfile ?? HardwareProfile.Unknown);
    }

    public void ClearCache()
    {
        // Write _cachedAt before nulling _cachedProfile so readers racing the
        // outer fast-path see an expired TTL before they see a null profile.
        _cachedAt = DateTimeOffset.MinValue;
        _cachedProfile = null; // volatile write — visible to all threads immediately
    }

    public void Dispose() => _semaphore.Dispose();

    private static async Task<T> SafeDetectAsync<T>(Func<Task<T>> detect, T fallback)
    {
        try
        {
            return await detect().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return fallback;
        }
    }
}
