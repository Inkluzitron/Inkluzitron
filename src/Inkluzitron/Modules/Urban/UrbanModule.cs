using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.UrbanApi;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Inkluzitron.Modules.Urban
{
    [Name("Urban Dictionary")]
    public class UrbanModule : ModuleBase, IReactionHandler
    {
        private static ConcurrentDictionary<string, UrbanQueryResult> UrbanResultsCache = new ();

        private string ApiUrl { get; }
        private string UrbanEmbedLogo { get; }
        private string UrbanNotFound { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private ReactionSettings ReactionSettings { get; }
        private DiscordSocketClient Client { get; }

        public UrbanModule(IConfiguration config, DiscordSocketClient client,
            IHttpClientFactory httpClientFactory, ReactionSettings reactionSettings)
        {
            Client = client;
            ApiUrl = config["UrbanApiUrl"];
            UrbanEmbedLogo = config["UrbanEmbedLogo"];
            UrbanNotFound = config["UrbanNotFound"];
            HttpClientFactory = httpClientFactory;
            ReactionSettings = reactionSettings;
        }

        private async Task<UrbanQueryResult> GetDefinitions(string query)
        {
            query = query.Trim();

            UrbanQueryResult cachedResult;
            if(UrbanResultsCache.TryGetValue(query, out cachedResult))
                return cachedResult;

            var response = await HttpClientFactory.CreateClient()
                .GetAsync(string.Format(ApiUrl, HttpUtility.UrlEncode(query)));

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<UrbanQueryResult>(responseData);

            result.Query = query;

            UrbanResultsCache[query] = result;
            return result;
        }

        [Command("urban")]
        [Alias("define", "wtf")]
        [Summary("Vyhledá definici zadaného výrazu v Urban Dictionary.")]
        public async Task QueryUrban([Remainder][Name("výraz")] string query)
        {
            var definitions = (await GetDefinitions(query)).Definitions;

            if(definitions.Count == 0)
            {
                await ReplyAsync(string.Format(UrbanNotFound, query));
                return;
            }

            var embed = await new UrbanEmbed().WithDefinitionAsync(
                definitions[0], query, UrbanEmbedLogo, definitions.Count);

            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            var embed = message.Embeds.FirstOrDefault();
            if (embed == null || embed.Author == null || embed.Footer == null)
                return false; // Embed checks

            if (!ReactionSettings.PaginationReactions.Any(emote => emote.IsEqual(reaction)))
                return false; // Reaction check.

            if (message.ReferencedMessage == null)
                return false;

            if (!embed.TryParseMetadata<UrbanEmbedMetadata>(out var metadata))
                return false; // Not an urban embed.

            var definitions = (await GetDefinitions(metadata.SearchQuery)).Definitions;

            if (definitions.Count == 0)
                return false;

            int newPage = metadata.PageNumber;
            if (reaction.IsEqual(ReactionSettings.MoveToFirst))
                newPage = 1;
            else if (reaction.IsEqual(ReactionSettings.MoveToLast))
                newPage = definitions.Count;
            else if (reaction.IsEqual(ReactionSettings.MoveToNext) && newPage < definitions.Count)
                newPage++;
            else if (reaction.IsEqual(ReactionSettings.MoveToPrevious) && newPage > 1)
                newPage--;

            if (newPage != metadata.PageNumber)
            {
                var newEmbed = (await new UrbanEmbed()
                    .WithDefinitionAsync(
                        definitions[newPage-1], metadata.SearchQuery, UrbanEmbedLogo, definitions.Count, newPage))
                    .Build();

                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }

            var context = new CommandContext(Client, message.ReferencedMessage);
            if (!context.IsPrivate) // DMs have blocked removing reactions.
                await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
