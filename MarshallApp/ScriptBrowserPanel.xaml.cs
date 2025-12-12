using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MarshallApp.Models;
using MarshallApp.Services;
using System.Text;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace MarshallApp;

public partial class ScriptBrowserPanel
{
    private BlockConfig? _selectedBlock;
    private Process? _runProcess;
    private Process? _cmdProcess;
    
    public ScriptBrowserPanel()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        InitializeComponent();
        Loaded += ScriptBrowserPanel_Loaded;
        
        InitializeCmd();
    }
    
    private void InitializeCmd()
    {
        _cmdProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                StandardErrorEncoding = Encoding.GetEncoding(866)
            }
        };
    
        _cmdProcess.Start();
    
        _cmdProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Dispatcher.Invoke(() =>
            {
                CmdHost.AppendText(e.Data + "\r\n");
                CmdHost.SelectionStart = CmdHost.Text.Length;
                CmdHost.ScrollToCaret();
            });
        };
    
        _cmdProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Dispatcher.Invoke(() =>
            {
                CmdHost.AppendText(e.Data + "\r\n");
                CmdHost.SelectionStart = CmdHost.Text.Length;
                CmdHost.ScrollToCaret();
            });
        };
    
        _cmdProcess.BeginOutputReadLine();
        _cmdProcess.BeginErrorReadLine();
    
        Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(100);
            CmdHost.AppendText("Windows Command Prompt (embedded)\r\n");
            CmdHost.AppendText(Environment.CurrentDirectory + ">");
        });
    
        CmdHost.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
    
            e.Handled = true;
            e.SuppressKeyPress = true;
    
            //var fullText = CmdHost.Text;
            var lines = CmdHost.Lines;
            var lastLine = lines.Length > 0 ? lines[^1] : "";
            
            if (lastLine.EndsWith($">"))
                lastLine = "";
    
            var command = lastLine.Trim();
    
            if (!string.IsNullOrEmpty(command))
            {
                _cmdProcess.StandardInput.WriteLine(command);
            }
    
            CmdHost.AppendText("\r\n" + Environment.CurrentDirectory + ">");
            CmdHost.SelectionStart = CmdHost.Text.Length;
            CmdHost.ScrollToCaret();
        };
    }

    private void ScriptBrowserPanel_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProjects(ConfigManager.RecentProjects);
    }
    
    public void Update() => LoadProjects(ConfigManager.RecentProjects);
    
    private static string GetShortPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "(no file)";

        var fileName = Path.GetFileName(fullPath);
        var root = Path.GetPathRoot(fullPath);

        Debug.Assert(root != null, nameof(root) + " != null");
        return fullPath.Length <= root.Length + fileName.Length + 4 ? fullPath : $"{root}...\\{fileName}";
    }
    
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
            
            var style = ((Style?)Application.Current.FindResource("ModernTreeViewItemStyle"))! ?? throw new InvalidOperationException();
            var root = new TreeViewItem
            {
                Header = project.ProjectName.ToUpper(),
                Tag = project,
                Style = style
            };

            foreach (var block in project.Blocks)
            {
                var blockNode = new TreeViewItem
                {
                    Header = Path.GetFileName(block.PythonFilePath ?? "(no file)"),
                    Tag = block,
                    Style = style
                };

                if (block.PythonFilePath != null)
                    blockNode.Items.Add(new TreeViewItem
                        { Header = $"Python File: {GetShortPath(block.PythonFilePath)}", Style = style, ToolTip = block.PythonFilePath });
                blockNode.Items.Add(new TreeViewItem { Header = $"Looping: {block.IsLooping}", Style = style });
                blockNode.Items.Add(new TreeViewItem { Header = $"Interval: {block.LoopIntervalSeconds}s", Style = style });
                blockNode.Items.Add(new TreeViewItem { Header = $"Font Size: {block.OutputFontSize}", Style = style });
                blockNode.Items.Add(new TreeViewItem { Header = $"Position: ({block.X}, {block.Y})", Style = style });
                blockNode.Items.Add(new TreeViewItem { Header = $"Size: {block.WidthUnits} × {block.HeightUnits}", Style = style });

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

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = MainWindow.Instance;
        
        var window = new ProjectOpenWindow(ConfigManager.RecentProjects)
        {
            Owner = mainWindow
        };

        if (window.ShowDialog() != true) return;
        if (mainWindow == null) return;
        mainWindow.CurrentProject = window.ResultProject;
        mainWindow.SetProjectName(mainWindow.CurrentProject?.ProjectName!);
        if (mainWindow.CurrentProject != null) ConfigManager.LoadBlocksFromProject(mainWindow.CurrentProject);
        
        mainWindow.OpenUiElement(mainWindow.MainCanvas, mainWindow.MainCanvasShowButton);
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = MainWindow.Instance;
        
        var window = new ProjectCreationWindow
        {
            Owner = mainWindow
        };
        
        if (window.ShowDialog() != true) return;
        if (mainWindow == null) return;
        mainWindow.CurrentProject = window.ResultProject;
        mainWindow.SetProjectName(mainWindow.CurrentProject?.ProjectName!);
        ConfigManager.SaveAppConfig();
        mainWindow.ClearBlocks();
        
        mainWindow.OpenUiElement(mainWindow.MainCanvas, mainWindow.MainCanvasShowButton);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Marshall Project (*.mpr)|*.mpr",
            Title = "Open Marshall Project"
        };
        if (MainWindow.Instance != null) dialog.InitialDirectory = MainWindow.Instance._defaultMarshallProjectsPath;
        if (dialog.ShowDialog()  != DialogResult.OK) return;
        
        var mainWindow = MainWindow.Instance;
        if (mainWindow == null) return;
        
        ConfigManager.SaveAppConfig();
        var project = ProjectManager.LoadProject(dialog.FileName);
        ConfigManager.AddRecentProject(dialog.FileName);
        $"Project {project.ProjectName} has opened.".Log();
        mainWindow.CurrentProject = project;
        mainWindow.SetProjectName(mainWindow.CurrentProject?.ProjectName!);
        if (mainWindow.CurrentProject != null) ConfigManager.LoadBlocksFromProject(mainWindow.CurrentProject);
        
        mainWindow.OpenUiElement(mainWindow.MainCanvas, mainWindow.MainCanvasShowButton);
    }

    private async void RunScript_Click(object? sender, RoutedEventArgs? e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Editor.Text)) return;

            StopScript_Click(null, null);

            var tempFile = Path.GetTempFileName() + ".py";
            await File.WriteAllTextAsync(tempFile, Editor.Text);
            
            
            _runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                    {
                    FileName = "python",
                    Arguments = $"-u \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    EnvironmentVariables =
                    {
                        ["PYTHONUNBUFFERED"] = "1",
                        ["PYTHONUTF8"] = "1"
                    }
                }
            };

            RunOutput.Text = "";
            _runProcess.OutputDataReceived += (_, a) => { if (a.Data != null) Dispatcher.Invoke(() => RunOutput.Text += a.Data + "\n"); };
            _runProcess.ErrorDataReceived += (_, a) => { if (a.Data != null) Dispatcher.Invoke(() => RunOutput.Text += "ERR: " + a.Data + "\n"); };

            _runProcess.Start();
            _runProcess.BeginOutputReadLine();
            _runProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            ex.Message.Log();
        }
    }

    private void StopScript_Click(object? sender, RoutedEventArgs? e)
    {
        _runProcess?.Kill();
        _runProcess?.Dispose();
        _runProcess = null;
    }
}