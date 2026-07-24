namespace WorkRestGuard;

public static class ExactTypingMatcher
{
    public static int? FindFirstMismatch(string expected, string actual)
    {
        var comparedLength = Math.Min(expected.Length, actual.Length);
        for (var index = 0; index < comparedLength; index++)
        {
            if (expected[index] != actual[index])
            {
                return index;
            }
        }

        return actual.Length > expected.Length ? expected.Length : null;
    }

    public static bool IsExactMatch(string expected, string actual) =>
        string.Equals(expected, actual, StringComparison.Ordinal);
}
