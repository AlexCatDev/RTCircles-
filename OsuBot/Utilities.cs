using Discord;
using Easy2D;
using Newtonsoft.Json;
using RTCircles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace OsuBot
{
    public static class Utilities
    {
        private static Random rng = new Random();

        public static int GetRandomNumber(int min, int max)
        {
            return rng.Next(min, max + 1);
        }

        public static bool GetRandomChance(double chancePercentage)
        {
            return rng.NextDouble() < (chancePercentage/100.0);
        }

        public static double GetRandomDouble() => rng.NextDouble();

        public static string ToFriendlyString(this Mods mod)
        {
            string output = mod.ToString().Replace(", ", "");

            if (mod.HasFlag(Mods.NC))
                output = output.Replace("DT", "");

            if (mod.HasFlag(Mods.PF))
                output = output.Replace("SD", "");

            return output;
        }

        //Tries to convert a continous string of mods to a Mods enum
        public static Mods StringToMod(string modString)
        {
            //Start with our output at null
            int output = (int)Mods.Null;

            //Split the input string into chunks of two characters per string to read mods: Ex. (NM, DT, FL, EZ) etc..
            var chunks = modString.SplitInParts(2);

            foreach (var chunk in chunks)
            {
                //check if the current two characters is a valid mod
                bool isMod = Enum.TryParse(chunk.ToString(), true, out Mods result);
                if (isMod)
                {
                    //If it's a valid mod, check if our output is set to null, if it is, set it to 0 so we can add from a clean position
                    if (((Mods)output) == Mods.Null)
                        output = 0;

                    //If the input mod was nomod, the above would already have it set to nomod, the if statement under will not go through

                    //If the output mods doesnt already contain the input mod example being given (DT, DT, DT) Only take 1 dt and add it to the output
                    if(((Mods)output).HasFlag(result) == false)
                        output += (int)result;

                    //Remember output is a flag so everything is additive
                }
            }
            return (Mods)output;
        }

        public static int FindBeatmapsetID(string mapData)
        {
            int index = mapData.ToLower().IndexOf("beatmapsetid:");

            int offset = index + "beatmapsetid:".Length;

            string beatmapset = "";

            while (mapData[offset] != '\r')
            {
                beatmapset += mapData[offset++];
            }

            if (int.TryParse(beatmapset, out int result))
                return result;
            else
                return 0;
        }

        public static IEnumerable<ReadOnlyMemory<char>> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.AsMemory().Slice(i, Math.Min(partLength, s.Length - i));
        }

        public static T Load<T>(string filename)
        {
            string path = $"./{filename}";

            if (!File.Exists(path))
            {
                Utils.Log($"{path} no such file exists, returned empty object", LogLevel.Warning);
                return Activator.CreateInstance<T>();
            }

            string json = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void Save<T>(this T t, string filename)
        {
            string json = JsonConvert.SerializeObject(t, Formatting.Indented);

            string pathFile = $"./{filename}";

            string pathFileTmp = $"{pathFile}.tmp";

            //Write to a temporary file..
            File.WriteAllText(pathFileTmp, json);

            //Move the temp file to the main file, overwriting it
            File.Move(pathFileTmp, pathFile, true);
        }

        public static string GetEmoteForRankLetter(string rankLetter)
        {
            switch (rankLetter)
            {
                case "F":
                    return "<:RankF:847994614370795570>";
                case "D":
                    return "<:DRank:953968398012911636>";
                case "C":
                    return "<:CRank:953968398054858762>";
                case "B":
                    return "<:BRank:953968398147133460>";
                case "A":
                    return "<:ARank:953968398017134592>";
                case "S":
                    return "<:SRank:953968398021300234>";
                case "X":
                    return "<:SSRank:953968330098737162>";
                case "SH":
                    return "<:SHRank:953968398096797706>";
                case "XH":
                    return "<:SSHRank:953968398268764200>";
                default:
                    return ":sunglasses:";
            }
        }
    }
}
