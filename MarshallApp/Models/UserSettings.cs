using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarshallApp.Models;

public class UserSettings : INotifyPropertyChanged
{
    public bool IsEnableLog
    {
        get;
        init => Set(ref field, value);
    }
    
    public bool OpenPythonInstallationPage
    {
        get;
        init => Set(ref field, value);
    }

    public bool IsEnableGcCollect
    {
        get;
        init => Set(ref field, value);
    }

    public bool IsEnableAutoModuleInstall
    {
        get;
        init => Set(ref field, value);
    }

    public double BaseBlockSize
    {
        get;
        init => Set(ref field, value);
    }

    public string? FontFamily
    {
        get;
        init => Set(ref field, value);
    }

    public static UserSettings Default =>
        new()
        {
            IsEnableLog = true,
            IsEnableGcCollect = true,
            IsEnableAutoModuleInstall = true,
            BaseBlockSize = 500,
            FontFamily = "Consolas"
        };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}