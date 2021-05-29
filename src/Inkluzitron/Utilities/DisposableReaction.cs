using Discord;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Utilities
{
    public sealed class DisposableReaction : IAsyncDisposable
    {
        public IMessage Message { get; }
        public IEmote Emote { get; }
        public IUser BotUser { get; }

        private DisposableReaction(IMessage message, IEmote emote, IUser botUser)
        {
            Message = message;
            Emote = emote;
            BotUser = botUser;
        }

        static public async Task<DisposableReaction> CreateAsync(IMessage message, IEmote emote, IUser botUser)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (emote == null)
                throw new ArgumentNullException(nameof(emote));
            if (botUser == null)
                throw new ArgumentNullException(nameof(botUser));

            await message.AddReactionAsync(emote);
            return new DisposableReaction(message, emote, botUser);
        }

        private bool _disposed;
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            else
                _disposed = true;

            await Message.RemoveReactionAsync(Emote, BotUser);
        }
    }
}
