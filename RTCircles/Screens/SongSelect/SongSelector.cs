using Easy2D;
using OpenTK.Mathematics;
using System;
using System.IO;
using Easy2D.Game;
using Silk.NET.Input;
using System.Collections;
using System.Collections.Generic;

namespace RTCircles
{
    public class SongSelector : Drawable
    {
        public override Rectangle Bounds => new Rectangle();

        public static Vector4 SongInfoColor = Colors.From255RGBA(37, 37, 37, 255);//Colors.From255RGBA(211, 79, 115, 255);//Colors.From255RGBA(255, 80, 175, 255);
        public static Vector4 SongInfoTextColor = Colors.White;

        public static Vector4 ItemColor = Colors.From255RGBA(21, 21, 21, 175);//Colors.From255RGBA(67, 64, 65, 255);
        public static Vector4 ItemTextColor = Colors.From255RGBA(150, 150, 150, 255);
        public static Vector4 ItemSelectedTextColor = Colors.From255RGBA(255, 255, 255, 255);
        public static Vector4 ItemSelectedColor = Colors.From255RGBA(100, 100, 100, 175);//Colors.From255RGBA(255, 80, 175, 255);

        public static Vector4 HeaderColor = Colors.From255RGBA(12, 12, 12, 255);//Colors.From255RGBA(52, 49, 50, 255);
        public static Vector4 HeaderTextColor1 = Colors.White;
        public static Vector4 HeaderTextColor2 = Colors.White;

        private MapBackground bg = new MapBackground();
        private FrameBuffer blurBuffer = new FrameBuffer(1, 1, Silk.NET.OpenGLES.FramebufferAttachment.ColorAttachment0, Silk.NET.OpenGLES.InternalFormat.Rgb, Silk.NET.OpenGLES.PixelFormat.Rgb);

        private double scrollOffset = 0;
        private double scrollMomentum;
        private CarouselItem selectedItem;

        public Vector2 HeaderSize => new Vector2(MainGame.WindowWidth, 70 * MainGame.Scale);

        public int ElementSpacing => (int)(8f * MainGame.Scale);

        public Vector2i ElementSize => (Vector2i)(new Vector2(950, 140) * MainGame.Scale);
        public Rectangle SongInfoBounds => new Rectangle(new Vector2(0, HeaderSize.Y), new Vector2(600 * MainGame.Scale, MainGame.WindowHeight - HeaderSize.Y));

        public Rectangle SongsBounds => new Rectangle(MainGame.WindowWidth - ElementSize.X, HeaderSize.Y, ElementSize.X, MainGame.WindowHeight);

        private bool clickedSomewhere = false;

        public FloatingPlayScreen FloatingPlayScreen = new FloatingPlayScreen() { Layer = 133769 };
        public SmoothFloat ConfirmPlayAnimation = new SmoothFloat();

        private Mods mods = Mods.NM;

        public int ScrollMin
        {
            get
            {
                int itemSize = ElementSize.Y + ElementSpacing;

                int min = -((BeatmapCollection.SearchItems.Count * itemSize)) + itemSize / 2 + (int)HeaderSize.Y;
                min = Math.Min(0, min);
                return min;
            }
        }

        public int ScrollMax => (int)(MainGame.WindowHeight - ElementSize.Y - ElementSize.Y / 2);

        private double? scrollTo;

        private bool shouldGenBlur;

        public SongSelector()
        {
            BeatmapCollection.SearchResultsChanged += () =>
            {
                scrollOffset = 0;
                scrollTo = null;
            };

            OsuContainer.BeatmapChanged += () =>
            {
                if (OsuContainer.Beatmap.IsNewBackground)
                {
                    shouldGenBlur = true;
                    OsuContainer.Beatmap.Background.Bind(0);
                }

                int index = BeatmapCollection.SearchItems.FindIndex((o) => o.Hash == OsuContainer.Beatmap.Hash);
                
                TryScrollToItemAtIndex(index);
            };
        }

        public void TryScrollToItemAtIndex(int index, bool instant = false)
        {
            if (index < 0)
            {
                //If the index couldn't be found, just force it to search all items
                BeatmapCollection.SearchItems = BeatmapCollection.Items;
                index = BeatmapCollection.SearchItems.FindIndex((o) => o.Hash == OsuContainer.Beatmap.Hash);
            }

            scrollTo = -(ElementSize.Y + ElementSpacing) * index;

            scrollTo += ((MainGame.WindowHeight - ElementSize.Y + ElementSpacing) / 2) - HeaderSize.Y;

            scrollMomentum = 0;

            if (selectedItem is null || instant)
                scrollOffset = scrollTo.Value;

            selectedItem = BeatmapCollection.SearchItems[index];
        }

        private BouncingButton downloadBtn, settingsBtn, modsBtn;

        public override void OnAdd()
        {
            bg.ParallaxAmount = 0;
            bg.ShowMenuFlash = false;
            bg.Opacity = 0.5f;
            bg.KiaiFlash = 0.75f;
            bg.TriggerFadeIn();
            Container.Add(bg);

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
        }


        private Vector2i prevViewport;
        private List<CarouselItem> lastViewedItems = new List<CarouselItem>();
        private List<CarouselItem> viewedItems = new List<CarouselItem>();
        public override void Render(Graphics g)
        {
            if (shouldGenBlur)
            {
                //Todo: Istedet for at resize blur buffer, så have blur bufferen relateret til screen resolution, og så render texture med offset i den og så blur den efterfølgende
                if (OsuContainer.Beatmap.Background.ImageDoneUploading)
                {
                    blurBuffer.EnsureSize(OsuContainer.Beatmap.Background.Width / 4, OsuContainer.Beatmap.Background.Height / 4);
                    Blur.BlurTexture(OsuContainer.Beatmap.Background, blurBuffer, 1, 2);
                    bg.TextureOverride = blurBuffer.Texture;
                    shouldGenBlur = false;
                }
            }

            if(prevViewport != (Vector2i)Viewport.Area.Size)
            {
                prevViewport = (Vector2i)Viewport.Area.Size;

                if (selectedItem == null)
                    TryScrollToItemAtIndex(0, instant: true);
                else
                    TryScrollToItemAtIndex(BeatmapCollection.SearchItems.FindIndex((o) => o == selectedItem), instant: true);
            }

            Vector2 offset = new Vector2();
            offset.Y += HeaderSize.Y;
            offset.X += MainGame.WindowWidth - ElementSize.X;
            offset.Y += (float)scrollOffset;

            //Console.WriteLine(scrollOffset.ToString() + " min: " + ScrollMin + " actual: " + offset.Y + $" diff: {scrollOffset-offset.Y}");
            for (int i = 0; i < BeatmapCollection.SearchItems.Count; i++)
            {
                if (offset.Y < -HeaderSize.Y * 2)
                {
                    //Item is not viewable because it's outside the screen
                    goto incrementYOffset;
                }

                Rectangle bounds = new Rectangle(offset, ElementSize);

                var currentItem = BeatmapCollection.SearchItems[i];

                viewedItems.Add(currentItem);
                currentItem.Update();

                float distance = MathF.Abs(MainGame.WindowCenter.Y - bounds.Center.Y);

                bounds.X = MainGame.WindowWidth - ElementSize.X * Interpolation.ValueAt(distance, 1, 0.85f, 0, MainGame.WindowCenter.Y, EasingTypes.In);//distance.Map(0, MainGame.WindowCenter.Y, 1, 0.8f);

                Vector4 color = ItemColor;

                if (currentItem == selectedItem)
                    color = ItemSelectedColor;

                Vector2 bgSize = new Vector2(160, 130) * MainGame.Scale;
                Rectangle textureRect = new Rectangle();

                var texture = currentItem.Texture;

                float textureAlpha = currentItem.TextureAlpha;
                //If texture is done uploading
                if (texture != null)
                {
                    float center = 0.5f;
                    float width = bgSize.AspectRatio() / texture.Size.AspectRatio();
                    center -= width / 2f;
                    //Clip the texture rectangle to make the background fit into the thumbnail size regardless of aspect ratio
                    textureRect = new Rectangle(center, 0, width, 1);
                }

                if (bounds.IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
                {
                    color.W = 0.85f;

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
                            SelectBeatmap(currentItem);
                        }
                        selectedItem = currentItem;
                    }
                }

                float bgPadding = (ElementSize.Y - bgSize.Y) / 2;

                g.DrawRectangle(bounds.Position, bounds.Size, color);
                g.DrawRectangle(bounds.Position + new Vector2(bgPadding), bgSize, new Vector4(1f, 1f, 1f, selectedItem == currentItem ? textureAlpha : 0.75f*textureAlpha), texture, textureRect, true);

                float textScale = 0.5f * MainGame.Scale;
                Vector2 textSize = Font.DefaultFont.MessureString(currentItem.Text, textScale);
                Vector2 textPos = bounds.Position + new Vector2(bgSize.X + bgPadding * 2, bgPadding * 2);
                g.DrawString(currentItem.Text, Font.DefaultFont, textPos, selectedItem == currentItem ? ItemSelectedTextColor : ItemTextColor, textScale);

                if (selectedItem == currentItem)
                {
                    string songInfoText = $"Objects: {OsuContainer.Beatmap?.HitObjects.Count} AR {OsuContainer.Beatmap?.AR} CS {OsuContainer.Beatmap?.CS} OD {OsuContainer.Beatmap?.OD} HP {OsuContainer.Beatmap?.HP}";
                    g.DrawString(songInfoText, Font.DefaultFont, textPos + new Vector2(0, textSize.Y), ItemSelectedTextColor, textScale * 0.8f);
                }

            incrementYOffset:
                offset.Y += ElementSize.Y + ElementSpacing;

                
                //We incremented the offset and it was outside the screen, so break the loop, since no more items can be rendered.
                if (offset.Y > MainGame.WindowHeight)
                    break;
            }

            for (int i = 0; i < viewedItems.Count; i++)
            {
                if (!lastViewedItems.Contains(viewedItems[i]))
                {
                    viewedItems[i].OnShow();
                    //Console.WriteLine($"Show item: {viewedItems[i].Hash}");
                }
            }

            for (int i = 0; i < lastViewedItems.Count; i++)
            {
                if (!viewedItems.Contains(lastViewedItems[i]))
                {
                    lastViewedItems[i].OnHide();
                    //Console.WriteLine($"Hide item: {lastViewedItems[i].Hash}");
                }
            }

            lastViewedItems.Clear();
            lastViewedItems.AddRange(viewedItems);
            viewedItems.Clear();

            string searchForString = "Search: ";
            float searchForStringScale = 0.5f * MainGame.Scale;
            Vector2 searchForStringSize = Font.DefaultFont.MessureString(searchForString, searchForStringScale, true);
            g.DrawRectangle(SongsBounds.Position, new Vector2(ElementSize.X, ElementSize.Y / 2.3f), new Vector4(0, 0, 0, 0.5f));

            Vector2 searchForStringPos = SongsBounds.Position;
            searchForStringPos.X += 15f * MainGame.Scale;
            searchForStringPos.Y -= 5f * MainGame.Scale;

            g.DrawStringNoAlign(searchForString, Font.DefaultFont, searchForStringPos, (Vector4)Color4.LawnGreen, searchForStringScale);

            if (SongSelectScreen.SearchText != null)
                g.DrawStringNoAlign(SongSelectScreen.SearchText, Font.DefaultFont, searchForStringPos + new Vector2(searchForStringSize.X, 0), Colors.White, searchForStringScale);

            searchForStringPos.Y += searchForStringSize.Y * 1.6f;
            g.DrawStringNoAlign($"Found {BeatmapCollection.SearchItems.Count} results", Font.DefaultFont, searchForStringPos, Colors.White, searchForStringScale / 2);

            g.DrawRectangle(Vector2.Zero, HeaderSize, HeaderColor);

            if (selectedItem is not null)
            {
                g.DrawStringCentered(selectedItem.Text, ResultScreen.Font, HeaderSize.Center(), HeaderTextColor1, 0.75f*MainGame.Scale);
            }

            clickedSomewhere = false;

            //Visualize songinfobounds
            //g.DrawRectangle(SongInfoBounds.Position, SongInfoBounds.Size, new Vector4(0f, 1f, 0f, 0.5f));

            float border = 0.98f;

            float infoWidth = SongInfoBounds.Width * border;

            Vector2 previewSize = new Vector2(infoWidth, infoWidth / 1.77777f);
            Vector2 previewPos = SongInfoBounds.Position;

            float padding = (SongInfoBounds.Width - previewSize.X) / 2;

            previewPos.X += padding;
            previewPos.Y += padding;

            FloatingPlayScreen.Position = previewPos;
            FloatingPlayScreen.Size = previewSize;
            /*
        Vector2 previewSize = new Vector2(SongInfoBounds.Width, SongInfoBounds.Width / 1.77777f);
        Vector2 previewPos = SongInfoBounds.Position;

        previewSize.X = ConfirmPlayAnimation.Value.Map(0f, 1f, previewSize.X, MainGame.WindowWidth);
        previewSize.Y = ConfirmPlayAnimation.Value.Map(0f, 1f, previewSize.Y, MainGame.WindowHeight);

        previewPos.X = ConfirmPlayAnimation.Value.Map(0f, 1f, previewPos.X, 0);
        previewPos.Y = ConfirmPlayAnimation.Value.Map(0f, 1f, previewPos.Y, 0);

        FloatingPlayScreen.Position = previewPos;
        FloatingPlayScreen.Size = previewSize;
            */
        }

        private void enterSelectedMap()
        {
            if (selectedItem is null)
                return;

            if (Input.IsKeyDown(Key.ControlRight) || Input.IsKeyDown(Key.ControlLeft))
                OsuContainer.Beatmap.Mods |= Mods.Auto;
            else
                OsuContainer.Beatmap.Mods &= ~Mods.Auto;

            ScreenManager.SetScreen<OsuScreen>();
            return;

            OsuContainer.Beatmap.Song.Pause();
            ConfirmPlayAnimation.ClearTransforms();
            ConfirmPlayAnimation.TransformTo(1f, 0.5f, EasingTypes.OutElasticHalf, () =>
            {
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
        }

        public override void Update(float delta)
        {
            updateUI(delta);

            if (scrollTo.HasValue)
            {
                scrollOffset = MathHelper.Lerp(scrollOffset, scrollTo.Value.Clamp(ScrollMin, ScrollMax), delta * 10f);
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
                    scrollOffset = MathHelper.Lerp(scrollOffset, ScrollMax, delta * 10f);
                }
                else if (scrollOffset < ScrollMin)
                {
                    scrollOffset = MathHelper.Lerp(scrollOffset, ScrollMin, delta * 10f);
                }

                scrollMomentum = MathHelper.Lerp(scrollMomentum, 0, delta * 7f);
            }

            ConfirmPlayAnimation.Update(delta);
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

            if (MathUtils.IsPointInsideRadius(dragEnd, dragStart, 100) && button == MouseButton.Left)
            {
                clickedSomewhere = true;
                dragEnd = dragStart;
                return true;
            }

            if (Math.Abs(scrollMomentum) < 600)
                scrollMomentum = 0;

            return false;
        }

        public override bool OnMouseWheel(float delta)
        {
            scrollMomentum += 800 * delta;
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

            if (key == Key.F2 && BeatmapCollection.SearchItems.Count > 0)
            {
                var randomBeatmap = BeatmapCollection.SearchItems[RNG.Next(0, BeatmapCollection.SearchItems.Count - 1)];

                SelectBeatmap(randomBeatmap);
            }

            if (key == Key.Up && selectedItem is not null)
            {
                int newIndex = BeatmapCollection.SearchItems.IndexOf(selectedItem) - 1;

                if (newIndex < 0)
                    return false;

                SelectBeatmap(BeatmapCollection.SearchItems[newIndex]);
            }

            if (key == Key.Down && selectedItem is not null)
            {
                int newIndex = BeatmapCollection.SearchItems.IndexOf(selectedItem) + 1;

                if (newIndex == 0 || newIndex == BeatmapCollection.SearchItems.Count)
                    return false;

                SelectBeatmap(BeatmapCollection.SearchItems[newIndex]);
            }

            return false;
        }

        public void SelectBeatmap(CarouselItem item)
        {
            if (selectedItem == item)
                return;

            if (!OsuContainer.SetMap(item, true, mods))
            {
                BeatmapCollection.SearchItems.Remove(item);
                return;
            }

            var osuScreen = ScreenManager.GetScreen<OsuScreen>();

            osuScreen.ResetState();

            int previewTime = OsuContainer.Beatmap.InternalBeatmap.GeneralSection.PreviewTime;

            //This monster is basically:
            //If the preview time is before the first hitobject
            //Try to find the first kiai instead
            //if that fails or the kiai is before the first hitobject, then use the first hitobject as the preview time.
            if (OsuContainer.Beatmap.HitObjects.Count > 0 && previewTime < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
            {
                var firstKiai = OsuContainer.Beatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai);

                if (firstKiai != null && firstKiai.Offset >= OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                    previewTime = firstKiai.Offset - (int)OsuContainer.Beatmap.Preempt;
                else
                    previewTime = OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime - (int)OsuContainer.Beatmap.Preempt;
            }

            OsuContainer.SongPosition = previewTime;
            osuScreen.EnsureObjectIndexSynchronization();

            if(previewTime > -1)
            OsuContainer.Beatmap.Song.Play(false);
        }
    }
}

