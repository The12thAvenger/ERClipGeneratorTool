using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ERClipGeneratorTool.ViewModels;

namespace ERClipGeneratorTool
{
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            string name = data!.GetType().FullName!.Replace("ViewModel", "View");
            Type? type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}