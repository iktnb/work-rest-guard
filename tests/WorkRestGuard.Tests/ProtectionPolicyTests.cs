namespace WorkRestGuard.Tests;

public sealed class ProtectionPolicyTests
{
    [Fact]
    public void IncreasingWorkIntervalRequiresDayStatement()
    {
        var current = TestData.Config();
        var candidate = current.Clone();
        candidate.WorkMinutes = 120;

        Assert.Equal(
            ChallengeKind.Day,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }

    [Fact]
    public void StrongerDaySettingsNeedNoStatement()
    {
        var current = TestData.Config();
        var candidate = current.Clone();
        candidate.WorkMinutes = 60;
        candidate.BreakMinutes = 20;

        Assert.Equal(
            ChallengeKind.None,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }

    [Fact]
    public void DelayingNightStartRequiresNightStatement()
    {
        var current = TestData.Config();
        var candidate = current.Clone();
        candidate.NightStart = new TimeOnly(21, 0);

        Assert.Equal(
            ChallengeKind.Night,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }

    [Fact]
    public void RemovingOneNightRequiresNightStatement()
    {
        var current = TestData.Config();
        var candidate = current.Clone();
        candidate.NightDays.Remove(DayOfWeek.Saturday);

        Assert.Equal(
            ChallengeKind.Night,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }

    [Fact]
    public void ExtendingNightProtectionNeedsNoStatement()
    {
        var current = TestData.Config();
        current.NightDays = [DayOfWeek.Monday];
        var candidate = current.Clone();
        candidate.NightStart = new TimeOnly(19, 0);
        candidate.NightEnd = new TimeOnly(7, 0);
        candidate.NightDays.Add(DayOfWeek.Tuesday);

        Assert.Equal(
            ChallengeKind.None,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }

    [Fact]
    public void ChangingMonitorRequiresNightStatement()
    {
        var current = TestData.Config();
        var candidate = current.Clone();
        candidate.ProtectedMonitor.HardwareIdHash = "OTHER";
        candidate.ProtectedMonitor.DeviceId = "OTHER";

        Assert.Equal(
            ChallengeKind.Night,
            ProtectionPolicy.RequiredChallenge(current, candidate));
    }
}
