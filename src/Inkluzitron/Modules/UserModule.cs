using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Enums.CommandArguments;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
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
        private KisSettings KisSettings { get; }

        public UserModule(IConfiguration configuration, ReactionSettings reactionSettings,
            DatabaseFactory databaseFactory, UsersService usersService, KisSettings kisSettings)
        {
            Configuration = configuration;
            ReactionSettings = reactionSettings;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
            KisSettings = kisSettings;
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

        [Command("about")]
        [Alias("o")]
        [Summary("Zobrazí údaje o uživateli, jako je preferované oslovení, s čím souhlasí a další věci.")]
        public async Task ShowUserInfoAsync([Name("uživatel")] IUser user = null)
        {
            // TODO Show user birthday

            var templateAboutBotSelf = Configuration["UserModule:AboutBotSelf"];
            var templateAboutBotOther = Configuration["UserModule:AboutBotOther"];
            var templateUserNotFound = Configuration["UserModule:UserNotFoundMessage"];

            if (user == null)
                user = Context.User;

            if (user.IsBot)
            {
                await ReplyAsync(string.Format(
                    (user.Id == Context.Client.CurrentUser.Id) ? templateAboutBotSelf : templateAboutBotOther,
                    Format.Sanitize(await UsersService.GetDisplayNameAsync(user))
                ));

                return;
            }

            var userDb = await DbContext.GetUserEntityAsync(user);
            if (userDb == null)
            {
                await ReplyAsync(string.Format(
                    templateUserNotFound,
                    Format.Sanitize(await UsersService.GetDisplayNameAsync(user))
                ));

                return;
            }

            var pronouns = userDb.Pronouns;
            if (pronouns == null)
            {
                if (userDb.Gender == Gender.Male)
                    pronouns = "he/him";
                else if (userDb.Gender == Gender.Female)
                    pronouns = "she/her";
            }

            var descBuilder = new StringBuilder();
            descBuilder.AppendLine(userDb.Gender == Gender.Unspecified ?
                "Nemá preferované oslovení" :
                $"**Oslovení:** {pronouns}, {userDb.Gender.GetDisplayName()}");

            var guildUser = await UsersService.GetUserFromHomeGuild(user);
            if (guildUser != null)
                descBuilder.AppendLine($"**Na serveru od:** {guildUser.JoinedAt.Value.LocalDateTime.ToShortDateString()}");
            else
                descBuilder.AppendLine($"Již není na serveru");

            var embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithCurrentTimestamp()
                .WithDescription(descBuilder.ToString())
                .WithFooter("informace o uživateli");

            EmbedAppendUserConsents(embed, userDb);

            var tests = await GetUserCompletedTests(user.Id);

            if (tests.Length > 0)
                embed.AddField("Vyplněné testy:", string.Join('\n', tests));

            await ReplyAsync(embed: embed.Build());
        }

        private async Task<string[]> GetUserCompletedTests(ulong userId)
        {
            var tests = new List<string>();

            if (await DbContext.BdsmTestOrgResults.AnyAsync(t => t.UserId == userId))
                tests.Add("BDSMTest.org");

            return tests.ToArray();
        }

        static private void EmbedAppendUserConsents(EmbedBuilder embed, User dbUser)
        {
            var userAllow = new List<string>();
            var userDeny = new List<string>();

            var consents = Enum.GetValues<CommandConsent>();
            foreach (var consent in consents)
            {
                var consentDesc = consent.GetAttribute<DisplayAttribute>()?.GetDescription();
                if (consentDesc == null) continue;

                if (dbUser.HasGivenConsentTo(consent))
                    userAllow.Add(consentDesc);
                else
                    userDeny.Add(consentDesc);
            }

            if (userAllow.Count > 0)
                embed.AddField("Souhlasí:", string.Join('\n', userAllow));

            if (userDeny.Count > 0)
                embed.AddField("Nesouhlasí:", string.Join('\n', userDeny));
        }

        [Command("pronouns set")]
        [Alias("osloveni set")]
        [Summary("Nastaví svoje preferované oslovení.")]
        public async Task SetPronounsAsync(Pronoun pronoun)
        {
            var gender = pronoun switch
            {
                Pronoun.He or Pronoun.Him or Pronoun.On => Gender.Male,
                Pronoun.She or Pronoun.Her or Pronoun.Ona => Gender.Female,
                _ => Gender.Unspecified,
            };

            await SetUserGenderAndPronouns(gender);
        }

        [Command("pronouns set custom")]
        [Alias("osloveni set custom", "pronouns set other", "osloveni set other")]
        [Summary("Nastaví vlastní preferované oslovení. Jako druhý argument se uvádí preferované skloňování v textu.")]
        public async Task SetPronounsAsync(string pronoun, GrammaticalGender gender)
        {
            var dbGender = gender switch
            {
                GrammaticalGender.On => Gender.Male,
                GrammaticalGender.Ona => Gender.Female,
                _ => Gender.Unspecified,
            };

            await SetUserGenderAndPronouns(dbGender, pronoun);
        }

        [Command("pronouns unset")]
        [Alias("osloveni unset")]
        [Summary("Odstraní uložené informace o oslovení.")]
        public Task UnsetPronounsAsync()
            => SetUserGenderAndPronouns(Gender.Unspecified);

        private async Task SetUserGenderAndPronouns(Gender gender, string pronouns = null)
        {
            var user = await DbContext.GetOrCreateUserEntityAsync(Context.User);
            user.Gender = gender;
            user.Pronouns = pronouns;
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }

        [Command("duck set")]
        [Alias("kachna set")]
        [Summary("Nastaví přezdívku používanou v kachničce, aby bylo možné stáhnout prestiž za nákupy.")]
        public async Task SetKisNicknameAsync([Remainder][Name("přezdívka")] string nickname)
        {
            if (await DbContext.Users.AnyAsync(o => o.KisNickname == nickname))
            {
                await ReplyAsync(Configuration["Kis:Messages:NonUniqueNick"]);
                return;
            }

            await Patiently.HandleDbConcurrency(async () =>
            {
                var user = await DbContext.GetOrCreateUserEntityAsync(Context.User);

                if (!string.IsNullOrEmpty(user.KisNickname))
                {
                    await ReplyAsync(KisSettings.Messages["AlreadySet"]);
                    return;
                }

                user.KisNickname = nickname;
                await DbContext.SaveChangesAsync();
                await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
            });
        }

        [Command("duck set")]
        [Alias("kachna set")]
        [Summary("Nastaví danému uživateli přezdívku používanou v kachničce.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetKisNicknameAsync(IUser user, [Remainder][Name("přezdívka")] string nickname)
        {
            if (nickname != null && await DbContext.Users.AnyAsync(o => o.KisNickname == nickname))
            {
                await ReplyAsync(KisSettings.Messages["NonUniqueNick"]);
                return;
            }

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await DbContext.GetOrCreateUserEntityAsync(user);

                userEntity.KisNickname = nickname;
                await DbContext.SaveChangesAsync();
                await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
            });
        }

        [Command("duck unset")]
        [Alias("duck clear", "kachna unset", "kachna clear")]
        [Summary("Smaže přezdívku používanou v kachničce.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public Task UnsetKisNicknameAsync(IUser user) => SetKisNicknameAsync(user, null);

        [Command("consent")]
        [Summary("Udělí nebo odvolá souhlas s funkcemi bota. Aktuální stav se zobrazí příkazem $about.")]
        public async Task ChangeConsentAsync(PermissionAction action, ConsentType consent)
        {
            var dbConsent = consent switch
            {
                ConsentType.All => CommandConsent.BdsmImageCommands,
                ConsentType.Bdsm => CommandConsent.BdsmImageCommands,
                _ => CommandConsent.None,
            };

            await UpdateConsentAsync(c => action == PermissionAction.Grant ? (c | dbConsent) : (c & ~dbConsent));
        }

        private async Task UpdateConsentAsync(Func<CommandConsent, CommandConsent> consentUpdaterFunc)
        {
            await DbContext.UpdateCommandConsentAsync(Context.User, consentUpdaterFunc);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
