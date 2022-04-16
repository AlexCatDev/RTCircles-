using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuParsers.Beatmaps;
using Easy2D.Game;
using Silk.NET.Input;
using Realms;

namespace RTCircles
{

    //recode this piece of shit//recode this piece of shit//recode this piece of shit//recode this piece of shit
    //recode this piece of shit
    //recode this piece of shit
    //recode this piece of shit

    public static class DynamicTexureCache
    {
        private static Dictionary<string, (Texture, List<Guid>)> textureCache = new Dictionary<string, (Texture, List<Guid>)>();

        public static (Guid, Texture) AquireCache(Guid id, string path)
        {
            if (textureCache.TryGetValue(path, out var value))
            {
                if (!value.Item2.Contains(id))
                {
                    value.Item2.Add(id);
                }

                return (id, value.Item1);
            }
            else
            {
                var tex = new Texture(System.IO.File.OpenRead(path));

                var toAdd = (tex, new List<Guid>() { id });
                textureCache.Add(path, toAdd);

                return (id, toAdd.Item1);
            }
        }

        public static void ReleaseCache(Guid guid, string path)
        {
            //Console.WriteLine($"Releasing cache for: {guid}");
            if (textureCache.TryGetValue(path, out var value))
            {
                var index = value.Item2.IndexOf(guid);
                if (index != -1)
                {
                    value.Item2.RemoveAt(index);
                    //Console.WriteLine($"Released cache for: {guid} Count: {value.Item2.Count}");

                    if (value.Item2.Count == 0)
                    {
                        value.Item1 = null;
                        //Console.WriteLine("No more references, it has been deleted");
                    }
                }
            }
        }
    }

    //Make items, sets
    public class CarouselItem
    {
        public string Text { get; private set; }

        public int ID { get; private set; }

        private Guid id = Guid.NewGuid();

        public string FullPath { get; private set; }

        private string BackgroundPath { get; set; }

        public float TextureAlpha { get; private set; }
        private SmoothFloat sFloat = new SmoothFloat();

        public Texture Texture { get; private set; }

        public bool IsVisible;

        public void SetDBBeatmap(DBBeatmap dbBeatmap)
        {
            Text = dbBeatmap.File;
            ID = dbBeatmap.ID;
            FullPath = $"{BeatmapMirror.SongsFolder}/{dbBeatmap.Folder}/{dbBeatmap.File}";
            BackgroundPath = dbBeatmap.Background is not null ? $"{BeatmapMirror.SongsFolder}/{dbBeatmap.Folder}/{dbBeatmap.Background}" : null;
        }

        public bool OnShow()
        {
            if (Texture?.ImageDoneUploading == true)
                sFloat.Update((float)Game.Instance.DeltaTime);

            TextureAlpha = sFloat.Value;

            if (IsVisible == false)
            {
                IsVisible = true;
                if (string.IsNullOrEmpty(BackgroundPath) == false)
                {
                    sFloat.TransformTo(1f, 0.5f, EasingTypes.Out);
                    Texture = DynamicTexureCache.AquireCache(id, BackgroundPath).Item2;
                }
            }

            return true;
        }

        public void OnHide()
        {
            if (IsVisible == true)
            {
                IsVisible = false;

                if (string.IsNullOrEmpty(BackgroundPath) == false)
                    DynamicTexureCache.ReleaseCache(id, BackgroundPath);

                sFloat.Value = 0;
                Texture = null;
            }
        }
    }

    public static class BeatmapCollection
    {

        public static List<CarouselItem> Items = new List<CarouselItem>();

        public static List<CarouselItem> SearchItems = new List<CarouselItem>();

        public static event Action SearchResultsChanged;
        public static string SearchQuery;

        static BeatmapCollection()
        {
            SearchItems = Items;
        }

        public static void AddItem(CarouselItem item)
        {
            Items.Add(item);
            FindText(SearchQuery);
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

    public class MapSelectScreen : Screen
    {
        public SongSelector SongSelector { get; private set; } = new SongSelector();

        public MapSelectScreen()
        {
            BeatmapMirror.OnNewBeatmapAvailable += (beatmap) =>
            {
                AddBeatmapToCarousel(beatmap);
            };

            Add(SongSelector);
        }

        public void AddBeatmapToCarousel(DBBeatmap dBBeatmap)
        {
            //Dont add to carousel if we already have this item
            Utils.Log($"Adding DBBeatmap: {dBBeatmap.File} Current carousel item count: {BeatmapCollection.Items.Count}", LogLevel.Debug);

            for (int i = 0; i < BeatmapCollection.Items.Count; i++)
            {
                if (BeatmapCollection.Items[i].ID == dBBeatmap.ID)
                {
                    Utils.Log($"A duplicate map was added to the carousel, the old map was changed to the new one", LogLevel.Warning);
                    BeatmapCollection.Items[i].SetDBBeatmap(dBBeatmap);
                    return;
                }
            }

            CarouselItem newItem = new CarouselItem();
            newItem.SetDBBeatmap(dBBeatmap);

            BeatmapCollection.AddItem(newItem);
        }

        public void LoadCarouselItems()
        {
            foreach (var item in BeatmapMirror.Realm.All<DBBeatmap>())
            {
                AddBeatmapToCarousel(item);
                Utils.Log($"Loaded DBBeatmap: {item.File}", LogLevel.Debug);
            }
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Delete && Input.IsKeyDown(Key.ControlLeft))
            {
                BeatmapMirror.Scheduler.Enqueue(() =>
                {
                    SongSelector.DeleteSelectedItem();
                    OsuContainer.UnloadMap();
                });
            }

            base.OnKeyDown(key);
        }

        private static float searchTimer;

        private static string _searchText;
        public static string SearchText
        {
            get { return _searchText; }
            private set
            {
                _searchText = value;
                searchTimer = 0.3f;
            }
        }

        public override void OnTextInput(char args)
        {
            SearchText += args;
            base.OnTextInput(args);
        }

        private float backSpaceTimer = 0;
        private float backSpaceRepeatTimer = 0;

        private void backSpaceSearch(float delta)
        {
            if (Input.IsKeyDown(Key.Backspace) && SearchText != null)
            {
                if (backSpaceTimer <= 0)
                {
                    backSpaceTimer = 0.05f;

                    if (SearchText.Length > 0)
                        SearchText = SearchText.Remove(SearchText.Length - 1);
                }
            }
            else
            {
                backSpaceTimer = 0;
                backSpaceRepeatTimer = 0.24f;
            }

            backSpaceRepeatTimer -= delta;

            if(backSpaceRepeatTimer <= 0)
                backSpaceTimer -= delta;

            backSpaceRepeatTimer.ClampRef(0, 1);
            backSpaceTimer.ClampRef(0, 1);
            
        }

        public override void Update(float delta)
        {
            backSpaceSearch(delta);

            if (searchTimer > 0f)
            {
                searchTimer -= delta;

                searchTimer = Math.Max(0, searchTimer);

                if (searchTimer == 0f)
                {
                    BeatmapCollection.FindText(SearchText);
                    searchTimer = -1;
                }
            }

            base.Update(delta);
        }

        public override void OnExit()
        {
            SongSelector.ConfirmPlayAnimation.Value = 0f;
        }

        public override void OnEntering()
        {
            ScreenManager.GetScreen<OsuScreen>().SyncObjectIndexToTime();
        }

        public override void Render(Graphics g)
        {

            base.Render(g);
        }

        public override void OnMouseWheel(float delta)
        {

            base.OnMouseWheel(delta);
        }
    }
}

