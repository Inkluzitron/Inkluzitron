using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public sealed class KisService : IDisposable
    {
        public KisSettings Settings { get; }
        private HttpClient HttpClient { get; }
        private ILogger<KisService> Logger { get; }

        public KisService(KisSettings settings, IHttpClientFactory httpClientFactory, ILogger<KisService> logger)
        {
            HttpClient = httpClientFactory.CreateClient("Kis");
            Settings = settings;
            Logger = logger;
        }

        public async Task<int> GetPrestigeAsync(string nickname, DateTime? from, DateTime? to)
        {
            Validate(nickname);

            if (from == null) from = DateTime.MinValue;
            if (to == null) to = DateTime.UtcNow;

            var timeFrom = from.Value.ToString(Settings.DateTimeFormat);
            var timeTo = to.Value.ToString(Settings.DateTimeFormat);

            var url = $"users/leaderboard?count=100&time_from={timeFrom}&time_to={timeTo}";
            var response = await HttpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError(content);
                throw new InvalidOperationException(Settings.Messages["ApiError"]);
            }

            var json = JArray.Parse(content);
            var usersPrestige = json.FirstOrDefault(o => o["nickname"].Value<string>() == nickname);

            if (usersPrestige == null)
                throw new InvalidOperationException(Settings.Messages["NoData"]);

            return usersPrestige["prestige_gain"].Value<int>();
        }

        private void Validate(string nickname)
        {
            if (HttpClient.BaseAddress == null)
                throw new InvalidOperationException(Settings.Messages["NotConfigured"]);

            if (string.IsNullOrEmpty(nickname))
                throw new ValidationException(Settings.Messages["MissingNickname"]);
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
