using System.Windows;

namespace MarshallApp;

public partial class LogViewer : Window
{
    public LogViewer(string scriptName, string output)
    {
        InitializeComponent();
        Title = $"Output — {scriptName}";
        Output.Text = output;
    }
}