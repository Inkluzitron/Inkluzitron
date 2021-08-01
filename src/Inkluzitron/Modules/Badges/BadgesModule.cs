using Discord;
using Discord.Commands;
using ImageMagick;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Badges
{
    [Name("Odznaky")]
    [Summary("Zobrazení a správa odznaků za splněné výzvy. Získané odznaky jsou také zobrazeny na profilu a bodech.")]
    public class BadgesModule : ModuleBase
    {
        private ReactionSettings ReactionSettings { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private DatabaseFactory DatabaseFactory { get; }
        private BotDatabaseContext DbContext { get; set; }

        public BadgesModule(DatabaseFactory databaseFactory, IHttpClientFactory httpClientFactory,
            ReactionSettings reactionSettings)
        {
            DatabaseFactory = databaseFactory;
            HttpClientFactory = httpClientFactory;
            ReactionSettings = reactionSettings;
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

        [Command("badge list")]
        [Alias("badges")]
        [Summary("Vypíše všechny odznaky a uživatele, kteří odznak získali.")]
        public async Task ListBadgesAsync()
        {
            var msg = new StringBuilder();

            var badges = DbContext.Badges.AsQueryable().Include(b => b.Users);
            foreach (var badge in badges)
            {
                msg.AppendLine($"\n**{Format.Sanitize(badge.Name)} ({Format.Sanitize(badge.Id)})**");

                if (badge.Description != null)
                {
                    msg.AppendLine($"*{Format.Sanitize(badge.Description)}*");
                }

                if (badge.Users.Count > 0)
                {
                    msg.AppendLine(string.Join(", ", badge.Users.Select(u => Format.Sanitize(u.Name))));
                }
                else
                {
                    msg.AppendLine("*Tento odznak nikdo nezískal*");
                }
            }

            if (msg.Length == 0)
            {
                msg.Append("*Nejsou k dispozici žádné odznaky.*");
            }

            await ReplyAsync(msg.ToString());
        }

        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("badge new")]
        [Alias("badge create")]
        [Summary("Vytvoří nový odznak. Za příkaz je třeba napsat id (bez mezery) a název odznaku. Na další řádek pak volitelně popis. Také je třeba přiložit obrázek odznaku (čtvercový, ideální velikost 64x64px).")]
        public async Task CreateBadgeAsync(string id, [Remainder][Name("název")] string args)
        {
            var splitArgs = args.Split('\n', 2, StringSplitOptions.TrimEntries);

            if (await DbContext.Badges.AsQueryable().AnyAsync(b => b.Id == id))
            {
                await ReplyAsync("Odznak s tímto id již existuje. Pro výpis všech odznaků použij `$badge list`.");
                return;
            }

            var imageAttachment = Context.Message.Attachments.FirstOrDefault();
            if (imageAttachment == null || !imageAttachment.Width.HasValue || imageAttachment.Width.Value != imageAttachment.Height.Value)
            {
                await ReplyAsync("Je třeba ke zprávě připojit obrázek odznaku (v poměru stran 1:1). Více informací v `$help badge`.");
                return;
            }

            using var memoryStream = await HttpClientFactory.CreateClient().GetStreamAsync(imageAttachment.Url);
            using var image = new MagickImage(memoryStream);
            image.Resize(64, 64);

            var badge = new Badge()
            {
                Id = id,
                Name = splitArgs[0],
                Description = splitArgs.Length > 1 ? splitArgs[1] : null,
                Image = image.ToByteArray()
            };

            DbContext.Add(badge);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }


        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("badge add")]
        [Alias("badge assign")]
        [Summary("Přidá odznak zvoleným uživatelům.")]
        public async Task AssignBadgeAsync([Name("id odznaku")] string badgeId, [Name("uživatelé...")] params IUser[] users)
        {
            var badge = await DbContext.Badges.AsQueryable().Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.Id == badgeId);
            if (badge == null)
            {
                await ReplyAsync($"Odznak s id `{Format.Sanitize(badgeId)}` neexistuje. Pro vypsání všech odznaků použij `$badge list`.");
                return;
            }

            foreach (var user in users)
            {
                var dbUser = await DbContext.GetOrCreateUserEntityAsync(user);
                if(!badge.Users.Any(u => u.Id == user.Id))
                    badge.Users.Add(dbUser);
            }

            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }


        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("badge add")]
        [Alias("badge assign")]
        [Summary("Přidá odznak všem uživatelům zvolené role.")]
        public async Task AssignBadgeAsync([Name("id odznaku")] string badgeId, [Name("role")] IRole role)
        {
            await Context.Guild.DownloadUsersAsync();
            var users = Context.Guild.Users.Where(u => u.Roles.Any(r => r.Id == role.Id));

            await AssignBadgeAsync(badgeId, users.ToArray());
        }


        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Command("badge delete")]
        [Alias("badge remove")]
        [Summary("Odstraní odznak.")]
        public async Task DeleteBadgeAsync([Name("id odznaku")] string badgeId)
        {
            var badge = await DbContext.Badges.AsQueryable().Where(b => b.Id == badgeId).FirstOrDefaultAsync();

            if (badge == null)
            {
                await ReplyAsync($"Odznak s id `{Format.Sanitize(badgeId)}` neexistuje. Pro vypsání všech odznaků použij `$badge list`.");
                return;
            }

            DbContext.Remove(badge);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
