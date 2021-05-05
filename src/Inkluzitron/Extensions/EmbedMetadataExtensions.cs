﻿using Discord;
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
        /// <summary>
        /// <para>Adjusts the embed under construction so that it includes recoverable representation of <paramref name="embedMetadata"/>.</para>
        /// <para>For optimal results, call this method after <see cref="EmbedBuilder.WithAuthor"/> or <see cref="EmbedBuilder.WithImageUrl"/>.</para>
        /// </summary>
        /// <param name="embedBuilder"></param>
        /// <param name="embedMetadata">The metadata to store inside the embed.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Serializes the provided instance of <paramref name="embedMetadata"/> and returns an updated version of <paramref name="uri"/>
        /// that contains the serialized string in the URI fragment.
        /// </summary>
        static private string StealthInto(string uri, IEmbedMetadata embedMetadata)
        {
            var oldUri = new Uri(uri);
            var existingFragmentData = HttpUtility.ParseQueryString(oldUri.Fragment);
            var newFragment = SerializeMetadata(embedMetadata, existingFragmentData);
            var newUriBuilder = new UriBuilder(uri) { Fragment = newFragment };
            return newUriBuilder.ToString();
        }

        /// <summary>
        /// Serializes the provided instance of <paramref name="embedMetadata"/> into a URL query string-ish representation.
        /// </summary>
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

        /// <summary>
        /// Attempts to recover <typeparamref name="TMetadata"/> from the provided <paramref name="embedMetadata"/>
        /// </summary>
        /// <typeparam name="TMetadata">The type of metadata to look for and restore.</typeparam>
        /// <param name="embed">The embed to search for serialized metadata.</param>
        /// <param name="embedMetadata">The recovered metadata.</param>
        /// <returns>Whether an instance of <typeparamref name="TMetadata"/> was found and recovered from the embed.</returns>
        static public bool TryParseMetadata<TMetadata>(this IEmbed embed, out TMetadata embedMetadata)
            where TMetadata : IEmbedMetadata, new()
        {
            embedMetadata = new TMetadata();

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
