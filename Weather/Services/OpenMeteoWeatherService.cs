using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Weather.Models;

namespace Weather.Services;

public record LiveWeatherData
{
    public required string FormattedCity { get; init; }
    public required int CurrentTemp { get; init; }
    public required string Condition { get; init; }
    public required string HeroIconPath { get; init; }
    public required string TempRange { get; init; }
    public required double WindSpeed { get; init; }
    public required double WindAngle { get; init; }
    public required string WindDirection { get; init; }
    public required int Humidity { get; init; }
    public required double UvIndex { get; init; }
    public required string UvLevel { get; init; }
    public required int Aqi { get; init; }
    public required string AqiStatus { get; init; }
    public required string Sunrise { get; init; }
    public required string Sunset { get; init; }
    public required string DaylightDuration { get; init; }
    public required string DaylightStatus { get; init; }
    public required List<DailyForecast> DailyForecasts { get; init; }
    public required List<HourlyForecast> HourlyForecasts { get; init; }
    public required List<double> HourlyTemps { get; init; }
}

public class OpenMeteoWeatherService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<LiveWeatherData?> FetchWeatherAsync(string cityName, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Geocoding
            string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=1";
            string geoJson = await HttpClient.GetStringAsync(geoUrl, cancellationToken);
            var geoResult = JsonSerializer.Deserialize<GeocodingResponse>(geoJson, JsonOptions);

            if (geoResult?.Results == null || geoResult.Results.Count == 0)
                return null;

            var loc = geoResult.Results[0];
            string cityDisplay = string.IsNullOrWhiteSpace(loc.Admin1)
                ? $"{loc.Name}, {loc.CountryCode}"
                : $"{loc.Name}, {loc.Admin1}";

            // 2. Forecast API
            string forecastUrl = FormattableString.Invariant(
                $"https://api.open-meteo.com/v1/forecast?latitude={loc.Latitude}&longitude={loc.Longitude}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max&timezone=auto&temperature_unit=fahrenheit&wind_speed_unit=mph");
            string forecastJson = await HttpClient.GetStringAsync(forecastUrl, cancellationToken);
            var forecast = JsonSerializer.Deserialize<ForecastResponse>(forecastJson, JsonOptions);

            if (forecast?.Current == null || forecast.Daily == null || forecast.Hourly == null)
                return null;

            // 3. Air Quality API
            int aqi = 32;
            string aqiStatus = "Good";
            try
            {
                string aqiUrl = FormattableString.Invariant(
                    $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={loc.Latitude}&longitude={loc.Longitude}&current=us_aqi");
                string aqiJson = await HttpClient.GetStringAsync(aqiUrl, cancellationToken);
                var aqiResp = JsonSerializer.Deserialize<AirQualityResponse>(aqiJson, JsonOptions);
                if (aqiResp?.Current?.UsAqi != null)
                {
                    aqi = (int)Math.Round(aqiResp.Current.UsAqi.Value);
                    aqiStatus = aqi switch
                    {
                        <= 50 => "Good",
                        <= 100 => "Moderate",
                        <= 150 => "Sensitive",
                        _ => "Unhealthy"
                    };
                }
            }
            catch
            {
                // Air quality non-fatal fallback
            }

            // Current metrics
            int curTemp = (int)Math.Round(forecast.Current.Temperature2m);
            var (condText, condIcon) = MapWeatherCode(forecast.Current.WeatherCode);
            double windAngle = forecast.Current.WindDirection10m;
            string windDir = DegreesToCardinal(windAngle);
            double windSpeed = Math.Round(forecast.Current.WindSpeed10m, 1);
            int humidity = (int)Math.Round(forecast.Current.RelativeHumidity2m);

            // UV
            double uvIndex = forecast.Daily.UvIndexMax != null && forecast.Daily.UvIndexMax.Count > 0
                ? Math.Round(forecast.Daily.UvIndexMax[0], 1)
                : 3.0;
            string uvLevel = uvIndex switch
            {
                <= 2.9 => "Low",
                <= 5.9 => "Moderate",
                <= 7.9 => "High",
                <= 10.9 => "Very High",
                _ => "Extreme"
            };

            // Sunrise & Sunset
            string sunriseStr = "6:42 AM";
            string sunsetStr = "6:18 PM";
            string daylightDuration = "11h 36m of daylight";
            string daylightStatus = "Sunset soon";

            if (forecast.Daily.Sunrise != null && forecast.Daily.Sunrise.Count > 0 &&
                forecast.Daily.Sunset != null && forecast.Daily.Sunset.Count > 0)
            {
                if (DateTime.TryParse(forecast.Daily.Sunrise[0], out var srTime) &&
                    DateTime.TryParse(forecast.Daily.Sunset[0], out var ssTime))
                {
                    sunriseStr = srTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
                    sunsetStr = ssTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
                    var duration = ssTime - srTime;
                    daylightDuration = $"{duration.Hours}h {duration.Minutes}m of daylight";

                    var now = DateTime.Now;
                    if (now < srTime)
                        daylightStatus = $"Sunrise in {(int)(srTime - now).TotalMinutes}m";
                    else if (now < ssTime)
                        daylightStatus = $"Sunset in {(int)(ssTime - now).TotalMinutes}m";
                    else
                        daylightStatus = "Night time";
                }
            }

            // Min/Max for today
            int todayMin = forecast.Daily.Temperature2mMin != null && forecast.Daily.Temperature2mMin.Count > 0
                ? (int)Math.Round(forecast.Daily.Temperature2mMin[0])
                : curTemp - 5;
            int todayMax = forecast.Daily.Temperature2mMax != null && forecast.Daily.Temperature2mMax.Count > 0
                ? (int)Math.Round(forecast.Daily.Temperature2mMax[0])
                : curTemp + 5;
            string tempRange = $"{DateTime.Now:HH:mm}, {todayMin}°F - {todayMax}°F";

            // 7-Day Daily Forecasts
            var dailyList = new List<DailyForecast>();
            if (forecast.Daily.Time != null && forecast.Daily.WeatherCode != null)
            {
                int count = Math.Min(7, forecast.Daily.Time.Count);
                for (int i = 0; i < count; i++)
                {
                    DateTime dayDate = DateTime.TryParse(forecast.Daily.Time[i], out var d) ? d : DateTime.Now.AddDays(i);
                    string dayName = i == 0 ? "Today" : dayDate.ToString("ddd", CultureInfo.InvariantCulture);
                    int minT = (int)Math.Round(forecast.Daily.Temperature2mMin?[i] ?? (curTemp - 5));
                    int maxT = (int)Math.Round(forecast.Daily.Temperature2mMax?[i] ?? (curTemp + 5));
                    var (dCond, dIcon) = MapWeatherCode(forecast.Daily.WeatherCode[i]);

                    dailyList.Add(new DailyForecast
                    {
                        Day = dayName,
                        Date = dayDate.ToString("MMM d", CultureInfo.InvariantCulture),
                        Condition = dCond,
                        IconPath = dIcon,
                        MinTemp = minT,
                        MaxTemp = maxT,
                        BarStartRatio = 0.2 + (i % 3) * 0.05,
                        BarWidthRatio = 0.5 + (i % 2) * 0.1
                    });
                }
            }

            // Hourly Forecasts (Next 5 hours)
            var hourlyList = new List<HourlyForecast>();
            var hourlyTemps = new List<double>();
            if (forecast.Hourly.Time != null && forecast.Hourly.Temperature2m != null)
            {
                // Find current hour index
                int startIndex = 0;
                string nowIsoPrefix = DateTime.Now.ToString("yyyy-MM-ddTHH:00");
                for (int i = 0; i < forecast.Hourly.Time.Count; i++)
                {
                    if (forecast.Hourly.Time[i].StartsWith(nowIsoPrefix))
                    {
                        startIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    int idx = startIndex + i;
                    if (idx < forecast.Hourly.Time.Count)
                    {
                        DateTime hTime = DateTime.TryParse(forecast.Hourly.Time[idx], out var ht) ? ht : DateTime.Now.AddHours(i);
                        string timeLabel = i == 0 ? "Now" : hTime.ToString("h tt", CultureInfo.InvariantCulture);
                        double temp = Math.Round(forecast.Hourly.Temperature2m[idx]);
                        int code = forecast.Hourly.WeatherCode != null && idx < forecast.Hourly.WeatherCode.Count
                            ? forecast.Hourly.WeatherCode[idx]
                            : forecast.Current.WeatherCode;
                        var (hCond, hIcon) = MapWeatherCode(code, hTime.Hour < 6 || hTime.Hour > 20);

                        hourlyList.Add(new HourlyForecast
                        {
                            Time = timeLabel,
                            Temperature = (int)temp,
                            Condition = hCond,
                            IconPath = hIcon,
                            IsCurrent = i == 0
                        });
                        hourlyTemps.Add(temp);
                    }
                }
            }

            return new LiveWeatherData
            {
                FormattedCity = cityDisplay,
                CurrentTemp = curTemp,
                Condition = condText,
                HeroIconPath = condIcon,
                TempRange = tempRange,
                WindSpeed = windSpeed,
                WindAngle = windAngle,
                WindDirection = windDir,
                Humidity = humidity,
                UvIndex = uvIndex,
                UvLevel = uvLevel,
                Aqi = aqi,
                AqiStatus = aqiStatus,
                Sunrise = sunriseStr,
                Sunset = sunsetStr,
                DaylightDuration = daylightDuration,
                DaylightStatus = daylightStatus,
                DailyForecasts = dailyList,
                HourlyForecasts = hourlyList,
                HourlyTemps = hourlyTemps
            };
        }
        catch
        {
            return null;
        }
    }

    public static (string Condition, string IconPath) MapWeatherCode(int code, bool isNight = false)
    {
        return code switch
        {
            0 => isNight ? ("Clear Night", "/Assets/Icons/clear-night.svg") : ("Clear Sky", "/Assets/Icons/sunny.svg"),
            1 => isNight ? ("Mostly Clear", "/Assets/Icons/clear-night.svg") : ("Mainly Clear", "/Assets/Icons/sunny.svg"),
            2 => ("Partly Cloudy", "/Assets/Icons/partly-cloudy.svg"),
            3 => ("Overcast", "/Assets/Icons/cloudy.svg"),
            45 or 48 => ("Foggy", "/Assets/Icons/cloudy.svg"),
            51 or 53 or 55 => ("Drizzle", "/Assets/Icons/rain.svg"),
            61 or 63 or 65 => ("Rain", "/Assets/Icons/rain.svg"),
            71 or 73 or 75 or 77 => ("Snow", "/Assets/Icons/snow.svg"),
            80 or 81 or 82 => ("Rain Showers", "/Assets/Icons/rain.svg"),
            85 or 86 => ("Snow Showers", "/Assets/Icons/snow.svg"),
            95 or 96 or 99 => ("Thunderstorm", "/Assets/Icons/thunderstorm.svg"),
            _ => ("Partly Cloudy", "/Assets/Icons/partly-cloudy.svg")
        };
    }

    public static string DegreesToCardinal(double degrees)
    {
        string[] cardinals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        int index = (int)Math.Round((degrees % 360) / 22.5, MidpointRounding.AwayFromZero);
        return cardinals[index % 16];
    }
}

// JSON DTOs
public class GeocodingResponse
{
    public List<GeocodingLocation>? Results { get; set; }
}

public class GeocodingLocation
{
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = "";
    public string? Admin1 { get; set; }
}

public class ForecastResponse
{
    public CurrentWeather? Current { get; set; }
    public DailyWeather? Daily { get; set; }
    public HourlyWeather? Hourly { get; set; }
}

public class CurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }
    [JsonPropertyName("relative_humidity_2m")]
    public double RelativeHumidity2m { get; set; }
    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }
    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed10m { get; set; }
    [JsonPropertyName("wind_direction_10m")]
    public double WindDirection10m { get; set; }
}

public class DailyWeather
{
    public List<string>? Time { get; set; }
    [JsonPropertyName("weather_code")]
    public List<int>? WeatherCode { get; set; }
    [JsonPropertyName("temperature_2m_max")]
    public List<double>? Temperature2mMax { get; set; }
    [JsonPropertyName("temperature_2m_min")]
    public List<double>? Temperature2mMin { get; set; }
    public List<string>? Sunrise { get; set; }
    public List<string>? Sunset { get; set; }
    [JsonPropertyName("uv_index_max")]
    public List<double>? UvIndexMax { get; set; }
}

public class HourlyWeather
{
    public List<string>? Time { get; set; }
    [JsonPropertyName("temperature_2m")]
    public List<double>? Temperature2m { get; set; }
    [JsonPropertyName("weather_code")]
    public List<int>? WeatherCode { get; set; }
}

public class AirQualityResponse
{
    public AirQualityCurrent? Current { get; set; }
}

public class AirQualityCurrent
{
    [JsonPropertyName("us_aqi")]
    public double? UsAqi { get; set; }
}
