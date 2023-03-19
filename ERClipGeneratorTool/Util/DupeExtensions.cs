using System.Collections.Generic;
using System.Linq;

namespace ERClipGeneratorTool.Util;

public static class DupeExtensions
{
    public static List<int> GetTaeIdsFromString(string idsString)
    {
        List<string> taeIdStrings = idsString.Split(",").Select(i => i.Trim()).ToList();
        if (taeIdStrings.Count == 0) taeIdStrings = new List<string> { idsString };
        List<int> taeIds = new();
        foreach (string idString in taeIdStrings)
        {
            if (idString.Contains('-'))
            {
                if (!int.TryParse(idString.Split("-")[0], out int startTaeId)
                    || !int.TryParse(idString.Split("-")[1], out int endTaeId)) return new List<int>();
                for (int i = startTaeId; i <= endTaeId; ++i) taeIds.Add(i);
            }
            else
            {
                if (!int.TryParse(idString, out int id)) return new List<int>();
                taeIds.Add(id);
            }
        }
        return taeIds.Distinct().ToList();
    }
}