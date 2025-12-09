using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MarshallApp.Models;
using MarshallApp.Services;

namespace MarshallApp;

public partial class ScriptBrowserPanel : UserControl
{
    private BlockConfig? _selectedBlock;
    
    public ScriptBrowserPanel()
    {
        InitializeComponent();
        Loaded += ScriptBrowserPanel_Loaded;
    }

    private void ScriptBrowserPanel_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProjects(ConfigManager.RecentProjects);
    }
    
    public void Update() => LoadProjects(ConfigManager.RecentProjects);
    
    public void LoadProjects(List<string> recentProjects)
    {
        ProjectTree.Items.Clear();

        if (recentProjects.Count == 0)
        {
            "No recent projects".Log();
            return;
        }
        recentProjects.ForEach(path =>
        {
            path.Log();
            var project = ProjectManager.LoadProject(path);
            
            var root = new TreeViewItem
            {
                Header = project.ProjectName,
                Tag = project
            };

            foreach (var block in project.Blocks)
            {
                var blockNode = new TreeViewItem
                {
                    Header = Path.GetFileName(block.PythonFilePath ?? "(no file)"),
                    Tag = block
                };

                
                blockNode.Items.Add(new TreeViewItem { Header = $"Python File: {block.PythonFilePath}" });
                blockNode.Items.Add(new TreeViewItem { Header = $"Looping: {block.IsLooping}" });
                blockNode.Items.Add(new TreeViewItem { Header = $"Interval: {block.LoopIntervalSeconds}s" });
                blockNode.Items.Add(new TreeViewItem { Header = $"Font Size: {block.OutputFontSize}" });
                blockNode.Items.Add(new TreeViewItem { Header = $"Position: ({block.X}, {block.Y})" });
                blockNode.Items.Add(new TreeViewItem { Header = $"Size: {block.WidthUnits} × {block.HeightUnits}" });

                root.Items.Add(blockNode);
            }

            ProjectTree.Items.Add(root);
            
            root.IsExpanded = true;
        });
    }


    private void ProjectTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var treeItem = ProjectTree.SelectedItem as TreeViewItem;
        if (treeItem?.Tag is not BlockConfig block)
        {
            ShowInspector(null);
            return;
        }

        ShowInspector(block);
    }


    private void ShowInspector(BlockConfig? block)
    {
        _selectedBlock = block;

        if (block == null || !File.Exists(block.PythonFilePath))
        {
            Editor.Text = "Select block to view code.";
            return;
        }

        Editor.Text = File.ReadAllText(block.PythonFilePath);
    }
    
    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedBlock?.PythonFilePath == null) return;

        var mw = MainWindow.Instance;

        mw!.CodeEditor.LoadFile(_selectedBlock.PythonFilePath);

        mw.OpenUiElement(mw.CodeEditor, mw.CodeEditorShowButton);
    }
}