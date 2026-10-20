using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Weather.ViewModels;

namespace Weather.Views;

public partial class MainWindow : Window
{
    private readonly MemoryLayer _pinLayer;
    private MainWindowViewModel? _currentVm;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Initialize Mapsui Map with OpenStreetMap standard raster tiles
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // 2. Setup interactive Pin Layer with Fluent pin styling
        _pinLayer = new MemoryLayer("PinLayer")
        {
            Style = new SymbolStyle
            {
                Fill = new Brush(Color.FromArgb(240, 37, 99, 235)),
                Outline = new Pen(Color.White, 3.0),
                SymbolScale = 0.85,
                SymbolType = SymbolType.Ellipse
            }
        };

        // 3. Set initial pin position (San Francisco default)
        var (initX, initY) = SphericalMercator.FromLonLat(-122.4194, 37.7749);
        var initialPoint = new MPoint(initX, initY);
        _pinLayer.Features = new[] { new PointFeature(initialPoint) };
        map.Layers.Add(_pinLayer);

        // 4. Attach Map to MapControl and set initial viewport center & resolution
        WeatherMap.Map = map;
        map.Navigator.CenterOnAndZoomTo(initialPoint, 2500);

        // 5. User interaction: Click anywhere on map to select location, then smoothly transition to Hourly Forecast
        WeatherMap.Info += async (sender, args) =>
        {
            if (args.WorldPosition != null)
            {
                // Immediate visual update of pin
                _pinLayer.Features = new[] { new PointFeature(args.WorldPosition) };
                _pinLayer.DataHasChanged();
                WeatherMap.RefreshGraphics();

                // Convert Spherical Mercator to WGS84 (Lon, Lat)
                var (lon, lat) = SphericalMercator.ToLonLat(args.WorldPosition.X, args.WorldPosition.Y);

                if (DataContext is MainWindowViewModel vm)
                {
                    // Fetch live weather data for chosen coordinates
                    await vm.LoadWeatherByCoordinatesAsync(lat, lon);

                    // Smooth transition back to Hourly Forecast after pin drop confirmation
                    await Task.Delay(400);
                    Dispatcher.UIThread.Post(() => vm.IsMapVisible = false);
                }
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_currentVm != null)
        {
            _currentVm.LocationChanged -= OnLocationChanged;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _currentVm = vm;
            _currentVm.LocationChanged += OnLocationChanged;
        }
        else
        {
            _currentVm = null;
        }
    }

    private void OnLocationChanged(double lat, double lon)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var (x, y) = SphericalMercator.FromLonLat(lon, lat);
            var targetPoint = new MPoint(x, y);

            // Update pin location
            _pinLayer.Features = new[] { new PointFeature(targetPoint) };
            _pinLayer.DataHasChanged();

            // Center map on the searched city
            WeatherMap.Map?.Navigator.CenterOnAndZoomTo(targetPoint, 2500);
            WeatherMap.RefreshGraphics();
        });
    }

    private void LocationBadge_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IsMapVisible = true;
        }
    }

    private void OpenMap_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IsMapVisible = true;
        }
    }

    private void CloseMap_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IsMapVisible = false;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
