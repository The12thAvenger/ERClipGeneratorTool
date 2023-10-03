using System;
using System.Collections.Generic;

namespace ERClipGeneratorTool.Util;

public class FilteredStringComparer : IComparer<string>
{
    private readonly string _filter;

    public FilteredStringComparer(string filter)
    {
        _filter = filter;
    }

    public int Compare(string? x, string? y)
    {
        if (x is null) return 1;
        if (y is null) return -1;
        if (x.StartsWith(_filter) && !y.StartsWith(_filter)) return -1;
        if (!x.StartsWith(_filter) && y.StartsWith(_filter)) return 1;
        return StringComparer.InvariantCultureIgnoreCase.Compare(x, y);
    }
}