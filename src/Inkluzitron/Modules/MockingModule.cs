using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Models;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    [Name("Mock")]
    public class MockingModule : ModuleBase
    {
        private IConfiguration Config { get; }

        public MockingModule(IConfiguration config)
        {
            Config = config;
        }

        // Get maximum range value for a random number generator that decides if the char should be uppercase.
        // When the char is uppercased, the index is set to last element.
        // The index is decremented for each lowercased char
        //
        // This means the char following uppercased char has 20% (1/5) chance of changing to uppercase.
        // If it's not changed, then the next char has 50% (1/2) chance of being uppercased. Finally if
        // even the second char is not uppercased, the next valid char has 100% chance.
        private readonly int[] MockRandomCoefficient = { 1, 2, 5 };

        [Command("mock")]
        [Summary("Mockuje zadanou zprávu, nebo zprávu na kterou uživatel reaguje.")]
        public async Task<RuntimeResult> MockAsync(params string[] strings)
        {
            var message = string.Join(" ", strings).ToLower();
            var hasReferencedMsg = false;
            if (message.Length == 0)
            {
                if (Context.Message.ReferencedMessage == null)
                {
                    await ReplyAsync("Chybí zpráva k mockování.");
                    return null;
                }

                hasReferencedMsg = true;

                // Easter egg. If user is mocking bot, send peepoangry instead
                if (Context.Message.ReferencedMessage.Author == Context.Guild.CurrentUser)
                    return new CommandRedirectResult($"angry {Context.User.Id}");

                message = Context.Message.ReferencedMessage.ToString().ToLower();
            }

            var mockedMessage = "";
            var coeffIndex = 0;

            foreach (var c in message)
            {
                // Letter 'i' cannot be uppercased and letter 'l' should be always uppercased.
                // This feature is here to prevent confusion of lowercase 'l' and uppercase 'i'
                if (char.IsLetter(c) && c != 'i' && (c == 'l' || ThreadSafeRandom.Next(MockRandomCoefficient[coeffIndex]) == 0))
                {
                    mockedMessage += char.ToUpperInvariant(c);
                    coeffIndex = MockRandomCoefficient.Length - 1;
                    continue;
                }

                mockedMessage += c;

                if (coeffIndex > 0)
                {
                    coeffIndex--;
                }
            }

            mockedMessage = $"{Config["Spongebob"]} {mockedMessage} {Config["Spongebob"]}";
            // if mocking of referenced message don't use prepared ReplyFileAsync function because we want to reply to
            // author of referenced message instead of replying to mocker
            if (hasReferencedMsg)
            {
                var am = new AllowedMentions() { MentionRepliedUser = true };
                var mr = new MessageReference(Context.Message.ReferencedMessage.Id, Context.Channel.Id,
                    Context.Guild.Id);

                await Context.Channel.SendMessageAsync(
                    mockedMessage,
                    options: RequestOptions.Default,
                    allowedMentions: am,
                    messageReference: mr
                );
            }
            else
            {
                await ReplyAsync(mockedMessage);
            }

            return null;
        }
    }
}
