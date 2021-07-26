using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Modules
{
    [Name("RicePurityTest.com")]
    [Group("purity")]
    [Summary("Vypíše, popř. přidá, skóre uživatele z rice purity testu.")]
    public class RicePurity : ModuleBase
    {
        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private UsersService UsersService { get; }

        public RicePurity(UsersService usersService,
            DatabaseFactory databaseFactory)
        {
            UsersService = usersService;
            DatabaseFactory = databaseFactory;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            DbContext = DatabaseFactory.Create();
            base.BeforeExecute(command);
        }

        protected override void AfterExecute(CommandInfo command)
        {
            DbContext?.Dispose();
            base.AfterExecute(command);
        }

        private async Task<RicePurityResult> GetMostRecentResultOfUser(IUser user)
        {
            return await DbContext.RicePurityResults.AsQueryable()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        [Command("")]
        [Summary("Zobrazí výsledky odesílatele nebo uživatele.")]
        public async Task ShowUserResultAsync([Name("koho")] IUser target = null)
        {
            if (target == null)
            {
                target = Context.Message.Author;
            }
            else
            {
                if (target.IsBot)
                {
                    await Context.Channel.SendMessageAsync(
                        "Bot je 100% pure... Divím se, že se musíš ptát."
                    );
                    return;
                }
            }

            var guildUser = target as SocketGuildUser;

            var mostRecentResultOfUser = await GetMostRecentResultOfUser(target);

            var userName = "";
            if (guildUser is not null)
                userName = guildUser.Nickname ?? guildUser.Username;
            else
                userName = target.Username;

            if (mostRecentResultOfUser == null)
            {
                await Context.Channel.SendMessageAsync(
                    "Uživatel nemá zadané výsledky testu!"
                );
                return;
            }

            await Context.Channel.SendMessageAsync(
                "Výsledek uživatele **" +
                userName +
                "** je: ***" +
                mostRecentResultOfUser.Score + "***"
            );
        }

        [Command("board")]
        [Summary("Zobrazí žebříček bodů v RicePurity testu.")]
        public async Task ShowLeaderboardAsync()
        {
            var data = DbContext.RicePurityResults
                .Include(x => x.User)
                .AsQueryable()
                .GroupBy(
                    x => x.User.Id,
                    (userId, results) => new
                    {
                        UserId = userId, MinScore = results.Min(r => r.Score)
                    }
                ).Join(DbContext.Users, x => x.UserId, user => user.Id, (x, y) => new
                {
                    Score = x.MinScore,
                    y.Name
                });

            foreach (var d in data)
            {
                Console.WriteLine(d.Name + " " + d.Score);
            }
        }

        [Command("add")]
        [Summary("Přidá do databáze výsledek testu.")]
        public async Task AddNewPointsAsync([Name("body")] uint points)
        {
            if (points > 100)
            {
                await Context.Channel.SendMessageAsync(
                    "Ty :snake: jeden prolhanej, nemůžeš mít více než 100 bodíků!"
                );
                return;
            }

            var mostRecentResultOfUser = await GetMostRecentResultOfUser(Context.Message.Author);
            if (mostRecentResultOfUser != null && mostRecentResultOfUser.Score <= points)
            {
                await Context.Channel.SendMessageAsync(
                    "Tak to teda ne! Nemá smysl ukládat záznam, kde máš vetší nebo rovné skóre jako posledně."
                );
                return;
            }

            var user = await DbContext.GetOrCreateUserEntityAsync(Context.Message.Author);

            var testResultDb = new RicePurityResult
            {
                Score = points,
                UserId = user.Id,
                SubmittedAt = DateTime.Now
            };

            await DbContext.RicePurityResults.AddAsync(testResultDb);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(new Emoji("\U00002705"));
        }
    }
}
