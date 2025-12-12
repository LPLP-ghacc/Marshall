using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MarshallApp.Models;
using MarshallApp.Services;

namespace MarshallApp;

public partial class ProjectOpenWindow
{
    public Project? ResultProject { get; private set; }
    
    public ProjectOpenWindow(List<string> recentProjects)
    {
        InitializeComponent();

        InitRecentProjectsButtons(recentProjects);
    }
    
    private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    
    private void Close_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "Marshall Project (*.mpr)|*.mpr",
            Title = "Open Marshall Project"
        };

        if (dialog.ShowDialog()  != System.Windows.Forms.DialogResult.OK) return;
        OpenProject(dialog.FileName);
        
        DialogResult = true;
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (ProjectsList.SelectedItem is not string file) return;

        OpenProject(file);
        
        DialogResult = true;
    }

    private void OpenProject(string file)
    {
        ConfigManager.SaveAppConfig();
        ResultProject = ProjectManager.LoadProject(file);
        ConfigManager.AddRecentProject(file);
        
        MainWindow.Instance?.SetProjectName(ResultProject.ProjectName);
        $"Project {ResultProject.ProjectName} has opened.".Log();
    }
    
        private void InitRecentProjectsButtons(List<string> recentProjects)
    {
        recentProjects.ForEach((project) =>
        {
            var fileName = Path.GetFileNameWithoutExtension(project);
            
            var style = ((Style?)Application.Current.FindResource("FlatButtonStyle"))! ?? throw new InvalidOperationException();
            var tbStyle = ((Style?)Application.Current.FindResource("FlatTextBlockStyle"))! ?? throw new InvalidOperationException();
            
            var dp = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                
                Margin = new Thickness(5,5,5,5),
            };
            dp.Children.Add(new TextBlock()
            {
                Text = fileName,
                FontWeight = FontWeights.Bold,
                Style = tbStyle,
                HorizontalAlignment = HorizontalAlignment.Left
            });
            dp.Children.Add(new TextBlock()
            {
                Text = project,
                Style = tbStyle,
                HorizontalAlignment = HorizontalAlignment.Left
            });
            var selector = new Button
            {
                Content = dp,
                Style = style,
                ToolTip = project,
                Margin = new Thickness(5,5,5,5),
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 0, 0, 0)
            };
            
            selector.HorizontalContentAlignment = HorizontalAlignment.Left;
            
            selector.Loaded += (_, _) =>
            {
                if (selector.ContentTemplate?.FindName("PART_ContentPresenter", selector) is ContentPresenter cp)
                {
                    cp.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else if (VisualTreeHelper.GetChildrenCount(selector) > 0)
                {
                    if (VisualTreeHelper.GetChild(selector, 0) is ContentPresenter contentPresenter)
                        contentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
                }
            };

            selector.Click += (sender, _) =>
            {
                var button = sender as Button;
                if (button?.ToolTip is string file)
                {
                    OpenProject(file);
                }
                DialogResult = true;
            };
            
            ProjectsList.Items.Add(selector);
        });
    }
}