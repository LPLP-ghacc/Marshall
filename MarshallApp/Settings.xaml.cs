using System.Windows;
using System.Windows.Input;

namespace MarshallApp;

public partial class Settings : Window
{
    public static Settings? Instanse;
    public Settings()
    {
        InitializeComponent();

        Instanse = this;
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