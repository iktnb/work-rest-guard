using System.Drawing;

namespace WorkRestGuard.Tests;

public sealed class MonitorCatalogTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "work-rest-guard-tests-" + Guid.NewGuid());
    private readonly FileLogger _logger;

    public MonitorCatalogTests()
    {
        Directory.CreateDirectory(_directory);
        _logger = new FileLogger(Path.Combine(_directory, "test.log"));
    }

    [Fact]
    public void HardwareIdentitySurvivesDisplayNameChange()
    {
        var catalog = new MonitorCatalog(_logger);
        var monitors = Monitors();
        var identity = new MonitorIdentity
        {
            DeviceId = "stale",
            HardwareIdHash = "WORK-HASH",
            FriendlyName = "Work",
            Width = 2560,
            Height = 1440
        };

        Assert.Same(monitors[0], catalog.Find(monitors, identity));
    }

    [Fact]
    public void PrimaryMonitorIsDefault()
    {
        var monitors = Monitors();

        Assert.Same(monitors[0], MonitorCatalog.ChooseDefault(monitors));
    }

    [Fact]
    public void MissingMonitorFailsOpen()
    {
        var catalog = new MonitorCatalog(_logger);

        Assert.Null(catalog.Find(
            Monitors(),
            new MonitorIdentity
            {
                DeviceId = "missing",
                HardwareIdHash = "missing",
                FriendlyName = "missing",
                Width = 1,
                Height = 1
            }));
    }

    public void Dispose()
    {
        _logger.Dispose();
        Directory.Delete(_directory, true);
    }

    private static IReadOnlyList<MonitorDescriptor> Monitors() =>
    [
        new(
            "DISPLAY2",
            "WORK",
            "WORK-HASH",
            "Work",
            new Rectangle(0, 0, 2560, 1440),
            new Rectangle(0, 0, 2560, 1392),
            true),
        new(
            "DISPLAY1",
            "OBSIDIAN",
            "OBSIDIAN-HASH",
            "Obsidian",
            new Rectangle(-1920, 357, 1920, 1080),
            new Rectangle(-1920, 357, 1920, 1032),
            false)
    ];
}
