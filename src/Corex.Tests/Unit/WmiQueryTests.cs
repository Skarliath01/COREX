using System.Runtime.InteropServices;
using Corex.Core.Models;
using Corex.Engine.Wmi;
using Corex.Tests.Fixtures;
using Moq;

namespace Corex.Tests.Unit;

// WmiQuery is [SupportedOSPlatform("windows")] but tests use a mocked
// IWmiQueryExecutor — no real Windows API is called (except the registry
// fallback test which guards on RuntimeInformation.IsOSPlatform).
#pragma warning disable CA1416

[Trait("Category", "Unit")]
public class WmiQueryTests
{
    private readonly Mock<IWmiQueryExecutor> _mockExecutor;
    private readonly WmiQuery _wmiQuery;

    public WmiQueryTests()
    {
        _mockExecutor = new Mock<IWmiQueryExecutor>(MockBehavior.Strict);
        _wmiQuery = new WmiQuery(_mockExecutor.Object);
    }

    // ── CPU ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DetectCpuAsync_IntelProcessor_ReturnsIntelVendorAndCores()
    {
        SetupCpu([HardwareFixtures.IntelCpuRow]);

        var result = await _wmiQuery.DetectCpuAsync();

        Assert.Equal(CpuVendor.Intel, result.Vendor);
        Assert.Equal("Intel(R) Core(TM) i7-12700K", result.Name);
        Assert.Equal(12, result.PhysicalCoreCount);
        Assert.Equal(20, result.LogicalCoreCount);
        Assert.Equal(3600.0, result.MaxClockSpeedMHz);
    }

    [Fact]
    public async Task DetectCpuAsync_AmdProcessor_ReturnsAmdVendor()
    {
        SetupCpu([HardwareFixtures.AmdCpuRow]);

        var result = await _wmiQuery.DetectCpuAsync();

        Assert.Equal(CpuVendor.Amd, result.Vendor);
        Assert.Equal("AMD Ryzen 9 7900X", result.Name);
    }

    [Fact]
    public async Task DetectCpuAsync_EmptyRows_ReturnsCpuInfoUnknown()
    {
        SetupCpu([]);

        var result = await _wmiQuery.DetectCpuAsync();

        Assert.Equal(CpuInfo.Unknown, result);
    }

    // ── GPU ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DetectGpusAsync_NvidiaGpu_ReturnsNvidiaVendorWithCorrectVram()
    {
        SetupGpu([HardwareFixtures.NvidiaGpuRow]);

        var result = await _wmiQuery.DetectGpusAsync();

        Assert.Single(result);
        Assert.Equal(GpuVendor.Nvidia, result[0].Vendor);
        Assert.Equal(2L * 1024 * 1024 * 1024, result[0].VramBytes);
    }

    [Fact]
    public async Task DetectGpusAsync_AdapterRamSaturated_FallsBackToRegistry()
    {
        // OQ1: AdapterRAM == uint.MaxValue → registry fallback.
        // Registry.LocalMachine throws PlatformNotSupportedException on non-Windows.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        SetupGpu([HardwareFixtures.GpuWmiTimeout]);

        var result = await _wmiQuery.DetectGpusAsync();

        Assert.Single(result);
        Assert.Equal(GpuVendor.Nvidia, result[0].Vendor);
        // Registry key absent in test env → fallback returns 0
        Assert.Equal(0L, result[0].VramBytes);
    }

    // ── RAM ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DetectRamAsync_Ddr4Ram_ReturnsDdr4GenerationAndCorrectBytes()
    {
        SetupRam([HardwareFixtures.Ddr4RamRow]);

        var result = await _wmiQuery.DetectRamAsync();

        Assert.Equal(RamGeneration.Ddr4, result.Generation);
        Assert.Equal(8L * 1024 * 1024 * 1024, result.TotalBytes);
    }

    [Fact]
    public async Task DetectRamAsync_Ddr5Ram_ReturnsDdr5Generation()
    {
        SetupRam([HardwareFixtures.Ddr5RamRow]);

        var result = await _wmiQuery.DetectRamAsync();

        Assert.Equal(RamGeneration.Ddr5, result.Generation);
        Assert.Equal(16L * 1024 * 1024 * 1024, result.TotalBytes);
    }

    [Fact]
    public async Task DetectRamAsync_UnknownMemoryType_ReturnsUnknownGeneration()
    {
        var unknownRow = new Dictionary<string, object?>
        {
            ["Capacity"] = 8L * 1024 * 1024 * 1024,
            ["SMBIOSMemoryType"] = 0u
        };
        SetupRam([unknownRow]);

        var result = await _wmiQuery.DetectRamAsync();

        Assert.Equal(RamGeneration.Unknown, result.Generation);
    }

    [Fact]
    public async Task DetectRamAsync_EmptyRows_ReturnsRamInfoUnknown()
    {
        SetupRam([]);

        var result = await _wmiQuery.DetectRamAsync();

        Assert.Equal(RamInfo.Unknown, result);
    }

    // ── Storage ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DetectStorageAsync_NvmeDisk_ReturnsNvmeSsdTypeAndHealthyStatus()
    {
        SetupStorage([HardwareFixtures.NvmeDiskRow]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(StorageType.NvmeSsd, result.Type);
        Assert.Equal(SmartStatus.Healthy, result.SmartStatus);
        Assert.Equal("Samsung SSD 970 EVO Plus", result.Model);
    }

    [Fact]
    public async Task DetectStorageAsync_SsdDisk_ReturnsSsdType()
    {
        var ssdRow = new Dictionary<string, object?>
        {
            ["FriendlyName"] = "Samsung SSD 870 EVO",
            ["HealthStatus"] = 0u,
            ["MediaType"] = 4u,  // SSD
            ["BusType"] = 7u,    // SATA — not NVMe
            ["Size"] = 1L * 1024 * 1024 * 1024 * 1024
        };
        SetupStorage([ssdRow]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(StorageType.Ssd, result.Type);
    }

    [Fact]
    public async Task DetectStorageAsync_HddDisk_ReturnsHddType()
    {
        SetupStorage([HardwareFixtures.HddRow]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(StorageType.Hdd, result.Type);
        Assert.Equal(SmartStatus.Healthy, result.SmartStatus);
    }

    [Fact]
    public async Task DetectStorageAsync_DegradedHealth_ReturnsWarningStatus()
    {
        var warningRow = new Dictionary<string, object?>
        {
            ["FriendlyName"] = "Samsung SSD 970 EVO Plus",
            ["HealthStatus"] = 1u,
            ["MediaType"] = 4u,
            ["BusType"] = 11u,
            ["Size"] = 1L * 1024 * 1024 * 1024 * 1024
        };
        SetupStorage([warningRow]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(SmartStatus.Warning, result.SmartStatus);
    }

    [Fact]
    public async Task DetectStorageAsync_FailingDisk_ReturnsUnhealthyStatus()
    {
        var failingRow = new Dictionary<string, object?>
        {
            ["FriendlyName"] = "Samsung SSD 970 EVO Plus",
            ["HealthStatus"] = 2u,
            ["MediaType"] = 4u,
            ["BusType"] = 11u,
            ["Size"] = 1L * 1024 * 1024 * 1024 * 1024
        };
        SetupStorage([failingRow]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(SmartStatus.Unhealthy, result.SmartStatus);
    }

    [Fact]
    public async Task DetectStorageAsync_EmptyRows_ReturnsStorageInfoUnknown()
    {
        SetupStorage([]);

        var result = await _wmiQuery.DetectStorageAsync();

        Assert.Equal(StorageInfo.Unknown, result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupCpu(IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        _mockExecutor.Setup(x => x.Execute("Win32_Processor", It.IsAny<string[]>(), null, @"root\cimv2"))
                     .Returns(rows);

    private void SetupGpu(IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        _mockExecutor.Setup(x => x.Execute("Win32_VideoController", It.IsAny<string[]>(), null, @"root\cimv2"))
                     .Returns(rows);

    private void SetupRam(IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        _mockExecutor.Setup(x => x.Execute("Win32_PhysicalMemory", It.IsAny<string[]>(), null, @"root\cimv2"))
                     .Returns(rows);

    private void SetupStorage(IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        _mockExecutor.Setup(x => x.Execute("MSFT_PhysicalDisk", It.IsAny<string[]>(), null, @"root\microsoft\windows\storage"))
                     .Returns(rows);
}

#pragma warning restore CA1416
