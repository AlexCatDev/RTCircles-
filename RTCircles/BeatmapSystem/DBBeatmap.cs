using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class DBBeatmapInfo : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public string Hash { get; set; }

        [Realms.Required]
        public string Filename { get; set; }

        public DBBeatmapSetInfo SetInfo { get; set; }

        public string BackgroundFilename { get; set; }

        public double Difficulty { get; set; } = 0;
    }

    public class DBBeatmapSetInfo : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public string Foldername { get; set; }

        public IList<DBBeatmapInfo> Beatmaps { get; }
    }
}
