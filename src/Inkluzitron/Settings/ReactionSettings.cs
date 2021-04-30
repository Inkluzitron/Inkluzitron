using Discord;
using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System;

namespace Inkluzitron.Settings
{
    public class ReactionSettings
    {
        public IEmote MoveToFirst { get; }
        public IEmote MoveToPrevious { get; }
        public IEmote MoveToNext { get; }
        public IEmote MoveToLast { get; }

        public IEmote[] PaginationReactions { get; }

        public ReactionSettings(IConfiguration config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            MoveToFirst = config["ReactionSettings:MoveToFirst"].ToDiscordEmote();
            MoveToPrevious = config["ReactionSettings:MoveToPrevious"].ToDiscordEmote();
            MoveToNext = config["ReactionSettings:MoveToNext"].ToDiscordEmote();
            MoveToLast = config["ReactionSettings:MoveToLast"].ToDiscordEmote();

            PaginationReactions = new[] { MoveToFirst, MoveToPrevious, MoveToNext, MoveToLast };
        }        
    }
}
