using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Nastavení uživatele")]
    [Summary("Nastavení oslovení se používá pro správný výpis hlášek bota. Oslovení je možné nastavit pomocí příkazů níže, nebo se nastaví automaticky po vložení BDSM testu.")]
    public class UserModule : ModuleBase
    {
        private ReactionSettings ReactionSettings { get; }
        private IConfiguration Configuration { get; }
        private DatabaseFactory DatabaseFactory { get; }
        private BotDatabaseContext DbContext { get; set; }
        private UsersService UsersService { get; }

        public UserModule(IConfiguration configuration, ReactionSettings reactionSettings,
            DatabaseFactory databaseFactory, UsersService usersService)
        {
            Configuration = configuration;
            ReactionSettings = reactionSettings;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
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

        [Command("pronouns")]
        [Alias("osloveni")]
        [Summary("Vypíše svoje preferované oslovení nebo oslovení vybraného uživatele.")]
        public async Task ShowGenderAsync([Name("uživatel")] IUser user = null)
        {
            var genderMsg = Configuration["UserModule:UserPronounsMessage"];
            var notFoundMsg = Configuration["UserModule:UserNotFoundMessage"];

            if (user == null) user = Context.User;

            if (user.IsBot)
            {
                if (user.Id == Context.Client.CurrentUser.Id)
                {
                    await ReplyAsync(string.Format(genderMsg, Format.Sanitize(await UsersService.GetDisplayNameAsync(user)), Configuration["UserModule:UserPronounsBotSelf"]));
                    return;
                }

                await ReplyAsync(string.Format(genderMsg, Format.Sanitize(await UsersService.GetDisplayNameAsync(user)), "je bot a nemá preferované oslovení."));
                return;
            }

            var userDb = await DbContext.GetUserEntityAsync(user);
            if (userDb == null)
            {
                await ReplyAsync(string.Format(notFoundMsg, Format.Sanitize(await UsersService.GetDisplayNameAsync(user))));
                return;
            }

            var gender = userDb.Gender == Gender.Unspecified ?
                "nemá preferované oslovení." :
                $"je {userDb.Gender.GetDisplayName()}.";

            await ReplyAsync(string.Format(genderMsg, Format.Sanitize(await UsersService.GetDisplayNameAsync(user)), gender));
        }

        [Command("pronouns set he")]
        [Alias("pronouns set him", "pronouns set on", "osloveni set he", "osloveni set him", "osloveni set on")]
        [Summary("Nastaví svoje preferované oslovení jako mužské - he/him, on.")]
        public Task SetGenderMaleAsync()
            => SetGenderAsync(Gender.Male);

        [Command("pronouns set she")]
        [Alias("pronouns set her", "pronouns set ona", "osloveni set she", "osloveni set her", "osloveni set ona")]
        [Summary("Nastaví svoje preferované oslovení jako ženské - she/her, ona.")]
        public Task SetGenderFemaleAsync()
            => SetGenderAsync(Gender.Female);

        [Command("pronouns unset")]
        [Alias("pronouns set other", "osloveni unset", "osloveni set other")]
        [Summary("Nastaví neutrální oslovení.")]
        public Task UnsetGenderAsync()
            => SetGenderAsync(Gender.Unspecified);

        [Command("duck set")]
        [Summary("Nastaví přezdívku používanou v kachničce, aby bylo možné stáhnout prestiž za nákupy.")]
        public Task SetKisNicknameAsync([Remainder][Name("prezdivka")] string nickname)
            => UpdateKisNicknameAsync(nickname);

        [Command("duck unset")]
        [Alias("duck clear")]
        [Summary("Smaže přezdívku používanou v kachničce.")]
        public Task UnsetKisNicknameAsync() => UpdateKisNicknameAsync(null);

        public async Task SetGenderAsync(Gender gender)
        {
            var user = await DbContext.GetOrCreateUserEntityAsync(Context.User);
            user.Gender = gender;
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }

        private async Task UpdateKisNicknameAsync(string nickname)
        {
            if (!string.IsNullOrEmpty(nickname) && await DbContext.Users.AnyAsync(o => o.KisNickname == nickname))
            {
                await ReplyAsync(Configuration["Kis:Messages:NonUniqueNick"]);
                return;
            }

            await Patiently.HandleDbConcurrency(async () =>
            {
                var user = await DbContext.GetOrCreateUserEntityAsync(Context.User);

                user.KisNickname = nickname;
                await DbContext.SaveChangesAsync();
            });


            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
