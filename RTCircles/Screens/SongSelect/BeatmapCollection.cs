using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTCircles
{
    public static class DynamicTexureCache
    {
        private static Dictionary<string, (Texture, List<Guid>)> textureCache = new Dictionary<string, (Texture, List<Guid>)>();

        public static Texture AquireCache(Guid id, string path)
        {
            if (textureCache.TryGetValue(path, out var value))
            {
                if (value.Item2.Contains(id))
                    throw new Exception("This id already has a cache???");

                value.Item2.Add(id);

                return value.Item1;
            }
            else
            {
                var tex = File.Exists(path) ?
                    new Texture(File.OpenRead(path)) { AutoDisposeStream = true, GenerateMipmaps = false } : Skin.DefaultBackground;

                var toAdd = (tex, new List<Guid>() { id });
                textureCache.Add(path, toAdd);

                return toAdd.Item1;
            }
        }

        public static void ReleaseCache(Guid guid, string path)
        {
            if (textureCache.TryGetValue(path, out var subscribers))
            {
                var texture = subscribers.Item1;
                var listSubscribers = subscribers.Item2;

                var index = listSubscribers.IndexOf(guid);

                if (index != -1)
                {
                    listSubscribers.RemoveAt(index);

                    //When theres no more subscribers
                    if (listSubscribers.Count == 0)
                    {
                        //Remove the texture cache item
                        textureCache.Remove(path);

                        GPUSched.Instance.Enqueue(() =>
                        {
                            //And delete the texture
                            texture.Delete();
                        });
                    }
                }
            }
        }
    }

    //Make items, sets
    public class CarouselItem
    {
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
        }
    }

    public static class BeatmapCollection
    {

        public static List<CarouselItem> Items = new List<CarouselItem>();

        public static List<CarouselItem> SearchItems = new List<CarouselItem>();

        public static Dictionary<string, CarouselItem> HashedItems = new Dictionary<string, CarouselItem>();

        public static event Action SearchResultsChanged;
        public static string SearchQuery;

        static BeatmapCollection()
        {
            SearchItems = Items;
        }

        public static void AddItem(CarouselItem item)
        {
            HashedItems.Add(item.Hash, item);
            Items.Add(item);
            FindText(SearchQuery);
        }

        public static void DeleteMap(CarouselItem item)
        {
            HashedItems.Remove(item.Hash);
            Items.Remove(item);
            SearchItems.Remove(item);

            bool fileExists = false;
            if (System.IO.File.Exists(item.FullPath))
            {
                fileExists = true;
                System.IO.File.Delete(item.FullPath);
            }

            BeatmapMirror.DatabaseAction((realm) =>
            {
                var bm = realm.Find<DBBeatmapInfo>(item.Hash);

                realm.Write(() =>
                {
                    bm.SetInfo.Beatmaps.Remove(bm);
                    realm.Remove(bm);
                });
            });

            NotificationManager.ShowMessage($"Beatmap with hash: {item.Hash} has been deleted! File: {(fileExists ? "Existed" : "Did not exist")}", ((Vector4)Color4.CornflowerBlue).Xyz, 5f);
        }

        public static void FindText(string text)
        {
            SearchQuery = text;

            if (string.IsNullOrEmpty(text))
            {
                SearchItems = Items;
                return;
            }

            var keywords = text.Split(' ');

            SearchItems = Items.Where((o) =>
            {
                var foundMatch = true;
                foreach (var keyword in keywords)
                {
                    if (o.Text.ToLower().Contains(keyword.ToLower()) == false)
                    {
                        foundMatch = false;
                        break;
                    }
                }

                return foundMatch;
            }).ToList();

            SearchResultsChanged?.Invoke();
        }
    }
}

