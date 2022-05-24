using Easy2D;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace OsuBot
{
    public static class BeatmapManager
    {
        public const string MapDirectory = "./Maps";

        private static object lockObject = new object();

        static BeatmapManager()
        {
            if (!Directory.Exists(MapDirectory))
            {
                Directory.CreateDirectory(MapDirectory);
                Utils.Log("Map directory didn't exist and has been created", LogLevel.Warning);
            }
        }

        public static string GetBeatmap(ulong id, bool reDownload = false)
        {
            lock (lockObject)
            {
                //Load map from disk (if available)
                if (File.Exists($"{MapDirectory}/{id}") && reDownload == false)
                {
                    string bm = File.ReadAllText($"{MapDirectory}/{id}");
                    return bm;
                }
                else
                {
                    if (reDownload == true)
                        Utils.Log("Doing a map redownload!", LogLevel.Info);

                    //If no file, download beatmap from osu.ppy.sh
                    using (WebClient wc = new WebClient())
                    {
                        Utils.Log($"Downloading beatmap: {id}", LogLevel.Info);
                        string beatmap = "";

                        Utils.Benchmark(() =>
                        {
                            beatmap = wc.DownloadString($"https://osu.ppy.sh/osu/{id}");
                        }, "\t");

                        string filename = $"{id}";

                        Utils.Log($"Saving beatmap [{filename}]", LogLevel.Debug);
                        //Save map to disk
                        Utils.Benchmark(() =>
                        {
                            File.WriteAllText($"{MapDirectory}/{filename}", beatmap);
                        }, "\t");

                        return beatmap;
                    }
                }
            }
        }
    }
}
