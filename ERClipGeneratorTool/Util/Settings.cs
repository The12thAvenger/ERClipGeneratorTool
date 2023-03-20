using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ERClipGeneratorTool.ViewModels.Interactions;

namespace ERClipGeneratorTool.Util;

public class Settings
{
    private static readonly string SettingsPath;

    static Settings()
    {
        SettingsPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");
        if (!File.Exists(SettingsPath))
        {
            Current = new Settings();
            Current.Save();
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath))!;
        }
        catch (Exception)
        {
            Current = new Settings();
            Current.Save();
        }
    }

    private Settings() : this(new ObservableCollection<string>(), new ObservableCollection<FileSource>()) { }

    public Settings(ObservableCollection<string> ignoredAnimationNames, ObservableCollection<FileSource> recentFiles)
    {
        IgnoredAnimationNames = ignoredAnimationNames;
        RecentFiles = recentFiles;
    }

    public static Settings Current { get; }

    public ObservableCollection<string> IgnoredAnimationNames { get; }
    public ObservableCollection<FileSource> RecentFiles { get; }

    public void Save()
    {
        string text = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText(SettingsPath, text);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}