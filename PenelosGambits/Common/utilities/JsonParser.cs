using System;
using System.Collections.Generic;

public static class JsonParser
{
    public static string GetString(string json, string key)
    {
        string search = "\"" + key + "\"";
        int keyIndex = json.IndexOf(search);
        if (keyIndex < 0) return null;

        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return null;

        int afterColon = colonIndex + 1;
        while (afterColon < json.Length && json[afterColon] == ' ')
        {
            afterColon++;
        }

        if (afterColon >= json.Length) return null;

        if (json.Length >= afterColon + 4 && json.Substring(afterColon, 4) == "null")
        {
            return null;
        }

        if (json[afterColon] == '"')
        {
            int start = afterColon + 1;
            int end = start;
            while (end < json.Length)
            {
                if (json[end] == '\\')
                {
                    end += 2;
                    continue;
                }
                if (json[end] == '"')
                {
                    break;
                }
                end++;
            }
            return UnescapeJson(json.Substring(start, end - start));
        }

        return GetRawValue(json, afterColon);
    }

    public static int GetInt(string json, string key, int defaultValue)
    {
        string value = GetRawValue(json, key);
        if (value == null) return defaultValue;

        int result;
        if (int.TryParse(value.Trim(), out result))
        {
            return result;
        }
        return defaultValue;
    }

    public static bool GetBool(string json, string key, bool defaultValue)
    {
        string value = GetRawValue(json, key);
        if (value == null) return defaultValue;

        string trimmed = value.Trim().ToLower();
        if (trimmed == "true") return true;
        if (trimmed == "false") return false;
        return defaultValue;
    }

    public static long GetLong(string json, string key, long defaultValue)
    {
        string value = GetRawValue(json, key);
        if (value == null) return defaultValue;

        long result;
        if (long.TryParse(value.Trim(), out result))
        {
            return result;
        }
        return defaultValue;
    }

    private static string GetRawValue(string json, string key)
    {
        string search = "\"" + key + "\"";
        int keyIndex = json.IndexOf(search);
        if (keyIndex < 0) return null;

        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return null;

        return GetRawValue(json, colonIndex + 1);
    }

    private static string GetRawValue(string json, int startIndex)
    {
        int afterColon = startIndex;
        while (afterColon < json.Length && json[afterColon] == ' ')
        {
            afterColon++;
        }

        if (afterColon >= json.Length) return null;

        char first = json[afterColon];

        if (first == '{' || first == '[')
        {
            return ExtractBracketedValue(json, afterColon, first);
        }

        int end = afterColon;
        while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
        {
            end++;
        }

        return json.Substring(afterColon, end - afterColon).Trim();
    }

    private static string ExtractBracketedValue(string json, int start, char openBracket)
    {
        char closeBracket = openBracket == '{' ? '}' : ']';
        int depth = 0;
        bool inString = false;

        for (int i = start; i < json.Length; i++)
        {
            if (json[i] == '\\' && inString)
            {
                i++;
                continue;
            }

            if (json[i] == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (json[i] == openBracket) depth++;
                if (json[i] == closeBracket) depth--;

                if (depth == 0)
                {
                    return json.Substring(start, i - start + 1);
                }
            }
        }

        return json.Substring(start);
    }

    public static string GetObject(string json, string key)
    {
        string search = "\"" + key + "\"";
        int keyIndex = json.IndexOf(search);
        if (keyIndex < 0) return null;

        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return null;

        int afterColon = colonIndex + 1;
        while (afterColon < json.Length && json[afterColon] == ' ')
        {
            afterColon++;
        }

        if (afterColon >= json.Length) return null;

        if (json[afterColon] == '{')
        {
            return ExtractBracketedValue(json, afterColon, '{');
        }

        if (json.Length >= afterColon + 4 && json.Substring(afterColon, 4) == "null")
        {
            return null;
        }

        return null;
    }

    public static List<string> GetArray(string json, string key)
    {
        string search = "\"" + key + "\"";
        int keyIndex = json.IndexOf(search);
        if (keyIndex < 0) return new List<string>();

        int colonIndex = json.IndexOf(':', keyIndex + search.Length);
        if (colonIndex < 0) return new List<string>();

        int afterColon = colonIndex + 1;
        while (afterColon < json.Length && json[afterColon] == ' ')
        {
            afterColon++;
        }

        if (afterColon >= json.Length || json[afterColon] != '[')
        {
            return new List<string>();
        }

        string arrayContent = ExtractBracketedValue(json, afterColon, '[');
        return SplitArrayElements(arrayContent);
    }

    private static List<string> SplitArrayElements(string arrayJson)
    {
        var result = new List<string>();

        if (arrayJson.Length < 2) return result;

        string inner = arrayJson.Substring(1, arrayJson.Length - 2).Trim();
        if (string.IsNullOrEmpty(inner)) return result;

        int depth = 0;
        bool inString = false;
        int start = 0;

        for (int i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '\\' && inString)
            {
                i++;
                continue;
            }

            if (inner[i] == '"')
            {
                inString = !inString;
                continue;
            }

            if (!inString)
            {
                if (inner[i] == '{' || inner[i] == '[') depth++;
                if (inner[i] == '}' || inner[i] == ']') depth--;

                if (inner[i] == ',' && depth == 0)
                {
                    result.Add(inner.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
        }

        string last = inner.Substring(start).Trim();
        if (!string.IsNullOrEmpty(last))
        {
            result.Add(last);
        }

        return result;
    }

    private static string UnescapeJson(string value)
    {
        if (value == null) return null;
        return value
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
    }
}
