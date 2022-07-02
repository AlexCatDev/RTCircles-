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

        private static Realm realm;

        public static event Action<DBBeatmapSetInfo, bool> OnNewBeatmapSetAvailable;

        private static object beatmapDecoderLock = new object();

        private static Thread realmsThread;
        public static Scheduler Scheduler { get; private set; } = new Scheduler();

        public static bool RealmThreadActive = true;

        public static readonly string RootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/RTCircles";
        public static readonly string SongsDirectory = RootDirectory + "/Songs";
        
        static BeatmapMirror()
        {
            //This does nothing if the directory already exists
            Directory.CreateDirectory(RootDirectory);

            realmsThread = new Thread(() =>
            {
                if (!RealmThreadActive)
                    return;
                
                realm = Realm.GetInstance(new RealmConfiguration(RootDirectory + "/RTCircles.realm") { SchemaVersion = 2, });
                Utils.Log($"Started Realm Thread.", LogLevel.Important);
                Utils.Log($"Realm Path: {realm.Config.DatabasePath}", LogLevel.Important);

                /*
                {
                    byte[] buffer = new byte[1024];
                    Utils.Log($"Checking database integrity...", LogLevel.Info);
                    foreach (var item in realm.All<DBBeatmapInfo>())
                    {
                        if (item.SetInfo == null)
                        {
                            Utils.Log($"The Set info was null", LogLevel.Error);
                            continue;
                        }

                        string originalHash = item.Hash;

                        string file = $"{BeatmapMirror.SongsFolder}/{item.SetInfo.Foldername}/{item.Filename}";

                        if (item.SetInfo.Beatmaps.Count == 0)
                            Utils.Log($"The Set info has 0 beatmaps", LogLevel.Error);

                        if (!File.Exists(file))
                        {
                            Utils.Log($"{file} doesn't exist!", LogLevel.Error);
                            continue;
                        }

                        using (var fs = File.OpenRead(file))
                        {
                            int length = (int)fs.Length;

                            if (length > buffer.Length)
                                Array.Resize(ref buffer, length);

                            fs.Read(buffer, 0, length);
                            var newHash = Utils.ComputeSHA256Hash(buffer, 0, length);

                            //Console.WriteLine($"{originalHash} -> {newHash}");
                        }
                    }

                    Utils.Log($"Checking beatmap sets integrity...", LogLevel.Info);
                    foreach (var item in realm.All<DBBeatmapSetInfo>())
                    {
                        if (item.Beatmaps.Count == 0)
                        {
                            Utils.Log($"{item.Foldername} has no beatmaps in it!", LogLevel.Error);

                            Utils.Log($"Deleting", LogLevel.Info);
                            try
                            {
                                System.IO.Directory.Delete($"{BeatmapMirror.SongsFolder}/{item.Foldername}", true);
                                Utils.Log($"Done!", LogLevel.Success);
                            }
                            catch (Exception ex)
                            {
                                Utils.Log($"Fail!", LogLevel.Error);
                            }
                        }

                        realm.Write(() =>
                        {
                            realm.Remove(item);
                        });
                    }

                    Utils.Log($"Done!", LogLevel.Success);
                }
                */
                while (MainGame.Instance.View.IsClosing == false)
                {
                    Scheduler.RunPendingTasks();
                    Thread.Sleep(1);
                }

                //Window is closing..
            });

            realmsThread.Start();

            Utils.Log($"SongsFolder: {SongsDirectory}", LogLevel.Important);
        }

        public static void DatabaseAction(Action<Realm> action)
        {
            Scheduler.Enqueue(() =>
            {
                action(realm);
            });
        }

        public static void ImportBeatmapFolder(string directory, ref byte[] buffer)
        {
            Utils.Log($"Importing Beatmap folder {directory}", LogLevel.Info);

            DBBeatmapSetInfo setInfo = new DBBeatmapSetInfo();

            string newFolderName = Guid.NewGuid().ToString();

            foreach (var file in Directory.GetFiles(directory))
            {
                if (!file.EndsWith(".osu"))
                    continue;

                Utils.Log($"Processing beatmap: {file}", LogLevel.Info);
                using (var beatmapStream = File.OpenRead(file))
                {
                    int streamLength = (int)beatmapStream.Length;

                    if (streamLength > buffer.Length)
                        Array.Resize(ref buffer, streamLength);

                    beatmapStream.Read(buffer, 0, streamLength);

                    beatmapStream.Position = 0;
                    Beatmap beatmap = null;

                    try
                    {
                        beatmap = DecodeBeatmap(beatmapStream);
                        //If decoding fails just continue
                    }
                    catch {
                        Utils.Log($"Skipping {file} because the decoder failed", LogLevel.Warning);
                        continue; 
                    }

                    Utils.Log($"\tBeatmap ID: {beatmap.MetadataSection.BeatmapID}", LogLevel.Success);

                    if (beatmap.GeneralSection.Mode != OsuParsers.Enums.Ruleset.Standard)
                    {
                        Utils.Log($"\tSkipping ID: {beatmap.MetadataSection.BeatmapID} NOT A STANDARD MAP!!", LogLevel.Warning);
                        continue;
                    }
                    else
                    {
                        var hashString = Utils.ComputeSHA256Hash(buffer, 0, streamLength);
                        Utils.Log($"\tHash: {hashString}", LogLevel.Success);

                        //If we already have this beatmap, then skip, ideally then we have to grab the beatmapset it is using?
                        if (BeatmapCollection.HashedItems.ContainsKey(hashString))
                        {
                            Utils.Log($"\tWe already have this map.", LogLevel.Info);
                            continue;
                        }

                        DBBeatmapInfo beatmapInfo = new DBBeatmapInfo();
                        //PrimaryKey
                        beatmapInfo.Hash = hashString;

                        beatmapInfo.SetInfo = setInfo;

                        beatmapInfo.Filename = new FileInfo(file).Name;
                        beatmapInfo.BackgroundFilename = beatmap.EventsSection.BackgroundImage;

                        setInfo.Beatmaps.Add(beatmapInfo);
                    }
                }
            }

            //Ignore if no beatmaps found
            if (setInfo.Beatmaps.Count == 0)
            {
                Utils.Log($"The whole beatmap set was skipped because no maps could be extracted", LogLevel.Warning);
                return;
            }

            Utils.Log("Copying...", LogLevel.Info);
            Utils.CopyDirectory(directory, $"{SongsDirectory}/{newFolderName}", recursive: true);

            Utils.Log("Writing to database...", LogLevel.Info);

            setInfo.Foldername = newFolderName;

            Scheduler.Enqueue(() =>
            {
                realm.Write(() =>
                {
                    realm.Add(setInfo, true);
                });

                OnNewBeatmapSetAvailable?.Invoke(setInfo, false);

                Utils.Log("Done!", LogLevel.Success);
            });
        }

        public static void ImportBeatmap(Stream oszStream)
        {
            try
            {
                ZipArchive archive = new ZipArchive(oszStream);

                var beatmapFiles = archive.Entries.Where((o) => o.FullName.EndsWith(".osu"));
                Utils.Log($"Importing Beatmap archive, found {beatmapFiles.Count()} beatmaps in archive...", LogLevel.Info);

                DBBeatmapSetInfo setInfo = new DBBeatmapSetInfo();

                string folderGUID = Guid.NewGuid().ToString();

                int itemsExtracted = 0;

                foreach (var item in beatmapFiles)
                {
                    Utils.Log($"Processing beatmap: {item.FullName}", LogLevel.Info);
                    var stream = item.Open();
                    using (MemoryStream beatmapStream = new MemoryStream())
                    {
                        stream.CopyTo(beatmapStream);

                        var bytes = beatmapStream.ToArray();
                        var hashString = Utils.ComputeSHA256Hash(bytes, 0, bytes.Length); 

                        beatmapStream.Position = 0;
                        Beatmap beatmap = DecodeBeatmap(beatmapStream);

                        Utils.Log($"\tBeatmap Hash: {hashString} ID: {beatmap.MetadataSection.BeatmapID}", LogLevel.Success);

                        if (beatmap.GeneralSection.Mode != OsuParsers.Enums.Ruleset.Standard)
                        {
                            Utils.Log($"\tSkipping [{hashString}] Mode: {beatmap.GeneralSection.Mode}", LogLevel.Warning);
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

                            itemsExtracted++;
                        }
                    }
                }

                if(itemsExtracted == 0)
                {
                    Utils.Log($"0 items were extracted from the archive..", LogLevel.Error);
                    return;
                }

                Directory.CreateDirectory($"{SongsDirectory}/{folderGUID}");
                try
                {
                    archive.ExtractToDirectory($"{SongsDirectory}/{folderGUID}", true);
                }
                catch (Exception ex)
                {
                    Utils.Log($"Extracting archive failed due to: {ex.Message} import process aborted :(", LogLevel.Error);
                    archive.Dispose();
                    return;
                }

                Utils.Log("Writing to database...", LogLevel.Info);

                setInfo.Foldername = folderGUID;

                Scheduler.Enqueue(() =>
                {
                    realm.Write(() =>
                    {
                        var existingSetInfo = (realm.Find<DBBeatmapInfo>(setInfo.Beatmaps[0].Hash)?.SetInfo);

                        if(existingSetInfo != null)
                        {
                            var oldDirectory = $"{SongsDirectory}/{existingSetInfo.Foldername}";

                            if (Directory.Exists(oldDirectory))
                            {
                                Directory.Delete(oldDirectory, recursive: true);
                                Utils.Log($"Setinfo existed before with these beatmaps but has now been deleted!", LogLevel.Warning);
                            }
                        }

                        realm.Add(setInfo, true);
                    });

                    OnNewBeatmapSetAvailable?.Invoke(setInfo , true);

                    Utils.Log("Done!", LogLevel.Success);
                });
            }catch (Exception ex)
            {
                NotificationManager.ShowMessage($"Something went horrible wrong when importing beatmap\n{ex.Message}", Colors.Red.Xyz, 20);
            }
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

        private static HttpClient client = MainGame.Instance.GetPlatformHttpClient();

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