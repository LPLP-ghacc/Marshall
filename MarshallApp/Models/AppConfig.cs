namespace MarshallApp.Models
{
    public class AppConfig
    {
        public List<BlockConfig> Blocks { get; init; } = [];
        public PanelState? PanelState { get; set; }

        public double WindowWidth { get; set; } = 1200;
        public double WindowHeight { get; set; } = 800;
    }
}
