using Discord;

namespace Inkluzitron.Extensions
{
    static public class EmojiParsingExtensions
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
