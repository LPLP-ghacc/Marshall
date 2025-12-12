using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MarshallApp.Models;
using MarshallApp.Services;

// ReSharper disable InconsistentNaming

namespace MarshallApp;

public partial class BlockElement
{
    public double WidthUnits { get; set; }
    public double HeightUnits { get; set; }
    private Point _mouseOffset;
    public const double GridSize = 15;
    private const double BaseBlockSize = 500;
    private bool _isResizing;
    private ResizeDirection _resizeDir = ResizeDirection.None;
    private enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public static double Snap(double value)
    {
        return Math.Round(value / GridSize) * GridSize;
    }
    
    private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(this);

        // --- TRY START RESIZE ---
        _resizeDir = GetResizeDirection(pos, 8);
        if (_resizeDir != ResizeDirection.None)
        {
            _isResizing = true;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        // --- START DRAG ---
        _mouseOffset = pos;
        _isDragging = true;
        CaptureMouse();
        e.Handled = true;
    }

    private void UserControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (VisualTreeHelper.GetParent(this) is not Canvas canvas) return;
    
        var pos = e.GetPosition(this);
    
        const double edge = 8;
    
        if (!_isResizing && !_isDragging)
        {
            _resizeDir = GetResizeDirection(pos, edge);
    
            Cursor = _resizeDir switch
            {
                ResizeDirection.Left => Cursors.SizeWE,
                ResizeDirection.Right => Cursors.SizeWE,
                ResizeDirection.Top => Cursors.SizeNS,
                ResizeDirection.Bottom => Cursors.SizeNS,
                ResizeDirection.TopLeft => Cursors.SizeNWSE,
                ResizeDirection.TopRight => Cursors.SizeNESW,
                ResizeDirection.BottomLeft => Cursors.SizeNESW,
                ResizeDirection.BottomRight => Cursors.SizeNWSE,
                _ => Cursors.Arrow
            };
    
            if (Cursor != Cursors.Arrow)
            {
                e.Handled = true;
                return;
            }
        }
    
        // ----- RESIZING -----
        if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
        {
            var globalPos = e.GetPosition(canvas);
    
            var left = Canvas.GetLeft(this);
            var top = Canvas.GetTop(this);
            var right = left + Width;
            var bottom = top + Height;

            switch (_resizeDir)
            {
                // horizon
                case ResizeDirection.Left or ResizeDirection.TopLeft or ResizeDirection.BottomLeft:
                {
                    var newLeft = GridUtils.Snap(globalPos.X);

                    if (newLeft < 0) newLeft = 0;
                    if (newLeft > right - GridSize) newLeft = right - GridSize;

                    var newWidth = right - newLeft;
                    if (newWidth > GridSize)
                    {
                        Width = newWidth;
                        Canvas.SetLeft(this, newLeft);
                    }
                    break;
                }
                case ResizeDirection.Right or ResizeDirection.TopRight or ResizeDirection.BottomRight:
                {
                    var newRight = GridUtils.Snap(globalPos.X);
                    
                    if (newRight > canvas.Width) newRight = canvas.Width;
                    if (newRight < left + GridSize) newRight = left + GridSize;

                    var newWidth = newRight - left;
                    if (newWidth > GridSize)
                        Width = newWidth;

                    break;
                }
            }

            switch (_resizeDir)
            {
                // vertical
                case ResizeDirection.Top or ResizeDirection.TopLeft or ResizeDirection.TopRight:
                {
                    var newTop = GridUtils.Snap(globalPos.Y);
                    if (newTop < 0) newTop = 0;
                    if (newTop > bottom - GridSize) newTop = bottom - GridSize;
                    var newHeight = bottom - newTop;
                    if (newHeight > GridSize)
                    {
                        Height = newHeight;
                        Canvas.SetTop(this, newTop);
                    }

                    break;
                }
                case ResizeDirection.Bottom or ResizeDirection.BottomLeft or ResizeDirection.BottomRight:
                {
                    var newBottom = GridUtils.Snap(globalPos.Y);
                    var newHeight = newBottom - top;
                    if (newHeight > GridSize)
                        Height = newHeight;
                    break;
                }
            }
    
            e.Handled = true;
            return;
        }
    
        // ----- DRAG -----
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed) return;
        DragMove(e, canvas);
        e.Handled = true;
    }
    
    private ResizeDirection GetResizeDirection(Point pos, double edge)
    {
        var left = pos.X <= edge;
        var right = pos.X >= ActualWidth - edge;
        var top = pos.Y <= edge;
        var bottom = pos.Y >= ActualHeight - edge;

        return (left, right, top, bottom) switch
        {
            (true, false, true, false) => ResizeDirection.TopLeft,
            (false, true, true, false) => ResizeDirection.TopRight,
            (true, false, false, true) => ResizeDirection.BottomLeft,
            (false, true, false, true) => ResizeDirection.BottomRight,
            (true, false, false, false) => ResizeDirection.Left,
            (false, true, false, false) => ResizeDirection.Right,
            (false, false, true, false) => ResizeDirection.Top,
            (false, false, false, true) => ResizeDirection.Bottom,
            _ => ResizeDirection.None
        };
    }
    
    private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var wasResizingOrDragging = _isResizing || _isDragging;

        if (_isResizing)
        {
            _isResizing = false;
        }

        if (_isDragging)
        {
            _isDragging = false;
        }

        ReleaseMouseCapture();

        if (wasResizingOrDragging)
        {
            WidthUnits = Math.Max(1, Math.Round(Width / GridSize));
            HeightUnits = Math.Max(1, Math.Round(Height / GridSize));
        }

        e.Handled = true;
    }
    
    private void DragMove(MouseEventArgs e, Canvas canvas)
    {
        var pos = e.GetPosition(canvas);

        var newLeft = GridUtils.Snap(pos.X - _mouseOffset.X);
        var newTop = GridUtils.Snap(pos.Y - _mouseOffset.Y);

        if (newLeft < 0) newLeft = 0;
        if (newTop < 0) newTop = 0;

        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);
    }
    
    private void BlockElement_OnMouseEnter(object sender, MouseEventArgs e)
    {
        //MainBorder.BorderThickness = new Thickness(2);
    }
    
    private void BlockElement_OnMouseLeave(object sender, MouseEventArgs e)
    {
        //MainBorder.BorderThickness = new Thickness(1);
    }
}

public partial class BlockElement
{
    public string? FilePath;
    public bool IsLooping;
    public double LoopInterval { get; set; } = 5.0;

    private readonly string _iconsPath;
    private CancellationTokenSource? _cts;
    private readonly Action<BlockElement>? _removeCallback;
    private readonly JobManager _jobManager;
    private DispatcherTimer? _loopTimer;
    private Process? _activeProcess;
    private bool _isInputVisible;
    private bool _isDragging;
    private bool _pendingClear;
    public double OutputFontSize { get; set; } = 14.0;
    public bool IsRunning => _activeProcess is { HasExited: false };
    
    public BlockElement(Action<BlockElement>? removeCallback, LimitSettings limitSettings)
    {
        InitializeComponent();
        
        Width = BaseBlockSize;
        Height = BaseBlockSize;

        SetFileNameText("(empty)");
        WidthUnits = Math.Max(1, Math.Round(Width / GridSize));
        HeightUnits = Math.Max(1, Math.Round(Height / GridSize));
        
        Width = WidthUnits * GridSize;
        Height = HeightUnits * GridSize;
        
        _removeCallback = removeCallback;
        _jobManager = new JobManager(limitSettings);
        
        _iconsPath = Path.Combine(Environment.CurrentDirectory + "/Resource/Icons/");
        UpdateLoopIcon(IsLooping);
        
        Application.Current.Exit += (_, _) => StopActiveProcess();
        this.Unloaded += (_, _) => StopActiveProcess();
    }
    
    private async void ReadOutOutput()
    {
        try
        {
            using var reader = _activeProcess?.StandardOutput;
            var buffer = new char[1024];
            int charsRead;
                    
            while ((charsRead = await reader?.ReadAsync(buffer, 0, buffer.Length)!) > 0)
            {
                var text = new string(buffer, 0, charsRead);
                Dispatcher.Invoke(() =>
                {
                    if (_pendingClear)
                    {
                        OutputText.Text = string.Empty;
                        _pendingClear = false;
                    }
                            
                    var cleaned = text.Replace("\r", "\n").Replace("\n\n", "\n");
                    OutputText.Text += cleaned;
                    Scroll.ScrollToEnd();
                });
            }
        }
        catch (Exception ex)
        {
            SendExceptionMessage(ex);
        }
    }

    private async void ReadOutError()
    {
        try
        {
            using var reader = _activeProcess?.StandardError;
            var buffer = new char[1024];
            int charsRead;
            
            while ((charsRead = await reader?.ReadAsync(buffer, 0, buffer.Length)!) > 0)
            {
                var text = new string(buffer, 0, charsRead);

                AutoInstallMissingModule(text);
            
                Dispatcher.Invoke(() =>
                {
                    var cleaned = text.Replace("\r", "\n");
                    OutputText.Text += cleaned;
                    Scroll.ScrollToEnd();
                });
            }
        }
        catch (Exception ex)
        {
            SendExceptionMessage(ex);
        }
    }
    
    private void StopActiveProcess(bool forceKill = false)
    {
        if (_activeProcess == null) return;

        try
        {
            _cts?.Cancel();

            try { _activeProcess.StandardInput.BaseStream.Close(); }
            catch (Exception ex)
            {
                SendExceptionMessage(ex);
            }

            if (_activeProcess.HasExited) return;
            
            if (!forceKill) return;
            if (!_activeProcess.HasExited)
            {
                _activeProcess.Kill(); 
            }

            if (_jobManager.JobHandle == IntPtr.Zero) return;
            _jobManager.Close();
        }
        catch (Exception ex)
        {
            SendExceptionMessage(ex);
        }
        finally
        {
            _activeProcess?.Dispose();
            _activeProcess = null;
        }
        
        // Sayonara
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    #region top menu buttons
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        StopLoop();
        _removeCallback?.Invoke(this);
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        StopLoop();
        _removeCallback?.Invoke(this);
    }
    
    private void OutputText_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Menu.ContextMenu == null) return;
        Menu.ContextMenu.PlacementTarget = OutputText;
        Menu.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        Menu.ContextMenu.IsOpen = true;
    }

    private void SelectPythonFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        FilePath = dlg.FileName;
        SetFileNameText();
        _ = RunPythonScript();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(OutputText.Text);

    #endregion

    #region loop button behaviour

    private void ToggleLoop_Click(object sender, RoutedEventArgs e)
    {
        IsLooping = !IsLooping;
        
        if (IsLooping)
        {
            var input = new InputBoxWindow("Loop Settings", "Interval in seconds:", (sec) => LoopInterval = double.Parse(sec) > 0 ? double.Parse(sec) : 0, LoopInterval.ToString(CultureInfo.InvariantCulture))
                {
                    Owner = Application.Current.MainWindow
                };
            input.ShowDialog();

            _loopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(LoopInterval) };
            _loopTimer.Tick += (_, _) => _ = RunPythonScript();
            _loopTimer.Start();

            UpdateLoopIcon(true);
        }
        else
        {
            StopLoop();
            UpdateLoopIcon(false); 
        }
        
        UpdatePeriodTimeTextBlock();
        UpdateLoopStatus();
    }
    
    private void UpdateLoopIcon(bool isLooping)
    {
        var iconName = isLooping ? "loopGreen.png" : "loop.png";
        var fullPath = Path.Combine(_iconsPath, iconName);

        var uri = new Uri(fullPath, UriKind.Absolute);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = uri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze(); 

        LoopIconImage.ImageSource = bitmap;
    }
    
    private void UpdateLoopStatus()
    {
        if (IsLooping)
            OutputText.Text = $"Waiting for the next call...\n\nLoop: ON | Interval: {LoopInterval}s | File: {Path.GetFileNameWithoutExtension(FilePath)}";
    }

    private void StopLoop()
    {
        _loopTimer?.Stop();
        _loopTimer = null;
    }

    private void UpdatePeriodTimeTextBlock()
    {
        var value = IsLooping ? LoopInterval.ToString(CultureInfo.InvariantCulture) : string.Empty;
        
        PeriodTime.Text = value;
    } 
    
    #endregion

    #region input things
    private void ToggleInput_Click(object sender, RoutedEventArgs e)
    {
        _isInputVisible = !_isInputVisible;
        UserInputBox.Visibility = _isInputVisible ? Visibility.Visible : Visibility.Collapsed;
        UpdateInputIcon(_isInputVisible);
    }
    
    private void UpdateInputIcon(bool value)
    {
        var path = value ? Path.Combine(_iconsPath + "./inputGreen.png") : Path.Combine(_iconsPath + "./input.png");
        InputIconImage.ImageSource = new BitmapImage(new Uri(path, UriKind.Relative));
    }

    private void UserInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _activeProcess == null || _activeProcess.HasExited) return;

        var input = UserInputBox.Text.Trim();
        if (string.IsNullOrEmpty(input)) return;

        var utf8Bytes = Encoding.UTF8.GetBytes(input + "\n");
        _activeProcess.StandardInput.BaseStream.Write(utf8Bytes, 0, utf8Bytes.Length);
        _activeProcess.StandardInput.BaseStream.Flush();

        OutputText.Text += $"\n>>> {input}\n";
        UserInputBox.Clear();
    }
    #endregion
    
    private void OutputText_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
        if (e.Delta > 0)
            OutputFontSize += 1;
        else
            OutputFontSize -= 1;

        if (OutputFontSize < 8) OutputFontSize = 8;
        if (OutputFontSize > 40) OutputFontSize = 40;

        OutputText.FontSize = OutputFontSize;

        e.Handled = true;
    }
    
    private void OnOutputLoaded(object sender, RoutedEventArgs e)
    {
        OutputText.FontSize = OutputFontSize;
    }
}

// extension part
public partial class BlockElement
{
    private void SetFileNameText()
    {
        if (!string.IsNullOrEmpty(FilePath))
            FileNameText.Text = Path.GetFileNameWithoutExtension(FilePath);
    }

    private void SetFileNameText(string name)
    {
        if (!string.IsNullOrEmpty(name))
            FileNameText.Text = name;
    }

    public void RestoreLoopState()
    {
        UpdateLoopIcon(IsLooping);
        if (IsLooping)
        {
            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                _loopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(LoopInterval) };
                _loopTimer.Tick += (_, _) => _ = RunPythonScript();
                _loopTimer.Start();

                UpdateLoopStatus();
                UpdatePeriodTimeTextBlock();
            }
            else
            {
                IsLooping = false;
            }
        }
        else
        {
            UpdateLoopStatus();
        }
    }

    private void Rerun_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            OutputText.Text = string.Empty;
        });
        _ = RunPythonScript();
    } 
    
    private void CallLogViewer_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ShowLogViewer(this);
    }

    private void SendExceptionMessage(Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            OutputText.Text = ex.Message;
        });
    }
}

// python extension part
public partial class BlockElement
{
    public async Task RunPythonScript()
    {
        StopActiveProcess();

        if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
        {
            OutputText.Text = "File not found or not selected!";
            return;
        }

        OpenPythonInstallerPage();
        SetFileNameText();
        _pendingClear = true;
        
        _cts = new CancellationTokenSource();
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-u \"{FilePath}\"",
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
            };

            CodeViewer.Text = await File.ReadAllTextAsync(FilePath);

            _activeProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _activeProcess.Start();
            
            // Create JobObject if not created yet
            _jobManager.CreateJobObject();
            JobManager.AssignProcessToJobObject(_jobManager.JobHandle, _activeProcess.Handle);
            
            await Task.Run(ReadOutOutput, _cts.Token);
            
            await Task.Run(ReadOutError, _cts.Token);
        }
        catch (Exception ex)
        {
            SendExceptionMessage(ex);
        }
    }
    
    private async void AutoInstallMissingModule(string output)
    {
        try
        {
            if (!output.Contains("No module named")) return;
            
            var missingModule = ParseMissingModule(output);
            if (string.IsNullOrEmpty(missingModule)) return;
            
            Dispatcher.Invoke(() => OutputText.Text += $"\n[AutoFix] Installing missing module: {missingModule}...\n");
            var installed = await InstallPythonPackage(missingModule);
            if (installed)
            {
                Dispatcher.Invoke(() => OutputText.Text += $"[AutoFix] Successfully installed {missingModule}. Restarting script...\n");
                await Dispatcher.Invoke(RunPythonScript);
            }
            else
            {
                Dispatcher.Invoke(() => OutputText.Text += $"[AutoFix] Failed to install {missingModule}.\n");
            }
        }
        catch (Exception ex)
        {
            SendExceptionMessage(ex);
        }
    }
    
    private static void OpenPythonInstallerPage()
    {
        if (!IsPythonInstalled())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.python.org/downloads/",
                UseShellExecute = true
            });
        }
    }

    private static async Task<bool> InstallPythonPackage(string package)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-m pip install {package}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8,
            };
            
            using var process = new Process();
            process.StartInfo = psi;
            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            var success = !error.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
            
            return success;
        }
        catch
        {
            return false;
        }
    }

    private static string? ParseMissingModule(string errorText)
    {
        var start = errorText.IndexOf("No module named '", StringComparison.Ordinal);
        if (start == -1)
            return null;
        start += "No module named '".Length;

        var end = errorText.IndexOf('\'', start);
        return end == -1 ? null : errorText[start..end];
    }
    
    private static bool IsPythonInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(2000);

            return process is { ExitCode: 0 };
        }
        catch
        {
            return false;
        }
    }
}