using Newtonsoft.Json;
using System.Net;

namespace OsuBot
{
    public enum OsuGamemode
    {
        Standard = 0,
        Taiko = 1,
        Catch = 2,
        CTB = 2,
        Mania = 3
    }

    public class BanchoAPI
    {
        public static string GetProfileImageUrl(string userID) => userID == "0" ? $"https://cdn.discordapp.com/attachments/734187670049128504/909922950508085338/logo_1.png" : $"https://a.ppy.sh/{userID}?{Guid.NewGuid().ToString()}.jpeg";
        public static string GetBeatmapImageUrl(string beatmapSetID) => $"https://b.ppy.sh/thumb/{beatmapSetID}l.jpg";
        public static string GetFlagImageUrl(string country) => $"https://osu.ppy.sh/images/flags/{country}.png";
        public static string GetBeatmapUrl(string beatmapID) => $"https://osu.ppy.sh/b/{beatmapID}";

        private string apiKey;

        public static int TotalAPICalls = 0;

        public BanchoAPI(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public List<BanchoRecentScore> GetRecentPlays(string username, int limit = 1, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user_recent?k={apiKey}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoRecentScore>>(json);
            }
        }

        public List<BanchoUser> GetUser(string username, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user?k={apiKey}&u={username}&m={(int)mode}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoUser>>(json);
            }
        }

        public List<BanchoBestScore> GetBestPlays(string username, int limit = 100, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_user_best?k={apiKey}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoBestScore>>(json);
            }
        }

        public List<BanchoScore> GetScores(string username, ulong beatmapID, int limit = 10, OsuGamemode mode = OsuGamemode.Standard)
        {
            using (WebClient wc = new WebClient())
            {
                TotalAPICalls++;
                string json = wc.DownloadString($"https://osu.ppy.sh/api/get_scores?k={apiKey}&b={beatmapID}&u={username}&m={(int)mode}&limit={limit}&type=string");
                return JsonConvert.DeserializeObject<List<BanchoScore>>(json);
            }
        }

        public class BanchoUser
        {
            [JsonProperty("playcount", NullValueHandling = NullValueHandling.Ignore)]
            public int Playcount;

            [JsonProperty("pp_rank", NullValueHandling = NullValueHandling.Ignore)]
            public int Rank;

            [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
            public float Level;

            [JsonProperty("accuracy", NullValueHandling = NullValueHandling.Ignore)]
            public float Accuracy;

            [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
            public string Country;

            [JsonProperty("pp_country_rank", NullValueHandling = NullValueHandling.Ignore)]
            public int CountryRank;

            [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
            public ulong ID;

            [JsonProperty("pp_raw", NullValueHandling = NullValueHandling.Ignore)]
            public float PP;

            [JsonProperty("ranked_score", NullValueHandling = NullValueHandling.Ignore)]
            public long RankedScore;

            [JsonProperty("count_rank_ssh", NullValueHandling = NullValueHandling.Ignore)]
            public int SSHCount;

            [JsonProperty("count_rank_ss", NullValueHandling = NullValueHandling.Ignore)]
            public int SSCount;

            [JsonProperty("count_rank_sh", NullValueHandling = NullValueHandling.Ignore)]
            public int SHCount;

            [JsonProperty("count_rank_s", NullValueHandling = NullValueHandling.Ignore)]
            public int SCount;

            [JsonProperty("count_rank_a", NullValueHandling = NullValueHandling.Ignore)]
            public int ACount;

            [JsonProperty("join_date")]
            public DateTime JoinDate;

            [JsonProperty("total_seconds_played", NullValueHandling = NullValueHandling.Ignore)]
            public int TotalPlaytimeInSeconds;

            [JsonProperty("username")]
            public string Username;
        }

        public class BanchoScore
        {
            [JsonProperty("score_id")]
            public ulong ScoreID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("username")]
            public string Username;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public RTCircles.Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
            [JsonProperty("pp")]
            public float? PP;
            [JsonProperty("replay_available")]
            public int ReplayAvailable;

            public double Accuracy => ((Count300 * 300.0) + (Count100 * 100.0) + (Count50 * 50.0)) / ((Count300 + Count100 + Count50 + CountMiss) * 300);
        }

        public class BanchoBestScore
        {
            [JsonProperty("beatmap_id")]
            public ulong BeatmapID;
            [JsonProperty("score_id")]
            public ulong ScoreID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public RTCircles.Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
            [JsonProperty("pp")]
            public float PP;
            [JsonProperty("replay_available")]
            public int ReplayAvailable;

            public double Accuracy => ((Count300 * 300.0) + (Count100 * 100.0) + (Count50 * 50.0)) / ((Count300 + Count100 + Count50 + CountMiss) * 300);
        }

        public class BanchoRecentScore
        {
            [JsonProperty("beatmap_id")]
            public ulong BeatmapID;
            [JsonProperty("score")]
            public int Score;
            [JsonProperty("maxcombo")]
            public int MaxCombo;
            [JsonProperty("count50")]
            public int Count50;
            [JsonProperty("count100")]
            public int Count100;
            [JsonProperty("count300")]
            public int Count300;
            [JsonProperty("countmiss")]
            public int CountMiss;
            [JsonProperty("countkatu")]
            public int CountKatu;
            [JsonProperty("countgeki")]
            public int CountGeki;
            [JsonProperty("perfect")]
            public int Perfect;
            [JsonProperty("enabled_mods")]
            public RTCircles.Mods EnabledMods;
            [JsonProperty("user_id")]
            public ulong UserID;
            [JsonProperty("date")]
            public DateTime DateOfPlay;
            [JsonProperty("rank")]
            public string RankLetter;
        }
    }
}
