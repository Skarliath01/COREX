using Corex.Core.Interfaces;
using Corex.Core.Models;
using Corex.Core.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Corex.Tests.Unit;

[Trait("Category", "Unit")]
public class HardwareDetectionTests
{
    private readonly Mock<IWmiQuery> _mockWmiQuery;
    private readonly HardwareDetectionService _service;

    public HardwareDetectionTests()
    {
        _mockWmiQuery = new Mock<IWmiQuery>(MockBehavior.Strict);
        var options = Options.Create(new HardwareDetectionOptions { CacheTtl = TimeSpan.Zero });
        _service = new HardwareDetectionService(_mockWmiQuery.Object, options);
    }

    [Fact]
    public async Task DetectAsync_NvidiaGpu_SetsPrimaryGpuCorrectly()
    {
        SetupHappyPath();
        var profile = await _service.DetectAsync();
        Assert.Equal(GpuVendor.Nvidia, profile.PrimaryGpu.Vendor);
    }

    [Fact]
    public async Task DetectAsync_IntelAndNvidiaGpus_PrimaryIsNvidiaNotIntel()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ReturnsAsync(MakeIntelCpu());
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default)).ReturnsAsync(new[]
        {
            new GpuInfo { Vendor = GpuVendor.Intel,  VramBytes = 1L * 1024 * 1024 * 1024, Name = "Intel UHD" },
            new GpuInfo { Vendor = GpuVendor.Nvidia, VramBytes = 8L * 1024 * 1024 * 1024, Name = "RTX 3080"  }
        });
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ReturnsAsync(RamInfo.Unknown);
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ReturnsAsync(StorageInfo.Unknown);

        var profile = await _service.DetectAsync();
        Assert.Equal(GpuVendor.Nvidia, profile.PrimaryGpu.Vendor);
    }

    [Fact]
    public async Task DetectAsync_CalledConcurrently_WmiQueriedOnlyOnce()
    {
        var options = Options.Create(new HardwareDetectionOptions { CacheTtl = TimeSpan.FromHours(1) });
        var service = new HardwareDetectionService(_mockWmiQuery.Object, options);
        SetupHappyPath();

        var tasks = Enumerable.Range(0, 5).Select(_ => service.DetectAsync()).ToArray();
        await Task.WhenAll(tasks);

        _mockWmiQuery.Verify(x => x.DetectCpuAsync(default), Times.Once());
        _mockWmiQuery.Verify(x => x.DetectGpusAsync(default), Times.Once());
        _mockWmiQuery.Verify(x => x.DetectRamAsync(default), Times.Once());
        _mockWmiQuery.Verify(x => x.DetectStorageAsync(default), Times.Once());
    }

    [Fact]
    public async Task DetectAsync_WhenGpuWmiThrows_ReturnsProfileWithUnknownGpu()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ReturnsAsync(MakeIntelCpu());
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default))
            .ThrowsAsync(new TimeoutException("WMI timeout"));
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ReturnsAsync(new RamInfo
        {
            TotalBytes = 16L * 1024 * 1024 * 1024, Generation = RamGeneration.Ddr4
        });
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ReturnsAsync(new StorageInfo
        {
            Type = StorageType.NvmeSsd, SmartStatus = SmartStatus.Healthy,
            SizeBytes = 1L * 1024 * 1024 * 1024 * 1024, Model = "Samsung 970"
        });

        var profile = await _service.DetectAsync();

        Assert.Equal(GpuVendor.Unknown, profile.PrimaryGpu.Vendor);
        Assert.True(profile.HasUnknownComponents);
    }

    [Fact]
    public async Task DetectAsync_AllWmiFail_ReturnsProfileWithAllUnknownComponents()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ThrowsAsync(new Exception("WMI crash"));
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default)).ThrowsAsync(new Exception("WMI crash"));
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ThrowsAsync(new Exception("WMI crash"));
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ThrowsAsync(new Exception("WMI crash"));

        var profile = await _service.DetectAsync();

        Assert.Equal(CpuInfo.Unknown, profile.Cpu);
        Assert.Equal(RamInfo.Unknown, profile.Ram);
        Assert.Equal(StorageInfo.Unknown, profile.PrimaryStorage);
        Assert.Equal(GpuInfo.Unknown, profile.PrimaryGpu);
        Assert.True(profile.HasUnknownComponents);
    }

    [Fact]
    public async Task RefreshAsync_BeforeDetect_ReturnsHardwareProfileUnknown()
    {
        var result = await _service.RefreshAsync();
        Assert.Equal(HardwareProfile.Unknown, result);
    }

    [Fact]
    public async Task RefreshAsync_AfterDetect_ReturnsCachedProfile()
    {
        SetupHappyPath();
        var detected = await _service.DetectAsync();
        var refreshed = await _service.RefreshAsync();
        Assert.Equal(detected, refreshed);
    }

    [Fact]
    public async Task ClearCache_ForcesNewWmiCallOnNextDetect()
    {
        var options = Options.Create(new HardwareDetectionOptions { CacheTtl = TimeSpan.FromHours(1) });
        var service = new HardwareDetectionService(_mockWmiQuery.Object, options);
        SetupHappyPath();

        await service.DetectAsync();
        service.ClearCache();
        await service.DetectAsync();

        _mockWmiQuery.Verify(x => x.DetectCpuAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task HardwareProfile_PrimaryGpu_OnlyIntelGpu_ReturnsIntelGpu()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ReturnsAsync(CpuInfo.Unknown);
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default)).ReturnsAsync(new[]
        {
            new GpuInfo { Vendor = GpuVendor.Intel, VramBytes = 1L * 1024 * 1024 * 1024, Name = "Intel UHD" }
        });
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ReturnsAsync(RamInfo.Unknown);
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ReturnsAsync(StorageInfo.Unknown);

        var profile = await _service.DetectAsync();
        Assert.Equal(GpuVendor.Intel, profile.PrimaryGpu.Vendor);
    }

    [Fact]
    public async Task HardwareProfile_PrimaryGpu_NoGpus_ReturnsGpuInfoUnknown()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ReturnsAsync(CpuInfo.Unknown);
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default)).ReturnsAsync(Array.Empty<GpuInfo>());
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ReturnsAsync(RamInfo.Unknown);
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ReturnsAsync(StorageInfo.Unknown);

        var profile = await _service.DetectAsync();
        Assert.Equal(GpuInfo.Unknown, profile.PrimaryGpu);
    }

    [Fact]
    public void StorageInfo_Unknown_HasSmartStatusUnknown()
    {
        Assert.Equal(SmartStatus.Unknown, StorageInfo.Unknown.SmartStatus);
    }

    [Fact]
    public async Task DetectAsync_CpuInfo_ReturnsCorrectVendorAndCores()
    {
        SetupHappyPath();
        var profile = await _service.DetectAsync();
        Assert.Equal(CpuVendor.Intel, profile.Cpu.Vendor);
        Assert.Equal(8, profile.Cpu.PhysicalCoreCount);
    }

    private void SetupHappyPath()
    {
        _mockWmiQuery.Setup(x => x.DetectCpuAsync(default)).ReturnsAsync(MakeIntelCpu());
        _mockWmiQuery.Setup(x => x.DetectGpusAsync(default)).ReturnsAsync(new[]
        {
            new GpuInfo { Vendor = GpuVendor.Nvidia, VramBytes = 8L * 1024 * 1024 * 1024, Name = "RTX 3080" }
        });
        _mockWmiQuery.Setup(x => x.DetectRamAsync(default)).ReturnsAsync(new RamInfo
        {
            TotalBytes = 16L * 1024 * 1024 * 1024, Generation = RamGeneration.Ddr4
        });
        _mockWmiQuery.Setup(x => x.DetectStorageAsync(default)).ReturnsAsync(new StorageInfo
        {
            Type = StorageType.NvmeSsd, SmartStatus = SmartStatus.Healthy,
            SizeBytes = 1L * 1024 * 1024 * 1024 * 1024, Model = "Samsung 970"
        });
    }

    private static CpuInfo MakeIntelCpu() => new()
    {
        Vendor = CpuVendor.Intel, Name = "Intel Core i7",
        PhysicalCoreCount = 8, LogicalCoreCount = 16, MaxClockSpeedMHz = 3600
    };
}
