using System;
using System.Collections.Generic;

namespace NOVR.Patches.HUD;

internal static class ExactCallReplacement
{
    public static bool TryReplace<T>(IReadOnlyList<T> source, Func<T, bool> matches,
        Func<T, T> replace, int expectedCount, out IReadOnlyList<T> result)
    {
        result = source;
        int count = 0;
        for (int i = 0; i < source.Count; i++) if (matches(source[i])) count++;
        if (count != expectedCount) return false;
        var patched = new List<T>(source.Count);
        for (int i = 0; i < source.Count; i++)
            patched.Add(matches(source[i]) ? replace(source[i]) : source[i]);
        result = patched;
        return true;
    }
}
