using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MarshallApp.Models;
using MarshallApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace MarshallApp;

public partial class ProjectCreationWindow : Window
{
    public Project? ResultProject { get; private set; }
    
    public ProjectCreationWindow()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            LocationBox.Text = dialog.SelectedPath;
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = ProjectNameBox.Text.Trim();
        var baseFolder = LocationBox.Text.Trim();
        
        if (!string.IsNullOrWhiteSpace(ProjectNameBox.Text) &&
            !string.IsNullOrWhiteSpace(LocationBox.Text))
        {
            FinalPathLabel.Text = 
                System.IO.Path.Combine(LocationBox.Text, ProjectNameBox.Text);
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(baseFolder))
        {
            MessageBox.Show("Fill all fields.", "Error");
            return;
        }

        var projectFolder = System.IO.Path.Combine(baseFolder, name);

        ResultProject = ProjectManager.CreateNewProject(projectFolder, name);
        var file = Path.Combine(projectFolder, name + ProjectManager.ProjectExtension);

        ConfigManager.AddRecentProject(file);
        
        this.DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void Close_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();
}
