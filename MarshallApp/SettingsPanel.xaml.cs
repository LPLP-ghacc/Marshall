using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MarshallApp.Services;

namespace MarshallApp;

public partial class SettingsPanel : UserControl
{
    public static SettingsPanel Instance { get; private set; } = null!;

    private readonly DispatcherTimer _appResourceMonitorTimer = new();
    private readonly AppResourceMonitor _appResourceMonitor;
    
    public SettingsPanel()
    {
        InitializeComponent();
        Instance = this;
        
        _appResourceMonitor = new AppResourceMonitor();
        InitMonitorTimer();
    }
    
    private void InitMonitorTimer()
    {
        _appResourceMonitorTimer.Interval = TimeSpan.FromSeconds(1);
        _appResourceMonitorTimer.Tick += async (_, _) =>
        {
            var usage = await Task.Run(() => _appResourceMonitor.GetTotalUsage());

            Dispatcher.Invoke(() =>
            {
                SettingsPanel.Instance.ResourceMonitorOutput.Text = 
                    $"CPU: {usage.cpuPercent:F1}%\nRAM: {usage.totalMemoryMB} MB";
                
                CpuProgressBar.Value = usage.cpuPercent;
                RamProgressBar.Value = usage.totalMemoryMB;
            });
        };
        _appResourceMonitorTimer.Start();
    }
    
    private void OpenPythonInstallationPage_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.python.org/downloads/",
            UseShellExecute = true
        });
    }
    
    private void OpenGitHub(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/LPLP-ghacc/Marshall#",
            UseShellExecute = true
        });
    }

    private void OpenUpdatesPages_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/LPLP-ghacc/Marshall/releases",
            UseShellExecute = true
        });
    }

    private void OpenBackgroundsFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var path = MainWindow.Instance.WallControl!.WorkingDirectory;
        path.Log();

        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}