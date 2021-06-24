using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
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

        public RuntimeService(DiscordSocketClient discordClient, IServiceProvider serviceProvider, CommandService commandService,
            IConfiguration configuration, FileCache cache)
        {
            DiscordClient = discordClient;
            ServiceProvider = serviceProvider;
            CommandService = commandService;
            Configuration = configuration;
            Cache = cache;

            DiscordClient.Ready += OnReadyAsync;
        }

        private async Task OnReadyAsync()
        {
            var cacheData = Cache.WithCategory("App")
                .WithUnique("GitVersion")
                .Build();

            string version = null;
            if (cacheData.TryFind(out var path))
            {
                version = File.ReadAllText(path);
            }
            else
            {
                path = cacheData.GetPathForWriting("txt");
                File.WriteAllText(path, ThisAssembly.Git.Sha);
            }

            if (version != ThisAssembly.Git.Sha && DiscordClient.GetChannel(Configuration.GetValue<ulong>("LoggingChannelId")) is IMessageChannel channel)
            {
                var commitDate = DateTime.Parse(ThisAssembly.Git.CommitDate);
                var message = string.Format(Configuration.GetValue<string>("OnlineAfterUpdate"), ThisAssembly.Git.Commit, commitDate.ToString("dd. MM. yyyy hh:MM:ss"));

                await channel.SendMessageAsync(message);
            }
        }

        public async Task StartAsync()
        {
            var token = Configuration["Token"];
            CheckToken(token);

            await DiscordClient.LoginAsync(TokenType.Bot, token);
            await DiscordClient.StartAsync();

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
