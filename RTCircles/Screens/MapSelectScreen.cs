using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
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
                sFloat.Update((float)Game.Instance.RenderDeltaTime);

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


    public class BeatmapCarousel : Drawable
    {
        public override Rectangle Bounds => new Rectangle(0, 0, 1920, 1000);

        public static List<CarouselItem> Items = new List<CarouselItem>();

        public static List<CarouselItem> SearchItems = new List<CarouselItem>();

        private Vector2 itemSize => new Vector2(Bounds.Size.X, 128);

        public static event Action SearchResultsChanged;
        public static string SearchQuery;

        public BeatmapCarousel()
        {
            SearchItems = Items;
        }

        public void AddItem(CarouselItem item)
        {
            Items.Add(item);
            FindText(SearchQuery);
        }

        public void FindText(string text)
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

        public override void Update(float delta)
        {
            throw new NotImplementedException();
        }

        public override void Render(Graphics g)
        {
            throw new NotImplementedException();
        }
    }

    public class SongSelector : Drawable
    {
        public override Rectangle Bounds => new Rectangle();

        public static Vector4 SongInfoColor = Colors.From255RGBA(37, 37, 37, 255);//Colors.From255RGBA(211, 79, 115, 255);//Colors.From255RGBA(255, 80, 175, 255);
        public static Vector4 SongInfoTextColor = Colors.White;

        public static Vector4 ItemColor = Colors.From255RGBA(21, 21, 21, 255);//Colors.From255RGBA(67, 64, 65, 255);
        public static Vector4 ItemTextColor = Colors.From255RGBA(249, 0, 147, 255);
        public static float ItemHightlight = 1.25f;
        public static Vector4 ItemSelectedColor = Colors.From255RGBA(45, 45, 45, 255);//Colors.From255RGBA(255, 80, 175, 255);

        public static Vector4 HeaderColor = Colors.From255RGBA(53, 125, 237, 255);//Colors.From255RGBA(52, 49, 50, 255);
        public static Vector4 HeaderTextColor1 = Colors.White;
        public static Vector4 HeaderTextColor2 = Colors.White;

        private float scrollOffset = 0;
        private float scrollMomentum;
        private CarouselItem selectedItem;

        public Vector2 HeaderSize => new Vector2(MainGame.WindowWidth, 70 * MainGame.Scale);
        public Vector2 ElementSize => new Vector2(MainGame.WindowWidth / 2, 150 * MainGame.Scale);
        public Rectangle SongInfoBounds => new Rectangle(new Vector2(ElementSize.X, HeaderSize.Y), new Vector2(MainGame.WindowWidth - ElementSize.X, MainGame.WindowHeight - HeaderSize.Y));

        public Rectangle SongsBounds => new Rectangle(0, HeaderSize.Y, ElementSize.X, MainGame.WindowHeight - HeaderSize.Y);

        private bool clickedSomewhere = false;

        public FloatingScreen FloatingPlayScreen = new FloatingScreen() { Layer = 133769 };
        public SmoothFloat ConfirmPlayAnimation = new SmoothFloat();

        private Mods mods = Mods.NM;

        public float ScrollMin
        {
            get
            {
                float min = -(BeatmapCarousel.SearchItems.Count) * ElementSize.Y;
                min += MainGame.WindowHeight - HeaderSize.Y - MapSelectScreen.TextboxHeight;
                min = MathF.Min(0, min);
                return min;
            }
        }

        public float ScrollMax => 0;

        private float? scrollTo;

        public SongSelector()
        {
            BeatmapCarousel.SearchResultsChanged += () =>
            {
                scrollOffset = 0;
                scrollTo = null;
            };

            OsuContainer.BeatmapChanged += () =>
            {
                int index = BeatmapCarousel.SearchItems.FindIndex((o) => o.ID == OsuContainer.Beatmap.InternalBeatmap.MetadataSection.BeatmapID);

                if(index == -1)
                {
                    Utils.Log($"Could not scroll to selected beatmap, it wasnt found it the list of beatmaps!", LogLevel.Error);
                    return;
                }

                scrollTo = index * -ElementSize.Y + ((MainGame.WindowHeight - ElementSize.Y) / 2) - HeaderSize.Y;

                scrollTo = scrollTo.Value.Clamp(ScrollMin, ScrollMax);
                scrollMomentum = 0;

                if (selectedItem is null)
                    scrollOffset = scrollTo.Value;

                selectedItem = BeatmapCarousel.SearchItems[index];
            };
        }

        private BouncingButton downloadBtn, settingsBtn, modsBtn;

        private SliderBar speedBar, csBar, arBar, odBar, hpBar;

        public override void OnAdd()
        {
            FloatingPlayScreen.SetTarget<OsuScreen>();
            Container.Add(FloatingPlayScreen);

            downloadBtn = new BouncingButton(new Texture(Utils.GetResource("Skin.download-button.png")));
            downloadBtn.OnClick += () =>
            {
                ScreenManager.SetScreen<DownloadScreen>();
            };

            Container.Add(downloadBtn);

            settingsBtn = new BouncingButton(new Texture(Utils.GetResource("Skin.settings-button.png")));
            settingsBtn.OnClick += () =>
            {
                ScreenManager.SetScreen<OptionsScreen>();
            };

            Container.Add(settingsBtn);

            modsBtn = new BouncingButton(Skin.HRModIcon.Texture);
            modsBtn.OnClick += () =>
            {
                Utils.Log($"not yet.", LogLevel.Error);
            };

            Container.Add(modsBtn);


            speedBar = new SliderBar();

            speedBar.ButtonTexture = Texture.WhiteCircle;
            speedBar.BarColor = Colors.From255RGBA(246, 174, 45, 255);
            speedBar.MinValue = 10;
            speedBar.Value = 100;
            speedBar.MaxValue = 200;

            speedBar.ValueChanged += (e) =>
            {
                float scale = e / 100f;
                OsuContainer.Beatmap.Song.PlaybackSpeed = scale;
                string bpm = "??";

                if(OsuContainer.CurrentBeatTimingPoint != null)
                    bpm = $"{(60000 / OsuContainer.CurrentBeatTimingPoint.BeatLength) * scale:F0}";

                speedBar.Text = $"Song Speed: {scale:F2}x ({bpm} BPM";
            };

            csBar = new SliderBar();

            csBar.ButtonTexture = Texture.WhiteCircle;
            csBar.BarColor = Colors.From255RGBA(246, 174, 45, 255);
            csBar.MinValue = 0;
            csBar.MaxValue = 100;

            csBar.ValueChanged += (e) =>
            {
                float cs = e / 10f;
                OsuContainer.Beatmap.OverrideDifficulty(cs, OsuContainer.Beatmap.AR, OsuContainer.Beatmap.OD, OsuContainer.Beatmap.HP);
                csBar.Text = $"Circle Size: {cs:F1}";
            };

            arBar = new SliderBar();

            arBar.ButtonTexture = Texture.WhiteCircle;
            arBar.BarColor = Colors.From255RGBA(246, 174, 45, 255);
            
            arBar.MinValue = 0;
            arBar.MaxValue = 100;

            arBar.ValueChanged += (e) =>
            {
                float ar = e / 10f;
                OsuContainer.Beatmap.OverrideDifficulty(OsuContainer.Beatmap.CS, ar, OsuContainer.Beatmap.OD, OsuContainer.Beatmap.HP);
                arBar.Text = $"Approach Rate: {ar:F1}";
            };

            odBar = new SliderBar();

            odBar.ButtonTexture = Texture.WhiteCircle;
            odBar.BarColor = Colors.From255RGBA(246, 174, 45, 255);
            odBar.MinValue = 0;
            odBar.MaxValue = 100;

            odBar.ValueChanged += (e) =>
            {
                float od = e / 10f;
                OsuContainer.Beatmap.OverrideDifficulty(OsuContainer.Beatmap.CS, OsuContainer.Beatmap.AR, od, OsuContainer.Beatmap.HP);
                odBar.Text = $"Overall Difficulty: {od:F1}";
            };

            hpBar = new SliderBar();

            hpBar.ButtonTexture = Texture.WhiteCircle;
            hpBar.BarColor = Colors.From255RGBA(246, 174, 45, 255);
            hpBar.MinValue = 0;
            hpBar.MaxValue = 100;

            hpBar.ValueChanged += (e) =>
            {
                float hp = e / 10f;
                OsuContainer.Beatmap.OverrideDifficulty(OsuContainer.Beatmap.CS, OsuContainer.Beatmap.AR, OsuContainer.Beatmap.OD, hp);
                hpBar.Text = $"Hitpoints: {hp:F1}";
            };

            Container.Add(speedBar);
            Container.Add(csBar);
            Container.Add(arBar);
            Container.Add(odBar);
            Container.Add(hpBar);
        }

        public override void Render(Graphics g)
        {
            Vector2 offset = new Vector2();
            offset.Y += HeaderSize.Y;
            offset.Y += scrollOffset;

            for (int i = 0; i < BeatmapCarousel.SearchItems.Count; i++)
            {
                Rectangle bounds = new Rectangle(offset, ElementSize);
                var currentItem = BeatmapCarousel.SearchItems[i];

                //I NEVER USE GOTOS BUT ITS FINE TO DO HERE HONESTLY IT FITS
                if (bounds.Y < -HeaderSize.Y * 2)
                {
                    currentItem.OnHide();
                    goto incrementYOffset;
                }

                if (currentItem.OnShow() == false)
                {
                    BeatmapCarousel.SearchItems.RemoveAt(i);
                    continue;
                }

                Vector4 color = ItemColor;

                if (currentItem == selectedItem)
                    color = ItemSelectedColor;

                Vector2 bgSize = new Vector2(170, 128) * MainGame.Scale;
                Rectangle textureRect = new Rectangle();

                var texture = currentItem.Texture;

                float textureAlpha = currentItem.TextureAlpha;

                if (texture == null)
                {
                    texture = Skin.DefaultBackground;
                    textureAlpha = 1f;
                }

                float center = 0.5f;
                float width = bgSize.AspectRatio() / texture.Size.AspectRatio();
                center -= width / 2f;
                //Clip the texture rectangle to make the background fit into the thumbnail size regardless of aspect ratio
                textureRect = new Rectangle(center, 0, width, 1);

                if (bounds.IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
                {
                    color = Vector4.Clamp(color * ItemHightlight, Vector4.Zero, Vector4.One);

                    //We clicked on a item
                    if (clickedSomewhere)
                    {
                        //We clicked on it twice
                        if (selectedItem == currentItem)
                        {
                            enterSelectedMap();
                        }
                        else
                        {
                            selectMap(currentItem);
                        }
                        selectedItem = currentItem;
                    }
                }

                float bgPadding = (ElementSize.Y - bgSize.Y) / 2;

                g.DrawRectangle(bounds.Position, bounds.Size, color);
                g.DrawRectangle(bounds.Position + new Vector2(bgPadding), bgSize, new Vector4(1f, 1f, 1f, textureAlpha), texture, textureRect, true);

                float textScale = 0.5f * MainGame.Scale;
                Vector2 textSize = Font.DefaultFont.MessureString(currentItem.Text, textScale);
                Vector2 textPos = bounds.Position + new Vector2(bgSize.X + bgPadding * 2, ElementSize.Y / 2f - textSize.Y / 2f);
                g.DrawString(currentItem.Text, Font.DefaultFont, textPos, ItemTextColor, textScale);
                if (selectedItem == currentItem)
                {
                    string songInfoText = $"Objects: {OsuContainer.Beatmap?.HitObjects.Count} AR {OsuContainer.Beatmap?.AR} CS {OsuContainer.Beatmap?.CS} OD {OsuContainer.Beatmap?.OD} HP {OsuContainer.Beatmap?.HP}";
                    g.DrawString(songInfoText, Font.DefaultFont, textPos + new Vector2(0, textSize.Y), ItemTextColor, textScale * 0.8f);
                }

            incrementYOffset:
                offset.Y += ElementSize.Y;

                if (offset.Y > MainGame.WindowHeight)
                {
                    if(BeatmapCarousel.SearchItems.Count > i + 1)
                        BeatmapCarousel.SearchItems[i + 1].OnHide();

                    break;
                }
            }

            g.DrawRectangle(Vector2.Zero, HeaderSize, HeaderColor);
            g.DrawString("Songs", Font.DefaultFont, new Vector2(20, 20) * MainGame.Scale, HeaderTextColor1, 0.75f * MainGame.Scale);
            string sText = string.IsNullOrEmpty(BeatmapCarousel.SearchQuery) == false ? $"{BeatmapCarousel.SearchItems.Count} searched" : "";
            g.DrawString($"{BeatmapCarousel.Items.Count} available {sText}", Font.DefaultFont, new Vector2(160, 42) * MainGame.Scale, HeaderTextColor2, 0.25f * MainGame.Scale);

            g.DrawRectangle(SongInfoBounds.Position, SongInfoBounds.Size, SongInfoColor);

            if (selectedItem is not null)
            {
                float songInfoScale = 0.5f * MainGame.Scale;

                string songInfoTextTitle = $"{selectedItem.Text.Replace(".osu", "")}";
                Vector2 songInfoTitleSize = Font.DefaultFont.MessureString(songInfoTextTitle, songInfoScale);

                string songInfoText = $"Objects: {OsuContainer.Beatmap?.HitObjects.Count} AR {OsuContainer.Beatmap?.AR} CS {OsuContainer.Beatmap?.CS} OD {OsuContainer.Beatmap?.OD} HP {OsuContainer.Beatmap?.HP}";
                Vector2 songInfoTextSize = Font.DefaultFont.MessureString(songInfoText, songInfoScale);


                g.DrawString(songInfoTextTitle, Font.DefaultFont, HeaderSize.Center() - songInfoTitleSize / 2f, Colors.White, songInfoScale);
                //g.DrawString(songInfoTextTitle, Font.DefaultFont, new Vector2(SongInfoBounds.Center.X - songInfoTitleSize.X / 2f, HeaderSize.Y + FloatingPlayScreen.Size.Y), SongInfoTextColor, songInfoScale);

                //g.DrawString(songInfoText, Font.DefaultFont, new Vector2(SongInfoBounds.Center.X - songInfoTextSize.X / 2f, HeaderSize.Y + FloatingPlayScreen.Size.Y + songInfoTitleSize.Y), SongInfoTextColor, songInfoScale);
            }

            clickedSomewhere = false;

            Vector2 previewSize = new Vector2(SongInfoBounds.Width, SongInfoBounds.Width / MainGame.WindowSize.AspectRatio());
            Vector2 previewPos = SongInfoBounds.Position;

            previewSize.X = ConfirmPlayAnimation.Value.Map(0f, 1f, previewSize.X, MainGame.WindowWidth);
            previewSize.Y = ConfirmPlayAnimation.Value.Map(0f, 1f, previewSize.Y, MainGame.WindowHeight);

            previewPos.X = ConfirmPlayAnimation.Value.Map(0f, 1f, previewPos.X, 0);
            previewPos.Y = ConfirmPlayAnimation.Value.Map(0f, 1f, previewPos.Y, 0);

            FloatingPlayScreen.Position = previewPos;
            FloatingPlayScreen.Size = previewSize;
        }

        private void enterSelectedMap()
        {
            if (selectedItem is null)
                return;

            OsuContainer.Beatmap.Song.Pause();
            ConfirmPlayAnimation.ClearTransforms();
            ConfirmPlayAnimation.TransformTo(1f, 0.5f, EasingTypes.OutElasticHalf, () => {
                ScreenManager.SetScreen<OsuScreen>();
                //OsuContainer.Beatmap.Mods &= ~Mods.Auto;
            });
        }

        private void updateUI(float delta)
        {
            downloadBtn.Size = new Vector2(HeaderSize.Y / 1.2f);
            downloadBtn.Position = new Vector2(MainGame.WindowWidth - downloadBtn.Size.X / 2f, HeaderSize.Y / 2f);

            settingsBtn.Size = downloadBtn.Size;
            settingsBtn.Position = downloadBtn.Position - new Vector2(modsBtn.Size.X + 5, 0);

            modsBtn.Size = downloadBtn.Size;
            modsBtn.Position = settingsBtn.Position - new Vector2(modsBtn.Size.X + 5, 0);

            if (OsuContainer.Beatmap != null)
            {
                var startBars = new Vector2(SongInfoBounds.X + 100, FloatingPlayScreen.Position.Y + FloatingPlayScreen.Size.Y + 20);

                float yIncrease = 60 * MainGame.Scale;

                Vector2 buttonSize = new Vector2(35 * MainGame.Scale);

                speedBar.Value = (int)(OsuContainer.Beatmap.Song.PlaybackSpeed * 100);
                speedBar.ButtonSize = buttonSize;
                speedBar.BarThickness = 10 * MainGame.Scale;
                speedBar.BarLength = SongInfoBounds.Width - 100;
                speedBar.Position = startBars;
                speedBar.IsVisible = true;

                startBars.Y += yIncrease;

                csBar.SetValue((int)(OsuContainer.Beatmap.CS * 10));
                csBar.BarThickness = 10 * MainGame.Scale;
                csBar.ButtonSize = buttonSize;
                csBar.BarLength = SongInfoBounds.Width - 100;
                csBar.Position = startBars;
                csBar.IsVisible = true;

                startBars.Y += yIncrease;

                arBar.SetValue((int)(OsuContainer.Beatmap.AR * 10));
                arBar.BarThickness = 10 * MainGame.Scale;
                arBar.BarLength = SongInfoBounds.Width - 100;
                arBar.ButtonSize = buttonSize;
                arBar.Position = startBars;
                arBar.IsVisible = true;

                startBars.Y += yIncrease;

                odBar.SetValue((int)(OsuContainer.Beatmap.OD * 10));
                odBar.BarThickness = 10 * MainGame.Scale;
                odBar.BarLength = SongInfoBounds.Width - 100;
                odBar.ButtonSize = buttonSize;
                odBar.Position = startBars;
                odBar.IsVisible = true;

                startBars.Y += yIncrease;

                hpBar.SetValue((int)(OsuContainer.Beatmap.HP * 10));
                hpBar.BarThickness = 10 * MainGame.Scale;
                hpBar.BarLength = SongInfoBounds.Width - 100;
                hpBar.ButtonSize = buttonSize;
                hpBar.Position = startBars;
                hpBar.IsVisible = true;
            }
            else
            {
                speedBar.IsVisible = false;
                csBar.IsVisible = false;
                arBar.IsVisible = false;
                odBar.IsVisible = false;
                hpBar.IsVisible = false;
            }
        }

        public override void Update(float delta)
        {
            updateUI(delta);
            
            if (scrollTo.HasValue)
            {
                scrollOffset = MathHelper.Lerp(scrollOffset, scrollTo.Value, delta * 10f);
            }
            else if (dragging)
            {
                var now = Input.MousePosition;

                Vector2 diff = now - dragLastPosition;
                dragLastPosition = now;

                var distFromMax = Math.Max(scrollOffset - ScrollMax, 70) / 70;
                var distFromMin = Math.Max(ScrollMin - scrollOffset, 70) / 70;
                scrollOffset += diff.Y / (distFromMax * distFromMin);

                scrollMomentum = MathHelper.Lerp(scrollMomentum, diff.Y / delta, 10f * delta);
            }
            else
            {
                scrollOffset += scrollMomentum * delta;

                if (scrollOffset > ScrollMax)
                {
                    scrollOffset = MathHelper.Lerp(scrollOffset, 0, delta * 10f);
                }
                else if (scrollOffset < ScrollMin)
                {
                    scrollOffset = MathHelper.Lerp(scrollOffset, ScrollMin, delta * 10f);
                }

                scrollMomentum = MathHelper.Lerp(scrollMomentum, 0, delta * 10f);
            }
            ConfirmPlayAnimation.Update(delta);

            bsTimer -= delta;

            if(bsTimer <= 0)
            {
                bsTimer = 0.01f;
                //var randomBeatmap = BeatmapCarousel.SearchItems[RNG.Next(0, BeatmapCarousel.SearchItems.Count - 1)];
                //selectMap(randomBeatmap);
            }
        }

        private float bsTimer = 0.05f;

        private bool dragging;
        private Vector2 dragStart;
        private Vector2 dragLastPosition;
        public override bool OnMouseDown(MouseButton args)
        {
            if (new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(SongsBounds))
            {
                dragging = true;
                scrollTo = null;
                dragStart = Input.MousePosition;
                dragLastPosition = dragStart;
            }
            return false;
        }

        public override bool OnMouseUp(MouseButton button)
        {
            dragging = false;
            Vector2 dragEnd = Input.MousePosition;

            if (MathUtils.PositionInsideRadius(dragEnd, dragStart, 100) && button == MouseButton.Left)
                clickedSomewhere = true;

            if (MathF.Abs(scrollMomentum) < 600)
                scrollMomentum = 0;

            return false;
        }

        public override bool OnMouseWheel(float delta)
        {
            scrollMomentum += 1200 * delta;
            scrollTo = null;
            return true;
        }

        public override bool OnKeyDown(Key key)
        {
            if (key == Key.Enter)
            {
                enterSelectedMap();
            }

            if (key == Key.Escape)
            {
                if (ConfirmPlayAnimation.HasCompleted == false)
                {
                    OsuContainer.Beatmap.Song.Play(false);
                    ConfirmPlayAnimation.ClearTransforms();
                    ConfirmPlayAnimation.TransformTo(0f, 0.5f, EasingTypes.OutElasticQuarter);
                }
                else
                {
                    ScreenManager.GoBack();
                }
            }

            if (key == Key.F2 && BeatmapCarousel.SearchItems.Count > 0)
            {
                var randomBeatmap = BeatmapCarousel.SearchItems[RNG.Next(0, BeatmapCarousel.SearchItems.Count - 1)];

                selectMap(randomBeatmap);
            }

            if(key == Key.Up && selectedItem is not null)
            {
                int newIndex = BeatmapCarousel.SearchItems.IndexOf(selectedItem) - 1;

                if (newIndex < 0)
                    return false;

                selectMap(BeatmapCarousel.SearchItems[newIndex]);
            }

            if (key == Key.Down && selectedItem is not null)
            {
                int newIndex = BeatmapCarousel.SearchItems.IndexOf(selectedItem) + 1;

                if (newIndex == 0 || newIndex == BeatmapCarousel.SearchItems.Count)
                    return false;

                selectMap(BeatmapCarousel.SearchItems[newIndex]);
            }

            return false;
        }

        private void selectMap(CarouselItem item)
        {
            var beatmap = BeatmapMirror.DecodeBeatmap(File.OpenRead(item.FullPath));

            OsuContainer.SetMap(beatmap, true, mods);
            ScreenManager.GetScreen<OsuScreen>().OnEntering();

            int previewTime = OsuContainer.Beatmap.InternalBeatmap.GeneralSection.PreviewTime;

            //This monster is basically:
            //If the preview time is before the first hitobject
            //Try to find the first kiai instead
            //if that fails or the kiai is before the first hitobject, then use the first hitobject as the preview time.
            if(OsuContainer.Beatmap.HitObjects.Count > 0 && previewTime < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
            {
                var firstKiai = OsuContainer.Beatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai);

                if (firstKiai != null && firstKiai.Offset >= OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                    previewTime = firstKiai.Offset - (int)OsuContainer.Beatmap.Preempt;
                else
                    previewTime = OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime - (int)OsuContainer.Beatmap.Preempt; 
            }

            OsuContainer.SongPosition = previewTime;
            ScreenManager.GetScreen<OsuScreen>().SyncObjectIndexToTime();
        }

        public void DeleteSelectedItem()
        {
            if(selectedItem != null)
            {
                BeatmapCarousel.SearchItems.Remove(selectedItem);
                BeatmapCarousel.Items.Remove(selectedItem);
                selectedItem = null;
            }
        }
    }

    public class MapSelectScreen : Screen
    {
        public BeatmapCarousel BeatmapCarousel { get; private set; } = new BeatmapCarousel();

        private Textbox searchBox = new Textbox();
        private SongSelector songSelector = new SongSelector();

        public static float TextboxHeight => 80f * MainGame.Scale;

        public MapSelectScreen()
        {
            BeatmapMirror.OnNewBeatmapAvailable += (beatmap) =>
            {
                AddBeatmapToCarousel(beatmap);
            };

            Add(songSelector);
            
            searchBox.TextHint = "Type search query";
            searchBox.Color = Colors.From255RGBA(37, 37, 37, 255);
            searchBox.TextHintColor = Colors.From255RGBA(67, 67, 67, 255);
            searchBox.TextColor = Colors.White;
            searchBox.TextDeleteColor = new Vector4(2f, 0f, 0f, 1f);
            searchBox.RemoveFocusOnEnter = false;
            searchBox.RemoveFocus = false;
            searchBox.HasFocus = true;

            searchBox.OnTextChanged += () => {
                searchTimer = 0.25f;
            };

            Add(searchBox);
        }

        public void AddBeatmapToCarousel(DBBeatmap dBBeatmap)
        {
            //Dont add to carousel if we already have this item
            Utils.Log($"Adding DBBeatmap: {dBBeatmap.File} Current carousel item count: {BeatmapCarousel.Items.Count}", LogLevel.Debug);

            for (int i = 0; i < BeatmapCarousel.Items.Count; i++)
            {
                if (BeatmapCarousel.Items[i].ID == dBBeatmap.ID)
                {
                    Utils.Log($"A duplicate map was added to the carousel, the old map was changed to the new one", LogLevel.Warning);
                    BeatmapCarousel.Items[i].SetDBBeatmap(dBBeatmap);
                    return;
                }
            }

            CarouselItem newItem = new CarouselItem();
            newItem.SetDBBeatmap(dBBeatmap);

            BeatmapCarousel.AddItem(newItem);
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
            if(key == Key.Delete && Input.IsKeyDown(Key.ControlLeft))
            {
                BeatmapMirror.Scheduler.Enqueue(() =>
                {
                    songSelector.DeleteSelectedItem();
                    OsuContainer.UnloadMap();
                    
                    //BeatmapMirror.Realm.Remove()
                });
            }

            base.OnKeyDown(key);
        }

        private float searchTimer;
        public override void Update(float delta)
        {
            searchBox.Size = new Vector2(songSelector.ElementSize.X, TextboxHeight);
            searchBox.Position = new Vector2(0, MainGame.WindowHeight - searchBox.Size.Y);

            if (searchTimer > 0f)
            {
                searchTimer -= delta;

                searchTimer = Math.Max(0, searchTimer);
                
                if (searchTimer == 0f)
                {
                    BeatmapCarousel.FindText(searchBox.Text);
                    searchTimer = -1;
                }
            }

            base.Update(delta);
        }

        public override void OnExit()
        {
            songSelector.ConfirmPlayAnimation.Value = 0f;
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
