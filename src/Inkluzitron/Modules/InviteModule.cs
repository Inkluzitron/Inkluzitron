using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    [Name("Invites")]
    [Group("invite")]
    [Summary("Vytvoří jednorázový invite link, nebo zobrazí kým byl uživatel pozván, popř. seznam lidí, které daný uživatel pozval, a počet nevyužitých pozvánek.")]
    public class InviteModule : ModuleBase
    {
        private IConfiguration Config { get; }

        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private UsersService UsersService { get; }
        private ReactionSettings ReactionSettings { get; }
        private BotSettings BotSettings { get; }

        public InviteModule(IConfiguration config,
            DatabaseFactory databaseFactory,
            UsersService usersService,
            ReactionSettings reactionSettings,
            BotSettings botSettings)
        {
            Config = config;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
            ReactionSettings = reactionSettings;
            BotSettings = botSettings;
        }

        override protected void BeforeExecute(CommandInfo command)
        {
            DbContext = DatabaseFactory.Create();
            base.BeforeExecute(command);
        }

        override protected void AfterExecute(CommandInfo command)
        {
            DbContext?.Dispose();
            base.AfterExecute(command);
        }

        [Command("new")]
        [Alias("create")]
        [Summary("Vytvoří jednorázový invite link a pošle ho do DM.")]
        [RequireBotPermission(GuildPermission.CreateInstantInvite)]
        public async Task CreateInviteAsync()
        {
            if (Context.Message.Author is IGuildUser guildUser && guildUser.RoleIds.Contains(BotSettings.NewbieRoleId))
            {
                await ReplyAsync(BotSettings.NewbieInviteMessage);
                return;
            }

            var invite = await Context.Guild.DefaultChannel.CreateInviteAsync(
                maxUses: 2, // create 2 uses for invite because if only 1 usage is available it's automatically deleted after joining
                isUnique: true,
                options: new RequestOptions()
                {
                    AuditLogReason = $"User with ID {Context.User.Id} and name {Context.User.Username} wants to invite someone!"
                });

            var user = await DbContext.GetOrCreateUserEntityAsync(
                Context.Message.Author);

            var inviteDb = new Invite
            {
                GeneratedAt = DateTime.Now,
                GeneratedByUserId = user.Id,
                InviteCode = invite.Url.Split('/').Last(),
            };

            await DbContext.Invites.AddAsync(inviteDb);
            await DbContext.SaveChangesAsync();

            await Context.User.SendMessageAsync(
                $"Byl ti vygenerován následující invite link na server **{Context.Guild.Name}**:\n{invite.Url}\n\nTento invite link můžeš poslat osobě, kterou chceš na server pozvat.\n\n**Informace:**\n • Pozvánka platí pouze na jedno použití\n • Pro pozvání více osob tedy musíš vygenerovat pozvánku pro každou osobu\n • Po připojení nové osoby se uloží informace o tom, kdo ji pozval");
            await ReplyAsync(
                $"Do DM jsem ti poslal vygenerovaný invite link. {Config["CrackTippingEmote"]}");
        }

        [Command("blame")]
        [Summary("Zjistí kým byl odesílatel nebo zadaný uživatel pozván.")]
        public async Task BlameInviterAsync([Name("kdo")] IUser target = null)
        {
            target ??= Context.User;

            var invite = await DbContext.Invites
                .Include(i => i.GeneratedBy)
                .Include(i => i.UsedBy)
                .Where(x => x.UsedByUserId == target.Id).FirstOrDefaultAsync();

            var inviteeName = await UsersService.GetDisplayNameAsync(target);

            if (invite == null)
            {
                await ReplyAsync(
                    string.Format(
                        "Bohužel nevím kým {1:byl:byla:byl/a} {0} {1:pozván:pozvána:pozván/a}. Více se dozvíš příkazem `$user`__`jméno`__.",
                        Format.Sanitize(inviteeName),
                        new FormatByValue((await DbContext.GetUserEntityAsync(target))?.Gender ?? Gender.Unspecified)
                    )
                );

                return;
            }

            var inviterName = await UsersService.GetDisplayNameAsync(invite.GeneratedByUserId);

            var message = string.Format(
                "**{0}** {1:byl pozván:byla pozvána:byl/a pozván/a} uživatelem **{2}**. Více se dozvíš příkazem `$user`__`jméno`__.",
                Format.Sanitize(inviteeName),
                new FormatByValue(invite.UsedBy.Gender),
                Format.Sanitize(inviterName)
            );

            await ReplyAsync(message);
        }

        [Command("")]
        [Summary("Rozcestník, jak lze invite modul používat.")]
        public Task GetInviteStatisticsForUserAsync()
             => ReplyAsync("Novou pozvánku vytvoříš příkazem `$invite new`.\nTaky můžeš pomocí `$user`__`jméno`__ zjistit, kdo byl kým pozván a koho dalšího pozval.");

        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("set")]
        [Summary("Nastaví manuální vazbu mezi uživateli, ale jen pokud pozvaný nemá záznam v db.\n*Jen pro moderátory*")]
        public async Task CreateManualConnectionAsync([Name("pozvaný (kdo)")] IUser invitee, [Name("zvoucí (kým)")] IUser inviter)
        {
            if (await DbContext.Invites.AsQueryable().AnyAsync(i => i.UsedByUserId == invitee.Id))
            {
                await ReplyAsync($"{await UsersService.GetDisplayNameAsync(invitee)} již má záznam kým byl pozván.");
                return;
            }

            var inviteDb = new Invite
            {
                GeneratedBy = await DbContext.GetOrCreateUserEntityAsync(inviter),
                UsedBy = await DbContext.GetOrCreateUserEntityAsync(invitee)
            };

            await DbContext.Invites.AddAsync(inviteDb);
            await DbContext.SaveChangesAsync();

            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
