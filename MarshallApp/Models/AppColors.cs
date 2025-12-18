using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MarshallApp.Models;

public class AppColors : INotifyPropertyChanged
{
    private Color _primary = Color.FromArgb(255, 17, 17, 17);
    public Color Primary { get => _primary; set => Set(ref _primary, value); }

    private Color _panels = Color.FromArgb(255, 30, 30, 30);
    public Color Panels { get => _panels; set => Set(ref _panels, value); }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static AppColors Default => new()
    {
        Primary = Color.FromArgb(255, 17, 17, 17),
        Panels = Color.FromArgb(255, 30, 30, 30),
    };
}