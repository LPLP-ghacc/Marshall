using MarshallApp.Models;
using MarshallApp.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MarshallApp.Controllers;
using Color = System.Drawing.Color;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace MarshallApp;

/*
 * 1. сетка не отрисовывается до конца
 * 2. на большом экране при передвижении панелей (слева справа) лагает
 * 3. блок выходит за границы
 * 4. когда блок большой (растянут) при передвижении он лагает
 * 5. убрать выделении при наведении
 * 6. крестик для удаления блока
 * 7. контекстное меню для добавлении блока
 * 8. проблема с растягиванием на весь экран
 * 9. иерархия папок для скрипт браузера
 */

public partial class MainWindow : INotifyPropertyChanged
{
    private NotifyIcon? _trayIcon;
    
    private bool _isRestoring;
    private readonly AppConfig _appConfig;
    private readonly List<BlockElement> _blocks = [];
    private WallpaperController? _wallControl;
    private readonly DispatcherTimer _wallpaperTimer = new();
    private readonly LimitSettings _limitSettings;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Instance = this;
        
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
        //Loaded += (_, _) => DrawGrid();

        _appConfig = ConfigManager.Load();
        _limitSettings = new LimitSettings(10, 10);

        Width = _appConfig.WindowWidth;
        Height = _appConfig.WindowHeight;

        LoadPanelState();
        LoadAllConfigs();
        NewScript();
        InitializeTray();
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
        );

        window.Show();
    }

    private void NewScript()
    {
        CodeEditor.NewScript();
        CodeEditor.Visibility = Visibility.Visible;
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
        CodeEditor.Visibility = Visibility.Visible;
    }
    #endregion

    #region Top Panel Menu
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (AddButton.ContextMenu == null) return;
        AddButton.ContextMenu.PlacementTarget = AddButton;
        AddButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        AddButton.ContextMenu.IsOpen = true;
    }
    
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        if (HelpButton.ContextMenu == null) return;
        HelpButton.ContextMenu.PlacementTarget = AddButton;
        HelpButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        HelpButton.ContextMenu.IsOpen = true;
    }

    private void WindowButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine($"{LeftCol.Width}, {LeftPanelVisible}");
        
        if (WindowButton.ContextMenu == null) return;
        WindowButton.ContextMenu.PlacementTarget = WindowButton;
        WindowButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        WindowButton.ContextMenu.IsOpen = true;
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

    private void ScriptBrowserHideChecker_Checked(object sender, RoutedEventArgs e)
    {
        if (_isRestoring) return;
        LeftCol.Width = new GridLength(LeftCol.MaxWidth);
    }

    private void ScriptBrowserHideChecker_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isRestoring) return;
        LeftCol.Width = new GridLength(0);
    }

    private void ScriptEditorHideChecker_Checked(object sender, RoutedEventArgs e)
    {
        if (_isRestoring) return;
        try
        {
            RightCol.Width = new GridLength(RightCol.MaxWidth);
        }
        catch
        {
            RightCol.Width = new GridLength(500);
        }
    }

    private void ScriptEditorHideChecker_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_isRestoring) return;
        RightCol.Width = new GridLength(0);
    }

    public bool LeftPanelVisible => LeftCol.Width != new GridLength(0);
    public bool RightPanelVisible => RightCol.Width != new GridLength(0);
    #endregion

    #region LoadSaving things

    private void SaveAppConfig()
    {
        _appConfig.WindowWidth = Width;
        _appConfig.WindowHeight = Height;

        _appConfig.PanelState = new PanelState(LeftCol.Width, RightCol.Width);
        
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

    private void LoadPanelState()
    {
        _isRestoring = true;

        if (_appConfig.PanelState != null)
        {
            LeftCol.Width = _appConfig.PanelState.Left;

            ScriptBrowser.Visibility = Visibility.Visible;
            ScriptBrowser.InvalidateMeasure();
            ScriptBrowser.UpdateLayout();

            RightCol.Width = _appConfig.PanelState.Right;
        }

        ScriptBrowserHideChecker.IsChecked = LeftCol.Width.Value > 0;
        ScriptEditorHideChecker.IsChecked = RightCol.Width.Value > 0;

        _isRestoring = false;
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
        if (WindowState == WindowState.Maximized && WindowStyle == WindowStyle.None)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            FullscreenEnter.Visibility = Visibility.Visible;
            FullscreenExit.Visibility = Visibility.Collapsed;
        }
        else
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
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

    private void Minimize_OnClick_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        
    }
    
    private void AddBlockElement(BlockElement element)
    {
        element.Width = 500;
        element.Height = 500;

        Canvas.SetLeft(element, GridUtils.Snap(20));
        Canvas.SetTop(element, GridUtils.Snap(20));

        MainCanvas.Children.Add(element);

        _blocks.Add(element);
    }
    
    private void DrawGrid()
    {
        const double grid = BlockElement.GridSize;

        for (double x = 0; x < MainCanvas.ActualWidth; x += grid)
            MainCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = MainCanvas.ActualHeight,
                StrokeThickness = 0.5,
                Stroke = new SolidColorBrush(Colors.Gray),
                Opacity = 0.1,
                IsHitTestVisible = false
            });

        for (double y = 0; y < MainCanvas.ActualHeight; y += grid)
            MainCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = MainCanvas.ActualWidth, Y2 = y,
                StrokeThickness = 0.5,
                Stroke = new SolidColorBrush(Colors.Gray),
                Opacity = 0.1,
                IsHitTestVisible = false
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