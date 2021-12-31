using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RTCircles
{
    public class ScrollBox : Drawable
    {
        public Vector2 Size;
        public Vector2 Position;

        public class Item
        {
            public string Text;
            public float TextScale = 1f;
            public Vector4 Color = Colors.From255RGBA(37, 37, 37, 255);
            public Vector4 HighlightColor = Colors.From255RGBA(37*2, 37*2, 37*2, 255);
            public Vector4? SelectColor;
            public Vector4 TextColor = Colors.White;
            public Action<Item> OnClick;
            public Action<Item> OnSelect;
            public object Tag;
            public Texture Icon;
            public double DownloadPercentage = 0;
            public Vector4 DownloadColor = new Vector4(0f, 1f, 0f, 1f);

            public Vector2? SizeOverride;
            public Vector2 PositionOffset;
        }

        public List<Item> Items = new List<Item>();

        private Item selectedItem;
        public int ItemsShown = 10;

        public override Rectangle Bounds => new Rectangle(Position, Size);

        public Vector2 ElementSize => new Vector2(Size.X, Size.Y / ItemsShown);

        public event Action OnReachedEndOfList;

        public float ScrollMin
        {
            get
            {
                float min = -(Items.Count) * ElementSize.Y;
                min += Size.Y - ElementSize.Y;
                min = MathF.Min(0, min);
                return min;
            }
        }

        public void Clear()
        {
            Items.Clear();
            scrollOffset = 0;
        }

        public float ScrollMax => 0;

        private float scrollOffset;
        private float scrollMomentum;
        public override void Render(Graphics g)
        {
            Vector2 offset = Position;
            offset.Y += scrollOffset;

            for (int i = 0; i < Items.Count; i++)
            {
                var currentItem = Items[i];

                Rectangle bounds = new Rectangle(offset, ElementSize);

                //I NEVER USE GOTOS BUT ITS FINE TO DO HERE HONESTLY IT FITS
                if (bounds.Y < 0)
                {
                    //currentItem.OnHide();
                    goto incrementYOffset;
                }

                /*
                if (currentItem.OnShow() == false)
                {
                    BeatmapCarousel.SearchItems.RemoveAt(i);
                    continue;
                }
                */

                Vector4 color = currentItem.Color;

                if (currentItem == selectedItem && currentItem.SelectColor.HasValue)
                    color = currentItem.SelectColor.Value;

                if (bounds.IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
                {
                    if(currentItem != selectedItem)
                        color = currentItem.HighlightColor;

                    //We clicked on a item
                    if (clickedSomewhere)
                    {
                        //We clicked on it twice
                        if (selectedItem == currentItem)
                        {
                            // enterSelectedMap();
                            currentItem.OnClick?.Invoke(currentItem);
                        }
                        else
                        {
                            currentItem.OnSelect?.Invoke(currentItem);
                        }
                        selectedItem = currentItem;
                        clickedSomewhere = false;
                    }
                }

                Vector2 bgSize = Vector2.Zero;

                var texture = currentItem.Icon;

                if (texture is not null)
                {
                    bgSize = new Vector2(((float)(texture.Width) / texture.Height) * 128f, 128) * MainGame.Scale;
                    bgSize.X = float.IsFinite(bgSize.X) ? bgSize.X : 0;
                }

                float bgPadding = (ElementSize.Y - bgSize.Y) / 2;

                g.DrawRectangle(bounds.Position, bounds.Size, color);
                g.DrawRectangle(bounds.Position + new Vector2(bgPadding), bgSize, new Vector4(1f, 1f, 1f, 1f), texture);

                g.DrawRectangle(bounds.Position, new Vector2((float)currentItem.DownloadPercentage.Map(0, 100, 0, bounds.Width), bounds.Height), currentItem.DownloadColor);

                float textScale = 0.5f * MainGame.Scale;
                Vector2 textSize = Font.DefaultFont.MessureString(currentItem.Text, textScale);
                Vector2 textPos = bounds.Position + new Vector2(bgSize.X, ElementSize.Y / 2f - textSize.Y / 2f);
                g.DrawString(currentItem.Text, Font.DefaultFont, textPos, Colors.White, textScale);

            incrementYOffset:
                offset.Y += ElementSize.Y;

                if (offset.Y > MainGame.WindowHeight)
                {
                    break;
                }
            }
        }

        private bool reachedEndOfListFireOnce;
        private Vector2 dragLastPosition;
        private Vector2 dragDiff;
        private float dragMomentum;
        public override void Update(float delta)
        {
            dragMomentum = MathHelper.Lerp(dragMomentum, 0, 5f * delta);
            if (dragging)
            {
                dragDiff = Input.MousePosition - dragLastPosition;
                dragLastPosition = Input.MousePosition;

                var distFromMax = Math.Max(scrollOffset - ScrollMax, 70) / 70;
                var distFromMin = Math.Max(ScrollMin - scrollOffset, 70) / 70;
                scrollOffset += dragDiff.Y / (distFromMax * distFromMin);
                dragMomentum += dragDiff.Y * 20;

                scrollMomentum = 0;
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

            if (scrollOffset < ScrollMin && reachedEndOfListFireOnce == false && Items.Count > 0)
            {
                OnReachedEndOfList?.Invoke();
                reachedEndOfListFireOnce = true;
            }
            else
            {
                reachedEndOfListFireOnce = false;
            }
        }

        private bool clickedSomewhere;
        public override bool OnMouseWheel(float delta)
        {
            return false;
        }

        private bool dragging;
        private Vector2 dragStart;
        public override bool OnMouseDown(MouseButton button)
        {
            dragging = true;
            dragStart = Input.MousePosition;
            dragLastPosition = dragStart;

            return false;
        }

        public override bool OnMouseUp(MouseButton button)
        {
            dragging = false;
            Vector2 dragEnd = Input.MousePosition;

            if (MathUtils.PositionInsideRadius(dragEnd, dragStart, 100) && button == MouseButton.Left)
                clickedSomewhere = true;
            else
            {
                if (MathF.Abs(dragMomentum) > 3000)
                {
                    scrollMomentum = dragMomentum;
                }
            }

            dragMomentum = 0;

            return false;
        }
    }

    public class DownloadScreen : Screen
    {
        private Textbox searchTextbox;

        private BeatmapMirror.SayobotBeatmapList beatmapList;
        private ScrollBox scrollBox;

        private string searchText;

        private int resultsGotten = 0;
        private int count = 0;
        private bool moreContentInProgress = false;

        public DownloadScreen()
        {
            scrollBox = new ScrollBox();

            scrollBox.OnReachedEndOfList += () =>
            {
                int dataleft = Math.Min(count - resultsGotten, 25);

                if (resultsGotten == count)
                    return;

                if (moreContentInProgress)
                    return;

                moreContentInProgress = true;

                Utils.Log($"Reached end of scrollbox list", LogLevel.Info);

                if (beatmapList is null)
                    return;

                if (beatmapList.data is null)
                    return;

                GPUSched.Instance.AddAsync((ct) =>
                {
                    beatmapList = BeatmapMirror.Sayobot_GetBeatmapList(searchText, beatmapList.endid, dataleft);

                    resultsGotten += beatmapList.data.Count;

                    AddToList(beatmapList);
                }, () =>
                {
                    moreContentInProgress = false;
                });
            };
            Add(scrollBox);

            searchTextbox = new Textbox();
            searchTextbox.TextHint = "Type beatmap search query";
            searchTextbox.RemoveFocusOnEnter = false;
            searchTextbox.HasFocus = true;

            searchTextbox.OnEnter += (s, e) =>
            {
                firstSearch();
            };

            Add(searchTextbox);
        }

        private void firstSearch()
        {
            searchTextbox.Disabled = true;
            moreContentInProgress = false;
            resultsGotten = 0;
            count = 0;
            searchTextbox.TextHint = "Searching...";
            GPUSched.Instance.AddAsync((ct) =>
            {
                searchText = searchTextbox.Text;
                searchTextbox.Text = "";
                scrollBox.Clear();

                beatmapList = BeatmapMirror.Sayobot_GetBeatmapList(searchText);

                searchTextbox.TextHint = $"{beatmapList.results} Results found";

                if (beatmapList.data is null)
                {
                    beatmapList = null;
                    return;
                }

                count = beatmapList.results;
                resultsGotten += beatmapList.data.Count;
                AddToList(beatmapList);
            }, () =>
            {
                searchTextbox.Disabled = false;
            });
        }

        private void AddToList(BeatmapMirror.SayobotBeatmapList list)
        {
            for (int i = list.data.Count - 1; i >= 0; i--)
            {
                var bm = list.data[i];

                if (Directory.Exists($"{BeatmapMirror.SongsFolder}/{bm.sid}"))
                {
                    Utils.Log($"Ignored a beatmap we already have", LogLevel.Info);
                    continue;
                }

                var item = new ScrollBox.Item()
                {
                    Text = $"{bm.approved} : {bm.artist} - {bm.title} // {bm.creator} -> [{bm.sid}]",
                    Tag = false,
                    OnClick = (i) =>
                    {
                        if ((bool)i.Tag == false)
                        {
                            i.Tag = true;
                            i.Text += " DOWNLOADING!";
                            BeatmapMirror.Sayobot_DownloadBeatmapSet(bm.sid, (toDownload, downloaded) =>
                            {
                                if (toDownload.HasValue)
                                {
                                    double percent = (double)downloaded / toDownload.Value;
                                    percent *= 100;

                                    i.DownloadPercentage = percent;
                                    if (percent == 100)
                                    {
                                        scrollBox.Items.Remove(i);
                                    }
                                }
                            }, () =>
                            {
                                i.Text += " <- Mirror said it's unavailable :(";
                            });
                        }
                    }
                };

                BeatmapMirror.GetIcon(bm.sid, (strim) =>
                {
                    item.Icon = new Texture(strim);
                });
                scrollBox.Items.Add(item);
            }
        }

        public static float TextboxHeight => 100 * MainGame.Scale;

        public override void Update(float delta)
        {
            searchTextbox.Size = new Vector2(MainGame.WindowWidth, TextboxHeight);
            searchTextbox.Position = new Vector2(0, 0);

            scrollBox.Position = new Vector2(0, TextboxHeight);
            scrollBox.Size = new Vector2(MainGame.WindowWidth, MainGame.WindowHeight - scrollBox.Position.Y);

            base.Update(delta);
        }

        public override void OnEnter()
        {
            if (beatmapList is null)
                firstSearch();
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();
            base.OnKeyDown(key);
        }
    }
}
