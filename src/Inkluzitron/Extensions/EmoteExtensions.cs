using DC = Discord;
using NeoSmart.Unicode;
using System.Linq;

namespace Inkluzitron.Extensions
{
    static public class EmoteExtensions
    {
        /// <summary>
        /// Compares two emotes. Supports unicode.
        /// </summary>
        static public bool IsEqual(this DC.IEmote emote, DC.IEmote another)
        {
            if (emote.GetType() != another.GetType())
                return false;

            if(emote is DC.Emoji emoji)
            {
                var emojiCodepoint = emoji.Name.Codepoints().FirstOrDefault();
                var anotherEmojiCodepoint = another.Name.Codepoints().FirstOrDefault();

                return emojiCodepoint == anotherEmojiCodepoint;
            }

            // In a case of standard emotes.
            return emote.Equals(another);
        }
    }
}
