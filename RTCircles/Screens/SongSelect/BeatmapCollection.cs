using System.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Easy2D;

namespace RTCircles
{
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
            FindText(SearchQuery, false);
        }

        public static void DeleteMap(CarouselItem item)
        {
            HashedItems.Remove(item.Hash);
            Items.Remove(item);
            SearchItems.Remove(item);

            if (File.Exists(item.FullPath))
            {
                File.Delete(item.FullPath);
            }

            BeatmapMirror.DatabaseAction((realm) =>
            {
                var bm = realm.Find<DBBeatmapInfo>(item.Hash);

                realm.Write(() =>
                {
                    bm.SetInfo.Beatmaps.Remove(bm);
                    realm.Remove(bm);

                    if(bm.SetInfo.Beatmaps.Count == 0)
                    {
                        Directory.Delete($"{BeatmapMirror.SongsDirectory}/{bm.SetInfo.Foldername}");
                    }
                });
            });

            NotificationManager.ShowMessage($"Beatmap with hash: {item.Hash} deleted", Colors.CornflowerBlue, 5f);
        }

        public static void FindText(string text, bool invokeSearchResultChange = true)
        {
            SearchQuery = text;

            if (string.IsNullOrEmpty(text))
            {
                SearchItems = Items;

                if(invokeSearchResultChange)
                    SearchResultsChanged?.Invoke();

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

            if(invokeSearchResultChange)
                SearchResultsChanged?.Invoke();
        }
    }
}

