using Discord;
using Discord.Commands;
using Inkluzitron.Models.Settings;
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
        private VoteTranslations VoteTranslations { get; }
        private Regex Segments { get; } = new Regex(@"(\S+)");
        private EmotesTypeReader EmoteReader { get; } = new();
        private TimeSpanTypeReader TsTypeReader { get; } = new();
        private DateTimeTypeReader DateTimeTypeReader { get; } = new();

        public VoteDefinitionParser(VoteTranslations voteTranslations)
        {
            VoteTranslations = voteTranslations;
        }

        public async Task<VoteDefinitionParserResult> TryParse(ICommandContext context, string voteDefinitionText)
        {
            var definition = new VoteDefinition();
            string problemDescription = null;

            using var reader = new StringReader(voteDefinitionText);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (definition.Question == null)
                {
                    definition.Question = line;
                    continue;
                }

                var lineSegments = Segments.Matches(line);

                if (lineSegments.Count == 0)
                    continue;

                var readResult = await EmoteReader.ReadAsync(context, lineSegments[0].Value, null);
                if (readResult.IsSuccess)
                {
                    var optionEmote = (IEmote)readResult.Values.First().Value;
                    var optionTextStartIndex = lineSegments[0].Value.Length;
                    var optionText = line[optionTextStartIndex..].Trim();

                    if (definition.Options.ContainsKey(optionEmote))
                    {
                        problemDescription = string.Format(VoteTranslations.DuplicateOption, optionEmote.ToString());
                        break;
                    }

                    definition.Options.Add(optionEmote, optionText);
                    continue;
                }

                var isDeadline = lineSegments[0].Value == "konec";
                var isDeadlineTimeSpan = lineSegments.Count >= 3 && lineSegments[1].Value == "za";
                var isDeadlineDate = lineSegments.Count >= 2 && lineSegments[1].Value != "za";

                if (!isDeadline || (!isDeadlineTimeSpan && !isDeadlineDate))
                {
                    problemDescription = string.Format(VoteTranslations.LineParseError, Format.Sanitize(line));
                    break;
                }
                else if (definition.Deadline.HasValue)
                {
                    problemDescription = string.Format(VoteTranslations.DuplicateDeadline, Format.Sanitize(line));
                    break;
                }
                else if (isDeadlineTimeSpan)
                {
                    var timeSpanTextStartIndex = lineSegments[1].Index + lineSegments[1].Value.Length;
                    var timeSpanText = line[timeSpanTextStartIndex..].Trim();
                    var timeSpanReadResult = await TsTypeReader.ReadAsync(context, timeSpanText, null);
                    if (!timeSpanReadResult.IsSuccess)
                    {
                        problemDescription = string.Format(VoteTranslations.DeadlineParseError, Format.Sanitize(line), timeSpanText);
                        break;
                    }
                    else
                    {
                        var relativeDeadline = (TimeSpan)timeSpanReadResult.Values.Single().Value;
                        definition.Deadline = (context.Message.EditedTimestamp ?? context.Message.CreatedAt) + relativeDeadline;
                    }
                }
                else if (isDeadlineDate)
                {
                    var dateTimeStartIndex = lineSegments[0].Index + lineSegments[0].Value.Length;
                    var dateTimeText = line[dateTimeStartIndex..].Trim();
                    var dateTimeReadResult = await DateTimeTypeReader.ReadAsync(context, dateTimeText, null);

                    if (!dateTimeReadResult.IsSuccess)
                    {
                        problemDescription = string.Format(VoteTranslations.DeadlineParseError, Format.Sanitize(line), dateTimeText);
                        break;
                    }
                    else
                    {
                        definition.Deadline = (DateTime) dateTimeReadResult.Values.Single().Value;
                    }
                }
            }

            if (problemDescription == null)
            {
                if (string.IsNullOrWhiteSpace(definition.Question))
                    problemDescription = VoteTranslations.NoQuestion;
                else if (definition.Options.Count == 0)
                    problemDescription = VoteTranslations.NoOptions;
            }

            return new VoteDefinitionParserResult
            {
                Definition = definition,
                ProblemDescription = problemDescription
            };
        }
    }
}
