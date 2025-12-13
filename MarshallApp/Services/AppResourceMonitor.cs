using System.Diagnostics;
using System.Management;
using System.Windows.Controls;

namespace MarshallApp.Services;

public class AppResourceMonitor
{
    private readonly Process _process = Process.GetCurrentProcess();
    private const string ProcessName = "python";


    public (long totalMemoryMB, double cpuPercent) GetTotalUsage()
    {
        var totalMem = _process.PrivateMemorySize64 / 1024 / 1024;

        var childProcesses = GetChildProcesses(_process.Id);
        totalMem += childProcesses.Sum(child => child.PrivateMemorySize64 / 1024 / 1024);

        var cpu = GetTotalCpuUsage(childProcesses);

        return (totalMem, cpu);
    }

    private List<Process> GetChildProcesses(int parentId)
    {
        var children = new List<Process>();
        using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessId={parentId}");
        foreach (var o in searcher.Get())
        {
            var obj = (ManagementObject)o;
            if (!uint.TryParse(obj["ProcessId"].ToString(), out var pid)) continue;
            try
            {
                var proc = Process.GetProcessById((int)pid);
                if (proc.ProcessName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                    children.Add(proc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return children;
    }

    private double GetTotalCpuUsage(List<Process> processes)
    {
        double total = 0;
        var cpuCounter = new PerformanceCounter("Process", "% Processor Time", _process.ProcessName, true);
        total += cpuCounter.NextValue();

        foreach (var p in processes)
        {
            try
            {
                var childCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName + "#" + p.Id);
                total += childCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        Thread.Sleep(100);
        total = cpuCounter.NextValue();
        foreach (var p in processes)
        {
            try
            {
                var childCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName + "#" + p.Id);
                total += childCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return total / Environment.ProcessorCount;
    }
}