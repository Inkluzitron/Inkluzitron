using System;

namespace Inkluzitron
{
    static public class ThreadSafeRandom
    {
        // Inspired by RandomGen2 @ https://devblogs.microsoft.com/pfxteam/getting-random-numbers-in-a-thread-safe-way/

        static private readonly Random GlobalGenerator = new();

        [ThreadStatic]
        static private Random LocalGenerator;

        static private Random GetLocalGenerator()
        {
            Random inst = LocalGenerator;
            if (inst == null)
            {
                int seed;
                lock (GlobalGenerator)
                {
                    seed = GlobalGenerator.Next();
                }
                LocalGenerator = inst = new Random(seed);
            }

            return inst;
        }

        static public int Next() => GetLocalGenerator().Next();
        static public int Next(int minValue) => GetLocalGenerator().Next(minValue);
        static public int Next(int minValue, int maxValue) => GetLocalGenerator().Next(minValue, maxValue);
        static public double NextDouble() => GetLocalGenerator().NextDouble();
    }
}
