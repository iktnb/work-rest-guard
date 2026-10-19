using System.Text.Json;

namespace WorkRestGuard.Tests;

public sealed class UsageStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "usage-store-tests", Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AdjacentEquivalentSpansMergeAndRoundTripWithoutPrivateContentFields()
    {
        var store = new UsageStore(_directory);
        store.Append(Span(Now, Now.AddSeconds(5)));
        store.Append(Span(Now.AddSeconds(5), Now.AddSeconds(10)));
        store.Flush(Now);
        var read = new UsageStore(_directory).Read(Now, Now.AddMinutes(1));
        Assert.Equal(TimeSpan.FromSeconds(10), Assert.Single(read).Duration);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, "2026-10-19.json")));
        var names = document.RootElement.GetProperty("spans")[0].EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(new[] { "application", "endUtc", "isNightWindow", "kind", "phase", "startUtc" }, names);
    }

    [Fact]
    public void UtcMidnightSplitsFilesWithoutLosingDuration()
    {
        var start = new DateTimeOffset(2026, 10, 18, 23, 59, 55, TimeSpan.Zero);
        var store = new UsageStore(_directory);
        store.Append(Span(start, start.AddSeconds(10)));
        store.Flush(Now);
        Assert.Equal(2, Directory.GetFiles(_directory, "*.json").Length);
        var spans = new UsageStore(_directory).Read(start, start.AddSeconds(10));
        Assert.Equal(2, spans.Count);
        Assert.Equal(10, spans.Sum(span => span.Duration.TotalSeconds));
    }

    [Fact]
    public void RetentionKeepsNinetyUtcDaysAndLeavesUnrelatedFilesAlone()
    {
        Directory.CreateDirectory(_directory);
        var unrelated = Path.Combine(_directory, "notes.json");
        File.WriteAllText(unrelated, "preserve");
        var store = new UsageStore(_directory);
        foreach (var offset in new[] { -90, -89, 0 })
        {
            var start = Now.AddDays(offset);
            store.Append(Span(start, start.AddSeconds(5)));
        }

        store.Flush(Now);
        Assert.Equal(3, Directory.GetFiles(_directory).Length);
        Assert.Empty(store.Read(Now.AddDays(-90), Now.AddDays(-89)));
        Assert.Single(store.Read(Now.AddDays(-89), Now.AddDays(-88)));
        Assert.Equal("preserve", File.ReadAllText(unrelated));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"schemaVersion\":2,\"spans\":[]}")]
    [InlineData("{\"schemaVersion\":1,\"spans\":[{}]}")]
    public void InvalidOrNewerDataIsNeverOverwritten(string original)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "2026-10-19.json");
        File.WriteAllText(path, original);
        var store = new UsageStore(_directory);
        Assert.ThrowsAny<Exception>(() => store.Append(Span(Now, Now.AddSeconds(5))));
        store.Flush(Now);
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Theory]
    [InlineData("C:\\private\\app.exe")]
    [InlineData("private\ndocument")]
    public void StoreRejectsPathsAndMultilineContentAsApplicationIdentity(string name)
    {
        var store = new UsageStore(_directory);
        Assert.Throws<InvalidDataException>(() => store.Append(Span(Now, Now.AddSeconds(5)) with { Application = name }));
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void OverlapIsRejectedInsteadOfDoubleCounting()
    {
        var store = new UsageStore(_directory);
        store.Append(Span(Now, Now.AddSeconds(10)));
        Assert.Throws<InvalidDataException>(() => store.Append(Span(Now.AddSeconds(5), Now.AddSeconds(15))));
        Assert.Single(store.Read(Now, Now.AddMinutes(1)));
    }

    private static UsageSpan Span(DateTimeOffset start, DateTimeOffset end) =>
        new(start, end, UsageKind.Active, "code.exe", GuardPhase.Working, false);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
