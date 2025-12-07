namespace MarshallApp.Models
{
    public class BlockConfig
    {
        public string? PythonFilePath { get; set; }
        public bool IsLooping { get; set; }
        public double LoopIntervalSeconds { get; set; } = 5.0;

        public double OutputFontSize { get; set; } = 14.0; 
        
        public double X { get; set; }
        public double Y { get; set; }
        public double WidthUnits { get; set; }
        public double HeightUnits { get; set; }
    }
}
