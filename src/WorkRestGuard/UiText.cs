using System.Globalization;

namespace WorkRestGuard;

internal enum TextKey
{
    BreakMinutes,
    Cancel,
    Date,
    DayCompleted,
    DaySkipped,
    DaySnoozed,
    EmergencyExit,
    EventTime,
    EventType,
    GuardEnabled,
    HistoryDescription,
    HistoryEmpty,
    HistoryTab,
    InvalidSchedule,
    Language,
    MonitorMissing,
    NightCompleted,
    NightEnd,
    NightGrace,
    NightInterrupted,
    NightOverridden,
    NightStart,
    OpenHistory,
    OpenSettings,
    PreviewMonitor,
    ProtectedMonitor,
    SafeMode,
    SaveApply,
    SettingsTab,
    SettingsTitle,
    SetupDescription,
    SetupHeading,
    StartBreakNow,
    StartWithWindows,
    StartupError,
    StatusDayResting,
    StatusNightLocked,
    StatusReady,
    StatusWorking,
    TimerAccessibleName,
    TimerPhaseAccessibleName,
    TimerRest,
    TimerWorking,
    WorkMinutes
}

internal static class UiText
{
    private static readonly IReadOnlyDictionary<TextKey, string> Ukrainian =
        new Dictionary<TextKey, string>
        {
            [TextKey.SettingsTitle] = "Work Rest Guard — Налаштування",
            [TextKey.SettingsTab] = "Налаштування",
            [TextKey.HistoryTab] = "Історія 30 днів",
            [TextKey.SetupHeading] = "Твій час і нагадування про відпочинок",
            [TextKey.SetupDescription] = "Статистика записується постійно. Маленький таймер нагадує відпочити й ховається у встановлений нічний час.",
            [TextKey.ProtectedMonitor] = "Монітор таймера",
            [TextKey.PreviewMonitor] = "Показати preview",
            [TextKey.WorkMinutes] = "Робота до перерви, хв",
            [TextKey.BreakMinutes] = "Тривалість перерви, хв",
            [TextKey.NightStart] = "Ховати таймер з",
            [TextKey.NightEnd] = "Показувати таймер з",
            [TextKey.Language] = "Мова",
            [TextKey.StartWithWindows] = "Запускати разом із Windows",
            [TextKey.GuardEnabled] = "Показувати нагадування про відпочинок",
            [TextKey.StartBreakNow] = "Почати перерву зараз",
            [TextKey.SaveApply] = "Зберегти й застосувати",
            [TextKey.Cancel] = "Скасувати",
            [TextKey.HistoryDescription] = "Події таймера за 30 днів. Завершення таймера не підтверджує фактичного відпочинку. Старі події збережено.",
            [TextKey.HistoryEmpty] = "Для цього дня подій немає.",
            [TextKey.Date] = "Дата",
            [TextKey.DayCompleted] = "Таймер завершено",
            [TextKey.DaySnoozed] = "День: відстрочки",
            [TextKey.DaySkipped] = "День: пропуски",
            [TextKey.NightCompleted] = "Ніч: завершено",
            [TextKey.NightGrace] = "Ніч: +10 хв",
            [TextKey.NightOverridden] = "Ніч: override",
            [TextKey.NightInterrupted] = "Ніч: перервано",
            [TextKey.EventTime] = "Час",
            [TextKey.EventType] = "Подія",
            [TextKey.StatusReady] = "Готовий — таймер почнеться з першого вводу",
            [TextKey.StatusWorking] = "Робота · {0}",
            [TextKey.StatusDayResting] = "Перерва · {0}",
            [TextKey.StatusNightLocked] = "Нічний блок · {0}",
            [TextKey.TimerRest] = "ЧАС ВІДПОЧИТИ",
            [TextKey.TimerWorking] = "РОБОТА",
            [TextKey.TimerAccessibleName] = "Таймер Work Rest Guard",
            [TextKey.TimerPhaseAccessibleName] = "Поточний режим",
            [TextKey.OpenSettings] = "Налаштування…",
            [TextKey.OpenHistory] = "Історія 30 днів…",
            [TextKey.SafeMode] = "Діагностика: запис і нагадування вимкнено",
            [TextKey.MonitorMissing] = "Монітор таймера не знайдено. Запис статистики триває.",
            [TextKey.InvalidSchedule] = "Час початку й завершення приховування таймера має відрізнятися.",
            [TextKey.StartupError] = "Не вдалося змінити автозапуск. Інші налаштування не змінено.",
            [TextKey.EmergencyExit] = "Аварійний вихід"
        };

    private static readonly IReadOnlyDictionary<TextKey, string> English =
        new Dictionary<TextKey, string>
        {
            [TextKey.SettingsTitle] = "Work Rest Guard — Settings",
            [TextKey.SettingsTab] = "Settings",
            [TextKey.HistoryTab] = "30-day history",
            [TextKey.SetupHeading] = "Your time and rest reminders",
            [TextKey.SetupDescription] = "Statistics are recorded continuously. The compact timer reminds you to rest and stays hidden during quiet hours.",
            [TextKey.ProtectedMonitor] = "Timer monitor",
            [TextKey.PreviewMonitor] = "Preview monitor",
            [TextKey.WorkMinutes] = "Work before break, minutes",
            [TextKey.BreakMinutes] = "Break duration, minutes",
            [TextKey.NightStart] = "Hide timer from",
            [TextKey.NightEnd] = "Show timer from",
            [TextKey.Language] = "Language",
            [TextKey.StartWithWindows] = "Start with Windows",
            [TextKey.GuardEnabled] = "Show rest reminders",
            [TextKey.StartBreakNow] = "Start a break now",
            [TextKey.SaveApply] = "Save and apply",
            [TextKey.Cancel] = "Cancel",
            [TextKey.HistoryDescription] = "Local timer events for 30 days. Completed timers do not confirm actual rest. Older events remain in history.",
            [TextKey.HistoryEmpty] = "There are no events for this day.",
            [TextKey.Date] = "Date",
            [TextKey.DayCompleted] = "Reminder ended",
            [TextKey.DaySnoozed] = "Day: snoozes",
            [TextKey.DaySkipped] = "Day: skipped",
            [TextKey.NightCompleted] = "Night: completed",
            [TextKey.NightGrace] = "Night: +10 min",
            [TextKey.NightOverridden] = "Night: override",
            [TextKey.NightInterrupted] = "Night: interrupted",
            [TextKey.EventTime] = "Time",
            [TextKey.EventType] = "Event",
            [TextKey.StatusReady] = "Ready — the timer starts with the first input",
            [TextKey.StatusWorking] = "Work · {0}",
            [TextKey.StatusDayResting] = "Break · {0}",
            [TextKey.StatusNightLocked] = "Night block · {0}",
            [TextKey.TimerRest] = "TIME TO REST",
            [TextKey.TimerWorking] = "WORK",
            [TextKey.TimerAccessibleName] = "Work Rest Guard timer",
            [TextKey.TimerPhaseAccessibleName] = "Current mode",
            [TextKey.OpenSettings] = "Settings…",
            [TextKey.OpenHistory] = "30-day history…",
            [TextKey.SafeMode] = "Diagnostic mode: recording and reminders are disabled",
            [TextKey.MonitorMissing] = "The timer monitor is missing. Statistics recording continues.",
            [TextKey.InvalidSchedule] = "Quiet hours must have different start and end times.",
            [TextKey.StartupError] = "Windows startup could not be changed. No other setting was changed.",
            [TextKey.EmergencyExit] = "Emergency exit"
        };

    internal static string Get(string languageCode, TextKey key) =>
        IsEnglish(languageCode) ? English[key] : Ukrainian[key];

    internal static CultureInfo Culture(string languageCode) =>
        CultureInfo.GetCultureInfo(IsEnglish(languageCode) ? "en-US" : "uk-UA");

    internal static bool IsEnglish(string languageCode) =>
        string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase);
}
