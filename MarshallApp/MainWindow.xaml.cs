using MarshallApp.Models;
using MarshallApp.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MarshallApp.Controllers;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Drawing.Color;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace MarshallApp;

/*
 * 1. сетка не отрисовывается до конца
 * 3. блок выходит за границы
 * 4. когда блок большой (растянут) при передвижении он лагае
 * 9. иерархия папок для скрипт браузера
 */

public partial class MainWindow : INotifyPropertyChanged
{
    private NotifyIcon? _trayIcon;
    private readonly AppConfig _appConfig;
    private readonly List<BlockElement> _blocks = [];
    private WallpaperController? _wallControl;
    private readonly DispatcherTimer _wallpaperTimer = new();
    private readonly LimitSettings _limitSettings;
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
        ScriptBrowser.ScriptSelected += ScriptBrowser_ScriptSelected;
        ScriptBrowser.ScriptOpenInNewPanel += ScriptBrowser_OpenInNewPanel;

        _appConfig = ConfigManager.Load();
        _limitSettings = new LimitSettings(10, 300);

        Width = _appConfig.WindowWidth;
        Height = _appConfig.WindowHeight;

        LoadAllConfigs();
        NewScript();
        InitializeTray();
    }

    private void OpenUiElement(UIElement? obj, object sender)
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

        if (_blocks.Count == 0)
        {
            menu.Items.Add("(no scripts)").Enabled = false;
        }
        else
        {
            foreach (var block in _blocks)
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
            SaveAppConfig();
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

    private void RemoveBlockElement(BlockElement element)
    {
        _blocks.Remove(element);
        MainCanvas.Children.Remove(element);

        SaveAppConfig();
    }

    #region Script Browser
    private void ScriptBrowser_OpenInNewPanel(string filePath)
    {
        var block = new BlockElement(RemoveBlockElement, _limitSettings)
        {
            FilePath = filePath
        };

        _blocks.Add(block);
        MainCanvas.Children.Add(block);
        _ = block.RunPythonScript();
        
        SaveAppConfig();
    }

    private void ScriptBrowser_ScriptSelected(string? filePath)
    {
        if (filePath != null) CodeEditor.LoadScript(filePath);
    }
    #endregion

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
    
    private void AddBlock_Click(object sender, RoutedEventArgs e)
    {
        var block = new BlockElement(RemoveBlockElement, _limitSettings);
        AddBlockElement(block);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        return;
        if (Settings.Instanse != null) return;
        
        var settings = new Settings();
        settings.Show();
    }
    #endregion

    #region LeftPanel
    
    private void MainCanvasShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(MainCanvas, sender);

    private void ScriptBrowserShowButton_OnClick(object sender, RoutedEventArgs e) => OpenUiElement(ScriptBrowser, sender);

    private void CodeEditorShowButton_OnClick(object sender, RoutedEventArgs e)=> OpenUiElement(CodeEditor, sender);

    #endregion

    #region LoadSaving things

    private void SaveAppConfig()
    {
        _appConfig.WindowWidth = Width;
        _appConfig.WindowHeight = Height;
        
        _appConfig.Blocks.Clear();
        foreach(var block in _blocks)
        {
            _appConfig.Blocks.Add(new BlockConfig
            {
                PythonFilePath = block.FilePath,
                IsLooping = block.IsLooping,
                LoopIntervalSeconds = block.LoopInterval,
                OutputFontSize = block.OutputFontSize,
                X = Canvas.GetLeft(block),
                Y = Canvas.GetTop(block),
                WidthUnits = block.WidthUnits,
                HeightUnits = block.HeightUnits
            });
        }

        ConfigManager.Save(_appConfig);
    }

    private void LoadAllConfigs()
    {
        foreach (var cfg in _appConfig.Blocks)
        {
            var block = new BlockElement(RemoveBlockElement, _limitSettings)
            {
                FilePath = cfg.PythonFilePath,
                IsLooping = cfg.IsLooping,
                LoopInterval = cfg.LoopIntervalSeconds,
                OutputFontSize = cfg.OutputFontSize,
                WidthUnits = Math.Max(1, cfg.WidthUnits),
                HeightUnits = Math.Max(1, cfg.HeightUnits)
            };

            block.Width = block.WidthUnits * BlockElement.GridSize;
            block.Height = block.HeightUnits * BlockElement.GridSize;

            _blocks.Add(block);
            MainCanvas.Children.Add(block);

            Canvas.SetLeft(block, GridUtils.Snap(cfg.X));
            Canvas.SetTop(block, GridUtils.Snap(cfg.Y));

            if(!string.IsNullOrEmpty(block.FilePath) && File.Exists(block.FilePath))
            {
                block.SetFileNameText();
                _ = block.RunPythonScript();
            }
            block.RestoreLoopState();
        }
    }
    
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        SaveAppConfig();
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
        SaveAppConfig();
        Environment.Exit(0);
    }

    private void AboutMarshall_Click(object sender, RoutedEventArgs e)
    {
        if(About.Instance != null) return;
        
        var aboutWindow = new About();
        aboutWindow.Show();
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

        SaveAppConfig();
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
        _blocks.RemoveAt(oldIndex);

        if (newIndex > MainCanvas.Children.Count)
            newIndex = MainCanvas.Children.Count;

        MainCanvas.Children.Insert(newIndex, element);
        _blocks.Insert(newIndex, element);
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

        _blocks.Add(element);
    }
}

public static class GridUtils
{
    public static double Snap(double value)
    {
        return Math.Round(value / BlockElement.GridSize) * BlockElement.GridSize;
    }
}