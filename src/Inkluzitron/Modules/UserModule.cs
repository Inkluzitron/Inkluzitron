
using Discord;
using Discord.Commands;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Nastavení uživatele")]
    [Summary("Informace o pohlaví se používají pro správný výpis hlášek bota. Pohlaví je možné nastavit pomocí příkazů níže, nebo se nastaví automaticky po vložení BDSM testu.")]
    public class UserModule : ModuleBase
    {
        private UsersService UsersService { get; }
        private ReactionSettings ReactionSettings { get; }
        private IConfiguration Configuration { get; }

        public UserModule(UsersService usersService, IConfiguration configuration,
            ReactionSettings reactionSettings)
        {
            UsersService = usersService;
            Configuration = configuration;
            ReactionSettings = reactionSettings;
        }

        [Command("gender")]
        [Summary("Vypíše svoje nastavené pohlaví nebo pohlaví vybraného uživatele.")]
        public async Task ShowGenderAsync(IUser user = null)

        {
            var genderMsg = Configuration["UserModule:UserGenderMessage"];
            var notFoundMsg = Configuration["UserModule:UserNotFoundMessage"];

            if (user == null) user = Context.User;

            if (user.IsBot)
            {
                await ReplyAsync(string.Format(genderMsg, user.GetDisplayName(), "je bot"));
                return;
            }

            var userDb = await UsersService.GetUserDbEntityAsync(user);
            if (userDb == null)
            {
                await ReplyAsync(string.Format(notFoundMsg, user.GetDisplayName()));
                return;
            }

            var gender = userDb.Gender == Gender.Unspecified ?
                "nemá zvolené pohlaví" :
                $"je {userDb.Gender.GetDisplayName()}";

            await ReplyAsync(string.Format(genderMsg, user.GetDisplayName(), gender));
        }

        [Command("gender set male")]
        [Alias("gender set m", "gender set muz")]
        [Summary("Nastaví svoje pohlaví jako muž.")]
        public async Task SetGenderMaleAsync()

        {
            await SetGenderAsync(Gender.Male);

        }

        [Command("gender set female")]
        [Alias("gender set f", "gender set zena", "gender set z")]
        [Summary("Nastaví svoje pohlaví jako žena.")]
        public async Task SetGenderFemaleAsync()

        {
            await SetGenderAsync(Gender.Female);

        }

        [Command("gender unset")]
        [Alias("gender set other")]
        [Summary("Vymaže informaci o pohlaví.")]
        public async Task UnsetGenderAsync()

        {
            await SetGenderAsync(Gender.Unspecified);

        }

        public async Task SetGenderAsync(Gender gender)

        {
            await UsersService.SetUserGenderAsync(Context.User, gender);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
