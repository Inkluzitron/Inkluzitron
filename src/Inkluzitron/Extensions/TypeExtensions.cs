using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Inkluzitron.Extensions
{
    static public class TypeExtensions
    {
        static public IEnumerable<string> ExtractCommandNames(this Type moduleType, params string[] methodNames)
        {
            foreach (var methodName in methodNames)
            {
                var methodInfo = moduleType.GetMethod(methodName);

                if (methodInfo.GetCustomAttributes<AliasAttribute>() is AliasAttribute aliasAttribute)
                {
                    foreach (var alias in aliasAttribute.Aliases)
                        yield return alias;
                }

                if (methodInfo.GetCustomAttribute<CommandAttribute>() is CommandAttribute commandAttribute)
                    yield return commandAttribute.Text;
            }
        }
    }
}
