using MarshallApp.Models;
using MarshallApp.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MarshallApp.Controllers;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Drawing.Color;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace MarshallApp;

public partial class MainWindow : INotifyPropertyChanged
{
    private NotifyIcon? _trayIcon;
    public readonly List<BlockElement> Blocks = [];
    private WallpaperController? _wallControl;
    private readonly DispatcherTimer _wallpaperTimer = new();
    private readonly DispatcherTimer _loggerTimer = new();
    public LimitSettings LimitSettings { get; } = new(10, 300);
    public Project? CurrentProject { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public static MainWindow? Instance { get; private set; }

    private readonly List<UIElement> _mainFieldElements = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Instance = this;
        
        _mainFieldElements.Add(ScriptBrowser);
        _mainFieldElements.Add(MainCanvas);
        _mainFieldElements.Add(CodeEditor);
        OpenUiElement(MainCanvas, MainCanvasShowButton);
        
        MainCanvas.AllowDrop = true;
        MainCanvas.DragOver += MStackPanel_DragOver;
        MainCanvas.Drop += MStackPanel_Drop;

        WallpaperControlInit();

        Top.MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        };
        
        if (ConfigManager.AppConfig != null) LimitSettings = 
            new LimitSettings(ConfigManager.AppConfig.CpuLimitPercent, ConfigManager.AppConfig.MemoryLimitMb) ;
        
        Loaded += async (_, _) =>
        {
            await ConfigManager.LoadAllConfigs();

            ScriptBrowser.LoadProjects(ConfigManager.RecentProjects);
            ScriptBrowser.Update();
            
            if (CurrentProject != null)
                return;

            var window = new ProjectCreationWindow
            {
                Owner = this
            };

            if (window.ShowDialog() != true) Environment.Exit(-1);
            CurrentProject = window.ResultProject;
            SetProjectName(CurrentProject?.ProjectName!);
            if (CurrentProject != null) ConfigManager.LoadBlocksFromProject(CurrentProject);
        };
        
        NewScript();
        InitializeTray();
        InitLoggerTimer();

        // Welcome to the home of the mentally ill
        "Hello World!".Log();
    }

    public void OpenUiElement(UIElement? obj, object sender)
    {
        var brush = ((Brush?)Application.Current.FindResource("SidebarButtonActiveBackground"))! ?? throw new InvalidOperationException(); // Господи, прости
        
        foreach (var element in _mainFieldElements)
        {
            element.Visibility = element != obj ? Visibility.Collapsed : Visibility.Visible;
        }

        var buttons = new List<Button>()
        {
            CodeEditorShowButton,
            ScriptBrowserShowButton,
            MainCanvasShowButton
        };

        foreach (var button in buttons)
        {
            var border = VisualTreeHelper.GetParent(button) as Border;

            border?.Background = button != sender ? Brushes.Transparent : brush;
        }
    }
    
    private void InitializeTray()
    {
        _trayIcon = new NotifyIcon();
        _trayIcon.Icon = new Icon(Path.Combine(Environment.CurrentDirectory, "Resource/Icons/IconSmall.ico"));
        _trayIcon.Visible = true;

        _trayIcon.Text = "Marshall";
        _trayIcon.ContextMenuStrip = new ContextMenuStrip();

        _trayIcon.MouseUp += TrayIcon_MouseUp;
    }

    private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;

        var menu = _trayIcon?.ContextMenuStrip;
        Debug.Assert(menu != null, nameof(menu) + " != null");
        
        menu.Items.Clear();

        menu.Items.Add("Running scripts:").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());

        if (Blocks.Count == 0)
        {
            menu.Items.Add("(no scripts)").Enabled = false;
        }
        else
        {
            foreach (var block in Blocks)
            {
                var name = string.IsNullOrEmpty(block.FilePath)
                    ? "(unnamed)"
                    : Path.GetFileName(block.FilePath);

                var item = new ToolStripMenuItem(name);

                item.Click += (_, _) => ShowLogViewer(block);

                if (block.IsLooping)
                    item.ForeColor = Color.Green;
                else if (block.IsRunning)
                    item.ForeColor = Color.Blue;

                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new ToolStripSeparator());

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => 
        {
            ConfigManager.SaveAppConfig();
            Environment.Exit(0);
        };

        menu.Items.Add(exit);

        menu.Show(System.Windows.Forms.Cursor.Position);
    }
    
    public void ShowLogViewer(BlockElement block)
    {
        var name = block.FilePath != null
            ? Path.GetFileName(block.FilePath)
            : "(unnamed script)";

        var window = new LogViewer(
            name,
            block.OutputText.Text
        )
        {
            Owner = this
        };

        window.Show();
    }

    private void NewScript()
    {
        CodeEditor.NewScript();
    }

    public void RemoveBlockElement(BlockElement element)
    {
        Blocks.Remove(element);
        MainCanvas.Children.Remove(element);

        ConfigManager.SaveAppConfig();
    }
    
    #region Top Panel Menu
    
    private void MainCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            var elementUnderMouse = source.FindVisualAncestor<BlockElement>();
            
            if (elementUnderMouse != null)
            {
                return;
            }
        }
        
        if (MainCanvas.ContextMenu != null)
        {
            MainCanvas.ContextMenu.PlacementTarget = MainCanvas;
            MainCanvas.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            MainCanvas.ContextMenu.IsOpen = true;
        }
        e.Handled = true;
    }
    
    private void ProjectButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ProjectButton.ContextMenu == null) return;
        ProjectButton.ContextMenu.PlacementTarget = ProjectButton;
        ProjectButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        ProjectButton.ContextMenu.IsOpen = true;
        
        e.Handled = true;
    }
    
    private void AddBlock_Click(object sender, RoutedEventArgs e)
    {
        var block = new BlockElement(RemoveBlockElement, LimitSettings);
        AddBlockElement(block);
    }
    #endregion

    #region LeftPanel
    
    private void MainCanvasShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(MainCanvas, sender);

    private void ScriptBrowserShowButton_OnClick(object sender, RoutedEventArgs e)
    {
        ScriptBrowser.Update();
        OpenUiElement(ScriptBrowser, sender);
    } 

    private void CodeEditorShowButton_OnClick(object sender, RoutedEventArgs e)=> OpenUiElement(CodeEditor, sender);

    #endregion

    #region LoadSaving things
    
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        ConfigManager.SaveAppConfig();
    }

    #endregion

    #region Toolbar buttons

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            FullscreenEnter.Visibility = Visibility.Visible;
            FullscreenExit.Visibility = Visibility.Collapsed;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            MaxWidth = SystemParameters.WorkArea.Width;
            MaxHeight = SystemParameters.WorkArea.Height;
            WindowState = WindowState.Maximized;

            FullscreenEnter.Visibility = Visibility.Collapsed;
            FullscreenExit.Visibility = Visibility.Visible;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ConfigManager.SaveAppConfig();
        Environment.Exit(0);
    }
    
    #endregion

    #region DragDropBlockElement

    private static void MStackPanel_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(BlockElement)) ? DragDropEffects.Move : DragDropEffects.None;

        e.Handled = true;
    }

    private void MStackPanel_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(BlockElement))) return;

        var dragged = (BlockElement?)e.Data.GetData(typeof(BlockElement));

        var mousePos = e.GetPosition(MainCanvas);

        var insertIndex = GetInsertIndex(mousePos);

        if (dragged != null) MoveBlockElement(dragged, insertIndex);

        ConfigManager.SaveAppConfig();
    }
    
    private int GetInsertIndex(Point mousePos)
    {
        var bestIndex = 0;
        var bestDistance = double.MaxValue;

        for (var i = 0; i < MainCanvas.Children.Count; i++)
        {
            var child = MainCanvas.Children[i];
            var transform = child.TransformToAncestor(MainCanvas);
            var rect = transform.TransformBounds(new Rect(0, 0, child.RenderSize.Width, child.RenderSize.Height));

            var centerY = rect.Top + rect.Height / 2;
            var centerX = rect.Left + rect.Width / 2;

            var dx = mousePos.X - centerX;
            var dy = mousePos.Y - centerY;
            var dist = dx * dx + dy * dy;

            if (!(dist < bestDistance)) continue;
            bestDistance = dist;
            bestIndex = i;
        }

        return bestIndex;
    }
    
    private void MoveBlockElement(BlockElement element, int newIndex)
    {
        var oldIndex = MainCanvas.Children.IndexOf(element);
        if (oldIndex == -1) return;

        if (newIndex == oldIndex) return;

        MainCanvas.Children.RemoveAt(oldIndex);
        Blocks.RemoveAt(oldIndex);

        if (newIndex > MainCanvas.Children.Count)
            newIndex = MainCanvas.Children.Count;

        MainCanvas.Children.Insert(newIndex, element);
        Blocks.Insert(newIndex, element);
    }

    #endregion

    private void WallpaperControlInit()
    {
        var imagesSource = Path.Combine(Environment.CurrentDirectory + "/Resource/Background");
        _wallControl = new WallpaperController(RootImageBrush, imagesSource);
        _wallControl.Update();
        
        _wallpaperTimer.Interval = TimeSpan.FromSeconds(30);
        _wallpaperTimer.Tick += (_, _) =>
        {
            _wallControl.Update();
        };
        _wallpaperTimer.Start();
    }

    private void Minimize_OnClick_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    
    private void AddBlockElement(BlockElement element)
    {
        element.Width = 500;
        element.Height = 500;

        Canvas.SetLeft(element, GridUtils.Snap(20));
        Canvas.SetTop(element, GridUtils.Snap(20));

        MainCanvas.Children.Add(element);

        Blocks.Add(element);
    }

    private void NewProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new ProjectCreationWindow
        {
            Owner = this
        };

        if (window.ShowDialog() != true) return;
        CurrentProject = window.ResultProject;
        SetProjectName(CurrentProject?.ProjectName!);
        ConfigManager.SaveAppConfig();
        ClearBlocks();
    }

    private void OpenProject_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new ProjectOpenWindow(ConfigManager.RecentProjects)
        {
            Owner = this
        };

        if (window.ShowDialog() != true) return;
        CurrentProject = window.ResultProject;
        SetProjectName(CurrentProject?.ProjectName!);
        if (CurrentProject != null) ConfigManager.LoadBlocksFromProject(CurrentProject);
    }
    
    public void ClearBlocks()
    {
        foreach (var block in Blocks.ToList())
            MainCanvas.Children.Remove(block);

        Blocks.Clear();
    }
    
    public void SetProjectName(string projectName) => ProjectButton.Content = projectName;
    
    public void Log(string message)
    {
        Console.WriteLine(message);
        //message = message.Length > 90 ? message.Remove(90, message.Length) : message;
        Logger.Text = message;
        _loggerTimer.Start();
    }

    private void InitLoggerTimer()
    {
        _loggerTimer.Interval = new TimeSpan(0, 0, 5);
        _loggerTimer.Tick += (_, _) =>
        {
            Logger.Text = string.Empty;
            _loggerTimer.Stop();
        };
    }

    private void ProjectContextMenu_OnLoaded(object sender, RoutedEventArgs e)
    {
        var menu = ProjectContextMenu;
        var separatorIndex = -1;
        for (var i = 0; i < menu.Items.Count; i++)
        {
            if (menu.Items[i] is not Separator) continue;
            separatorIndex = i;
            break;
        }

        if (separatorIndex == -1)
        {
            separatorIndex = menu.Items.Count - 1;
        }
        
        while (menu.Items.Count > separatorIndex + 1)
        {
            menu.Items.RemoveAt(separatorIndex + 1);
        }
        
        ConfigManager.RecentProjects.ForEach(project =>
        {
            var fileName = Path.GetFileNameWithoutExtension(project);
            
            var selector = new MenuItem()
            {
                Header = fileName,
                ToolTip = project,
                Style = ((Style?)Application.Current.Resources["DarkMenuItemStyle"] ?? throw new InvalidOperationException())
            };
            
            selector.Click += (o, _) =>
            {
                var button = o as MenuItem;
                if (button?.ToolTip is not string file) return;

                ConfigManager.SaveAppConfig();

                CurrentProject = ProjectManager.LoadProject(file);
                ConfigManager.AddRecentProject(file);

                ClearBlocks();

                if (CurrentProject != null)
                    ConfigManager.LoadBlocksFromProject(CurrentProject);

                SetProjectName(CurrentProject!.ProjectName);
                $"Project {CurrentProject.ProjectName} has opened.".Log();
            };

            ProjectContextMenu.Items.Add(selector);
        });
        
    }
}

public static class GridUtils
{
    public static double Snap(double value)
    {
        return Math.Round(value / BlockElement.GridSize) * BlockElement.GridSize;
    }
}