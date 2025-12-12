using System.Windows;
using System.Windows.Input;

namespace MarshallApp;

public partial class Settings
{
    public static Settings? Instance;
    public Settings()
    {
        InitializeComponent();

        Instance = this;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SelectScriptsFolder_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        throw new NotImplementedException();
    }
}