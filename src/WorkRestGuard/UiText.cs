using System.Globalization;

namespace WorkRestGuard;

internal enum TextKey
{
    SettingsTitle,
    SettingsTab,
    HistoryTab,
    SetupHeading,
    SetupDescription,
    ProtectedMonitor,
    PreviewMonitor,
    WorkMinutes,
    BreakMinutes,
    NightMode,
    NightStart,
    NightEnd,
    NightDays,
    Language,
    StartWithWindows,
    GuardEnabled,
    StartBreakNow,
    SaveApply,
    Cancel,
    HistoryDescription,
    HistoryEmpty,
    Date,
    DayCompleted,
    DaySnoozed,
    DaySkipped,
    NightCompleted,
    NightGrace,
    NightOverridden,
    NightInterrupted,
    EventTime,
    EventType,
    DayTitle,
    DayDetail,
    NightTitle,
    NightDetail,
    Snooze5,
    Snooze15,
    Snooze30,
    NightGrace10,
    SkipDay,
    OverrideNight,
    ExactInstruction,
    ExactProgress,
    ExactMismatch,
    Confirm,
    Back,
    StatusReady,
    StatusWorking,
    StatusDayResting,
    StatusDaySnoozed,
    StatusNightLocked,
    StatusNightGrace,
    OpenSettings,
    OpenHistory,
    Exit,
    SafeMode,
    MonitorMissing,
    InvalidSchedule,
    StartupError,
    ChallengeRequired,
    EmergencyHold,
    EmergencyExit
}

internal static class UiText
{
    internal const string DayStatementUk =
        "Я свідомо вирішую проігнорувати заплановану перерву і продовжити роботу. " +
        "Я розумію, що мої очі, спина, увага та нервова система вже потребують відновлення, " +
        "а регулярне ігнорування втоми погіршує мій фізичний і психологічний стан. " +
        "Я не перекладаю відповідальність на дедлайни, повідомлення, цікаву задачу чи інших людей: " +
        "це мій навмисний вибір. Я приймаю, що додатковий час може знизити якість моїх рішень, " +
        "посилити втому і зробити наступний день важчим. Я використовую це розблокування лише для " +
        "справді необхідного завершення, після чого зупинюся, відійду від екрана, розімнуся, " +
        "зроблю кілька ковтків води й дам собі повноцінно відпочити.";

    internal const string NightStatementUk =
        "Я свідомо вирішую скасувати нічне блокування і продовжити роботу після 20:00, " +
        "хоча цей час був заздалегідь відведений для завершення дня, відновлення та сну. " +
        "Я розумію, що робота вночі скорочує мій відпочинок, посилює втому, погіршує увагу, " +
        "настрій і здатність приймати якісні рішення завтра. Жоден дедлайн, повідомлення, " +
        "цікава задача чи бажання зробити ще трохи не змушує мене продовжувати: це мій свідомий вибір. " +
        "Я приймаю відповідальність за його наслідки і не буду називати їх випадковістю. " +
        "Я використовую цей доступ лише для справді необхідної справи, встановлю чіткий час завершення, " +
        "після цього закрию робочі програми, відійду від екрана і дам собі можливість нормально відпочити.";

    internal const string DayStatementEn =
        "I am consciously choosing to ignore the scheduled break and continue working. " +
        "I understand that my eyes, back, attention, and nervous system already need recovery, " +
        "and that repeatedly ignoring fatigue harms my physical and psychological well-being. " +
        "I am not shifting responsibility to deadlines, messages, an interesting task, or other people: " +
        "this is my deliberate choice. I accept that extra time may reduce the quality of my decisions, " +
        "increase fatigue, and make tomorrow harder. I will use this unlock only to finish something " +
        "genuinely necessary, then I will stop, step away from the screen, stretch, drink some water, " +
        "and give myself a proper rest.";

    internal const string NightStatementEn =
        "I am consciously choosing to cancel the night block and continue working after 8:00 PM, " +
        "even though this time was set aside in advance for ending the day, recovering, and sleeping. " +
        "I understand that working at night shortens my rest, increases fatigue, and worsens my attention, " +
        "mood, and ability to make good decisions tomorrow. No deadline, message, interesting task, " +
        "or desire to do a little more is forcing me to continue: this is my deliberate choice. " +
        "I accept responsibility for its consequences and will not call them accidental. " +
        "I will use this access only for something genuinely necessary, set a clear stopping time, " +
        "then close my work applications, step away from the screen, and give myself the chance to rest properly.";

    private static readonly IReadOnlyDictionary<TextKey, string> Ukrainian =
        new Dictionary<TextKey, string>
        {
            [TextKey.SettingsTitle] = "Work Rest Guard — Налаштування",
            [TextKey.SettingsTab] = "Налаштування",
            [TextKey.HistoryTab] = "Історія 30 днів",
            [TextKey.SetupHeading] = "Зупиняй роботу до того, як виснаження зробить це за тебе",
            [TextKey.SetupDescription] = "Guard перекриває лише вибраний робочий монітор. Аварійний вихід: утримуй Ctrl+Alt+Shift+F11 п’ять секунд.",
            [TextKey.ProtectedMonitor] = "Робочий монітор",
            [TextKey.PreviewMonitor] = "Показати preview",
            [TextKey.WorkMinutes] = "Робота до перерви, хв",
            [TextKey.BreakMinutes] = "Тривалість перерви, хв",
            [TextKey.NightMode] = "Щоденний нічний режим",
            [TextKey.NightStart] = "Блокувати з",
            [TextKey.NightEnd] = "Відкрити о",
            [TextKey.NightDays] = "Активні дні",
            [TextKey.Language] = "Мова",
            [TextKey.StartWithWindows] = "Запускати разом із Windows",
            [TextKey.GuardEnabled] = "Захист увімкнено",
            [TextKey.StartBreakNow] = "Почати перерву зараз",
            [TextKey.SaveApply] = "Зберегти й застосувати",
            [TextKey.Cancel] = "Скасувати",
            [TextKey.HistoryDescription] = "Локальні події за 30 календарних днів. Вміст введеного тексту не зберігається.",
            [TextKey.HistoryEmpty] = "Для цього дня подій немає.",
            [TextKey.Date] = "Дата",
            [TextKey.DayCompleted] = "День: відпочинок",
            [TextKey.DaySnoozed] = "День: відстрочки",
            [TextKey.DaySkipped] = "День: пропуски",
            [TextKey.NightCompleted] = "Ніч: завершено",
            [TextKey.NightGrace] = "Ніч: +10 хв",
            [TextKey.NightOverridden] = "Ніч: override",
            [TextKey.NightInterrupted] = "Ніч: перервано",
            [TextKey.EventTime] = "Час",
            [TextKey.EventType] = "Подія",
            [TextKey.DayTitle] = "Час відпочити",
            [TextKey.DayDetail] = "Основний екран закрито. Відійди, розімни плечі та подивися вдалину.",
            [TextKey.NightTitle] = "Робочий день завершено",
            [TextKey.NightDetail] = "Робочий монітор залишиться закритим і автоматично відкриється о {0}.",
            [TextKey.Snooze5] = "Доробити 5 хв",
            [TextKey.Snooze15] = "Доробити 15 хв",
            [TextKey.Snooze30] = "Доробити 30 хв",
            [TextKey.NightGrace10] = "Мені потрібно ще 10 хвилин",
            [TextKey.SkipDay] = "Свідомо пропустити цю перерву",
            [TextKey.OverrideNight] = "Свідомо працювати цієї ночі",
            [TextKey.ExactInstruction] = "Набери текст вручну, символ у символ. Вставлення вимкнено.",
            [TextKey.ExactProgress] = "Точно збігається: {0} із {1} символів",
            [TextKey.ExactMismatch] = "Перша невідповідність у символі {0}. Виправ її, щоб продовжити.",
            [TextKey.Confirm] = "Підтвердити свідоме рішення",
            [TextKey.Back] = "Назад",
            [TextKey.StatusReady] = "Готовий — таймер почнеться з першого вводу",
            [TextKey.StatusWorking] = "Робочий цикл до {0}",
            [TextKey.StatusDayResting] = "Перерва до {0}",
            [TextKey.StatusDaySnoozed] = "Відстрочка до {0}",
            [TextKey.StatusNightLocked] = "Нічний блок до {0}",
            [TextKey.StatusNightGrace] = "Нічні 10 хв до {0}",
            [TextKey.OpenSettings] = "Налаштування…",
            [TextKey.OpenHistory] = "Історія 30 днів…",
            [TextKey.Exit] = "Вийти…",
            [TextKey.SafeMode] = "Безпечний режим: overlay вимкнено",
            [TextKey.MonitorMissing] = "Вибраний монітор не знайдено. Нічого не перекривається.",
            [TextKey.InvalidSchedule] = "Час початку й завершення нічного режиму має відрізнятися.",
            [TextKey.StartupError] = "Не вдалося змінити автозапуск. Інші налаштування не змінено.",
            [TextKey.ChallengeRequired] = "Щоб послабити захист, спочатку введи підтвердження.",
            [TextKey.EmergencyHold] = "Аварійний вихід через {0:0.0} с",
            [TextKey.EmergencyExit] = "Аварійний вихід"
        };

    private static readonly IReadOnlyDictionary<TextKey, string> English =
        new Dictionary<TextKey, string>
        {
            [TextKey.SettingsTitle] = "Work Rest Guard — Settings",
            [TextKey.SettingsTab] = "Settings",
            [TextKey.HistoryTab] = "30-day history",
            [TextKey.SetupHeading] = "Stop work before exhaustion stops it for you",
            [TextKey.SetupDescription] = "Guard covers only the selected work monitor. Emergency exit: hold Ctrl+Alt+Shift+F11 for five seconds.",
            [TextKey.ProtectedMonitor] = "Work monitor",
            [TextKey.PreviewMonitor] = "Preview monitor",
            [TextKey.WorkMinutes] = "Work before break, minutes",
            [TextKey.BreakMinutes] = "Break duration, minutes",
            [TextKey.NightMode] = "Daily night mode",
            [TextKey.NightStart] = "Block from",
            [TextKey.NightEnd] = "Open at",
            [TextKey.NightDays] = "Active days",
            [TextKey.Language] = "Language",
            [TextKey.StartWithWindows] = "Start with Windows",
            [TextKey.GuardEnabled] = "Guard enabled",
            [TextKey.StartBreakNow] = "Start a break now",
            [TextKey.SaveApply] = "Save and apply",
            [TextKey.Cancel] = "Cancel",
            [TextKey.HistoryDescription] = "Local events for 30 calendar days. Typed statement content is never stored.",
            [TextKey.HistoryEmpty] = "There are no events for this day.",
            [TextKey.Date] = "Date",
            [TextKey.DayCompleted] = "Day: completed",
            [TextKey.DaySnoozed] = "Day: snoozes",
            [TextKey.DaySkipped] = "Day: skipped",
            [TextKey.NightCompleted] = "Night: completed",
            [TextKey.NightGrace] = "Night: +10 min",
            [TextKey.NightOverridden] = "Night: override",
            [TextKey.NightInterrupted] = "Night: interrupted",
            [TextKey.EventTime] = "Time",
            [TextKey.EventType] = "Event",
            [TextKey.DayTitle] = "Time to rest",
            [TextKey.DayDetail] = "The work monitor is closed. Step away, move your shoulders, and look into the distance.",
            [TextKey.NightTitle] = "The workday is over",
            [TextKey.NightDetail] = "The work monitor will stay closed and open automatically at {0}.",
            [TextKey.Snooze5] = "Finish for 5 min",
            [TextKey.Snooze15] = "Finish for 15 min",
            [TextKey.Snooze30] = "Finish for 30 min",
            [TextKey.NightGrace10] = "I need 10 more minutes",
            [TextKey.SkipDay] = "Consciously skip this break",
            [TextKey.OverrideNight] = "Consciously work tonight",
            [TextKey.ExactInstruction] = "Type the statement manually, character for character. Pasting is disabled.",
            [TextKey.ExactProgress] = "Exact match: {0} of {1} characters",
            [TextKey.ExactMismatch] = "The first mismatch is at character {0}. Correct it to continue.",
            [TextKey.Confirm] = "Confirm this conscious decision",
            [TextKey.Back] = "Back",
            [TextKey.StatusReady] = "Ready — the timer starts with the first input",
            [TextKey.StatusWorking] = "Work cycle until {0}",
            [TextKey.StatusDayResting] = "Break until {0}",
            [TextKey.StatusDaySnoozed] = "Snoozed until {0}",
            [TextKey.StatusNightLocked] = "Night block until {0}",
            [TextKey.StatusNightGrace] = "Night grace until {0}",
            [TextKey.OpenSettings] = "Settings…",
            [TextKey.OpenHistory] = "30-day history…",
            [TextKey.Exit] = "Exit…",
            [TextKey.SafeMode] = "Safe mode: overlays are disabled",
            [TextKey.MonitorMissing] = "The selected monitor is missing. Nothing is covered.",
            [TextKey.InvalidSchedule] = "Night start and end times must be different.",
            [TextKey.StartupError] = "Windows startup could not be changed. No other setting was changed.",
            [TextKey.ChallengeRequired] = "Type the confirmation before weakening protection.",
            [TextKey.EmergencyHold] = "Emergency exit in {0:0.0} s",
            [TextKey.EmergencyExit] = "Emergency exit"
        };

    internal static string Get(string languageCode, TextKey key) =>
        IsEnglish(languageCode) ? English[key] : Ukrainian[key];

    internal static string DayStatement(string languageCode) =>
        IsEnglish(languageCode) ? DayStatementEn : DayStatementUk;

    internal static string NightStatement(string languageCode) =>
        IsEnglish(languageCode) ? NightStatementEn : NightStatementUk;

    internal static CultureInfo Culture(string languageCode) =>
        CultureInfo.GetCultureInfo(IsEnglish(languageCode) ? "en-US" : "uk-UA");

    internal static bool IsEnglish(string languageCode) =>
        string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase);
}
