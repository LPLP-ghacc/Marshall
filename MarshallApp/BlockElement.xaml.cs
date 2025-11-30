// 悲しいという気持ち - Yuyoyuppe

using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace MarshallApp;

public partial class BlockElement : UserControl
{
    private readonly Action<BlockElement>? _onRemove;
    public string? PythonFilePath;
    public bool IsLooping = false;
    public double LoopInterval { get; set; } = 5.0;
    private DispatcherTimer? _loopTimer;
    private Process? _activeProcess;
    private bool _isInputVisible = false;

    public BlockElement(Action<BlockElement>? onRemove)
    {
        InitializeComponent();
        _onRemove = onRemove;
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
    
    private static void OpenPythonDownloadPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.python.org/downloads/",
            UseShellExecute = true
        });
    }

    public void RunPythonScript()
    {
        if (!IsPythonInstalled())
        {
            OpenPythonDownloadPage();
        }
        
        if (string.IsNullOrEmpty(PythonFilePath) || !File.Exists(PythonFilePath))
        {
            OutputText.Text = "File not found or not selected!";
            return;
        }

        SetFileNameText();

        try
        {
            StopActiveProcess();

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{PythonFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                EnvironmentVariables =
                {
                    ["PYTHONUTF8"] = "1"
                }
            };

            _activeProcess = new Process { StartInfo = psi };
            _activeProcess.Start();

            _ = Task.Run(async () =>
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = string.Empty;
                });

                var buffer = new char[1];
                var reader = _activeProcess.StandardOutput;
                while (!reader.EndOfStream)
                {
                    var count = await reader.ReadAsync(buffer, 0, 1);
                    if (count > 0)
                    {
                        Dispatcher.Invoke(() => OutputText.Text += buffer[0]);
                    }
                }
            });

            _ = Task.Run(async () =>
            {
                while (await _activeProcess.StandardError.ReadLineAsync() is { } line)
                {
                    if (line.Contains("No module named"))
                    {
                        var missingModule = ParseMissingModule(line);
                        if (string.IsNullOrEmpty(missingModule)) continue;
                        Dispatcher.Invoke(() => OutputText.Text += $"\n[AutoFix] Installing missing module: {missingModule}...\n");
                        var installed = await InstallPythonPackage(missingModule);
                        if (installed)
                        {
                            Dispatcher.Invoke(() => OutputText.Text += $"[AutoFix] Successfully installed {missingModule}. Restarting script...\n");
                            Dispatcher.Invoke(RunPythonScript);
                        }
                        else
                        {
                            Dispatcher.Invoke(() => OutputText.Text += $"[AutoFix] Failed to install {missingModule}.\n");
                        }
                    }
                    else
                    {
                        var line1 = line;
                        Dispatcher.Invoke(() => OutputText.Text += "\n[Error] " + line1);
                    }
                }
            });

            _activeProcess.Exited += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    OutputText.Text = string.Empty;
                });
            };
        }
        catch (Exception ex)
        {
            OutputText.Text = $"\nPython startup error:\n{ex.Message}";
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
                StandardErrorEncoding = Encoding.UTF8
            };
            
            using var process = new Process();
            process.StartInfo = psi;
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
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

    private void StopActiveProcess()
    {
        try
        {
            if (_activeProcess != null && !_activeProcess.HasExited)
                _activeProcess.Kill();
        }
        catch
        {
            // ignored
        }
    }

    #region top menu buttons
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        StopLoop();
        _onRemove?.Invoke(this);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (EditBlockButton.ContextMenu == null) return;
        EditBlockButton.ContextMenu.PlacementTarget = EditBlockButton;
        EditBlockButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        EditBlockButton.ContextMenu.IsOpen = true;
    }

    private void SelectPythonFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Python files (*.py)|*.py|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        PythonFilePath = dlg.FileName;
        SetFileNameText();
        RunPythonScript();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(OutputText.Text);
    }

    #endregion

    #region loop button behaviour

    private void ToggleLoop_Click(object sender, RoutedEventArgs e)
    {
        IsLooping = !IsLooping;

        if (IsLooping)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Interval in seconds:", "Loop Settings", LoopInterval.ToString(CultureInfo.InvariantCulture));
            if (double.TryParse(input, out var sec) && sec > 0)
                LoopInterval = sec;

            _loopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(LoopInterval) };
            _loopTimer.Tick += (s, _) => RunPythonScript();
            _loopTimer.Start();
        }
        else
        {
            StopLoop();
        }

        UpdateLoopStatus();
    }

    private void UpdateLoopStatus()
    {
        if (IsLooping)
            OutputText.Text = $"Loop: ON | Interval: {LoopInterval}s | File: {Path.GetFileNameWithoutExtension(PythonFilePath)}";
    }

    private void StopLoop()
    {
        _loopTimer?.Stop();
        _loopTimer = null;
    }
    #endregion

    #region input things
    private void ToggleInput_Click(object sender, RoutedEventArgs e)
    {
        _isInputVisible = !_isInputVisible;
        UserInputBox.Visibility = _isInputVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UserInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _activeProcess != null && !_activeProcess.HasExited)
        {
            string input = UserInputBox.Text.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                _activeProcess.StandardInput.WriteLine(input);
                OutputText.Text += $"\n>>> {input}\n";
                UserInputBox.Clear();
            }
        }
    }
    #endregion

    public void SetFileNameText()
    {
        if (!string.IsNullOrEmpty(PythonFilePath))
            FileNameText.Text = Path.GetFileNameWithoutExtension(PythonFilePath);
    }

    public void RestoreLoopState()
    {
        if (IsLooping)
        {
            if (!string.IsNullOrEmpty(PythonFilePath) && File.Exists(PythonFilePath))
            {
                _loopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(LoopInterval) };
                _loopTimer.Tick += (s, _) => RunPythonScript();
                _loopTimer.Start();

                UpdateLoopStatus();
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
}
