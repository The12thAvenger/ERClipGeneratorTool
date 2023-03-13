using System.Collections.Generic;

namespace ERClipGeneratorTool.ViewModels.Interactions;

public class FilePathOptions
{
    public enum FilePathMode
    {
        Open,
        Save
    }

    public FilePathOptions(string prompt, FilePathMode mode, List<FileTypeFilter> filters)
    {
        Prompt = prompt;
        Mode = mode;
        Filters = filters;
    }

    public string Prompt { get; init; }
    public FilePathMode Mode { get; init; }

    public List<FileTypeFilter> Filters { get; init; }

    public record FileTypeFilter(string Name, List<string> Extensions);
}