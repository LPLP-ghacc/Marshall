using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarshallApp.Models;

[AttributeUsage(AttributeTargets.Property)]
public class DisplayNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public class UserSettings : INotifyPropertyChanged
{
    [DisplayName("Run at Windows startup")]
    public bool RunAtWindowsStartup
    {
        get;
        init => Set(ref field, value);
    }
    
    [DisplayName("Enable logging")]
    public bool IsEnableLog
    {
        get;
        init => Set(ref field, value);
    }
    
    [DisplayName("Collapse into the tray when closing")]
    public bool MinimizeToTrayOnClose 
    {
        get;
        init => Set(ref field, value);
    }
    
    [DisplayName("Open the Python installation page")]
    public bool OpenPythonInstallationPage
    {
        get;
        init => Set(ref field, value);
    }

    [DisplayName("Enable forced GC.Collect")]
    public bool IsEnableGcCollect
    {
        get;
        init => Set(ref field, value);
    }

    [DisplayName("Automatic installation of modules")]
    public bool IsEnableAutoModuleInstall
    {
        get;
        init => Set(ref field, value);
    }

    [DisplayName("Basic block size\ndefault 100")]
    public double BaseBlockSize
    {
        get;
        init => Set(ref field, value);
    }

    [DisplayName("Font of output blocks\ndefault Consolas")]
    public string? FontFamily
    {
        get;
        init => Set(ref field, value);
    }
    
    //300
    [DisplayName("Block Memory Limit (MB)\ndefault 300")]
    public int MemoryLimitMb     
    {
        get;
        init => Set(ref field, value);
    }
    
    //10
    [DisplayName("Block CPU Limit\ndefault 10%")]
    public int CpuLimitPercent     
    {
        get;
        init => Set(ref field, value);
    }

    public static UserSettings Default =>
        new()
        {
            RunAtWindowsStartup = false,
            IsEnableLog = true,
            MinimizeToTrayOnClose = false,
            IsEnableGcCollect = true,
            IsEnableAutoModuleInstall = true,
            BaseBlockSize = 100,
            FontFamily = "Consolas",
            MemoryLimitMb = 300,
            CpuLimitPercent = 10
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