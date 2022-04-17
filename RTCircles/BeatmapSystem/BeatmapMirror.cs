using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Easy2D;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;
using Realms;

namespace RTCircles
{
    public static class BeatmapMirror
    {
        public enum RankStatus
        {
            Graveyard = -2,
            WIP = -1,
            Pending = 0,
            Ranked = 1,
            Approved = 2,
            Qualified = 3,
            Loved = 4,
        }

        public class SayobotBeatmap
        {
            public RankStatus approved { get; set; }
            public string artist { get; set; }
            public string artistU { get; set; }
            public string creator { get; set; }
            public int favourite_count { get; set; }
            public int lastupdate { get; set; }
            public int modes { get; set; }
            public double order { get; set; }
            public int play_count { get; set; }
            public int sid { get; set; }
            public string title { get; set; }
            public string titleU { get; set; }
        }

        public class SayobotBeatmapList
        {
            public List<SayobotBeatmap> data { get; set; }
            public int endid { get; set; }
            public int match_artist_results { get; set; }
            public int match_creator_results { get; set; }
            public int match_tags_results { get; set; }
            public int match_title_results { get; set; }
            public int match_version_results { get; set; }
            public int results { get; set; }
            public int status { get; set; }
            public int time_cost { get; set; }
        }

        class SearchQuery
        {
            public string cmd { get; set; }
            public int limit { get; set; }
            public int offset { get; set; }
            public string type { get; set; }
            public string keyword { get; set; }
            public int mode { get; set; }
            public int @class { get; set; }
            public int subtype { get; set; }
            public int genre { get; set; }
            public int language { get; set; }
        }

        public static Realm Realm;

        public static event Action<DBBeatmapInfo> OnNewBeatmapAvailable;
        //public static ConcurrentQueue<DBBeatmap> NewBeatmaps = new ConcurrentQueue<DBBeatmap>();

        public static MD5 MD5 { get; private set; }

        public static string SongsFolder { get; private set; }

        private static object beatmapDecoderLock = new object();

        private static Thread realmsThread;
        public static Scheduler Scheduler { get; private set; } = new Scheduler();

        static BeatmapMirror()
        {
            SongsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Songs";

            realmsThread = new Thread(() =>
            {
                Realm = Realm.GetInstance(new RealmConfiguration("RTCircles.realm") { SchemaVersion = 1 });
                Utils.Log($"Realm Path: {Realm.Config.DatabasePath}", LogLevel.Important);

                while (MainGame.Instance.View.IsClosing == false)
                {
                    Scheduler.RunPendingTasks();
                    Thread.Sleep(1);
                }

                //Window is closing..
            });

            realmsThread.Start();

            Utils.Log($"SongsFolder: {SongsFolder}", LogLevel.Important);

            MD5 = MD5.Create();
            MD5.Initialize();
        }

        public static void ImportBeatmap(Stream oszStream)
        {
            ZipArchive archive = new ZipArchive(oszStream);

            var beatmapFiles = archive.Entries.Where((o) => o.FullName.EndsWith(".osu"));
            Utils.Log($"Importing Beatmap archive, found {beatmapFiles.Count()} beatmaps in archive...", LogLevel.Info);

            int setID = -1;

            DBBeatmapSetInfo setInfo = new DBBeatmapSetInfo();

            foreach (var item in beatmapFiles)
            {
                Utils.Log($"Processing beatmap: {item.FullName}", LogLevel.Info);
                var stream = item.Open();
                using (MemoryStream beatmapStream = new MemoryStream())
                {
                    stream.CopyTo(beatmapStream);

                    var hash = MD5.ComputeHash(beatmapStream.ToArray());
                    var hashString = Convert.ToBase64String(hash);
                    Utils.Log($"\tHash: {hashString}", LogLevel.Success);

                    //optionally open the beatmap to write extra database entries
                    beatmapStream.Position = 0;
                    Beatmap beatmap = DecodeBeatmap(beatmapStream);
                    Utils.Log($"\tBeatmap ID: {beatmap.MetadataSection.BeatmapID}", LogLevel.Success);

                    if (beatmap.GeneralSection.Mode != OsuParsers.Enums.Ruleset.Standard)
                    {
                        Utils.Log($"\tSkipping ID: {beatmap.MetadataSection.BeatmapID} NOT A STANDARD MAP!!", LogLevel.Warning);
                        continue;
                    }
                    else
                    {
                        DBBeatmapInfo beatmapInfo = new DBBeatmapInfo();
                        //PrimaryKey
                        beatmapInfo.Hash = hashString;

                        beatmapInfo.SetInfo = setInfo;

                        beatmapInfo.Filename = item.FullName;
                        beatmapInfo.BackgroundFilename = beatmap.EventsSection.BackgroundImage;

                        setInfo.Beatmaps.Add(beatmapInfo);
                    }

                    if (setID == -1)
                        setID = beatmap.MetadataSection.BeatmapSetID;
                }
            }

            Debug.Assert(setID != -1);

            //Handle when the beatmap already exists and is open, just ignore it?
            Directory.CreateDirectory($"{SongsFolder}/{setID}");
            try
            {
                archive.ExtractToDirectory($"{SongsFolder}/{setID}", true);
            }
            catch (Exception ex)
            {
                Utils.Log($"Extracting archive failed due to: {ex.Message} import process aborted :(", LogLevel.Error);
                archive.Dispose();

                setID = -1;

                return;
            }

            Utils.Log("Writing to database...", LogLevel.Info);

            setInfo.Foldername = setID.ToString();

            Scheduler.Enqueue(() =>
            {
                Realm.Write(() =>
                {
                    Realm.Add(setInfo, true);
                });

                foreach (var item in setInfo.Beatmaps)
                {
                    OnNewBeatmapAvailable?.Invoke(item);
                }

                Utils.Log("Done!", LogLevel.Success);
            });
        }

        /// <summary>
        /// Threadsafe beatmap decoding, since beatmap decoder is static and not threadsafe
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Beatmap DecodeBeatmap(Stream stream)
        {
            Beatmap bm = null;

            Utils.Benchmark(() =>
            {
                lock (beatmapDecoderLock)
                    bm = BeatmapDecoder.Decode(getLines(stream));
            }, "Beatmap Decoding");

            return bm;
        }

        private static IEnumerable<string> getLines(Stream stream)
        {
            using(StreamReader sr = new StreamReader(stream))
            {
                while (!sr.EndOfStream)
                {
                    yield return sr.ReadLine();
                }
            }
        }

        private static HttpClient client = new HttpClient();

        public static async Task GetIcon(int setID, Action<Stream> action)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadDataCompleted += (s, e) =>
                {
                    if (e.Error is null)
                        action?.Invoke(new MemoryStream(e.Result));

                };

                client.DownloadDataAsync(new Uri($"https://cdn.sayobot.cn:25225/beatmaps/{setID}/covers/cover.jpg"));
            }
        }

        //I_ HAVE TO CLUE WTF IM DOING LMAO 
        //90000000+ social credits 中国中国中国中国中国中国中国中国中国中国中国中国中国中国中国中国
        public static async Task Sayobot_DownloadBeatmapSet(int setID, Action<long?, long> progressChanged = null, Action onError = null)
        {
            Utils.Log($"Downloading beatmap: {setID}!", LogLevel.Info);
            using (WebClient wc = new WebClient())
            {
                wc.DownloadDataCompleted += (sender, e) =>
                {
                    if(e.Error is not null)
                    {
                        onError?.Invoke();
                        Utils.Log($"ERROR downloading beatmap: {setID}. {e.Error.Message}", LogLevel.Error);
                        return;
                    }

                    Utils.Log($"Finished downloading beatmap: {setID}!", LogLevel.Success);
                    
                    using (MemoryStream oszStream = new MemoryStream(e.Result))
                        ImportBeatmap(oszStream);
                };

                wc.DownloadProgressChanged += (sender, e) =>
                {
                    progressChanged?.Invoke(e.TotalBytesToReceive, e.BytesReceived);
                };

                wc.DownloadDataAsync(new Uri($"https://txy1.sayobot.cn/beatmaps/download/novideo/{setID}?server=auto"));
            }
        }

        //90000000+ social credits 中国中国中国中国中国中国中国中国中国中国中国中国中国中国中国中国
        public static SayobotBeatmapList Sayobot_GetBeatmapList(string searchQuery, int offset = 0, int limit = 25)
        {
            /*
            class: 31
            cmd: "beatmaplist"
            genre: 1535
            keyword: "lol"
            language: 4095
            limit: 25
            mode: 1
            offset: 0
            subtype: 63
            type: "search"*/
            SearchQuery query = new SearchQuery();
            query.@class = 31;
            query.cmd = "beatmaplist";
            query.genre = 1535;
            query.keyword = searchQuery;
            query.language = 4095;
            query.limit = limit;
            query.mode = 1;
            query.offset = offset;
            query.subtype = 63;
            query.type = "search";

            var jsonPost = JsonConvert.SerializeObject(query);

            var content = new StringContent(jsonPost, Encoding.UTF8, "application/json");

            var response = client.PostAsync("https://api.sayobot.cn/?post", content).Result;

            var data = response.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<SayobotBeatmapList>(data);
        }
    }
}

//90000000+ social credits