namespace MarshallApp.Models
{
    public class BlockConfig
    {
        public string? PythonFilePath { get; init; }
        public bool IsLooping { get; init; }
        public double LoopIntervalSeconds { get; init; } = 5.0;

        public double OutputFontSize { get; init; } = 14.0; 
        
        public double X { get; init; }
        public double Y { get; init; }
        public double WidthUnits { get; init; }
        public double HeightUnits { get; init; }
    }
}
