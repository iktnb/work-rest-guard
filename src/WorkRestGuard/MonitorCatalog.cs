using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace WorkRestGuard;

public sealed class MonitorCatalog
{
    private readonly FileLogger _logger;

    public MonitorCatalog(FileLogger logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<MonitorDescriptor> GetMonitors()
    {
        var devices = EnumerateDisplayDevices();
        var monitors = new List<MonitorDescriptor>();

        foreach (var screen in Screen.AllScreens)
        {
            devices.TryGetValue(screen.DeviceName, out var device);
            var deviceId = device.DeviceId ?? screen.DeviceName;
            var modelCode = ExtractModelCode(deviceId);
            var edid = ReadEdid(modelCode);
            var friendlyName = ParseEdidText(edid, 0xFC)
                ?? CleanFriendlyName(device.DeviceString)
                ?? modelCode
                ?? screen.DeviceName;
            var material = BuildHardwareMaterial(deviceId, modelCode, edid, screen.Bounds.Size);

            monitors.Add(new MonitorDescriptor(
                screen.DeviceName,
                deviceId,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))),
                friendlyName,
                screen.Bounds,
                screen.WorkingArea,
                screen.Primary));
        }

        return monitors;
    }

    public MonitorDescriptor? Find(
        IReadOnlyList<MonitorDescriptor> monitors,
        MonitorIdentity identity)
    {
        var exact = monitors.Where(monitor =>
            !string.IsNullOrWhiteSpace(identity.DeviceId) &&
            string.Equals(
                monitor.DeviceId,
                identity.DeviceId,
                StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1)
        {
            return exact[0];
        }

        var hardware = monitors.Where(monitor =>
            !string.IsNullOrWhiteSpace(identity.HardwareIdHash) &&
            string.Equals(
                monitor.HardwareIdHash,
                identity.HardwareIdHash,
                StringComparison.OrdinalIgnoreCase)).ToList();
        if (hardware.Count == 1)
        {
            return hardware[0];
        }

        var fallback = monitors.Where(monitor =>
            monitor.Bounds.Width == identity.Width &&
            monitor.Bounds.Height == identity.Height &&
            string.Equals(
                monitor.FriendlyName,
                identity.FriendlyName,
                StringComparison.OrdinalIgnoreCase)).ToList();

        return fallback.Count == 1 ? fallback[0] : null;
    }

    public static MonitorDescriptor? ChooseDefault(IReadOnlyList<MonitorDescriptor> monitors) =>
        monitors.FirstOrDefault(monitor => monitor.Primary)
        ?? monitors
            .OrderByDescending(monitor => (long)monitor.Bounds.Width * monitor.Bounds.Height)
            .FirstOrDefault();

    private Dictionary<string, (string? DeviceId, string? DeviceString)> EnumerateDisplayDevices()
    {
        var result = new Dictionary<string, (string?, string?)>(
            StringComparer.OrdinalIgnoreCase);

        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            var adapter = NativeMethods.DisplayDevice.Create();
            if (!NativeMethods.EnumDisplayDevices(null, adapterIndex, ref adapter, 0))
            {
                break;
            }

            if ((adapter.StateFlags & NativeMethods.DisplayDeviceActive) == 0)
            {
                continue;
            }

            var monitor = NativeMethods.DisplayDevice.Create();
            if (NativeMethods.EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0))
            {
                result[adapter.DeviceName] = (monitor.DeviceId, monitor.DeviceString);
            }
            else
            {
                result[adapter.DeviceName] = (adapter.DeviceId, adapter.DeviceString);
            }
        }

        return result;
    }

    private byte[]? ReadEdid(string? modelCode)
    {
        if (string.IsNullOrWhiteSpace(modelCode))
        {
            return null;
        }

        try
        {
            using var modelKey = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{modelCode}");
            if (modelKey is null)
            {
                return null;
            }

            foreach (var instance in modelKey.GetSubKeyNames())
            {
                using var parameters = modelKey.OpenSubKey($@"{instance}\Device Parameters");
                if (parameters?.GetValue("EDID") is byte[] edid && edid.Length >= 128)
                {
                    return edid;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.Warn(
                $"Could not read monitor hardware metadata: {exception.GetType().Name}");
        }

        return null;
    }

    private static string BuildHardwareMaterial(
        string deviceId,
        string? modelCode,
        byte[]? edid,
        Size resolution)
    {
        if (edid is not null)
        {
            return Convert.ToHexString(SHA256.HashData(edid));
        }

        return $"{deviceId}|{modelCode}|{resolution.Width}x{resolution.Height}"
            .ToUpperInvariant();
    }

    private static string? ExtractModelCode(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var parts = deviceId.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : null;
    }

    private static string? CleanFriendlyName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains("Generic", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? ParseEdidText(byte[]? edid, byte descriptorType)
    {
        if (edid is null)
        {
            return null;
        }

        for (var offset = 54; offset + 18 <= edid.Length && offset < 126; offset += 18)
        {
            if (edid[offset] != 0 ||
                edid[offset + 1] != 0 ||
                edid[offset + 3] != descriptorType)
            {
                continue;
            }

            var text = Encoding.ASCII.GetString(edid, offset + 5, 13)
                .Trim('\0', '\r', '\n', ' ');
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        return null;
    }
}
