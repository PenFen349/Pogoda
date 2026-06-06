using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pogoda.Models
{
    public class PogodaResponse
    {
        public MainData? Main { get; set; }
        public WindData? Wind { get; set; }
        public List<PogodaDescription>? Weather { get; set; }
        public string? Name { get; set; }
        public Gorod? Sys { get; set; }
    }

    public class MainData
    {
        public double Temp { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double Temp_Min { get; set; }
        public double Temp_Max { get; set; }
        public double Feels_Like { get; set; }
    }

    public class WindData
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
        public double Gust { get; set; }
    }

    public class PogodaDescription
    {
        public string? Main { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
    }

    public class ForecastResponse
    {
        public List<ForecastItem>? List { get; set; }
        public GorodInfo? City { get; set; }
    }

    public class ForecastItem
    {
        public long Dt { get; set; }
        public MainData? Main { get; set; }
        public List<PogodaDescription>? Weather { get; set; }
        public WindData? Wind { get; set; }
        public string? Dt_Txt { get; set; }
        public int Visibility { get; set; }
    }
}