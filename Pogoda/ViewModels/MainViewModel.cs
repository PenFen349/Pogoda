using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Pogoda.Models;
using Pogoda.Services;

namespace Pogoda.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PogodaService _pogodaService;
        private System.Timers.Timer? _autoUpdateTimer;

        private string _cityInput = "";
        private string _cityName = "";
        private double _temperature;
        private double _feelsLike;
        private double _humidity;
        private double _windSpeed;
        private double _pressure;
        private string _pogodaDescription = "";
        private string _pogodaIcon = "";
        private bool _isLoading;
        private string _statusMessage = "";

        public MainViewModel()
        {
            _pogodaService = new PogodaService();
            Cities = new ObservableCollection<string> { "London", "New York", "Tokyo", "Moscow" };
            DailyForecasts = new ObservableCollection<DailyForecast>();

            GetPogodaCommand = new RelayCommand(_ => _ = LoadPogodaDataAsync());
            AddCityCommand = new RelayCommand(_ => AddCity(), _ => !string.IsNullOrWhiteSpace(CityInput));
            RemoveCityCommand = new RelayCommand(city => RemoveCity(city?.ToString()), _ => SelectedCity != null);
            SelectCityCommand = new RelayCommand(city =>
            {
                if (city?.ToString() != null)
                {
                    CityInput = city.ToString()!;
                    _ = LoadPogodaDataAsync();
                }
            });

            _ = InitializeAsync();
        }

        public ObservableCollection<string> Cities { get; }
        public ObservableCollection<DailyForecast> DailyForecasts { get; }

        public ISeries[] TemperatureSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = new[] { new Axis { Name = "Hour" } };
        public Axis[] YAxes { get; private set; } = new[] { new Axis { Name = "Temperature °C" } };

        public string CityInput
        {
            get => _cityInput;
            set { _cityInput = value; OnPropertyChanged(); }
        }

        public string CityName
        {
            get => _cityName;
            set { _cityName = value; OnPropertyChanged(); }
        }

        public double Temperature
        {
            get => _temperature;
            set { _temperature = value; OnPropertyChanged(); }
        }

        public double FeelsLike
        {
            get => _feelsLike;
            set { _feelsLike = value; OnPropertyChanged(); }
        }

        public double Humidity
        {
            get => _humidity;
            set { _humidity = value; OnPropertyChanged(); }
        }

        public double WindSpeed
        {
            get => _windSpeed;
            set { _windSpeed = value; OnPropertyChanged(); }
        }

        public double Pressure
        {
            get => _pressure;
            set { _pressure = value; OnPropertyChanged(); }
        }

        public string PogodaDescription
        {
            get => _pogodaDescription;
            set { _pogodaDescription = value; OnPropertyChanged(); }
        }

        public string PogodaIcon
        {
            get => _pogodaIcon;
            set { _pogodaIcon = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string? SelectedCity { get; set; }

        public ICommand GetPogodaCommand { get; }
        public ICommand AddCityCommand { get; }
        public ICommand RemoveCityCommand { get; }
        public ICommand SelectCityCommand { get; }

        private async Task InitializeAsync()
        {
            await LoadPogodaDataAsync();
            StartAutoUpdateTimer();
        }

        private async Task LoadPogodaDataAsync()
        {
            if (string.IsNullOrWhiteSpace(CityInput)) return;

            IsLoading = true;
            StatusMessage = "Loading pogoda data...";

            try
            {
                var city = CityInput.Trim();
                var currentPogoda = await _pogodaService.GetCurrentPogodaAsync(city);
                var forecast = await _pogodaService.GetForecastAsync(city);
                if (currentPogoda == null)
                {
                    StatusMessage = $"City '{city}' not found or API error! Check API key.";
                    return;
                }
                if (currentPogoda != null && currentPogoda.Main != null)
                {
                    CityName = currentPogoda.Name ?? city;
                    Temperature = Math.Round(currentPogoda.Main.Temp, 1);
                    FeelsLike = Math.Round(currentPogoda.Main.Feels_Like, 1);
                    Humidity = currentPogoda.Main.Humidity;
                    WindSpeed = currentPogoda.Wind?.Speed ?? 0;
                    Pressure = currentPogoda.Main.Pressure;
                    PogodaDescription = currentPogoda.Weather?.FirstOrDefault()?.Description ?? "No data";
                    PogodaIcon = currentPogoda.Weather?.FirstOrDefault()?.Icon ?? "01d";

                    var dailyForecasts = _pogodaService.GetDailyForecast(forecast);
                    DailyForecasts.Clear();
                    foreach (var item in dailyForecasts)
                        DailyForecasts.Add(item);

                    var hourlyTemps = _pogodaService.GetHourlyTemperatures(forecast);
                    UpdateChart(hourlyTemps);

                    StatusMessage = $"Last update: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    StatusMessage = $"City '{city}' not found!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateChart(List<HourlyTemp> hourlyTemps)
        {
            // Если данных нет, создаём тестовые
            if (hourlyTemps == null || !hourlyTemps.Any())
            {
                hourlyTemps = new List<HourlyTemp>();
                var random = new Random();
                for (int i = 0; i < 24; i++)
                {
                    hourlyTemps.Add(new HourlyTemp
                    {
                        Hour = i,
                        Temperature = random.Next(-5, 30)
                    });
                }
            }

            // Создаём серию для графика
            var values = hourlyTemps.Select(x => x.Temperature).ToArray();
            var labels = hourlyTemps.Select(x => $"{x.Hour}:00").ToArray();

            var series = new LineSeries<double>
            {
                Values = values,
                Name = "Температура",
                Fill = null,
                Stroke = new SolidColorPaint(new SkiaSharp.SKColor(0, 150, 255), 2),
                GeometrySize = 5,
                GeometryStroke = new SolidColorPaint(new SkiaSharp.SKColor(0, 150, 255), 2)
            };

            TemperatureSeries = new ISeries[] { series };

            XAxes = new[] { new Axis
    {
        Name = "Час",
        Labels = labels,
        LabelsRotation = 45
    } };

            YAxes = new[] { new Axis
    {
        Name = "Температура °C"
    } };

            OnPropertyChanged(nameof(TemperatureSeries));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
        }
        private void AddCity()
        {
            var newCity = CityInput.Trim();
            if (!string.IsNullOrEmpty(newCity) && !Cities.Contains(newCity))
            {
                Cities.Add(newCity);
            }
        }

        private void RemoveCity(object? city)
        {
            if (city?.ToString() != null && Cities.Contains(city.ToString()))
            {
                Cities.Remove(city.ToString());
            }
        }

        private void StartAutoUpdateTimer()
        {
            _autoUpdateTimer = new System.Timers.Timer(300000);
            _autoUpdateTimer.Elapsed += async (sender, e) =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadPogodaDataAsync();
                });
            };
            _autoUpdateTimer.Start();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}