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
        dialog.InitialDirectory = App.DefaultMarshallProjectsPath;
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

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        try
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

            ResultProject = await ProjectManager.CreateNewProject(projectFolder, name);
            var file = Path.Combine(projectFolder, name + ProjectManager.ProjectExtension);
            await ConfigManager.AddRecentProjectAsync(file);
        
            this.DialogResult = true;
        }
        catch (Exception exception) { exception.Message.Log(); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void Close_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Marshall Project (*.mpr)|*.mpr",
                Title = "Open Marshall Project"
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        
            var project = await ProjectManager.LoadProjectAsync(dialog.FileName);
            await ConfigManager.AddRecentProjectAsync(dialog.FileName);
        
            $"Project {project.ProjectName} has opened.".Log();

            ResultProject = project;
        
            DialogResult = true;
        }
        catch (Exception exception) { exception.Message.Log(); }
    }
}
