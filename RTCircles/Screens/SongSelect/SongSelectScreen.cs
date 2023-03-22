﻿using Easy2D;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OsuParsers.Beatmaps;
using Easy2D.Game;
using Silk.NET.Input;
using Realms;
using System.Threading;
using System.IO;

namespace RTCircles
{
    public class SongSelectScreen : Screen
    {
        public SongSelector SongSelector { get; private set; } = new SongSelector();

        private bool shouldGenGraph;

        public SongSelectScreen()
        {
            BeatmapMirror.OnNewBeatmapSetAvailable += (set, showNotification) =>
            {
                CarouselItem firstItem = null;
                foreach (var beatmap in set.Beatmaps)
                {
                    var itemAdded = AddBeatmapToCarousel(beatmap, BeatmapMirror.SongsDirectory);

                    if(firstItem == null)
                        firstItem = itemAdded;
                }

                if (!showNotification)
                    return;

                NotificationManager.ShowMessage($"Imported {firstItem.Folder}", Colors.LightGreen, 3, () => {
                    if (ScreenManager.ActiveScreen is not OsuScreen)
                    {
                        this.SongSelector.SelectBeatmap(firstItem);
                    }
                });
            };

            OsuContainer.BeatmapChanged += () =>
            {
                shouldGenGraph = true;
            };

            Add(SongSelector);
        }

        public CarouselItem AddBeatmapToCarousel(DBBeatmapInfo dBBeatmap, string songsDirectory)
        {
            //Dont add to carousel if we already have this item
            //Utils.Log($"Adding DBBeatmap: {dBBeatmap.Filename} Current carousel item count: {BeatmapCollection.Items.Count}", LogLevel.Debug);

            if (BeatmapCollection.HashedItems.TryGetValue(dBBeatmap.Hash, out var existingItem))
            {
                existingItem.SetDBBeatmap(dBBeatmap, songsDirectory);
                return existingItem;
            }

            if(dBBeatmap.SetInfo == null)
            {
                //Utils.Log($"Beatmap set info was null", LogLevel.Error);
                throw new Exception("SetInfo was null?");
            }

            CarouselItem newItem = new CarouselItem();
            newItem.SetDBBeatmap(dBBeatmap, songsDirectory);

            BeatmapCollection.AddItem(newItem);

            return newItem;
        }

        public void LoadCarouselItems(Realm realm)
        {
            
            foreach (var item in realm.All<DBBeatmapInfo>())
            {
                AddBeatmapToCarousel(item, BeatmapMirror.SongsDirectory);
                //Thread.Sleep(10);
                //Utils.Log($"Loaded DBBeatmap: {item.Filename}", LogLevel.Debug);
            }
            

            if(GlobalOptions.ShowStableMaps.Value)
            {
                string file = $"{GlobalOptions.OsuFolder.Value}/osu!.db";

                string songsDirectory = $"{GlobalOptions.OsuFolder.Value}/Songs";

                if (File.Exists(file) && Directory.Exists(songsDirectory))
                {
                    var dataBase = OsuParsers.Decoders.DatabaseDecoder.DecodeOsu(file);

                    foreach (var beatmap in dataBase.Beatmaps)
                    {
                        if (beatmap.Ruleset != OsuParsers.Enums.Ruleset.Standard)
                            continue;

                        if (string.IsNullOrEmpty(beatmap.MD5Hash))
                            continue;

                        DBBeatmapInfo fakeInfo = new DBBeatmapInfo();
                        fakeInfo.Hash = beatmap.MD5Hash;
                        fakeInfo.Filename = beatmap.FileName;
                        fakeInfo.SetInfo = new DBBeatmapSetInfo() { Foldername = beatmap.FolderName };

                        //Console.WriteLine($"Adding {fakeInfo.Filename}");
                        AddBeatmapToCarousel(fakeInfo, songsDirectory);
                    }

                    dataBase = null;

                    GPUSched.Instance.EnqueueDelayed(() =>
                    {
                        GC.Collect();
                    }, 5000);
                }
                else
                {
                    NotificationManager.ShowMessage($"{GlobalOptions.ShowStableMaps.Name} is on but no valid osu! db found or there were no songs directory",
                        new Vector4(1, 1, 0, 1), 2);
                }
            }
            
            //NotificationManager.ShowMessage($"{BeatmapCollection.Items.Count} beatmaps available", new Vector3(0.5f, 1, 0), 2);
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
            ScreenManager.GetScreen<OsuScreen>().EnsureObjectIndexSynchronization();
        }

        private FrameBuffer strainFB = new FrameBuffer(1, 1);
        private void drawDifficultyGraph(Graphics g)
        {
            if (OsuContainer.Beatmap == null || OsuContainer.Beatmap.DifficultyGraph.Count == 0 || MainGame.WindowWidth == 0 || MainGame.WindowHeight == 0)
                return;

            Vector2 size = new Vector2(MainGame.WindowWidth, 100);

            if (strainFB.Width != size.X || strainFB.Height != size.Y)
            {
                strainFB.EnsureSize(size.X, size.Y);
                shouldGenGraph = true;
            }

            if (shouldGenGraph)
            {
                Utils.BeginProfiling("StrainGraphGeneration");

                List<Vector2> graph = new List<Vector2>();

                foreach (var item in OsuContainer.Beatmap.DifficultyGraph)
                {
                    graph.Add(new Vector2(0, (float)item));
                }

                shouldGenGraph = false;

                var graphVertices = graph.Count * 2;

                if (graphVertices > 2)
                {
                    g.DrawInFrameBuffer(strainFB, () =>
                    {
                        //graph = PathApproximator.ApproximateCatmull(graph);

                        var vertices = g.VertexBuilder.GetTriangleStripSpan((uint)graphVertices);

                        int vertexIndex = 0;

                        int textureSlot = g.GetTextureSlot(null);

                        float stepX = size.X / graph.Count;

                        //Fake anti aliasing, only the tippy top of graph fades to 0 opacity
                        Vector4 peakColor = new Vector4(1f, 1f, 1f, 0f);
                        Vector4 bottomColor = new Vector4(1f, 1f, 1f, 10f);

                        Vector2 movingPos = Vector2.Zero;

                        for (int i = 0; i < graph.Count; i++)
                        {
                            //float height = graph[i].Y.Map(0, 10, 0, size.Y);

                            float height = graph[i].Y.Map(0, 10000, 10, size.Y);

                            //Grundlinje
                            vertices[vertexIndex].TextureSlot = textureSlot;
                            vertices[vertexIndex].Color = bottomColor;
                            vertices[vertexIndex].Position = movingPos;

                            vertexIndex++;

                            movingPos.Y += height;

                            //TopLinje
                            vertices[vertexIndex].TextureSlot = textureSlot;
                            vertices[vertexIndex].Color = peakColor;
                            vertices[vertexIndex].Position = movingPos;

                            movingPos.Y -= height;
                            movingPos.X += stepX;

                            vertexIndex++;
                        }
                    });
                }
                Utils.EndProfiling("StrainGraphGeneration");
            }


            Vector2 position = new Vector2(0, MainGame.WindowHeight - size.Y);

            float songX = (float)OsuContainer.SongPosition.Map(OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime, OsuContainer.Beatmap.HitObjects[^1].BaseObject.StartTime, 0, 1).Clamp(0, 1);

            Vector4 progressColor = new Vector4(0.6f, 1f, 0.6f, 0.35f);
            Vector4 progressNotColor = new Vector4(0f, 0f, 0f, 0.5f);

            Rectangle texRectProgress = new Rectangle(0, 0, songX, 1);

            Vector2 sizeProgress = new Vector2(strainFB.Texture.Width * songX, strainFB.Texture.Height);

            //Progress
            g.DrawRectangle(position, sizeProgress, progressColor, strainFB.Texture, texRectProgress, true);

            Rectangle texRectNotProgress = new Rectangle(songX, 0, 1 - songX, 1);
            Vector2 sizeNotProgress = new Vector2(strainFB.Texture.Width - (strainFB.Texture.Width * songX), strainFB.Texture.Height);

            //Not progress
            g.DrawRectangle(new Vector2(MainGame.WindowWidth - sizeNotProgress.X, MainGame.WindowHeight - sizeNotProgress.Y), sizeNotProgress, progressNotColor, strainFB.Texture, texRectNotProgress, true);
        }

        public override void Render(Graphics g)
        {
            base.Render(g);
            drawDifficultyGraph(g);
        }

        public override void OnMouseWheel(float delta)
        {
            base.OnMouseWheel(delta);
        }
    }
}

