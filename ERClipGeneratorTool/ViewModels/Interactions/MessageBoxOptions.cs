using System;

namespace ERClipGeneratorTool.ViewModels.Interactions;

public class MessageBoxOptions
{
    public enum MessageBoxMode
    {
        Ok,
        YesNo,
        OkCancel,
        OkAbort,
        YesNoCancel,
        YesNoAbort
    }

    [Flags]
    public enum MessageBoxResult
    {
        Ok = 0,
        Yes = 1,
        No = 2,
        Abort = No | Yes,
        Cancel = 4,
        None = Cancel | Yes
    }

    public MessageBoxOptions(string header, string message, MessageBoxMode mode)
    {
        Header = header;
        Message = message;
    }

    public string Header { get; init; }

    public string Message { get; init; }

    public MessageBoxMode Mode { get; init; }
}