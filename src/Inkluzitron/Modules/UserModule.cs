using Discord;
using Discord.Commands;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Nastavení uživatele")]
    [Summary("Nastavení oslovení se používá pro správný výpis hlášek bota. Oslovení je možné nastavit pomocí příkazů níže, nebo se nastaví automaticky po vložení BDSM testu.")]
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

        [Command("pronouns")]
        [Alias("osloveni")]
        [Summary("Vypíše svoje preferované oslovení nebo oslovení vybraného uživatele.")]
        public async Task ShowGenderAsync(IUser user = null)
        {
            var genderMsg = Configuration["UserModule:UserPronounsMessage"];
            var notFoundMsg = Configuration["UserModule:UserNotFoundMessage"];

            if (user == null) user = Context.User;

            if (user.IsBot)
            {
                if(user.Id == Context.Client.CurrentUser.Id)
                {
                    await ReplyAsync(string.Format(genderMsg, user.GetDisplayName(), Configuration["UserModule:UserPronounsBotSelf"]));
                    return;
                }

                await ReplyAsync(string.Format(genderMsg, user.GetDisplayName(), "je bot a nemá preferované oslovení."));
                return;
            }

            var userDb = await UsersService.GetUserDbEntityAsync(user);
            if (userDb == null)
            {
                await ReplyAsync(string.Format(notFoundMsg, user.GetDisplayName()));
                return;
            }

            var gender = userDb.Gender == Gender.Unspecified ?
                "nemá preferované oslovení." :
                $"je {userDb.Gender.GetDisplayName()}.";

            await ReplyAsync(string.Format(genderMsg, user.GetDisplayName(), gender));
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

        public async Task SetGenderAsync(Gender gender)
        {
            await UsersService.SetUserGenderAsync(Context.User, gender);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
