using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public static class RNG
    {
        private static readonly Random random = new Random();

        public static int Next(int minValue, int maxValue) => random.Next(minValue, maxValue + 1);

        public static double Next(double minValue, double maxValue) => minValue + random.NextDouble() * (maxValue - minValue);

        public static float Next(float minValue, float maxValue) => minValue + (float)random.NextDouble() * (maxValue - minValue);

        public static bool TryChance(double chance = 0.5) => random.NextDouble() < chance;
    }
}
