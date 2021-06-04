using System;

namespace WeatherServiceDotNetCore
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC * 9 / 5.0);

        public string Summary { get; set; }
    }
}
