using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkRestGuard;

public sealed class AtomicJsonStore<T>
    where T : class, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly FileLogger _logger;
    private readonly string _path;

    public AtomicJsonStore(string path, FileLogger logger)
    {
        _path = path;
        _logger = logger;
    }

    public T Load()
    {
        if (!File.Exists(_path))
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(_path), JsonOptions) ?? new T();
        }
        catch (Exception exception)
        {
            _logger.Error($"Could not load {Path.GetFileName(_path)}", exception);
            return new T();
        }
    }

    public void Save(T value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(temporaryPath, _path, true);
        }
        catch (Exception exception)
        {
            _logger.Error($"Could not save {Path.GetFileName(_path)}", exception);
            throw;
        }
    }
}
