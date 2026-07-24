namespace WorkRestGuard.Tests;

public sealed class AtomicJsonStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "work-rest-json-" + Guid.NewGuid());
    private readonly FileLogger _logger;

    public AtomicJsonStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _logger = new FileLogger(Path.Combine(_directory, "test.log"));
    }

    [Fact]
    public void ConfigurationRoundTripsWithoutTemporaryFile()
    {
        var path = Path.Combine(_directory, "config.json");
        var store = new AtomicJsonStore<GuardConfig>(path, _logger);
        var config = TestData.Config();

        store.Save(config);
        var restored = store.Load();

        Assert.Equal(90, restored.WorkMinutes);
        Assert.Equal("WORK-HASH", restored.ProtectedMonitor.HardwareIdHash);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void CorruptJsonReturnsSafeDefault()
    {
        var path = Path.Combine(_directory, "state.json");
        File.WriteAllText(path, "not-json");
        var store = new AtomicJsonStore<GuardRuntimeState>(path, _logger);

        var restored = store.Load();

        Assert.Equal(GuardPhase.Ready, restored.Phase);
    }

    public void Dispose()
    {
        _logger.Dispose();
        Directory.Delete(_directory, true);
    }
}
