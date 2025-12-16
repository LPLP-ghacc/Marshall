namespace MarshallApp.Models;

public class LimitSettings(int cpuLimitPercent, int memoryLimitMb)
{
    public int MemoryLimitMb { get; } = memoryLimitMb;
    public int CpuLimitPercent { get; } = cpuLimitPercent;
}