using UnityEngine;
using System.Collections.Generic;

public static class SessionRegistry
{
    public static HashSet<string> ActiveSessions = new HashSet<string>();

    public static string GenerateSessionCode(int length = 4)
    {
        const string chars = "ABCEFGHJKMNPQRTUVWXYZ456789"; // removed confusing characters like I/1/O/0
        string result = "";

        for (int i = 0; i < length; i++)
        {
            char c = chars[UnityEngine.Random.Range(0, chars.Length)];
            result += c;
        }

        return result;
    }

    public static bool IsValidSession(string code)
    {
        return ActiveSessions.Contains(code);
    }
}
