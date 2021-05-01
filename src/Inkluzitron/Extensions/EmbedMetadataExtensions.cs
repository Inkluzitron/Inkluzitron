using Discord;
using Inkluzitron.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Inkluzitron.Extensions
{
    static public class EmbedMetadataExtensions
    {
        static public EmbedBuilder WithMetadata(this EmbedBuilder embedBuilder, IEmbedMetadata embedMetadata)
        {
            if (embedBuilder.Author?.IconUrl is string authorIconUrl)
            {
                authorIconUrl = StealthInto(authorIconUrl, embedMetadata);
                return embedBuilder.WithAuthor(embedBuilder.Author.Name, authorIconUrl, embedBuilder.Author.Url);
            }
            else if (embedBuilder.ImageUrl is string imageUrl)
            {
                imageUrl = StealthInto(imageUrl, embedMetadata);
                return embedBuilder.WithImageUrl(imageUrl);
            }
            else
            {
                var metadataText = SerializeMetadata(embedMetadata);
                var oldFooterText = embedBuilder.Footer.Text;
                if (!string.IsNullOrEmpty(oldFooterText))
                    oldFooterText = " " + oldFooterText;

                return embedBuilder.WithFooter(metadataText + oldFooterText);
            }
        }

        static private string StealthInto(string uri, IEmbedMetadata embedMetadata)
        {
            var oldUri = new Uri(uri);
            var existingFragmentData = HttpUtility.ParseQueryString(oldUri.Fragment);
            var newFragment = SerializeMetadata(embedMetadata, existingFragmentData);
            var newUriBuilder = new UriBuilder(uri) { Fragment = newFragment };
            return newUriBuilder.ToString();
        }

        static private string SerializeMetadata(IEmbedMetadata embedMetadata, NameValueCollection existingFragmentData = null)
        {
            var fragmentData = existingFragmentData ?? new NameValueCollection();
            var fragmentDict = new Dictionary<string, string>();

            // Update it with the required values
            embedMetadata.SaveInto(fragmentDict);
            foreach (var (key, value) in fragmentDict)
                fragmentData[key] = value;

            fragmentData["_k"] = embedMetadata.EmbedKind;

            // Make a list of query pairs
            var keyValuePairs = new List<string>();
            foreach (var key in fragmentData.AllKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                keyValuePairs.Add(Escape(key) + "=" + Escape(fragmentData[key]));

            return string.Join("&", keyValuePairs);
        }

        static private string Escape(string s)
            => Uri.EscapeDataString(s);

        static public bool TryParseMetadata<T>(this IEmbed embed, out T embedMetadata)
            where T : IEmbedMetadata, new()
        {
            embedMetadata = new T();

            NameValueCollection metadata;
            var sourceUrl = embed?.Author?.IconUrl ?? embed?.Image?.Url;
            if (sourceUrl != null)
                metadata = HttpUtility.ParseQueryString(new UriBuilder(sourceUrl).Fragment.TrimStart('#'));
            else if (embed?.Footer?.Text is string footerText && Regex.Match(footerText, @"^\S+") is Match match && match.Success)
                metadata = HttpUtility.ParseQueryString(match.Groups[0].Value);
            else
                return false;

            var fragmentDict = new Dictionary<string, string>();
            foreach (var key in metadata.AllKeys)
                fragmentDict[key] = metadata[key];

            if (!fragmentDict.TryGetValue("_k", out var embedKind))
                return false;
            if (!embedKind.Equals(embedMetadata.EmbedKind))
                return false;

            fragmentDict.Remove("_k");
            return embedMetadata.TryLoadFrom(fragmentDict);
        }
    }
}
