using System.Globalization;

namespace WorkRestGuard;

internal static class UsageText
{
    internal static string Pick(string language, string ukrainian, string english) =>
        UiText.IsEnglish(language) ? english : ukrainian;

    internal static string Kind(string language, UsageKind kind) => kind switch
    {
        UsageKind.Active => Pick(language, "Активність", "Activity"),
        UsageKind.Idle => Pick(language, "Без вводу", "No input"),
        UsageKind.Locked => Pick(language, "Windows заблоковано", "Windows locked"),
        UsageKind.Suspended => Pick(language, "Сон комп’ютера", "Computer suspended"),
        UsageKind.Unknown => Pick(language, "Не вдалося визначити", "Unknown state"),
        _ => Pick(language, "Немає запису", "Unrecorded")
    };

    internal static string Duration(string language, TimeSpan duration)
    {
        var culture = UiText.Culture(language);
        if (duration.TotalMinutes < 1)
        {
            return Math.Max(0, (int)duration.TotalSeconds).ToString(culture) + Pick(language, " с", " s");
        }

        var minutes = (long)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero);
        return string.Format(culture, Pick(language, "{0} год {1} хв", "{0} h {1} min"),
            minutes / 60, minutes % 60);
    }

    internal static string Application(string language, string? name) => name ??
        Pick(language, "Невизначена програма", "Unattributed application");
}
