using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Services.TypeReaders;
using Inkluzitron.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class RuntimeService
    {
        private DiscordSocketClient DiscordClient { get; }
        private IServiceProvider ServiceProvider { get; }
        private CommandService CommandService { get; }
        private IConfiguration Configuration { get; }
        private IServiceScope CommandServiceScope { get; set; }
        private FileCache Cache { get; }
        private bool GuildReadyStateAnnounced { get; set; }

        public RuntimeService(DiscordSocketClient discordClient, IServiceProvider serviceProvider, CommandService commandService,
            IConfiguration configuration, FileCache cache)
        {
            DiscordClient = discordClient;
            ServiceProvider = serviceProvider;
            CommandService = commandService;
            Configuration = configuration;
            Cache = cache;

            DiscordClient.Ready += OnReadyAsync;
            DiscordClient.MessageUpdated += OnMessageUpdatedAsync;
            DiscordClient.MessageDeleted += OnMessageDeletedAsync;
            DiscordClient.MessagesBulkDeleted += OnMessagesBulkDeletedAsync;
            DiscordClient.GuildAvailable += OnGuildAvailableAsync;
        }
        
        private async Task OnGuildAvailableAsync(SocketGuild arg)
        {
            if (arg.Id.ToString() != Configuration["HomeGuildId"])
                return;
            else if (GuildReadyStateAnnounced)
                return;

            GuildReadyStateAnnounced = true;

            foreach (var runtimeEventHandler in ServiceProvider.GetServices<IRuntimeEventHandler>())
                await runtimeEventHandler.OnHomeGuildReadyAsync();
        }

        private async Task OnMessageUpdatedAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            var freshMessageFactory = new Lazy<Task<IMessage>>(() => channel.GetMessageAsync(newMessage.Id));

            foreach (var messageEventHandler in ServiceProvider.GetServices<IMessageEventHandler>())
            {
                var handled = await messageEventHandler.HandleMessageUpdatedAsync(channel, newMessage, freshMessageFactory);
                if (handled)
                    break;
            }
        }

        private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> deletedMessage, ISocketMessageChannel channel)
        {
            foreach (var messageEventHandler in ServiceProvider.GetServices<IMessageEventHandler>())
            {
                var handled = await messageEventHandler.HandleMessageDeletedAsync(channel, deletedMessage.Id);
                if (handled)
                    break;
            }
        }

        private async Task OnMessagesBulkDeletedAsync(IReadOnlyCollection<Cacheable<IMessage, ulong>> deletedMessages, ISocketMessageChannel channel)
        {
            var messageIds = deletedMessages.Select(msg => msg.Id).ToList();

            foreach (var messageEventHandler in ServiceProvider.GetServices<IMessageEventHandler>())
            {
                var handled = await messageEventHandler.HandleMessagesBulkDeletedAsync(channel, messageIds);
                if (handled)
                    break;
            }
        }

        private async Task OnReadyAsync()
        {
            var cacheData = Cache.WithCategory("App")
                .WithUnique("GitVersion")
                .Build();

            string version = null;
            if (cacheData.TryFind(out var path))
            {
                version = await File.ReadAllTextAsync(path);
            }
            else
            {
                path = cacheData.GetPathForWriting("txt");
                await File.WriteAllTextAsync(path, ThisAssembly.Git.Sha);
            }

            if (version != ThisAssembly.Git.Sha && DiscordClient.GetChannel(Configuration.GetValue<ulong>("LoggingChannelId")) is IMessageChannel channel)
            {
                var commitDate = DateTime.Parse(ThisAssembly.Git.CommitDate);
                var message = string.Format(Configuration.GetValue<string>("OnlineAfterUpdate"), ThisAssembly.Git.Commit, commitDate);

                await channel.SendMessageAsync(message);
                await File.WriteAllTextAsync(cacheData.GetPathForWriting("txt"), ThisAssembly.Git.Sha);
            }
        }

        public async Task StartAsync()
        {
            var token = Configuration["Token"];
            CheckToken(token);

            await DiscordClient.LoginAsync(TokenType.Bot, token);
            await DiscordClient.StartAsync();

            CommandService.AddTypeReader<Guid>(new GuidTypeReader());
            CommandService.AddTypeReader<IMessage>(new MessageTypeReader(), true);
            CommandService.AddTypeReader<IEmote>(new EmotesTypeReader());
            CommandService.AddTypeReader<IUser>(new UserTypeReader(), true);
            CommandService.AddTypeReader<DateTime>(new DateTimeTypeReader(), true);
            CommandService.AddTypeReader<bool>(new BooleanTypeReader(), true);
            CommandService.AddTypeReader<TimeSpan>(new TimeSpanTypeReader(), true);

            CommandServiceScope = ServiceProvider.CreateScope();
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), CommandServiceScope.ServiceProvider);
        }

        static private void CheckToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Missing token. Please fill configuration.");
        }

        public async Task StopAsync()
        {
            await DiscordClient.StopAsync();
            await DiscordClient.LogoutAsync();
            CommandServiceScope?.Dispose();
            CommandServiceScope = null;
        }
    }
}
