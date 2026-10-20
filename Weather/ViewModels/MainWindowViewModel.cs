using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Weather.Models;
using Weather.Services;

namespace Weather.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly OpenMeteoWeatherService _weatherService = new();

    public event Action<double, double>? LocationChanged;

    [ObservableProperty]
    private string _city = "San Francisco, CA";

    [ObservableProperty]
    private string _searchQuery = "San Francisco, CA";

    [ObservableProperty]
    private double _latitude = 37.7749;

    [ObservableProperty]
    private double _longitude = -122.4194;

    [ObservableProperty]
    private string _coordinatesDisplay = "37.77° N, 122.42° W";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHourlyForecastVisible))]
    private bool _isMapVisible;

    public bool IsHourlyForecastVisible => !IsMapVisible;

    [RelayCommand]
    public void ShowMap() => IsMapVisible = true;

    [RelayCommand]
    public void HideMap() => IsMapVisible = false;

    [RelayCommand]
    public void ToggleMap() => IsMapVisible = !IsMapVisible;

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
    private double _windAngle = 315.0;

    [ObservableProperty]
    private string _humidity = "58%";

    [ObservableProperty]
    private double _humidityValue = 58.0;

    [ObservableProperty]
    private string _uvIndex = "3";

    [ObservableProperty]
    private double _uvIndexValue = 3.0;

    [ObservableProperty]
    private string _uvLevel = "Moderate";

    [ObservableProperty]
    private string _airQuality = "32";

    [ObservableProperty]
    private double _aqiValue = 32.0;

    [ObservableProperty]
    private string _airQualityStatus = "Good";

    [ObservableProperty]
    private string _pressure = "1013 hPa";

    [ObservableProperty]
    private double _pressureValue = 1013.0;

    [ObservableProperty]
    private string _pressureStatus = "Steady";

    [ObservableProperty]
    private string _visibility = "16 mi";

    [ObservableProperty]
    private double _visibilityValue = 16.0;

    [ObservableProperty]
    private string _visibilityStatus = "Clear";

    [ObservableProperty]
    private string _sunrise = "6:42 AM";

    [ObservableProperty]
    private string _sunset = "6:18 PM";

    [ObservableProperty]
    private string _daylightDuration = "11h 36m of daylight";

    [ObservableProperty]
    private string _daylightStatus = "Sunset in ~35m";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IReadOnlyList<double> _hourlyTemps = new double[] { 71, 69, 66, 64, 62 };

    public ObservableCollection<DailyForecast> DailyForecasts { get; } = new();
    public ObservableCollection<HourlyForecast> HourlyForecasts { get; } = new();
    public ObservableCollection<WeatherMetric> MetricCards { get; } = new();

    public MainWindowViewModel()
    {
        // 1. Populate demo baseline immediately so the UI is never empty
        LoadDemoWeatherData();

        // 2. Fetch live data asynchronously for default location
        _ = LoadWeatherAsync("San Francisco");
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        await LoadWeatherAsync(SearchQuery.Trim());
    }

    public async Task LoadWeatherAsync(string query)
    {
        IsLoading = true;
        try
        {
            var liveData = await _weatherService.FetchWeatherAsync(query);
            if (liveData != null)
            {
                ApplyWeatherData(liveData);
                LocationChanged?.Invoke(liveData.Latitude, liveData.Longitude);
            }
        }
        catch
        {
            // Retain existing data if request fails
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadWeatherByCoordinatesAsync(double latitude, double longitude)
    {
        IsLoading = true;
        try
        {
            var liveData = await _weatherService.FetchWeatherByCoordinatesAsync(latitude, longitude);
            if (liveData != null)
            {
                ApplyWeatherData(liveData);
                // No need to fire LocationChanged when triggered by map click, as the map is already there
            }
        }
        catch
        {
            // Retain existing data if request fails
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyWeatherData(LiveWeatherData liveData)
    {
        City = liveData.FormattedCity;
        SearchQuery = liveData.FormattedCity;
        Latitude = liveData.Latitude;
        Longitude = liveData.Longitude;
        CoordinatesDisplay = FormatCoordinates(liveData.Latitude, liveData.Longitude);

        CurrentTemp = liveData.CurrentTemp;
        Condition = liveData.Condition;
        HeroIconPath = liveData.HeroIconPath;
        TempRange = liveData.TempRange;
        WindSpeed = $"{liveData.WindSpeed} mph";
        WindAngle = liveData.WindAngle;
        WindDirection = liveData.WindDirection;
        Humidity = $"{liveData.Humidity}%";
        HumidityValue = liveData.Humidity;
        UvIndex = liveData.UvIndex.ToString("0.#", CultureInfo.InvariantCulture);
        UvIndexValue = liveData.UvIndex;
        UvLevel = liveData.UvLevel;
        AirQuality = liveData.Aqi.ToString();
        AqiValue = liveData.Aqi;
        AirQualityStatus = liveData.AqiStatus;
        Pressure = liveData.PressureDisplay;
        PressureValue = liveData.Pressure;
        PressureStatus = liveData.PressureStatus;
        Visibility = liveData.VisibilityDisplay;
        VisibilityValue = liveData.Visibility;
        VisibilityStatus = liveData.VisibilityStatus;
        Sunrise = liveData.Sunrise;
        Sunset = liveData.Sunset;
        DaylightDuration = liveData.DaylightDuration;
        DaylightStatus = liveData.DaylightStatus;

        if (liveData.DailyForecasts.Count > 0)
        {
            DailyForecasts.Clear();
            foreach (var d in liveData.DailyForecasts)
                DailyForecasts.Add(d);
        }

        if (liveData.HourlyForecasts.Count > 0)
        {
            HourlyForecasts.Clear();
            foreach (var h in liveData.HourlyForecasts)
                HourlyForecasts.Add(h);
        }

        if (liveData.HourlyTemps.Count > 0)
        {
            HourlyTemps = liveData.HourlyTemps;
        }
    }

    private static string FormatCoordinates(double lat, double lon)
    {
        string latDir = lat >= 0 ? "N" : "S";
        string lonDir = lon >= 0 ? "E" : "W";
        return FormattableString.Invariant($"{Math.Abs(lat):F2}° {latDir}, {Math.Abs(lon):F2}° {lonDir}");
    }

    private void LoadDemoWeatherData()
    {
        DailyForecasts.Clear();
        DailyForecasts.Add(new DailyForecast
        {
            Day = "Today",
            Date = DateTime.Now.ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(1).ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(2).ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(3).ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(4).ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(5).ToString("MMM d"),
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
            Date = DateTime.Now.AddDays(6).ToString("MMM d"),
            IconPath = "/Assets/Icons/sunny.svg",
            Condition = "Clear",
            MinTemp = 65,
            MaxTemp = 78,
            BarStartRatio = 0.32,
            BarWidthRatio = 0.58
        });

        HourlyForecasts.Clear();
        HourlyForecasts.Add(new HourlyForecast
        {
            Time = "Now",
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
    }
}
