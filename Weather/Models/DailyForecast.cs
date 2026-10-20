namespace Weather.Models;

public record DailyForecast
{
    public required string Day { get; init; }
    public required string Date { get; init; }
    public required string IconPath { get; init; }
    public required string Condition { get; init; }
    public required int MinTemp { get; init; }
    public required int MaxTemp { get; init; }
    public string MinTempDisplay => $"{MinTemp}°";
    public string MaxTempDisplay => $"{MaxTemp}°";
    public string TempRangeDisplay => $"{MinTemp}°/{MaxTemp}°";
    public double BarStartRatio { get; init; } = 0.2;
    public double BarWidthRatio { get; init; } = 0.6;
}
