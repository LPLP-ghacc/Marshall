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
using MarshallApp.Utils;
using Microsoft.Win32;
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
using UserControl = System.Windows.Controls.UserControl;

namespace MarshallApp;

public partial class MainWindow
{
    public static MainWindow Instance { get; private set; } = null!;
    public UserSettings? Settings;
    public Project? CurrentProject { get; set; }
    public LimitSettings LimitSettings { get; private set; } = new(10, 300);
    private NotifyIcon? _trayIcon;
    private Point _mousePosition;
    
    public readonly List<BlockElement> Blocks = [];
    public WallpaperController? WallControl;
    private readonly DispatcherTimer _wallpaperTimer = new();
    private readonly DispatcherTimer _loggerTimer = new();
    private readonly List<UIElement> _mainFieldElements = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Instance = this;
        
        _mainFieldElements.Add(ScriptBrowser);
        _mainFieldElements.Add(MainCanvas);
        _mainFieldElements.Add(CodeEditor);
        _mainFieldElements.Add(UserSettingsGrid);
        OpenUiElement(MainCanvas, MainCanvasShowButton);
        
        MainCanvas.AllowDrop = true;
        MainCanvas.DragOver += MStackPanel_DragOver;
        MainCanvas.Drop += MStackPanel_Drop;

        WallpaperControlInit();
        
        TopBorder.MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        };
        
        Loaded += async (_, _) =>
        {
            Settings = await SettingsManager.Load();
            
            if (ConfigManager.AppConfig != null) LimitSettings = 
                new LimitSettings(Settings.CpuLimitPercent, Settings.MemoryLimitMb) ;
            
            Settings.PropertyChanged += (_, e) =>
            {
                SettingsManager.Save(Settings);
                ShowNotificationButton();
                
                if (e.PropertyName == nameof(UserSettings.RunAtWindowsStartup))
                    ApplyStartupSetting();
            };

            SettingsManager.GenerateUI(SettingsPanel.Instance.UserSettingsField, Settings);
            
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
        var brush = ((Brush?)Application.Current.FindResource("SidebarButtonActiveBackground"))! ?? throw new InvalidOperationException();
        
        foreach (var element in _mainFieldElements)
        {
            if (element == obj)
            {
                element.Visibility = Visibility.Visible;
                if (element is UserControl uc) uc.IsEnabled = true;
            }
            else
            {
                element.Visibility = Visibility.Collapsed;
                if (element is UserControl uc) uc.IsEnabled = false;
            }
        }

        var buttons = new List<Button>()
        {
            CodeEditorShowButton,
            ScriptBrowserShowButton,
            MainCanvasShowButton,
            SettingsShowButton
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

        _trayIcon.Text = App.APPNAME;
        _trayIcon.ContextMenuStrip = new ContextMenuStrip();
        
        _trayIcon.MouseUp += TrayIconMouseUp;
        return;

        void TrayIconMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                MaxWidth = SystemParameters.WorkArea.Width;
                MaxHeight = SystemParameters.WorkArea.Height;
                WindowState = WindowState.Normal;

                FullscreenEnter.Visibility = Visibility.Collapsed;
                FullscreenExit.Visibility = Visibility.Visible;
                Show();
            }
            else
            {
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
                exit.Click += async (_, _) => 
                {
                    await ConfigManager.SaveAppConfigAsync();
                    Environment.Exit(0);
                };

                menu.Items.Add(exit);

                menu.Show(System.Windows.Forms.Cursor.Position);
            }
        }
    }
    
    public static void ShowLogViewer(BlockElement block)
    {
        var name = block.FilePath != null
            ? Path.GetFileName(block.FilePath)
            : "(unnamed script)";

        var window = new LogViewer(
            name,
            block.OutputText.Text
        );

        window.Show();
    }

    private void NewScript() => CodeEditor.NewScript();
    
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
    
    public async Task RemoveBlockElement(BlockElement element)
    {
        Blocks.Remove(element);
        MainCanvas.Children.Remove(element);

        await ConfigManager.SaveAppConfigAsync();
    }
    
    private void AddBlock_Click(object sender, RoutedEventArgs e)
    {
        var block = new BlockElement(async void (block) =>
        {
            try
            {
                await RemoveBlockElement(block);
            }
            catch (Exception exception) { exception.Message.Log(); }
        }, LimitSettings);
        AddBlockElement(block);
    }
    
    private void AddBlockElement(BlockElement element)
    {
        Canvas.SetLeft(element, GridUtils.Snap(_mousePosition.X));
        Canvas.SetTop(element, GridUtils.Snap(_mousePosition.Y));

        MainCanvas.Children.Add(element);

        Blocks.Add(element);
    }
    
    #endregion

    #region LeftPanel
    
    private void MainCanvasShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(MainCanvas, sender);

    private void ScriptBrowserShowButton_OnClick(object sender, RoutedEventArgs e)
    {
        ScriptBrowser.Update();
        OpenUiElement(ScriptBrowser, sender);
    } 

    private void CodeEditorShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(CodeEditor, sender);
    
    private void SettingsShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(UserSettingsGrid, sender);

    #endregion

    #region LoadSaving things
    
    protected override async void OnClosing(CancelEventArgs e)
    {
        try
        {
            base.OnClosing(e);
            await ConfigManager.SaveAppConfigAsync();
        }
        catch (Exception exception) { exception.Message.Log(); }
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

    private async void Close_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Settings!.MinimizeToTrayOnClose)
            {
                await ConfigManager.SaveAppConfigAsync();
                Environment.Exit(0);
            }
            else
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
        }
        catch (Exception exception) { exception.Message.Log(); }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _wallpaperTimer.Stop();
        _loggerTimer.Stop();
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }
    
    #endregion

    #region DragDropBlockElement

    private static void MStackPanel_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(BlockElement)) ? DragDropEffects.Move : DragDropEffects.None;

        e.Handled = true;
    }

    private async void MStackPanel_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (!e.Data.GetDataPresent(typeof(BlockElement))) return;

            var dragged = (BlockElement?)e.Data.GetData(typeof(BlockElement));

            var mousePos = e.GetPosition(MainCanvas);

            var insertIndex = GetInsertIndex(mousePos);

            if (dragged != null) MoveBlockElement(dragged, insertIndex);

            await ConfigManager.SaveAppConfigAsync();
        }
        catch (Exception exception) { exception.Message.Log(); }
    }
    
    private int GetInsertIndex(Point mousePos)
    {
        int low = 0, high = Blocks.Count - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var child = Blocks[mid];
            var top = Canvas.GetTop(child);
            var height = child.ActualHeight;
            var centerY = top + height / 2;

            if (mousePos.Y < centerY) high = mid - 1;
            else low = mid + 1;
        }
        return low;
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
        WallControl = new WallpaperController(RootImageBrush, imagesSource);
        WallControl.Update();
        
        _wallpaperTimer.Interval = TimeSpan.FromSeconds(30);
        _wallpaperTimer.Tick += (_, _) =>
        {
            WallControl.Update();
        };
        _wallpaperTimer.Start();
    }

    private void Minimize_OnClick_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private async void NewProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new ProjectCreationWindow
            {
                Owner = this
            };

            if (window.ShowDialog() != true) return;
            CurrentProject = window.ResultProject;
            SetProjectName(CurrentProject?.ProjectName!);
            await ConfigManager.SaveAppConfigAsync();
            ClearBlocks();
        }
        catch (Exception exception) { exception.Message.Log(); }
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

        if (Settings is { IsEnableLog: false }) return;
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
            
            selector.Click += async (o, _) =>
            {
                var button = o as MenuItem;
                if (button?.ToolTip is not string file) return;

                await ConfigManager.SaveAppConfigAsync();

                CurrentProject = await ProjectManager.LoadProjectAsync(file);
                await ConfigManager.AddRecentProjectAsync(file);

                ClearBlocks();

                if (CurrentProject != null)
                    ConfigManager.LoadBlocksFromProject(CurrentProject);

                SetProjectName(CurrentProject!.ProjectName);
                $"Project {CurrentProject.ProjectName} has opened.".Log();
            };

            ProjectContextMenu.Items.Add(selector);
        });
        
    }

    private void MainCanvas_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e) => _mousePosition = e.GetPosition((Canvas)sender);

    private void ShowNotificationButton() => NotificationButton.Visibility = Visibility.Visible;

    private void NotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        var wind = new NotificationWindow("Applying the changes", "To apply the changed settings, the application needs to be restarted.",
            () =>
            {
                Application.Current.MainWindow!.Close();

                var window = new MainWindow();
                Application.Current.MainWindow = window;
                window.Show();
                
                NotificationButton.Visibility = Visibility.Hidden;
            },
            () =>
            {
                NotificationButton.Visibility = Visibility.Hidden;
            });
        
        wind.ShowDialog();
    }

    private void ApplyStartupSetting()
    {
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (Settings!.RunAtWindowsStartup)
            key?.SetValue(App.APPNAME, $"\"{System.Reflection.Assembly.GetExecutingAssembly().Location}\"");
        else
            key?.DeleteValue(App.APPNAME, false);
    }
}