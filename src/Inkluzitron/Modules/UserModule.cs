using Discord;
using Discord.Commands;
using ImageMagick;
using Discord.Rest;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

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
        private BirthdayNotificationService BirthdayService { get; }
        private BirthdaySettings BirthdaySettings { get; }

        public UserModule(IConfiguration configuration, ReactionSettings reactionSettings,
            DatabaseFactory databaseFactory, UsersService usersService, KisSettings kisSettings,
            BirthdayNotificationService birthdayService, BirthdaySettings birthdaySettings)
        {
            Configuration = configuration;
            ReactionSettings = reactionSettings;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
            KisSettings = kisSettings;
            BirthdayService = birthdayService;
            BirthdaySettings = birthdaySettings;
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
        [Alias("o", "whois", "user")]
        [Summary("Zobrazí údaje o uživateli, jako je preferované oslovení, s čím souhlasí a další věci.")]
        public async Task ShowUserInfoAsync([Name("uživatel")] IUser user = null)
        {
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

            var userDb = await DbContext.GetUserEntityAsync(user, u => u.Include(u => u.Badges));
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
                descBuilder.AppendLine($"**V Bobánkově od:** {guildUser.JoinedAt.Value.LocalDateTime:d. M. yyyy}");
            else
                descBuilder.AppendLine("Již není v Bobánkově");

            var inviteUsed = await DbContext.Invites.AsQueryable()
                .Where(i => i.UsedByUserId == user.Id)
                .OrderByDescending(i => i.GeneratedAt)
                .Select(i => i.GeneratedByUserId)
                .FirstOrDefaultAsync();

            var invitedBy = await UsersService.GetDisplayNameAsync(inviteUsed);

            if (invitedBy != null)
                descBuilder.AppendLine($"**Pozván od:** {invitedBy}");

            var embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithCurrentTimestamp()
                .WithDescription(descBuilder.ToString())
                .WithFooter("informace o uživateli")
                .WithThumbnailUrl("attachment://badges.png");

            if (userDb.BirthdayDate is DateTime birthday)
                embed.AddField(
                    "Narozeniny:",
                    birthday.ToString("M")
                );

            var invitedUsersIds = DbContext.Invites.AsQueryable()
                    .Where(i => i.GeneratedByUserId == user.Id && i.UsedByUserId.HasValue)
                    .Select(i => i.UsedByUserId)
                    .Distinct();

            var invitedUsersList = new List<string>();
            foreach (var invitedUserId in invitedUsersIds)
            {
                var invitedUser = await UsersService.GetDisplayNameAsync(invitedUserId.Value);
                if (invitedUser != null)
                    invitedUsersList.Add(invitedUser);
            }

            if(invitedUsersList.Count > 0)
                embed.AddField("Pozval:", string.Join(", ", invitedUsersList));

            EmbedAppendUserConsents(embed, userDb);

            var tests = await GetUserCompletedTests(user.Id);

            if (tests.Length > 0)
                embed.AddField("Vyplněné testy:", string.Join('\n', tests));

            if (!userDb.HasGivenConsentTo(CommandConsent.ShowBadges))
            {
                await ReplyAsync(embed: embed.Build());
                return;
            }

            using var badgesImage = GetBadgesAsSingleImage(userDb.Badges);
            var badges = userDb.Badges.Select(b => Format.Sanitize(b.Name)).ToArray();
            if (badges.Length > 0)
                embed.AddField("Získané odznaky:", string.Join(", ", badges));

            await ReplyFileAsync(badgesImage, "badges.png", embed: embed.Build());
        }

        private MemoryStream GetBadgesAsSingleImage(ICollection<Badge> badges)
        {
            var margin = 2;
            var badgeSize = 24;

            var gridCount = 2;
            if (badges.Count > 4)
                gridCount = 3;
            else if (badges.Count > 9)
                gridCount = 4;
            else if (badges.Count > 16)
                gridCount = 5;

            using var image = new MagickImage(
                MagickColors.Transparent,
                (badgeSize + margin) * gridCount - margin,
                (badgeSize + margin) * gridCount - margin
            );

            var index = 0;
            foreach (var badge in badges)
            {
                using var badgeImage = new MagickImage(badge.Image);
                badgeImage.Resize(badgeSize, badgeSize);
                image.Composite(
                    badgeImage,
                    image.Width - badgeSize - (badgeSize + margin) * (index % gridCount),
                    (badgeSize + margin) * (index / gridCount),
                    CompositeOperator.Over
                );

                index++;
                if (index >= gridCount * gridCount)
                    break;
            }

            var stream = new MemoryStream();
            image.Write(stream, MagickFormat.Png);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private async Task<string[]> GetUserCompletedTests(ulong userId)
        {
            var tests = new List<string>();

            if (await DbContext.BdsmTestOrgResults.AsQueryable().AnyAsync(t => t.UserId == userId))
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
            if (await DbContext.Users.AsQueryable().AnyAsync(o => o.KisNickname == nickname))
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
            if (await DbContext.Users.AsQueryable().AnyAsync(o => o.KisNickname == nickname))
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
                ConsentType.All => CommandConsent.All,
                ConsentType.Bdsm => CommandConsent.BdsmImageCommands,
                ConsentType.Badge => CommandConsent.ShowBadges,
                _ => CommandConsent.None,
            };

            await UpdateConsentAsync(c => action == PermissionAction.Grant ? (c | dbConsent) : (c & ~dbConsent));
        }

        private async Task UpdateConsentAsync(Func<CommandConsent, CommandConsent> consentUpdaterFunc)
        {
            await DbContext.UpdateCommandConsentAsync(Context.User, consentUpdaterFunc);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }

        [Command("birthday")]
        [Alias("narozeniny")]
        [Summary("Vypíše seznam členů serveru, kteří dnes mají narozeniny.")]
        public async Task ShowBirthdaysAsync()
        {
            var embed = await BirthdayService.ComposeBirthdaysEmbedAsync(Context.Guild, false);
            await ReplyAsync(embed: embed);
        }

        [Command("birthday unset")]
        [Alias("narozeniny")]
        [Summary("Vymaže nastavené datum narozenin.")]
        public Task UnsetOwnBirthdayAsync() => SetOwnBirthdayImplAsync(null);

        [Command("birthday")]
        [Alias("narozeniny")]
        [Summary("Nastaví vlastní datum narozenin.")]
        public Task SetOwnBirthdayAsync([Name("DD.MM. nebo DD.MM.YYYY"), Remainder] string birthday)
        {
            var culture = CultureInfo.InvariantCulture;
            var styles = DateTimeStyles.AssumeLocal;

            birthday = Regex.Replace(birthday, "\\s+", string.Empty);
            DateTime birthdayDate;

            if (DateTime.TryParseExact(birthday, "d'.'M'.'", culture, styles, out birthdayDate)) {
                birthdayDate = new DateTime(DateTime.UnixEpoch.Year, birthdayDate.Month, birthdayDate.Day, 0, 0, 0, DateTimeKind.Utc);
                return SetOwnBirthdayImplAsync(birthdayDate);
            }

            if (DateTime.TryParseExact(birthday, "d'.'M'.'yyyy", culture, styles, out birthdayDate))
                return SetOwnBirthdayImplAsync(birthdayDate);

            return ReplyAsync(BirthdaySettings.UnrecognizedBirthdayDateFormatMessage);
        }

        private async Task SetOwnBirthdayImplAsync(DateTime? birthdayDate)
        {
            using var context = DatabaseFactory.Create();
            var user = Context.User;

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await DbContext.GetOrCreateUserEntityAsync(user);

                userEntity.BirthdayDate = birthdayDate;
                await DbContext.SaveChangesAsync();
                await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
            });
        }
    }
}
