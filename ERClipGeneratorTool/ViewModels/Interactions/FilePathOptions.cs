using System.Collections.Generic;

namespace ERClipGeneratorTool.ViewModels.Interactions;

public class FilePathOptions
{
    public enum FilePathMode
    {
        Open,
        Save
    }

    public FilePathOptions(string prompt, FilePathMode mode, List<Filter> filters)
    {
        Prompt = prompt;
        Mode = mode;
        Filters = filters;
    }

    public string Prompt { get; init; }
    public FilePathMode Mode { get; init; }

    public List<Filter> Filters { get; init; }

    public record Filter(string Name, List<string> Extensions);
}