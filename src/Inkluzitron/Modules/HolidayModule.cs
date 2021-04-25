using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

#pragma warning disable S3358 // Ternary operators should not be nested (Useless warning)
namespace Inkluzitron.Modules
{
    public class HolidayModule : ModuleBase
    {
        private IConfiguration Configuration { get; }

        public HolidayModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Command("svatek")]
        public async Task GetCzechHolidaysAsync()
        {
            await GetHolidayAsync(false);
        }

        [Command("meniny")]
        public async Task GetSlovakHolidaysAsync()
        {
            await GetHolidayAsync(true);
        }

        private async Task GetHolidayAsync(bool slovak)
        {
            var uri = CreateUri(slovak);

            using var client = new HttpClient();
            using var response = await client.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                await ReplyAsync(Configuration[$"HolidayErrorTexts:{(slovak ? "SK" : "CZ")}"]);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var names = JArray.Parse(content).Select(o => o["name"]).ToList();
            var formatedNames = names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " a " + names[^1];
            var formatedHave = names.Count == 1 ? "má" : (slovak ? "majú" : "mají");

            await ReplyAsync(string.Format(Configuration[$"HolidayOkTexts:{(slovak ? "SK" : "CZ")}"], formatedHave, formatedNames));
        }

        private Uri CreateUri(bool slovak)
        {
            var queryParts = new List<string>()
            {
                $"date={DateTime.Now:ddMM}",
                $"lang={(slovak ? "sk" : "cz")}"
            };

            var builder = new UriBuilder(Configuration["HolidayApi"])
            {
                Query = string.Join("&", queryParts)
            };

            return builder.Uri;
        }
    }
}
