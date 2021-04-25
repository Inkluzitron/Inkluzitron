using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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

        public RuntimeService(DiscordSocketClient discordClient, IServiceProvider serviceProvider, CommandService commandService,
            IConfiguration configuration)
        {
            DiscordClient = discordClient;
            ServiceProvider = serviceProvider;
            CommandService = commandService;
            Configuration = configuration;
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
