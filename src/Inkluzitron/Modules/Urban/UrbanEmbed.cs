using Discord;
using Inkluzitron.Extensions;
using Inkluzitron.Models.UrbanApi;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Inkluzitron.Modules.Urban
{
    public class UrbanEmbed : EmbedBuilder
    {
        static private string CreateLinks(string text)
        {
            return Regex.Replace(text,
                @"\[(.*?)\]",
                m =>
                {
                    var found = m.Groups[1].Value;
                    return $"[{found}](https://www.urbandictionary.com/define.php?term={HttpUtility.UrlEncode(found)})";
                });
        }

        public Task<UrbanEmbed> WithDefinitionAsync(
            UrbanDefinition definition,
            string query,
            string userPicture,
            int pagesCount,
            int page = 1)
        {
            WithTitle(definition.Word);
            WithUrl(definition.Permalink);
            WithAuthor(new EmbedAuthorBuilder()
            {
                Name = "Urban Dictionary",
                Url = "https://www.urbandictionary.com/",
                IconUrl = userPicture
            });

            WithTimestamp(definition.WrittenOn);
            WithColor(new Color(221, 92, 46));
            WithFooter($"{page}/{pagesCount} | {definition.Author}");
            this.WithMetadata(new UrbanEmbedMetadata { PageNumber = page, SearchQuery = query });

            WithDescription(CreateLinks(definition.Definition));

            AddField("Example", CreateLinks(definition.ExampleUsage));
            AddField("👍", definition.ThumbsUp, inline: true);
            AddField("👎", definition.ThumbsDown, inline: true);

            return Task.FromResult(this);
        }
    }
}
