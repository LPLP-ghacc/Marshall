namespace MarshallApp.Models
{
    public class AppConfig(double windowHeight, double windowWidth, List<string> recentProjects, string? lastProjectPath, int memoryLimitMb, int cpuLimitPercent)
    {
        public string? LastProjectPath { get; set; } = lastProjectPath;
        public double WindowWidth { get; set; } = windowWidth;
        public double WindowHeight { get; set; } = windowHeight;
        
        public List<string> RecentProjects { get; init; } = recentProjects;
        
        public int MemoryLimitMb { get; init; } = memoryLimitMb;
        public int CpuLimitPercent { get; init; } = cpuLimitPercent;

        public static AppConfig Default => new(1200, 800, [], string.Empty, 10, 300);
    }
}
