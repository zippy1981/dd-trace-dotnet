using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WeatherServiceDotNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly Random _random = new Random();
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<WeatherForecast> Get()
        {
            HttpClient httpClient = new();
            string summary = await httpClient.GetStringAsync("http://localhost:15000/WeatherForecast/Summary");

            return new WeatherForecast
            {
                Date = DateTime.UtcNow,
                TemperatureC = _random.Next(-20, 55),
                Summary = summary
            };
        }

        [HttpGet]
        [Route("Summary")]
        public string GetSummary()
        {
            return Summaries[_random.Next(Summaries.Length)];
        }
    }
}
