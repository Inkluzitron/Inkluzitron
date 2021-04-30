using Discord;
using System.Globalization;

namespace Inkluzitron.Extensions
{
    public static class EmojiParsingExtensions
    {
        static public IEmote ToDiscordEmote(this string configuredValue)
        {
            if (Emote.TryParse(configuredValue, out var emote))
                return emote;
            else
                return new Emoji(configuredValue);
        }
    }
}
