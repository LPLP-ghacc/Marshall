namespace MarshallApp.Utils;

public static class GridUtils
{
    public static double Snap(double value)
    {
        return Math.Round(value / BlockElement.GridSize) * BlockElement.GridSize;
    }
}