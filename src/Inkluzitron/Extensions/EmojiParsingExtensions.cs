using Discord;
using System.Globalization;

namespace Inkluzitron.Extensions
{
    public static class EmojiParsingExtensions
    {
        static public IEmote ToDiscordEmote(this string configuredValue)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(configuredValue);
            var hasFirstElement = enumerator.MoveNext();
            var hasSecondElement = enumerator.MoveNext();

            if (hasFirstElement && !hasSecondElement)
                return new Emoji(configuredValue);
            else
                return Emote.Parse(configuredValue);
        }
    }
}
