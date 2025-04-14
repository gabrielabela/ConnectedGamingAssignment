using UnityEngine;
using System.Collections.Generic;

public static class SessionRegistry
{
    // Stores all currently active session codes (host-created)
    public static HashSet<string> ActiveSessions = new HashSet<string>();

    // Generates a new random alphanumeric session code of specified length (default: 4 characters)
    public static string GenerateSessionCode(int length = 4)
    {
        // Characters to choose from (excluding confusing ones like I, O, 0, 1)
        const string chars = "ABCEFGHJKMNPQRTUVWXYZ456789";

        string result = ""; // Resulting session code

        // Loop to generate each character of the session code
        for (int i = 0; i < length; i++)
        {
            // Select a random character from the allowed set
            char c = chars[UnityEngine.Random.Range(0, chars.Length)];
            result += c; // Append it to the result string
        }

        return result; // Return the generated code
    }

    // Checks if a given session code exists in the ActiveSessions set
    public static bool IsValidSession(string code)
    {
        return ActiveSessions.Contains(code); // True if code exists, false otherwise
    }
}
