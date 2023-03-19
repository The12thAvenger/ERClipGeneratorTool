using System.Collections.Generic;
using System.Linq;

namespace ERClipGeneratorTool.Util;

public static class DupeExtensions
{
    public static List<int> GetTaeIdsFromString(string idsString)
    {
        List<string> taeIdStrings = idsString.Split(",").Select(i => i.Trim()).ToList();
        if (taeIdStrings.Count == 0) taeIdStrings = new List<string> { idsString };
        var taeIds = new List<int>();
        foreach (string idString in taeIdStrings)
        {
            if (idString.Contains("-"))
            {
                int startTaeId = int.TryParse(idString.Split("-")[0], out int st) ? st : -1;
                if (startTaeId == -1) return new List<int>();
                int endTaeId = int.TryParse(idString.Split("-")[1], out int et) ? et : -1;
                if (endTaeId == -1) return new List<int>();
                for (int i = startTaeId; i <= endTaeId; ++i) taeIds.Add(i);
            }
            else
            {
                int id = int.TryParse(idString, out int i) ? i : -1;
                if (id == -1) return new List<int>();
                taeIds.Add(id);
            }
        }
        return taeIds.Distinct().ToList();
    }
}