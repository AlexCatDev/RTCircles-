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
        public string Folder { get; set; }

        [Realms.Required]
        public string File { get; set; }

        public string BackgroundFile { get; set; }

        public double Difficulty { get; set; }
    }

    public class DBBeatmapSetInfo : Realms.RealmObject
    {
        [Realms.PrimaryKey]
        public Guid ID { get; set; } = Guid.NewGuid();

        public IList<DBBeatmapInfo> Beatmaps { get; }
    }
}
