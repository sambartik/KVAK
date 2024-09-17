namespace KVAK.Core;

public static class Utils
{
    /// <summary>
    /// Lexicographically compares both strings
    /// </summary>
    /// <param name="s1">First string</param>
    /// <param name="s2">Second string</param>
    /// <returns>True if s2 string is lexicographically after s1</returns>
    public static bool LexicographicComparator(string s1, string s2)
    {
        return String.Compare(s2, s1, StringComparison.Ordinal) > 0;
    }
}