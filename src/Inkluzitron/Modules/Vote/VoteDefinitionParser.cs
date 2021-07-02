using Discord;
using Discord.Commands;
using Inkluzitron.Models;
using Inkluzitron.Services.TypeReaders;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class VoteDefinitionParser
    {
        private Regex Segments = new Regex(@"(\S+)");
        private EmotesTypeReader EmoteReader = new();
        private TimeSpanTypeReader TsTypeReader = new(); // todo: inject
        private DateTimeTypeReader DateTimeTypeReader = new();

        public struct ParseResult
        {
            public bool Success => ProblemDescription == null;
            public VoteDefinition Definition { get; set; }
            public string ProblemDescription { get; set; }
            public string Notice { get; set; }
        }

        public bool IsVoteCommand(IMessage message)
            => message != null
            && message.Channel is ITextChannel
            && message.Content.StartsWith("$vote")
            && !message.Author.IsBot;

        public async Task<ParseResult> TryParse(ICommandContext ctx, string prefix, string message)
        {
            var def = new VoteDefinition();
            string desc = null;
            string notice = null;

            if (!IsVoteCommand(ctx.Message))
            {
                return new ParseResult
                {
                    Definition = null,
                    ProblemDescription = "Chybí hlasovací příkaz."
                };
            }

            using var reader = new StringReader(message);
            string line;

            var prefixTrimmer = new Regex("^" + Regex.Escape(prefix) + @"\w+\s*");

            while ((line = reader.ReadLine()) != null)
            {
                if (def.Question == null)
                {
                    def.Question = prefixTrimmer.Replace(line, string.Empty);
                    continue;
                }

                line = line.Trim();
                var lineSegments = Segments.Matches(line);

                if (lineSegments.Count == 0)
                    continue;

                var readResult = await EmoteReader.ReadAsync(ctx, lineSegments[0].Value, null);
                if (readResult.IsSuccess)
                {
                    var optionEmote = (IEmote)readResult.Values.First().Value;
                    var optionText = line.Substring(lineSegments[0].Value.Length).Trim();

                    if (def.Options.ContainsKey(optionEmote))
                    {
                        desc = $"Odpověď {optionEmote} nemůže být použita vícekrát.";
                        break;
                    }

                    def.Options.Add(optionEmote, optionText);
                    continue;
                }

                var isDeadline = lineSegments[0].Value == "konec";
                var isDeadlineTimeSpan = lineSegments.Count >= 3 && lineSegments[1].Value == "za";
                var isDeadlineDate = lineSegments.Count >= 2 && lineSegments[1].Value != "za";

                if (!isDeadline || (!isDeadlineTimeSpan && !isDeadlineDate))
                {
                    desc = $"Řádek `{Format.Sanitize(line)}` měl být možnost k hlasování, `konec <datum a čas>` nebo `konec za <čas>`";
                    break;
                }
                else if (def.Deadline.HasValue)
                {
                    desc = $"Řádek `{Format.Sanitize(line)}` obsahuje duplicitní deadline.";
                    break;
                }
                else if (isDeadlineTimeSpan)
                {
                    var gde = line.Substring(lineSegments[1].Index + lineSegments[1].Value.Length).Trim();
                    var deadlineInResult = await TsTypeReader.ReadAsync(ctx, gde, null);
                    if (!deadlineInResult.IsSuccess)
                    {
                        desc = $"Na řádku `{Format.Sanitize(line)}` se nepovedlo rozluštit čas obsažený v `{Format.Sanitize(gde)}`.";
                        break;
                    }
                    else
                    {
                        var zaGdy = (TimeSpan)deadlineInResult.Values.Single().Value;
                        if (zaGdy < TimeSpan.Zero)
                        {
                            desc = "Hlasování nemůže skončit v minulosti.";
                            break;
                        }

                        def.Deadline = (ctx.Message.EditedTimestamp ?? ctx.Message.CreatedAt) + zaGdy;
                    }
                }
                else if (isDeadlineDate)
                {
                    var gde = line.Substring(lineSegments[0].Index + lineSegments[0].Value.Length).Trim();
                    var deadlineInResult = await DateTimeTypeReader.ReadAsync(ctx, gde, null);
                    if (!deadlineInResult.IsSuccess)
                    {
                        desc = $"Na řádku `{Format.Sanitize(line)}` se nepovedlo rozluštit datum a čas obsažený v `{Format.Sanitize(gde)}`.";
                        break;
                    }
                    else
                    {
                        var gdy = (DateTime)deadlineInResult.Values.Single().Value;
                        var diff = gdy - DateTimeOffset.UtcNow;

                        if (diff.TotalDays < -1)
                        {
                            desc = "Hlasování nemůže skončit v minulosti.";
                            break;
                        }
                        else if (diff.TotalDays < 0)
                        {
                            gdy += TimeSpan.FromDays(1);
                            notice = "Zadané datum bylo těsně v minulosti a bylo proto posunuto o 24 hodin vpřed.";
                        }

                        def.Deadline = gdy;
                    }
                }
            }

            if (desc == null)
                def.Validate(out desc);

            return new ParseResult { Definition = def, ProblemDescription = desc, Notice = notice };
        }
    }
}
