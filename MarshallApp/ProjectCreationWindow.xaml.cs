using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MarshallApp.Models;
using MarshallApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace MarshallApp;

public partial class ProjectCreationWindow
{
    public Project? ResultProject { get; private set; }
    
    public ProjectCreationWindow()
    {
        InitializeComponent();

        ProjectNameBox.TextChanged += UpdateFinalPath;
        LocationBox.TextChanged += UpdateFinalPath;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            LocationBox.Text = dialog.SelectedPath;
        }
    }

    private void UpdateFinalPath(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var name = ProjectNameBox.Text.Trim();
            var baseFolder = LocationBox.Text.Trim();
        
            var projectFolder = Path.Combine(baseFolder, name);
            var file = Path.Combine(projectFolder, name + ProjectManager.ProjectExtension);
            FinalPathLabel.Text = file;
        });
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = ProjectNameBox.Text.Trim();
        var baseFolder = LocationBox.Text.Trim();
        
        if (!string.IsNullOrWhiteSpace(ProjectNameBox.Text) &&
            !string.IsNullOrWhiteSpace(LocationBox.Text))
        {
            FinalPathLabel.Text = 
                Path.Combine(LocationBox.Text, ProjectNameBox.Text);
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(baseFolder))
        {
            MessageBox.Show("Fill all fields.", "Error");
            return;
        }

        var projectFolder = Path.Combine(baseFolder, name);

        ResultProject = ProjectManager.CreateNewProject(projectFolder, name);
        var file = Path.Combine(projectFolder, name + ProjectManager.ProjectExtension);
        ConfigManager.AddRecentProject(file);
        
        this.DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void Close_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();
}
