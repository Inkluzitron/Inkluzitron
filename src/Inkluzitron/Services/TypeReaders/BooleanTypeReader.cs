﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services.TypeReaders
{
    public class BooleanTypeReader : TypeReader
    {
        private Dictionary<Func<Regex>, bool> MatchingFunctions { get; } = new Dictionary<Func<Regex>, bool>()
        {
            { () => new Regex("^(ano|yes|true?)$"), true }, // ano, yes, true, tru
            { () => new Regex("^(ne|no|false?)$"), false } // ne, no, false, fals
        };

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (bool.TryParse(input, out bool result))
                return Task.FromResult(TypeReaderResult.FromSuccess(result));

            foreach (var func in MatchingFunctions)
            {
                var regex = func.Key();

                if (regex.IsMatch(input))
                    return Task.FromResult(TypeReaderResult.FromSuccess(func.Value));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Řetězec není pravdivostní hodnota (ano/yes/true/tru/ne/no/false/fals)."));
        }
    }
}
