namespace MarshallApp.Models
{
    public class AppConfig(double windowHeight, double windowWidth, List<string> recentProjects, string? lastProjectPath)
    {
        public string? LastProjectPath { get; set; } = lastProjectPath;
        public double WindowWidth { get; set; } = windowWidth;
        public double WindowHeight { get; set; } = windowHeight;
        
        public List<string> RecentProjects { get; init; } = recentProjects;

        public static AppConfig Default => new(1200, 800, [], string.Empty);
    }
}
