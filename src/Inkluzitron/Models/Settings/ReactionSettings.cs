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
        public IEmote PointLeft { get; }
        public IEmote PointRight { get; }
        public IEmote Superlike { get; }
        public IEmote Blobshhh { get; }

        public IEmote[] PaginationReactions { get; }
        public IEmote[] PaginationReactionsWithRemoval { get; }
        public IEmote[] TinderReactions { get; }

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

            PointLeft = config["ReactionSettings:PointLeft"].ToDiscordEmote();
            PointRight = config["ReactionSettings:PointRight"].ToDiscordEmote();
            Superlike = config["ReactionSettings:Superlike"].ToDiscordEmote();
            Blobshhh = config["ReactionSettings:Blobshhh"].ToDiscordEmote();

            PaginationReactions = new[] { MoveToFirst, MoveToPrevious, MoveToNext, MoveToLast };
            PaginationReactionsWithRemoval = new[] { MoveToFirst, MoveToPrevious, Remove, MoveToNext, MoveToLast };
            TinderReactions = new[] { PointLeft, Superlike, PointRight, Blobshhh};
        }
    }
}
