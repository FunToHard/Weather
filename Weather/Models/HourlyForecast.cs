namespace Weather.Models;

public class HourlyForecast
{
    public required string Time { get; init; }
    public required int Temperature { get; init; }
    public string TempDisplay => $"{Temperature}°";
    public required string IconPath { get; init; }
    public required string Condition { get; init; }
    public int PrecipitationChance { get; init; }
    public bool IsCurrent { get; init; }
}
