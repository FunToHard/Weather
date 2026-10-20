namespace Weather.Models;

public record WeatherMetric
{
    public required string Title { get; init; }
    public required string Value { get; init; }
    public string? SubValue { get; init; }
    public required string IconPath { get; init; }
    public string? BadgeText { get; init; }
    public string? BadgeColor { get; init; }
}
