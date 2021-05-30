using Discord;
using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System;

namespace Inkluzitron.Models.Settings
{
    public class ReactionSettings
    {
        public IEmote MoveToFirst { get; }
        public IEmote MoveToPrevious { get; }
        public IEmote MoveToNext { get; }
        public IEmote MoveToLast { get; }
        public IEmote Remove { get; }
        public IEmote BdsmTestResultAdded { get; }
        public IEmote Shrunk { get; }
        public IEmote Loading { get; }
        public IEmote Checkmark { get; }

        public IEmote[] PaginationReactions { get; }
        public IEmote[] PaginationReactionsWithRemoval { get; }

        public ReactionSettings(IConfiguration config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            MoveToFirst = config["ReactionSettings:MoveToFirst"].ToDiscordEmote();
            MoveToPrevious = config["ReactionSettings:MoveToPrevious"].ToDiscordEmote();
            MoveToNext = config["ReactionSettings:MoveToNext"].ToDiscordEmote();
            MoveToLast = config["ReactionSettings:MoveToLast"].ToDiscordEmote();
            Remove = config["ReactionSettings:Remove"].ToDiscordEmote();

            BdsmTestResultAdded = config["ReactionSettings:BdsmTestResultAdded"].ToDiscordEmote();
            Shrunk = config["ReactionSettings:Shrunk"].ToDiscordEmote();
            Loading = config["ReactionSettings:Loading"].ToDiscordEmote();
            Checkmark = config["ReactionSettings:Checkmark"].ToDiscordEmote();

            PaginationReactions = new[] { MoveToFirst, MoveToPrevious, MoveToNext, MoveToLast };
            PaginationReactionsWithRemoval = new[] { MoveToFirst, MoveToPrevious, Remove, MoveToNext, MoveToLast };
        }
    }
}
