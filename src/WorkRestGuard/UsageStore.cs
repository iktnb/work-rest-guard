using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkRestGuard;

internal sealed class UsageDay
{
    [JsonRequired]
    public int SchemaVersion { get; set; } = 1;
    [JsonRequired]
    public List<UsageSpan> Spans { get; set; } = [];
}

internal sealed class UsageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _directory;
    private readonly Dictionary<DateOnly, UsageDay> _cache = [];
    private readonly HashSet<DateOnly> _dirty = [];
    private DateOnly? _lastPrunedDay;

    internal UsageStore(string directory) => _directory = directory;

    internal void Append(UsageSpan span)
    {
        ValidateSpan(span);
        var start = span.StartUtc.ToUniversalTime();
        while (start < span.EndUtc)
        {
            var day = DateOnly.FromDateTime(start.UtcDateTime);
            var boundary = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = span.EndUtc < boundary ? span.EndUtc : boundary;
            if (!_cache.TryGetValue(day, out var document))
            {
                document = ReadDay(day);
                _cache.Add(day, document);
            }

            var piece = span with { StartUtc = start, EndUtc = end };
            var last = document.Spans.LastOrDefault();
            if (last is not null && last.EndUtc > start)
            {
                throw new InvalidDataException("Usage intervals overlap.");
            }

            if (last is not null && last.EndUtc == start &&
                last.Kind == piece.Kind && last.Application == piece.Application &&
                last.Phase == piece.Phase && last.IsNightWindow == piece.IsNightWindow)
            {
                document.Spans[^1] = last with { EndUtc = end };
            }
            else
            {
                document.Spans.Add(piece);
            }

            _dirty.Add(day);
            start = end;
        }
    }

    internal void Flush(DateTimeOffset nowUtc)
    {
        if (_dirty.Count > 0)
        {
            Directory.CreateDirectory(_directory);
        }

        foreach (var day in _dirty.ToArray())
        {
            var path = DayPath(day);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(_cache[day], JsonOptions));
            File.Move(temporary, path, overwrite: true);
            _dirty.Remove(day);
        }

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        if (_lastPrunedDay == today || !Directory.Exists(_directory))
        {
            return;
        }

        var oldest = today.AddDays(-89);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(path), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) && day < oldest)
            {
                File.Delete(path);
                _cache.Remove(day);
            }
        }

        _lastPrunedDay = today;
    }

    internal IReadOnlyList<UsageSpan> Read(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
        {
            return [];
        }

        var result = new List<UsageSpan>();
        var first = DateOnly.FromDateTime(startUtc.UtcDateTime);
        var last = DateOnly.FromDateTime(endUtc.AddTicks(-1).UtcDateTime);
        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var document = _cache.TryGetValue(day, out var cached) ? cached : ReadDay(day);
            result.AddRange(document.Spans.Where(span => span.StartUtc < endUtc && span.EndUtc > startUtc));
        }

        return result;
    }

    private UsageDay ReadDay(DateOnly day)
    {
        var path = DayPath(day);
        if (!File.Exists(path))
        {
            return new UsageDay();
        }

        if (new FileInfo(path).Length > 16 * 1024 * 1024)
        {
            throw new InvalidDataException("Usage day exceeds the supported file size.");
        }

        var document = JsonSerializer.Deserialize<UsageDay>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Usage document is empty.");
        if (document.SchemaVersion != 1 || document.Spans is null)
        {
            throw new InvalidDataException("Unsupported usage document.");
        }

        DateTimeOffset? previousEnd = null;
        foreach (var span in document.Spans)
        {
            ValidateSpan(span);
            if (DateOnly.FromDateTime(span.StartUtc.UtcDateTime) != day ||
                DateOnly.FromDateTime(span.EndUtc.AddTicks(-1).UtcDateTime) != day ||
                (previousEnd is not null && previousEnd > span.StartUtc))
            {
                throw new InvalidDataException("Invalid usage day boundaries.");
            }

            previousEnd = span.EndUtc;
        }

        return document;
    }

    private string DayPath(DateOnly day) => Path.Combine(
        _directory, day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".json");

    private static void ValidateSpan(UsageSpan span)
    {
        if (span is null || span.EndUtc <= span.StartUtc || !Enum.IsDefined(span.Kind) ||
            span.Kind == UsageKind.Unrecorded || !Enum.IsDefined(span.Phase) ||
            (span.Kind != UsageKind.Active && span.Application is not null) ||
            span.Application is { Length: > 160 } ||
            span.Application?.IndexOfAny(['/', '\\', ':', '\r', '\n', '\0']) >= 0)
        {
            throw new InvalidDataException("Invalid usage interval.");
        }
    }
}
