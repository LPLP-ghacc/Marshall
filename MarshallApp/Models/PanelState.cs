using System.Windows;

namespace MarshallApp.Models;

public class PanelState
{
    public double LeftWidth { get; set; }
    public GridUnitType LeftUnitType { get; set; }

    public double RightWidth { get; set; }
    public GridUnitType RightUnitType { get; set; }

    public PanelState() {}

    public PanelState(GridLength left, GridLength right)
    {
        LeftWidth = left.Value;
        LeftUnitType = left.GridUnitType;

        RightWidth = right.Value;
        RightUnitType = right.GridUnitType;
    }

    public GridLength Left => new GridLength(LeftWidth, LeftUnitType);
    public GridLength Right => new GridLength(RightWidth, RightUnitType);
}