using System.Windows;
using System.Windows.Input;

namespace MarshallApp;

public partial class NotificationWindow : Window
{
    private Action OnOkButtonCLick { get; set; }
    private Action OnCancelButtonCLick { get; set; }

    public NotificationWindow(string title, string message, Action ok, Action cancel)
    {
        InitializeComponent();
        Title.Text = title;
        DescTextField.Text = message;
        OnOkButtonCLick = ok;
        OnCancelButtonCLick = cancel;
    }

    private void TopBar_MouseDown(object sender, MouseButtonEventArgs e) => this.DragMove();

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        OnOkButtonCLick.Invoke();
        this.Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        OnCancelButtonCLick.Invoke();
        this.Close();
    }
}