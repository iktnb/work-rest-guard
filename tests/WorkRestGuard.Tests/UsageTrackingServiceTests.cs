using System.Text.Json;

namespace WorkRestGuard.Tests;

public sealed class UsageTrackingServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "usage-service-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
    private long _ticks;

    [Fact]
    public void LegacyPauseAndDisabledRemindersCannotStopCollection()
    {
        using var logger = Logger();
        var config = TestData.Config();
        config.GuardEnabled = false;
        config.UsageTrackingEnabled = false;
        var service = Service(logger, config);
        service.Capture(GuardPhase.Ready);
        Advance(5);
        service.Capture(GuardPhase.Ready);
        service.ApplyConfig(config, GuardPhase.Ready);
        Advance(5);
        service.Capture(GuardPhase.Ready);
        Advance(5);
        service.Stop(GuardPhase.Ready);
        var spans = service.Store.Read(_clock.UtcNow.AddMinutes(-1), _clock.UtcNow);
        Assert.Equal(15, spans.Sum(span => span.Duration.TotalSeconds));
    }

    [Fact]
    public void QuietHoursDoNotInterruptStatistics()
    {
        using var logger = Logger();
        var local = TimeZoneInfo.ConvertTime(_clock.UtcNow, TimeZoneInfo.Local);
        var config = TestData.Config();
        config.NightStart = TimeOnly.FromDateTime(local.DateTime.AddHours(-1));
        config.NightEnd = TimeOnly.FromDateTime(local.DateTime.AddHours(1));
        var service = Service(logger, config);
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.Stop(GuardPhase.Working);
        var spans = service.Store.Read(_clock.UtcNow.AddMinutes(-1), _clock.UtcNow);
        Assert.Single(spans);
        Assert.True(spans[0].IsNightWindow);
        Assert.Equal("code.exe", spans[0].Application);
        Assert.Equal(5, spans[0].Duration.TotalSeconds);
    }

    [Fact]
    public void LockAndSuspendSignalsDoNotChargeTheForegroundApp()
    {
        using var logger = Logger();
        var service = Service(logger, TestData.Config());
        var start = _clock.UtcNow;
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.SetLocked(true, GuardPhase.Working);
        Advance(5);
        service.Capture(GuardPhase.Working);
        service.SetSuspended(true, GuardPhase.Working);
        Advance(3600);
        service.SetSuspended(false, GuardPhase.Working);
        service.SetLocked(false, GuardPhase.Working);
        Advance(5);
        service.Stop(GuardPhase.Working);
        var report = UsageReport.Build(service.Store.Read(start, _clock.UtcNow), start, _clock.UtcNow, TimeZoneInfo.Utc);
        Assert.Equal(TimeSpan.FromSeconds(10), report.Total(UsageKind.Active));
        Assert.Equal(TimeSpan.FromSeconds(5), report.Total(UsageKind.Locked));
        Assert.Equal(TimeSpan.FromHours(1), report.Total(UsageKind.Suspended));
    }

    [Fact]
    public void SafeModeDoesNotCollect()
    {
        using var logger = Logger();
        var service = new UsageTrackingService(new UsageStore(Path.Combine(_directory, "usage")), logger,
            TestData.Config(), _clock, safeMode: true, new FakeSource(), () => _ticks);
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.Stop(GuardPhase.Working);
        Assert.Empty(service.Store.Read(_clock.UtcNow.AddHours(-1), _clock.UtcNow));
    }

    [Fact]
    public void StorageFailureIsReportedAndDoesNotEscapeIntoProtection()
    {
        using var logger = Logger();
        var usagePath = Path.Combine(_directory, "usage");
        File.WriteAllText(usagePath, "blocked");
        var service = Service(logger, TestData.Config());
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.Capture(GuardPhase.Working);
        service.Flush();
        Assert.True(service.HasError);
        File.Delete(usagePath);
        service.Flush();
        Assert.False(service.HasError);
        Assert.Single(service.Store.Read(_clock.UtcNow.AddMinutes(-1), _clock.UtcNow));
    }

    [Fact]
    public void OldConfigurationNormalizesToContinuousRecordingWithoutNightBlocking()
    {
        var old = JsonSerializer.Deserialize<GuardConfig>("{\"SchemaVersion\":1,\"NightModeEnabled\":true,\"UsageTrackingEnabled\":false}")!;
        old.Normalize();
        Assert.True(old.UsageTrackingEnabled);
        Assert.False(old.NightModeEnabled);
        Assert.Equal(5, old.UsageIdleMinutes);
    }

    [Fact]
    public void RefreshCannotHideACorruptedStoreObservationError()
    {
        using var logger = Logger();
        var usagePath = Path.Combine(_directory, "usage");
        Directory.CreateDirectory(usagePath);
        var path = Path.Combine(usagePath, "2026-10-19.json");
        File.WriteAllText(path, "{broken");
        var service = Service(logger, TestData.Config());
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.Capture(GuardPhase.Working);
        Assert.True(service.HasError);
        service.Flush();
        Assert.True(service.HasError);
        Assert.Equal("{broken", File.ReadAllText(path));
    }

    [Fact]
    public void FailedErrorLoggingDoesNotInterruptProtectionEither()
    {
        var logDirectory = Path.Combine(_directory, "logs");
        using var logger = new FileLogger(Path.Combine(logDirectory, "guard.log"));
        Directory.Delete(logDirectory);
        File.WriteAllText(Path.Combine(_directory, "usage"), "blocked");
        var service = Service(logger, TestData.Config());
        service.Capture(GuardPhase.Working);
        Advance(5);
        service.Capture(GuardPhase.Working);
        service.Flush();
        Assert.True(service.HasError);
    }

    private FileLogger Logger() => new(Path.Combine(_directory, "guard.log"));
    private UsageTrackingService Service(FileLogger logger, GuardConfig config) =>
        new(new UsageStore(Path.Combine(_directory, "usage")), logger, config, _clock,
            source: new FakeSource(), ticks: () => _ticks);
    private void Advance(int seconds)
    {
        _clock.Advance(TimeSpan.FromSeconds(seconds));
        _ticks += seconds * 1000L;
    }

    private sealed class FakeSource : IUsageSource
    {
        public UsageSnapshot Capture(int idleMinutes) => new(UsageKind.Active, "code.exe");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
