using Easy2D;
using System;

namespace RTCircles
{
    //Make items, sets
    public class CarouselItem
    {
        public float XOffset;

        public string Text { get; private set; }

        public string Hash { get; private set; }

        private Guid id = Guid.NewGuid();

        public string FullPath { get; private set; }
        public string Folder { get; private set; }

        private string BackgroundPath { get; set; }

        public float TextureAlpha => sFloat.Value;
        private SmoothFloat sFloat = new SmoothFloat();

        public Texture Texture { get; private set; }

        public bool IsVisible;

        public double Difficulty;

        public DBBeatmapInfo DBBeatmapInfo { get; private set; }

        private double loadTextureDelay = 0;

        public void SetDBBeatmap(DBBeatmapInfo dbBeatmap)
        {
            Text = dbBeatmap.Filename.Replace(".osu", "");
            Hash = dbBeatmap.Hash;
            Folder = dbBeatmap.SetInfo.Foldername;
            FullPath = $"{BeatmapMirror.SongsDirectory}/{dbBeatmap.SetInfo.Foldername}/{dbBeatmap.Filename}";
            BackgroundPath = dbBeatmap.BackgroundFilename is not null ? $"{BeatmapMirror.SongsDirectory}/{dbBeatmap.SetInfo.Foldername}/{dbBeatmap.BackgroundFilename}" : "";
        }

        public void Update()
        {
            if (!IsVisible)
                return;

            double delta = MainGame.Instance.DeltaTime;

            loadTextureDelay += delta;

            if (loadTextureDelay > 0.075 && Texture == null)
            {
                sFloat.TransformTo(1f, 0.5f, EasingTypes.Out);
                Texture = DynamicTexureCache.AquireCache(id, BackgroundPath);
            }

            if (Texture?.ImageDoneUploading == true)
                sFloat.Update((float)delta);
        }

        public void OnShow()
        {
            System.Diagnostics.Debug.Assert(!IsVisible);

            IsVisible = true;
        }

        public void OnHide()
        {
            System.Diagnostics.Debug.Assert(IsVisible);

            IsVisible = false;

            loadTextureDelay = 0;

            sFloat.Value = 0;

            DynamicTexureCache.ReleaseCache(id, BackgroundPath);

            Texture = null;

            XOffset = 0;
        }
    }
}

