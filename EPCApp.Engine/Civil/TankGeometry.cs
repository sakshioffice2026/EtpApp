namespace EPCApp.Engine.Civil;

public static class TankGeometry
{
    public static (double Length, double Width) Rectangular(double volumeM3, double waterDepthM, double lengthToWidth = 2.0)
    {
        if (volumeM3 <= 0) throw new ArgumentOutOfRangeException(nameof(volumeM3));
        if (waterDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(waterDepthM));
        if (lengthToWidth <= 0) throw new ArgumentOutOfRangeException(nameof(lengthToWidth));

        var area = volumeM3 / waterDepthM;
        var width = Math.Sqrt(area / lengthToWidth);
        var length = lengthToWidth * width;

        return (Math.Round(length, 2), Math.Round(width, 2));
    }

    public static double CircularDiameter(double areaM2)
    {
        if (areaM2 <= 0) throw new ArgumentOutOfRangeException(nameof(areaM2));

        return Math.Round(Math.Sqrt(4.0 * areaM2 / Math.PI), 2);
    }
}