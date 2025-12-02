using System.Windows;
using System.Windows.Input;

namespace MarshallApp;

public partial class InputBoxWindow : Window
{
    private readonly Action<string> _okCallback;
    public static InputBoxWindow? Instance;
    
    public InputBoxWindow(string title, string desc, Action<string> okCallback, string defaultValue = "")
    {
        InitializeComponent();

        Instance = this;
        _okCallback = okCallback;
        Title.Text =  title;
        DescTextField.Text = desc;
    }
    
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _okCallback?.Invoke(Input.Text);
        CloseWind();
    }

    private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseWind();
    }

    private void CloseWind()
    {
        this.Close();
        Instance = null;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        CloseWind();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Input.Focus();
        Input.SelectAll();
    }
}