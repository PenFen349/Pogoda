using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pogoda.Models;

namespace Pogoda.Services
{
    public class PogodaService
    {
        private readonly HttpClient _httpClient;
        // БЕСПЛАТНЫЙ API БЕЗ КЛЮЧА. Показывает погоду на 3 дня из-за ограничений
        private const string CurrentPogodaUrl = "https://wttr.in/{0}?format=j1";
        private const string ForecastUrl = "https://wttr.in/{0}?format=j1";

        public PogodaService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<PogodaResponse?> GetCurrentPogodaAsync(string city)
        {
            try
            {
                var url = string.Format(CurrentPogodaUrl, city);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Парсим ответ wttr.in
                dynamic? data = JsonConvert.DeserializeObject(json);
                if (data?.current_condition != null)
                {
                    var current = data.current_condition[0];
                    return new PogodaResponse
                    {
                        Name = city,
                        Main = new MainData
                        {
                            Temp = double.Parse(current.temp_C.ToString()),
                            Humidity = double.Parse(current.humidity.ToString()),
                            Pressure = double.Parse(current.pressure.ToString()),
                            Feels_Like = double.Parse(current.FeelsLikeC.ToString()),
                            Temp_Min = double.Parse(current.temp_C.ToString()),
                            Temp_Max = double.Parse(current.temp_C.ToString())
                        },
                        Wind = new WindData
                        {
                            Speed = double.Parse(current.windspeedKmph.ToString()) / 3.6
                        },
                        Weather = new List<PogodaDescription>
                        {
                            new PogodaDescription
                            {
                                Description = current.weatherDesc[0].value.ToString(),
                                Icon = GetIconCode(current.weatherCode.ToString())
                            }
                        }
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GetIconCode(string weatherCode)
        {
            return weatherCode switch
            {
                "113" => "01d", // Sunny
                "116" => "02d", // Partly cloudy
                "119" => "03d", // Cloudy
                "122" => "04d", // Overcast
                "176" => "09d", // Light rain
                "179" => "13d", // Snow
                "185" => "10d", // Rain
                _ => "01d"
            };
        }

        public async Task<ForecastResponse?> GetForecastAsync(string city)
        {
            try
            {
                var url = string.Format(ForecastUrl, city);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                dynamic? data = JsonConvert.DeserializeObject(json);

                if (data?.weather != null)
                {
                    var forecast = new ForecastResponse();
                    var list = new List<ForecastItem>();

                    foreach (var day in data.weather)
                    {
                        var item = new ForecastItem
                        {
                            Dt_Txt = DateTime.Now.AddDays(list.Count).ToString("yyyy-MM-dd HH:mm:ss"),
                            Main = new MainData
                            {
                                Temp_Min = double.Parse(day.mintempC.ToString()),
                                Temp_Max = double.Parse(day.maxtempC.ToString()),
                                Temp = double.Parse(day.avgtempC.ToString())
                            },
                            Weather = new List<PogodaDescription>
                            {
                                new PogodaDescription
                                {
                                    Description = day.hourly[0].weatherDesc[0].value.ToString(),
                                    Icon = GetIconCode(day.hourly[0].weatherCode.ToString())
                                }
                            }
                        };
                        list.Add(item);
                    }

                    forecast.List = list.Take(5).ToList();
                    return forecast;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public List<DailyForecast> GetDailyForecast(ForecastResponse? forecast)
        {
            if (forecast?.List == null) return new List<DailyForecast>();

            return forecast.List
                .Select(item => new DailyForecast
                {
                    Date = DateTime.Parse(item.Dt_Txt!),
                    MinTemp = Math.Round(item.Main?.Temp_Min ?? 0, 1),
                    MaxTemp = Math.Round(item.Main?.Temp_Max ?? 0, 1),
                    Description = item.Weather?.FirstOrDefault()?.Description ?? "",
                    Icon = item.Weather?.FirstOrDefault()?.Icon ?? "01d"
                })
                .ToList();
        }

        public List<HourlyTemp> GetHourlyTemperatures(ForecastResponse? forecast)
        {
            var result = new List<HourlyTemp>();

            if (forecast?.List == null)
            {
                System.Diagnostics.Debug.WriteLine("Forecast list is null, generating test data");
                // Генерируем тестовые данные
                var random = new Random();
                for (int i = 0; i < 24; i++)
                {
                    result.Add(new HourlyTemp
                    {
                        Hour = i,
                        Temperature = random.Next(-5, 35)
                    });
                }
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"Got {forecast.List.Count} forecast items");

            // Берем первые 24 часа (8 записей по 3 часа = 24 часа)
            var hourlyData = forecast.List.Take(8).ToList();

            for (int i = 0; i < hourlyData.Count; i++)
            {
                var item = hourlyData[i];
                var hour = DateTime.Parse(item.Dt_Txt!).Hour;
                var temp = item.Main?.Temp ?? 0;

                result.Add(new HourlyTemp
                {
                    Hour = hour,
                    Temperature = temp
                });

                System.Diagnostics.Debug.WriteLine($"Hour: {hour}, Temp: {temp}");
            }

            // Если меньше 24 точек, интерполируем
            if (result.Count < 24)
            {
                System.Diagnostics.Debug.WriteLine($"Only {result.Count} points, interpolating to 24");
                var interpolated = new List<HourlyTemp>();
                for (int i = 0; i < 24; i++)
                {
                    var sourceIndex = i * result.Count / 24;
                    interpolated.Add(new HourlyTemp
                    {
                        Hour = i,
                        Temperature = result[Math.Min(sourceIndex, result.Count - 1)].Temperature
                    });
                }
                return interpolated;
            }

            return result;
        }
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class HourlyTemp
    {
        public int Hour { get; set; }
        public double Temperature { get; set; }
    }
}

/*
using System.Net.Http;
using Newtonsoft.Json;
using Pogoda.Models;

namespace Pogoda.Services
{
    public class PogodaService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "88d5072b67c093674cb5222da27b6bb8";
        private const string CurrentPogodaUrl = "https://api.openweathermap.org/data/2.5/weather?q={0}&units=metric&appid={1}";
        private const string ForecastUrl = "https://api.openweathermap.org/data/2.5/forecast?q={0}&units=metric&appid={1}";


        public PogodaService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<PogodaResponse?> GetCurrentPogodaAsync(string city)
        {
            try
            {
                var url = string.Format(CurrentPogodaUrl, city, ApiKey);
                System.Diagnostics.Debug.WriteLine($"Request URL: {url}");
                var response = await _httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
                //response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Response Body: {json}");
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<PogodaResponse>(json);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        

        public async Task<ForecastResponse?> GetForecastAsync(string city)
        {
            try
            {
                var url = string.Format(ForecastUrl, city, ApiKey);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ForecastResponse>(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public List<DailyForecast> GetDailyForecast(ForecastResponse? forecast)
        {
            if (forecast?.List == null) return new List<DailyForecast>();

            return forecast.List
                .GroupBy(item => DateTime.Parse(item.Dt_Txt!).Date)
                .Take(5)
                .Select(group => new DailyForecast
                {
                    Date = group.Key,
                    MinTemp = group.Min(x => x.Main?.Temp_Min ?? 0),
                    MaxTemp = group.Max(x => x.Main?.Temp_Max ?? 0),
                    Description = group.First().Weather?.FirstOrDefault()?.Description ?? "",
                    Icon = group.First().Weather?.FirstOrDefault()?.Icon ?? "01d"
                })
                .ToList();
        }

        public List<HourlyTemp> GetHourlyTemperatures(ForecastResponse? forecast)
        {
            if (forecast?.List == null) return new List<HourlyTemp>();

            return forecast.List
                .Take(24)
                .Select(item => new HourlyTemp
                {
                    Hour = DateTime.Parse(item.Dt_Txt!).Hour,
                    Temperature = item.Main?.Temp ?? 0
                })
                .ToList();
        }
    }

    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class HourlyTemp
    {
        public int Hour { get; set; }
        public double Temperature { get; set; }
    }
}
*/