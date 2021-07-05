using Discord;
using Discord.Commands;
using Inkluzitron.Models.Vote;
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
        private Regex Segments { get; } = new Regex(@"(\S+)");
        private EmotesTypeReader EmoteReader { get; } = new();
        private TimeSpanTypeReader TsTypeReader { get; } = new();
        private DateTimeTypeReader DateTimeTypeReader { get; } = new();

        public async Task<VoteDefinitionParserResult> TryParse(ICommandContext context, string voteDefinitionText)
        {
            var def = new VoteDefinition();
            string desc = null;
            string notice = null;

            using var reader = new StringReader(voteDefinitionText);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (def.Question == null)
                {
                    def.Question = line.Trim();
                    continue;
                }

                line = line.Trim();
                var lineSegments = Segments.Matches(line);

                if (lineSegments.Count == 0)
                    continue;

                var readResult = await EmoteReader.ReadAsync(context, lineSegments[0].Value, null);
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
                    var deadlineInResult = await TsTypeReader.ReadAsync(context, gde, null);
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

                        def.Deadline = (context.Message.EditedTimestamp ?? context.Message.CreatedAt) + zaGdy;
                    }
                }
                else if (isDeadlineDate)
                {
                    var gde = line.Substring(lineSegments[0].Index + lineSegments[0].Value.Length).Trim();
                    var deadlineInResult = await DateTimeTypeReader.ReadAsync(context, gde, null);
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

            return new VoteDefinitionParserResult
            {
                Definition = def,
                ProblemDescription = desc,
                Notice = notice
            };
        }
    }
}
