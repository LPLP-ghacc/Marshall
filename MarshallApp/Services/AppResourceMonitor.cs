using System.Diagnostics;
using System.Management;

namespace MarshallApp.Services;

public class AppResourceMonitor
{
    private readonly Process _process = Process.GetCurrentProcess();
    private const string ProcessName = "python";

    public (long totalMemoryMB, double cpuPercent) GetTotalUsage()
    {
        _process.Refresh();
        var totalMem = _process.PrivateMemorySize64 / 1024 / 1024;

        var childProcesses = GetChildProcesses(_process.Id);
        foreach (var child in childProcesses)
        {
            try
            {
                child.Refresh();
                if (!child.HasExited)
                    totalMem += child.PrivateMemorySize64 / 1024 / 1024;
            }
            catch { /* ignored */ }
        }

        var cpu = GetTotalCpuUsage(_process, childProcesses);
        return (totalMem, cpu);
    }

    private static List<Process> GetChildProcesses(int parentId)
    {
        var children = new List<Process>();
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={parentId}");
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                if (!uint.TryParse(obj["ProcessId"]?.ToString(), out var pid)) continue;
                try
                {
                    var proc = Process.GetProcessById((int)pid);
                    if (string.Equals(proc.ProcessName, ProcessName, StringComparison.OrdinalIgnoreCase))
                        children.Add(proc);
                }
                catch { /* ignored */ }
            }
        }
        catch { /* ignored */ }
        return children;
    }

    private static double GetTotalCpuUsage(Process main, List<Process> children)
    {
        double total = 0;
        var all = new List<Process> { main };
        all.AddRange(children.Where(p => !p.HasExited));

        foreach (var p in all)
        {
            try
            {
                using var counter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName + (p.Id != main.Id ? $"#{p.Id}" : ""), true);
                counter.NextValue();
                Thread.Sleep(100);
                total += counter.NextValue();
            }
            catch { /* ignored */ }
        }

        return total / Environment.ProcessorCount;
    }
}