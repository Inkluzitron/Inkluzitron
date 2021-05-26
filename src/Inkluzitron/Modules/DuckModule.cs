using Discord;
using Discord.Commands;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Kachna")]
    public class DuckModule : ModuleBase
    {
        private IConfiguration Configuration { get; }

        public DuckModule(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Command("kachna")]
        [Alias("duck")]
        [Summary("Zjištění aktuálního stavu kachny.")]
        public async Task GetDuckAsync()
        {
            try
            {
                var duckState = await GetCurrentDuckStateAsync();
                var embed = GetDuckStateEmbed(duckState);

                await ReplyAsync(embed: embed);
            }
            catch (WebException ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        private async Task<DuckCurrentState> GetCurrentDuckStateAsync()
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(Configuration["IsKachnaOpen:Api"])
            };

            using var response = await client.GetAsync("api/duck/currentState");

            if (!response.IsSuccessStatusCode)
                throw new WebException($"Nepodařilo se zjistit stav kachny. Zkus <{Configuration["IsKachnaOpen:Api"]}>");

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DuckCurrentState>(json);
        }

        private Embed GetDuckStateEmbed(DuckCurrentState state)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                {
                    Name = "U Kachničky",
                    IconUrl = Configuration["IsKachnaOpen:EmbedImage"]
                })
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();

            var titleBuilder = new StringBuilder();

            switch (state.State)
            {
                case DuckState.Private:
                case DuckState.Closed:
                    ProcessPrivateOrClosed(titleBuilder, state, embed);
                    break;
                case DuckState.OpenBar:
                    ProcessOpenBar(titleBuilder, state, embed);
                    break;
                case DuckState.OpenChillzone:
                    ProcessChillzone(titleBuilder, state, embed);
                    break;
                case DuckState.OpenEvent:
                    ProcessOpenEvent(titleBuilder, state);
                    break;
            }

            return embed.WithTitle(titleBuilder.ToString()).Build();
        }

        #region Private or Closed

        static private void ProcessPrivateOrClosed(StringBuilder titleBuilder, DuckCurrentState currentState, EmbedBuilder embedBuilder)
        {
            titleBuilder.AppendLine("Kachna je zavřená.");

            if (currentState.NextOpeningDateTime.HasValue)
            {
                FormatWithNextOpening(titleBuilder, currentState, embedBuilder);
                return;
            }

            if (currentState.NextOpeningDateTime.HasValue && currentState.State != DuckState.Private)
            {
                FormatWithNextOpeningNoPrivate(currentState, embedBuilder);
                return;
            }

            titleBuilder.Append("Další otvíračka není naplánovaná.");
            AddNoteToEmbed(embedBuilder, currentState.Note);
        }

        static private void FormatWithNextOpening(StringBuilder titleBuilder, DuckCurrentState currentState, EmbedBuilder embedBuilder)
        {
            var left = currentState.NextOpeningDateTime.Value - DateTime.Now;

            titleBuilder
                .Append("Do další otvíračky zbývá ")
                .Append(left.FullTextFormat())
                .Append('.');

            AddNoteToEmbed(embedBuilder, currentState.Note);
        }

        static private void FormatWithNextOpeningNoPrivate(DuckCurrentState currentState, EmbedBuilder embed)
        {
            if (string.IsNullOrEmpty(currentState.Note))
            {
                embed.AddField("A co dál?",
                                $"Další otvíračka není naplánovaná, ale tento stav má skončit {currentState.NextStateDateTime:dd. MM. v HH:mm}. Co bude pak, to nikdo neví.",
                                false);

                return;
            }

            AddNoteToEmbed(embed, currentState.Note, "A co dál?");
        }

        #endregion

        #region OpenBar

        private void ProcessOpenBar(StringBuilder titleBuilder, DuckCurrentState currentState, EmbedBuilder embedBuilder)
        {
            titleBuilder.Append("Kachna je otevřená!");
            embedBuilder.AddField("Otevřeno", currentState.LastChange.ToString("HH:mm"), true);

            if (currentState.ExpectedEnd.HasValue)
            {
                var left = currentState.ExpectedEnd.Value - DateTime.Now;

                titleBuilder.Append(" Do konce zbývá ").Append(left.FullTextFormat()).Append('.');
                embedBuilder.AddField("Zavíráme", currentState.ExpectedEnd.Value.ToString("HH:mm"), true);
            }

            var enableBeers = Configuration.GetSection("IsKachnaOpen").GetValue<bool>("EnableBeersOnTap");
            if (enableBeers && currentState.BeersOnTap?.Length > 0)
            {
                var beers = string.Join(Environment.NewLine, currentState.BeersOnTap);
                embedBuilder.AddField("Aktuálně na čepu", beers, false);
            }

            AddNoteToEmbed(embedBuilder, currentState.Note);
        }

        #endregion

        #region Chillzone

        static private void ProcessChillzone(StringBuilder titleBuilder, DuckCurrentState currentState, EmbedBuilder embedBuilder)
        {
            titleBuilder
                .Append("Kachna je otevřená v režimu chillzóna až do ")
                .AppendFormat("{0:HH:mm}", currentState.ExpectedEnd.Value)
                .Append('!');

            AddNoteToEmbed(embedBuilder, currentState.Note);
        }

        #endregion

        #region OpenEvent

        static private void ProcessOpenEvent(StringBuilder titleBuilder, DuckCurrentState currentState)
        {
            titleBuilder
                .Append("V Kachně právě probíhá akce „")
                .Append(currentState.EventName)
                .Append("“.");
        }

        #endregion

        static private void AddNoteToEmbed(EmbedBuilder embed, string note, string title = "Poznámka")
        {
            if (!string.IsNullOrEmpty(note))
                embed.AddField(title, note, false);
        }
    }
}
