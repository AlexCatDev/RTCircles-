using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class DBBeatmap : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public int ID { get; set; }

        public int SetID { get; set; }

        [Realms.Required]
        public string Folder { get; set; }

        [Realms.Required]
        public string File { get; set; }

        public string Background { get; set; }

        public double Difficulty { get; set; }

        [Realms.Required]
        public string Hash { get; set; }
    }

    public class DBBeatmapInfo : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Realms.Required]
        public string Hash { get; set; }

        [Realms.Required]
        public string OsuFile { get; set; }

        public DBBeatmapSetInfo SetInfo { get; set; }

        public string BackgroundFile { get; set; }

        public double Difficulty { get; set; } = 0;
    }

    public class DBBeatmapSetInfo : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        [Realms.Required]
        public string Folder { get; set; }

        public IList<DBBeatmapInfo> Beatmaps { get; }
    }
}
