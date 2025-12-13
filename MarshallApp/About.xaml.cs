using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace MarshallApp;

public partial class About
{
    public About()
    {
        InitializeComponent();
    }
    
    private void OpenGitHub(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/LPLP-ghacc/Marshall#",
            UseShellExecute = true
        });
    }
}