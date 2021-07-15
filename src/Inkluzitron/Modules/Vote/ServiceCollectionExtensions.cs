using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Inkluzitron.Modules.Vote
{
    static public class ServiceCollectionExtensions
    {
        static public IServiceCollection AddVoteModule(this IServiceCollection services)
            => services.AddSingletonWithInterface<EndOfVotingScheduledTaskHandler, IScheduledTaskHandler>()
                .AddSingleton<VoteDefinitionParser>()
                .AddSingleton<VoteService>()
                .AddSingleton<VoteTranslations>()
                .AddSingletonWithInterface<VoteMessageEventHandler, IMessageEventHandler>();
    }
}
