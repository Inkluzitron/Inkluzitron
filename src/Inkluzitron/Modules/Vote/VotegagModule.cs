using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    [Name("Hlasování o umlčení uživatele")]
    public class VotegagModule : ModuleBase
    {
        private UsersService UsersService { get; }
        private VoteSettings VoteSettings { get; }
        private BotSettings BotSettings { get; }
        private IMemoryCache Cache { get; }

        static private readonly TimeSpan VoteTime = new (0, 5, 0);

        public VotegagModule(UsersService usersService, VoteSettings voteSettings,
            BotSettings botSettings, IMemoryCache cache)
        {
            UsersService = usersService;
            VoteSettings = voteSettings;
            BotSettings = botSettings;
            Cache = cache;
        }

        static private string FormatPeople(int number)
        {
            if (number == 1) return "člověk";
            else if (number > 1 && number < 5) return "lidi";
            return "lidí";
        }

        static public string GenerateVoteMessage(IUser target, int minutes, int minVotes, int votes, DateTime voteEnd)
            => GenerateVoteMessage(target.Mention, minutes, minVotes, votes, voteEnd);

        static public string GenerateVoteMessage(string targetMention, int minutes, int minVotes, int votes, DateTime voteEnd)
        {
            var text = new StringBuilder(
                $"Hlasování o umlčení {targetMention} na dobu **{minutes} minut**.\n");

            text.Append($"S návrhem musí souhlasit alespoň {minVotes} {FormatPeople(minVotes)}");
            if (votes != 0) text.Append($" ({votes} {FormatPeople(votes)} {(votes > 1 && votes < 5 ? "jsou" : "je")} pro)");
            text.Append($".\n*Hlasování bude ukončeno v {voteEnd:HH:mm}.*");

            return text.ToString();
        }

        static public string CreateCacheKey(ulong userId)
            => $"votegag_{userId}";

        private async Task MuteUser(SocketGuildUser user, int minutes)
        {
            var muteRole = user.Guild.Roles.First(r => r.Id == VoteSettings.MuteRole);
            await user.AddRoleAsync(muteRole);

            var mutedUntil = DateTime.Now.AddMinutes(minutes);
            await UsersService.SetMutedUntilAsync(user, mutedUntil);
        }

        static public async Task UnmuteUser(SocketGuildUser user, UsersService usersService, SocketRole muteRole)
        {
            await user.RemoveRoleAsync(muteRole);

            await usersService.SetMutedUntilAsync(user, null);

            try
            {
                await user.SendMessageAsync("Tvé umlčení právě skončilo.");
            }
            catch (HttpException)
            {
                // User has blocked DMs
            }
        }

        [Command("votegag")]
        [Summary("Zahájí hlasování o umlčení uživatele na stanovenou dobu v minutách (výchozí=30, min=5, max=240).")]
        public async Task StartVoteAsync([Name("uživatel")] IUser user, [Name("minut")] int minutes = 30)
        {
            var minMinutes = 5;
            var maxMinutes = 240;

            if (minutes < minMinutes || minutes > maxMinutes)
            {
                await ReplyAsync("Doba umlčení musí být v rozsahu 5-240 minut.");
                return;
            }

            if (Context.IsPrivate || Context.Guild.Id != BotSettings.HomeGuildId)
            {
                await ReplyAsync("Tento příkaz lze spustit jen na hlavním serveru.");
                return;
            }

            if (user.IsBot)
            {
                await ReplyAsync("Nemůžeš umlčet bota.");
                return;
            }

            var guildUser = Context.Guild.GetUser(user.Id);

            if (guildUser == null)
            {
                await ReplyAsync($"{user.GetDisplayName()} není na tomto serveru.");
                return;
            }

            var userDb = await UsersService.GetOrCreateUserDbEntityAsync(user);

            var mutedGendered =
                userDb.Gender == Gender.Male ? "umlčený" :
                userDb.Gender == Gender.Female ? "umlčená" :
                "umlčený/á";

            var wasGendered =
                userDb.Gender == Gender.Male ? "byl" :
                userDb.Gender == Gender.Female ? "byla" :
                "byl/a";

            if (userDb.MutedUntil != null && userDb.MutedUntil > DateTime.Now)
            {

                await ReplyAsync($"{user.GetDisplayName()} již je {mutedGendered} do {userDb.MutedUntil?.ToString("HH:mm")}. Nelze tedy zahájit nové hlasování.");
                return;
            }

            if(Cache.TryGetValue<DateTime>(CreateCacheKey(user.Id), out var voteEnd))
            {
                await ReplyAsync($"Již probíhá hlasování o umlčení {user.GetDisplayName()}. Do {voteEnd:HH:mm} tedy nelze zahájit nové hlasování.");
                return;
            }

            var mentions = new AllowedMentions
            {
                MentionRepliedUser = false
            };
            mentions.UserIds.Add(guildUser.Id);

            voteEnd = DateTime.Now.Add(VoteTime);
            Cache.Set(CreateCacheKey(user.Id), voteEnd);

            var minVotesCalculation = VoteSettings.MuteVoteMaxVotes -
                Math.Pow((maxMinutes - minutes) / (double)(maxMinutes - minMinutes), 2)
                * (VoteSettings.MuteVoteMaxVotes - VoteSettings.MuteVoteMinVotes);

            var minVotes = (int)Math.Floor(minVotesCalculation);

            var voteMessage = await ReplyAsync(GenerateVoteMessage(guildUser, minutes, minVotes, 0, voteEnd), allowedMentions:mentions);
            await voteMessage.AddReactionAsync(VoteSettings.MuteReactionFor);
            await voteMessage.AddReactionAsync(VoteSettings.MuteReactionAgainst);
            await Task.Delay(VoteTime);

            // Process result

            Cache.Remove(CreateCacheKey(user.Id));
            var finalVoteMessage = await Context.Channel.GetMessageAsync(voteMessage.Id);
            if (finalVoteMessage == null) return;

            var totalVotes = 0;

            var reactions = finalVoteMessage.Reactions;
            if (reactions.TryGetValue(VoteSettings.MuteReactionFor, out var votesFor))
                totalVotes = votesFor.ReactionCount;

            if (reactions.TryGetValue(VoteSettings.MuteReactionAgainst, out var votesAgainst))
                totalVotes -= votesAgainst.ReactionCount;

            var success = totalVotes >= minVotes;
            var resultText = new StringBuilder($"Hlasování dokončeno, {guildUser.GetDisplayName()} ");
            if (!success) resultText.Append("ne");
            resultText.Append(wasGendered);
            resultText.Append($" umlčený (hlasů: {totalVotes}, potřeba: {minVotes}).");

            var resultMessage = await voteMessage.ReplyAsync(resultText.ToString());

            var voteMessageContent = string.Join('\n', finalVoteMessage.Content.Split('\n').SkipLast(1));
            await voteMessage.ModifyAsync(m => m.Content = $"{voteMessageContent}\n*Hlasování ukončeno, výsledek: <{resultMessage.GetJumpUrl()}>*");

            if (!success) return;
            await MuteUser(guildUser, minutes);
            try
            {
                await guildUser.SendMessageAsync($"Byl jsi umlčený na {minutes} minut " +
                    $"(do {DateTime.Now.AddMinutes(minutes):HH:mm}) " +
                    $"hlasováním od uživatele {Context.Message.Author.GetDisplayName()}." +
                    $"\n*Odkaz na hlasování: <{voteMessage.GetJumpUrl()}>*");
            }
            catch (HttpException)
            {
                // User has blocked DMs
            }

            await Task.Delay(new TimeSpan(0, minutes, 0));
            // Unmute user

            var muteRole = guildUser.Guild.Roles.First(r => r.Id == VoteSettings.MuteRole);
            await UnmuteUser(guildUser, UsersService, muteRole);
        }
    }
}
