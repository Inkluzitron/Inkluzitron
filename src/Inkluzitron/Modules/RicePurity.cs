using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Services;

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

        [Command("")]
        [Summary("Zobrazí výsledky odesílatele nebo uživatele.")]
        public async Task ShowUserResultAsnyc([Name("koho")] IUser target = null)
        {
            if (target is null)
                target = Context.Message.Author;

            // vypise vysledek
            await Context.Channel.SendMessageAsync("Vypis vysledky!");
        }

        [Command("add")]
        [Summary("Přidá do databáze výsledek testu.")]
        public async Task AddNewPoints([Name("body")] uint points)
        {
            var user = await UsersService.GetOrCreateUserDbEntityAsync(Context.Message.Author);

            var testResultDb = new RicePurityResult
            {
                Score = points,
                UserId = user.Id,
                SubmittedAt = DateTime.Now
            };

            await DbContext.RicePurityResults.AddAsync(testResultDb);
            await DbContext.SaveChangesAsync();
            await Context.Channel.SendMessageAsync("Successfully added!");
        }
    }
}
