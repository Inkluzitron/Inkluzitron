﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [Name("Uživatelsky volitelné role")]
    [Group("rolepicker")]
    public class RolePickerModule : ModuleBase
    {
        private DiscordSocketClient Client { get; }
        private BotDatabaseContext DbContext { get; }
        private ulong RoleEmoteGuildId { get; }

        public RolePickerModule(DiscordSocketClient client, BotDatabaseContext dbContext, IConfiguration config)
        {
            Client = client;
            DbContext = dbContext;
            RoleEmoteGuildId = config.GetValue<ulong>("RoleEmoteGuildId");
        }

        private async Task<RolePickerMessage> GetMessageDataAsync(IUserMessage msg)
        {
            var channel = msg.Channel as ITextChannel;

            return await DbContext.UserRoleMessage.Include(m => m.Roles).FirstOrDefaultAsync(m =>
                m.GuildId == channel.Guild.Id &&
                m.ChannelId == channel.Id &&
                m.MessageId == msg.Id);
        }

        private async Task UpdateMessageAsync(RolePickerMessage data, IUserMessage msg)
        {
            var builder = new StringBuilder($"**\n ~ ~\u2003{data.Title}\u2003~ ~**\n");

            var hasRoles = false;
            foreach (var role in data.Roles)
            {
                hasRoles = true;
                builder.Append($"\n{role.Emote}\u2003{role.Description ?? role.Mention}\n");
            }

            if (!hasRoles) builder.Append("\n*Zpráva neobsahuje žádné role k nakliknutí.*");
            else if (data.CanSelectMultiple) builder.Append("\n*kliknutím na reakce si přiřadíš role*");
            else builder.Append("\n*kliknutím na reakci si přiřadíš jednu roli*");

            await msg.ModifyAsync(m => m.Content = builder.ToString());
        }

        [Command("list")]
        [Summary("Vypíše všechny zprávy pro výběr rolí.")]
        public async Task ListMessagesAsync()
        {
            var replyBuilder = new EmbedBuilder();
            replyBuilder.WithTitle("Seznam zpráv pro výběr rolí");

            var messages = DbContext.UserRoleMessage.AsEnumerable()
                .OrderBy(m => new { m.GuildId, m.ChannelId });

            SocketTextChannel channel = null;
            StringBuilder channelBuilder = null;
            foreach (var data in messages)
            {
                if(channel == null || channel.Id != data.ChannelId || channel.Guild.Id != data.GuildId)
                {
                    if(channelBuilder != null)
                    {
                        replyBuilder.AddField($"`#{channel.Name}`", channelBuilder.ToString());
                    }

                    channel = Client
                        .GetGuild(data.GuildId)
                        ?.GetTextChannel(data.ChannelId);
                    channelBuilder = new StringBuilder();
                }

                var msg = await channel?.GetMessageAsync(data.MessageId);

                if(msg == null)
                {
                    // TODO cleanup broken db entries
                    continue;
                }

                channelBuilder.AppendLine(Format.Url(data.Title, msg.GetJumpUrl()));
            }

            if (channelBuilder != null)
            {
                replyBuilder.AddField($"`#{channel.Name}`", channelBuilder.ToString());
            }
            else
            {
                replyBuilder.WithDescription("Nebyly nalezeny žádné zprávy pro výběr rolí.");
            }

            await ReplyAsync(embed: replyBuilder.Build());
        }

        [Command("new")]
        [Summary("Vytvoří zprávu pro výběr více rolí v aktuálním kanálu.")]
        public async Task NewRoleMessageAsync(
            [Summary("nadpis")][Remainder] string title
        )
        {
            var message = await Context.Channel.SendMessageAsync(title);

            var data = new RolePickerMessage()
            {
                Title = title,
                MessageId = message.Id,
                ChannelId = Context.Channel.Id,
                GuildId = Context.Guild.Id,
                Roles = new List<RolePickerMessageRole>(),
                CanSelectMultiple = true
            };

            await DbContext.UserRoleMessage.AddAsync(data);
            await DbContext.SaveChangesAsync();

            await UpdateMessageAsync(data, message);

            await Context.Message.DeleteAsync();
        }

        [Command("title")]
        [Summary("Změní nadpis výběru rolí. Musí reagovat na cílovou zprávu, kde se přidávají reakce.")]
        public async Task ChangeMessageTitleAsync(
            [Summary("popis")][Remainder] string title
        )
        {
            var msg = Context.Message.ReferencedMessage;
            if (msg == null)
            {
                await ReplyAsync("Příkaz musí reagovat na cílovou zprávu, kde se přidávají reakce.");
                return;
            }

            var data = await GetMessageDataAsync(msg);
            if (data == null)
            {
                await ReplyAsync("Tato zpráva není pro nastavování reakcí.");
                return;
            }

            data.Title = title;

            await DbContext.SaveChangesAsync();

            await UpdateMessageAsync(data, msg);

            await Context.Message.DeleteAsync();
        }

        [Command("exclusive")]
        [Summary("Změní způsob výběru rolí. Přepíná mezi módem výběru jedné nebo více rolí.")]
        public async Task ToggleExclusivityAsync()
        {
            var msg = Context.Message.ReferencedMessage;
            if (msg == null)
            {
                await ReplyAsync("Příkaz musí reagovat na cílovou zprávu, kde se přidávají reakce.");
                return;
            }

            var data = await GetMessageDataAsync(msg);
            if (data == null)
            {
                await ReplyAsync("Tato zpráva není pro nastavování reakcí.");
                return;
            }

            data.CanSelectMultiple = !data.CanSelectMultiple;

            await DbContext.SaveChangesAsync();

            await UpdateMessageAsync(data, msg);

            await Context.Message.DeleteAsync();
        }

        [Command("purge")]
        [Summary("Odstraní zprávu pro výběr rolí. Musí reagovat na cílovou zprávu, kde se přidávají reakce.")]
        public async Task RemoveRoleAsync()
        {
            var msg = Context.Message.ReferencedMessage;
            if (msg == null)
            {
                await ReplyAsync("Příkaz musí reagovat na cílovou zprávu, kde se přidávají reakce.");
                return;
            }

            var data = await GetMessageDataAsync(msg);
            if (data == null)
            {
                await ReplyAsync("Tato zpráva není pro nastavování reakcí.");
                return;
            }

            DbContext.UserRoleMessage.Remove(data);

            await DbContext.SaveChangesAsync();

            await Context.Message.ReferencedMessage.DeleteAsync();
            await Context.Message.DeleteAsync();


            var emoteRolesInUse = await DbContext.UserRoleMessageItem.AsQueryable()
                .Select(i => i.Emote)
                .Where(e => e.Contains("<:role_"))
                .ToArrayAsync();

            var emoteGuild = Client.GetGuild(RoleEmoteGuildId);
            foreach (var emote in emoteGuild.Emotes)
            {
                if (emote.CreatorId == Client.CurrentUser.Id
                    && emote.Name.StartsWith("role_")
                    && !emoteRolesInUse.Contains(emote.ToString()))
                {
                    await emoteGuild.DeleteEmoteAsync(emote);
                }
            }
        }

        private async Task<string> CreateEmoteAsync(IRole role)
        {
            var name = $"role_{role.Id}";

            var emoteGuild = Client.GetGuild(RoleEmoteGuildId);

            var emote = emoteGuild.Emotes.FirstOrDefault(e => e.Name == name);
            if (emote != null) return emote.ToString();

            var memoryStream = new MemoryStream();
            var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillEllipse(
                new SolidBrush(System.Drawing.Color.FromArgb(
                    role.Color.R, role.Color.G, role.Color.B)),
                0, 0, bitmap.Width, bitmap.Height);

            //bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Save("temp.png");

            var file = File.OpenRead("temp.png");
            var image = new Discord.Image(file);
            try
            {
                emote = await emoteGuild.CreateEmoteAsync(name, image);
                return emote.ToString();
            }
            catch(Exception)
            {
                return null;
            }
            finally
            {
                file.Close();
            }
        }

        [Command("add")]
        [Summary("Přidá roli do zprávy pro uživatelsky volitelné role. Musí reagovat na cílovou zprávu, kde se přidávají reakce.")]
        public async Task AddRoleAsync(
            [Summary("role")] IRole role,
            [Summary("emote")] string emote = null,
            [Summary("popis")][Remainder] string desc = null
        )
        {
            var msg = Context.Message.ReferencedMessage;
            if (msg == null)
            {
                await ReplyAsync("Příkaz musí reagovat na cílovou zprávu, kde se přidávají reakce.");
                return;
            }

            var data = await GetMessageDataAsync(msg);
            if (data == null)
            {
                await ReplyAsync("Tato zpráva není pro nastavování reakcí.");
                return;
            }

            if (data.Roles.Any(r => r.Id == role.Id && r.Message == data))
            {
                await ReplyAsync($"Tato zpráva již obsahuje roli {role.Mention}");
                return;
            }

            if (emote == null) emote = await CreateEmoteAsync(role);
            if (emote == null)
            {
                await ReplyAsync("Nepodařilo se vytvořit emote pro roli. Je nutné zadat emote.");
                return;
            }

            if (data.Roles.Any(r => r.Emote == emote && r.Message == data))
            {
                await ReplyAsync($"Tato zpráva již obsahuje emote {emote}");
                return;
            }

            data.Roles.Add(new RolePickerMessageRole()
            {
                Id = role.Id,
                Mention = role.Mention,
                Description = desc,
                Emote = emote
            });

            await DbContext.SaveChangesAsync();

            await UpdateMessageAsync(data, msg);

            await msg.AddReactionAsync(
                emote[0].Equals('<') ? Emote.Parse(emote) : new Emoji(emote));

            await Context.Message.DeleteAsync();
        }

        [Command("remove")]
        [Summary("Odebere roli ze zprávy pro uživatelsky volitelné role. Musí reagovat na cílovou zprávu, kde se přidávají reakce.")]
        public async Task RemoveRoleAsync(
            [Summary("role")] IRole role
        )
        {
            var msg = Context.Message.ReferencedMessage;
            if (msg == null)
            {
                await ReplyAsync("Příkaz musí reagovat na cílovou zprávu, kde se přidávají reakce.");
                return;
            }

            var data = await GetMessageDataAsync(msg);
            if (data == null)
            {
                await ReplyAsync("Tato zpráva není pro nastavování reakcí.");
                return;
            }

            var dbRole = data.Roles.FirstOrDefault(r => r.Id == role.Id && r.Message == data);
            if (role == null)
            {
                await ReplyAsync($"Tato zpráva neobsahuje roli {role.Mention}");
                return;
            }

            var emote = dbRole.Emote;
            data.Roles.Remove(dbRole);
            await DbContext.SaveChangesAsync();

            await msg.RemoveAllReactionsForEmoteAsync(
                emote[0].Equals('<') ? Emote.Parse(emote) : new Emoji(emote));

            // remove special role emote if it's not used elsewhere
            if (emote.Contains($"role_{role.Id}"))
            {
                if (!DbContext.UserRoleMessageItem.Any(r => r.Emote == emote))
                {
                    var emoteGuild = Client.GetGuild(RoleEmoteGuildId);
                    var deleted = emoteGuild.Emotes.FirstOrDefault(e => e.ToString() == emote && e.CreatorId == Client.CurrentUser.Id);
                    if (deleted != null) await emoteGuild.DeleteEmoteAsync(deleted);
                }
            }

            await UpdateMessageAsync(data, msg);

            await Context.Message.DeleteAsync();
        }
    }
}
