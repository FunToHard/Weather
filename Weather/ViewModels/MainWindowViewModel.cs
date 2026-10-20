using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Weather.Models;

namespace Weather.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _city = "San Francisco, CA";

    [ObservableProperty]
    private string _searchQuery = "San Francisco, CA";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTempDisplay))]
    private int _currentTemp = 72;

    public string CurrentTempDisplay => $"{CurrentTemp}°";

    [ObservableProperty]
    private string _condition = "Partly Cloudy";

    [ObservableProperty]
    private string _tempRange = "18:43, 67°F - 78°F";

    [ObservableProperty]
    private string _heroIconPath = "/Assets/Icons/partly-cloudy.svg";

    [ObservableProperty]
    private string _windSpeed = "12 mph";

    [ObservableProperty]
    private string _windDirection = "NW";

    [ObservableProperty]
    private string _humidity = "58%";

    [ObservableProperty]
    private string _uvIndex = "3";

    [ObservableProperty]
    private string _uvLevel = "Moderate";

    [ObservableProperty]
    private string _airQuality = "32";

    [ObservableProperty]
    private string _airQualityStatus = "Good";

    public ObservableCollection<DailyForecast> DailyForecasts { get; } = new();
    public ObservableCollection<HourlyForecast> HourlyForecasts { get; } = new();
    public ObservableCollection<WeatherMetric> MetricCards { get; } = new();

    public MainWindowViewModel()
    {
        LoadDemoWeatherData();
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        City = SearchQuery.Trim();
        // In showcase mode, keep data rich and vibrant
    }

    private void LoadDemoWeatherData()
    {
        // 7-Day Forecast Data
        DailyForecasts.Clear();
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Sat",
            Date = "Today",
            IconPath = "/Assets/Icons/partly-cloudy.svg",
            Condition = "Partly Cloudy",
            MinTemp = 62,
            MaxTemp = 74,
            BarStartRatio = 0.25,
            BarWidthRatio = 0.50
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Sun",
            Date = "Tomorrow",
            IconPath = "/Assets/Icons/sunny.svg",
            Condition = "Sunny",
            MinTemp = 64,
            MaxTemp = 78,
            BarStartRatio = 0.30,
            BarWidthRatio = 0.60
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Mon",
            Date = "Oct 22",
            IconPath = "/Assets/Icons/cloudy.svg",
            Condition = "Cloudy",
            MinTemp = 60,
            MaxTemp = 72,
            BarStartRatio = 0.20,
            BarWidthRatio = 0.50
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Tue",
            Date = "Oct 23",
            IconPath = "/Assets/Icons/rain.svg",
            Condition = "Light Rain",
            MinTemp = 58,
            MaxTemp = 68,
            BarStartRatio = 0.15,
            BarWidthRatio = 0.45
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Wed",
            Date = "Oct 24",
            IconPath = "/Assets/Icons/sunny.svg",
            Condition = "Sunny",
            MinTemp = 63,
            MaxTemp = 76,
            BarStartRatio = 0.28,
            BarWidthRatio = 0.55
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Thu",
            Date = "Oct 25",
            IconPath = "/Assets/Icons/partly-cloudy.svg",
            Condition = "Mostly Sunny",
            MinTemp = 62,
            MaxTemp = 74,
            BarStartRatio = 0.25,
            BarWidthRatio = 0.50
        });
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Fri",
            Date = "Oct 26",
            IconPath = "/Assets/Icons/sunny.svg",
            Condition = "Clear",
            MinTemp = 65,
            MaxTemp = 78,
            BarStartRatio = 0.32,
            BarWidthRatio = 0.58
        });

        // Hourly Forecast Timeline
        HourlyForecasts.Clear();
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "6 PM",
            Temperature = 71,
            IconPath = "/Assets/Icons/sunny.svg",
            Condition = "Sunny",
            IsCurrent = true
        });
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "7 PM",
            Temperature = 69,
            IconPath = "/Assets/Icons/partly-cloudy.svg",
            Condition = "Partly Cloudy"
        });
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "8 PM",
            Temperature = 66,
            IconPath = "/Assets/Icons/cloudy.svg",
            Condition = "Cloudy"
        });
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "9 PM",
            Temperature = 64,
            IconPath = "/Assets/Icons/clear-night.svg",
            Condition = "Clear"
        });
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "10 PM",
            Temperature = 62,
            IconPath = "/Assets/Icons/clear-night.svg",
            Condition = "Clear"
        });

        // Atmospheric Metric Cards
        MetricCards.Clear();
        MetricCards.Add(new WeatherMetric
        {
            Title = "Wind speed",
            Value = "12 mph",
            SubValue = "NW • Gusts to 18 mph",
            IconPath = "/Assets/Icons/wind.svg",
            BadgeText = "NW"
        });
        MetricCards.Add(new WeatherMetric
        {
            Title = "Humidity",
            Value = "58%",
            SubValue = "The dew point is 56° right now",
            IconPath = "/Assets/Icons/humidity.svg",
            BadgeText = "Normal"
        });
        MetricCards.Add(new WeatherMetric
        {
            Title = "UV Index",
            Value = "3",
            SubValue = "Moderate risk of harm from UV",
            IconPath = "/Assets/Icons/uv-index.svg",
            BadgeText = "Moderate",
            BadgeColor = "#F59E0B"
        });
        MetricCards.Add(new WeatherMetric
        {
            Title = "Air Quality",
            Value = "32",
            SubValue = "Air quality is satisfactory (AQI)",
            IconPath = "/Assets/Icons/air-quality.svg",
            BadgeText = "Good",
            BadgeColor = "#10B981"
        });
    }
}
