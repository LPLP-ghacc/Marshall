using System.Windows;
using System.Windows.Input;

namespace MarshallApp;

public partial class LogViewer : Window
{
    public LogViewer(string scriptName, string output)
    {
        InitializeComponent();
        Log.Text = $"Output — {scriptName}";
        Output.Text = output;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();
}