using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkRestGuard;

public sealed class GuardEventCollection
{
    public int SchemaVersion { get; set; } = 1;
    public List<GuardEvent> Events { get; set; } = [];
}

public sealed class HistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _gate = new();
    private readonly string _path;
    private readonly FileLogger _logger;
    private readonly TimeZoneInfo _timeZone;

    public HistoryStore(
        FileLogger logger,
        string? path = null,
        TimeZoneInfo? timeZone = null)
    {
        _logger = logger;
        _path = path ?? AppPaths.HistoryPath;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public void Append(GuardEvent guardEvent, DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var collection = LoadCore();
            collection.Events.Add(guardEvent);
            Prune(collection, nowUtc);
            SaveCore(collection);
        }
    }

    public IReadOnlyList<GuardEvent> GetEvents(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var collection = LoadCore();
            Prune(collection, nowUtc);
            return collection.Events
                .OrderByDescending(item => item.OccurredAtUtc)
                .ToList();
        }
    }

    public IReadOnlyList<HistoryDaySummary> GetDailySummaries(DateTimeOffset nowUtc)
    {
        var events = GetEvents(nowUtc);
        var localToday = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(nowUtc, _timeZone).DateTime);

        return Enumerable.Range(0, 30)
            .Select(offset => localToday.AddDays(-offset))
            .Select(date =>
            {
                var dayEvents = events.Where(item =>
                    DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTime(item.OccurredAtUtc, _timeZone).DateTime) == date);

                return new HistoryDaySummary(
                    date,
                    dayEvents.Count(item => item.Type == GuardEventType.DayBreakCompleted),
                    dayEvents.Count(item => item.Type == GuardEventType.DaySnoozed),
                    dayEvents.Count(item => item.Type == GuardEventType.DayBreakSkipped),
                    dayEvents.Count(item => item.Type == GuardEventType.NightCompleted),
                    dayEvents.Count(item => item.Type == GuardEventType.NightGraceUsed),
                    dayEvents.Count(item => item.Type == GuardEventType.NightOverridden),
                    dayEvents.Count(item => item.Type == GuardEventType.NightInterrupted));
            })
            .ToList();
    }

    private GuardEventCollection LoadCore()
    {
        if (!File.Exists(_path))
        {
            return new GuardEventCollection();
        }

        try
        {
            return JsonSerializer.Deserialize<GuardEventCollection>(
                File.ReadAllText(_path),
                JsonOptions) ?? new GuardEventCollection();
        }
        catch (Exception exception)
        {
            _logger.Error("Could not load history", exception);
            return new GuardEventCollection();
        }
    }

    private void SaveCore(GuardEventCollection collection)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(collection, JsonOptions));
            File.Move(temporaryPath, _path, true);
        }
        catch (Exception exception)
        {
            _logger.Error("Could not save history", exception);
        }
    }

    private void Prune(GuardEventCollection collection, DateTimeOffset nowUtc)
    {
        collection.Events ??= [];
        var localToday = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(nowUtc, _timeZone).DateTime);
        var firstDate = localToday.AddDays(-29);

        collection.Events.RemoveAll(item =>
            DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(item.OccurredAtUtc, _timeZone).DateTime) < firstDate);
    }
}
