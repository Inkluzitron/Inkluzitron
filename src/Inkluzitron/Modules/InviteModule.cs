using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    [Name("Invites")]
    [Group("invite")]
    [Summary("Vytvoří jednorázový invite link, nebo zobrazí kým byl uživatel pozván, popř. seznam lidí, které daný uživatel pozval, a počet nevyužitých pozvánek.")]
    public class InviteModule : ModuleBase
    {
        private IConfiguration Config { get; }
        private DiscordSocketClient DiscordSocketClient { get; }

        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private UsersService UsersService { get; }

        public InviteModule(IConfiguration config,
            DatabaseFactory databaseFactory,
            UsersService usersService,
            DiscordSocketClient discordSocketClient)
        {
            Config = config;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
            DiscordSocketClient = discordSocketClient;
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
            var invite = await Context.Guild.DefaultChannel.CreateInviteAsync(
                null,
                2, // create 2 uses for invite because if only 1 usage is available it's automatically deleted after joining
                false,
                true,
                new RequestOptions()
                {
                    AuditLogReason = $"User with ID {Context.User.Id} and name {Context.User.Username} wants to invite someone!"
                });

            var user = await DbContext.GetOrCreateUserEntityAsync(
                Context.Message.Author);

            var inviteDb = new Invite
            {
                GeneratedAt = DateTime.Now,
                GeneratedByUserId = user.Id,
                InviteLink = invite.Url,
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

            if (invite == null)
            {
                await ReplyAsync(
                    "Tento uživatel byl s největší pravděpodobností pozván před vznikem této fíčury.");
                return;
            }

            var inviteeName = await UsersService.GetDisplayNameAsync(target);
            var inviterName = await UsersService.GetDisplayNameAsync(invite.GeneratedByUserId);

            var message = $"Uživatel ***{Format.Sanitize(inviteeName)}*** byl pozván uživatelem ***{Format.Sanitize(inviterName)}***";

            await ReplyAsync(message);
        }

        [Command("")]
        [Summary("Vypíše počet uživatelů, kteří byli daným uživatelem pozvání, a počet nevyužitých pozvánek.")]
        public async Task GetInviteStatisticsForUser([Name("kdo")] IUser target = null)
        {
            target ??= Context.User;

            var pendingInvites = DbContext.Invites
                .Count(x => x.UsedByUserId == null && x.GeneratedByUserId == target.Id);

            var invitedPeople = DbContext.Invites
                .Include(x => x.UsedBy)
                .Where(x => x.UsedByUserId != null && x.GeneratedByUserId == target.Id)
                .Select(x => x.UsedBy.Name).Distinct()
                .AsQueryable();


            var name = await UsersService.GetDisplayNameAsync(target.Id);
            await ReplyAsync(
                $"Informace o invitech pro uživatele ***{name}:***\n_Počet nevyužitých pozvánek:_ {pendingInvites}\n_Seznam pozvaných lidí:_ {string.Join(", ", invitedPeople)}");
        }
    }
}
