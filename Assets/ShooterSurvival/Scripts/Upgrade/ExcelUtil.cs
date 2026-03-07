using System;
using System.Collections.Generic;

public static class ExcelUtil
{
    public static string NormalizeKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\u00A0", "") // NBSP
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
    }

    public static int ToInt(object v)
    {
        if (v == null) return 0;
        if (v is double d) return (int)d;

        var s = v.ToString().Trim();
        if (int.TryParse(s, out var n)) return n;
        if (double.TryParse(s, out var dd)) return (int)dd;
        return 0;
    }

    public static float ToFloat(object v)
    {
        if (v == null) return 0f;
        if (v is double d) return (float)d;

        var s = v.ToString().Trim();
        if (float.TryParse(s, out var n)) return n;
        if (double.TryParse(s, out var dd)) return (float)dd;
        return 0f;
    }

    public static int GetIdx(Dictionary<string, int> map, string header)
    {
        if (map.TryGetValue(header, out var idx)) return idx;

        var norm = NormalizeKey(header);
        if (map.TryGetValue(norm, out idx)) return idx;

        throw new Exception($"엑셀 헤더에 '{header}' 컬럼이 없어요. (철자/공백/탭/병합셀/빈 행 확인)");
    }

    public static int GetIdxOptional(Dictionary<string, int> map, string header)
    {
        if (map.TryGetValue(header, out var idx)) return idx;

        var norm = NormalizeKey(header);
        return map.TryGetValue(norm, out idx) ? idx : -1;
    }
}
