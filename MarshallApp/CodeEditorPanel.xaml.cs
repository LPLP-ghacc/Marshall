using System.IO;
using System.Windows;

namespace MarshallApp;

public partial class CodeEditorPanel
{
    private string? _currentFilePath;

    public CodeEditorPanel()
    {
        InitializeComponent();
    }

    private void LoadScript(string filePath)
    {
        _currentFilePath = filePath;
        FileNameText.Text = Path.GetFileName(filePath);
        Editor.Text = File.ReadAllText(filePath);
    }

    public void NewScript()
    {
        _currentFilePath = null;
        FileNameText.Text = "new_script.py";
        Editor.Clear();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveScript(false);
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveScript(true);
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        if(ConfirmUnsavedChanges())
            NewScript();
    }

    private static string GetInitialScriptsDirectory()
    {
        return MainWindow.Instance.CurrentProject?.ProjectPath != null ? Path.Combine(MainWindow.Instance.CurrentProject.ProjectPath, "Scripts") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if(!ConfirmUnsavedChanges())
            return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            InitialDirectory = GetInitialScriptsDirectory(),
            Filter = "Python files (*.py)|*.py|All files (*.*)|*.*"
        };

        if(dlg.ShowDialog() == true)
            LoadScript(dlg.FileName);
    }

    private void SaveScript(bool saveAs)
    {
        var code = Editor.Text;

        if(_currentFilePath == null || saveAs)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = GetInitialScriptsDirectory(),
                Filter = "Python files (*.py)|*.py",
                FileName = FileNameText.Text
            };
            if(dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                FileNameText.Text = Path.GetFileName(_currentFilePath);
            }
            else return;
        }

        File.WriteAllText(_currentFilePath, code);
        MessageBox.Show($"Saved: {Path.GetFileName(_currentFilePath)}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    public void LoadFile(string path)
    {
        _currentFilePath = path;
        Dispatcher.Invoke(() =>
        {
            Editor.Text = File.ReadAllText(path);
            FileNameText.Text = Path.GetFileName(path);
        });
    }

    private bool ConfirmUnsavedChanges()
    {
        if (string.IsNullOrWhiteSpace(Editor.Text)) 
            return true;

        var result = MessageBox.Show(
            "Save changes before continuing?",
            "Confirm",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Cancel:
                return false;

            case MessageBoxResult.Yes:
                SaveScript(false);
                break;

            case MessageBoxResult.No:
            default:
                break;
        }

        return true;
    }
}
