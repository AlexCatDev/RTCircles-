using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    //TODO: SmoothScroll

    public class DropdownItem
    {
        public object Tag;

        public event Action OnClick;

        public string Text;

        public Vector4 Color = Colors.DarkPink;
        public Vector4 HighlightColor = Colors.Pink;
        public Vector4 TextColor = Colors.White;

        public bool CollapseOnClick { get; set; } = true;

        public void InvokeClick() => OnClick?.Invoke();
    }

    public class Dropdown : Drawable
    {
        private Animation dropdownAnimation = new Animation();
        public override Rectangle Bounds => new Rectangle(Position, Size);

        public Vector2 Position;
        public Vector2 Size = new Vector2(256, 34);

        public Vector2? ItemSizeOverride;

        public Vector2 ItemSize => ItemSizeOverride ?? Size;

        public Vector4 HeaderColor = Colors.White;
        public Vector4 HeaderTextColor = Colors.Pink;
        public bool RightSideArrow = false;

        public string Text = "default";

        public List<DropdownItem> Items = new List<DropdownItem>();
        private bool isExpanded;

        private FrameBuffer frameBuffer = new FrameBuffer(100, 100);

        private Vector2 scrollOffset;
        private Vector2 scrollInertia;

        public int MaxItemsShown = 20;

        public void ClearItems() => Items.Clear();

        public void AddItem(DropdownItem item)
        {
            Items.Add(item);
        }

        public void RemoveItem(DropdownItem item)
        {
            Items.Remove(item);
        }

        public Dropdown()
        {

        }

        public override void OnRemove()
        {

        }

        private void collapse()
        {
            dropdownAnimation.From = dropdownAnimation.Output;
            dropdownAnimation.To = 0f;
            dropdownAnimation.Reset();

            isExpanded = false;
        }

        private void expand()
        {

            dropdownAnimation.From = dropdownAnimation.Output;
            dropdownAnimation.To = 1f;
            dropdownAnimation.Reset();
        }

        private bool clickEvent;
        public override bool OnMouseDown(MouseButton args)
        {
            if (new Rectangle(Position, Size).IntersectsWith(Input.MousePosition))
            {
                dropdownAnimation.Easing = isExpanded ? EasingTypes.InQuint : EasingTypes.OutQuint;
                dropdownAnimation.Duration = 0.25f;
                isExpanded = !isExpanded;

                if (isExpanded)
                    expand();
                else
                    collapse();
            }
            else
            {
                if (isExpanded && args == MouseButton.Left)
                    clickEvent = true;
            }

            return false;
        }
        public override bool OnMouseWheel(float delta)
        {
            if (isExpanded)
            {
                scrollInertia += new Vector2(0, delta * 1000);

                return true;
            }

            return false;
        }

        private void drawHeader(Graphics g)
        {
            g.DrawRectangle(Position, Size, HeaderColor);

            Vector2 arrowSize = new Vector2(Size.Y, Size.Y);
            Vector2 arrowPos = RightSideArrow ? Position + new Vector2(Size.X - arrowSize.X, 0) : Position;
            float animArrow = MathUtils.Map(dropdownAnimation.Output, 0, 1f, 270f, 360f);

            g.DrawRectangle(arrowPos, arrowSize, Vector4.One, Skin.Arrow, null, false, animArrow);

            var scale = Size.Y / Font.DefaultFont.Size;

            Vector2 textSize = Font.DefaultFont.MessureString(Text, scale);
            g.DrawString(Text, Font.DefaultFont, new Vector2(Position.X + Size.X / 2f - textSize.X / 2f, Position.Y + textSize.Y / 2f), HeaderTextColor, scale);
        }

        public override void Update(float delta)
        {
            dropdownAnimation.Update(delta);

            int itemCount = Math.Max(Items.Count - MaxItemsShown, 0);
            //scrollOffset = Vector2.Clamp(scrollOffset + scrollInertia * delta, -new Vector2(0, Size.Y * itemCount), new Vector2(0));

            if (scrollOffset.Y > 0 && scrollInertia.Y < 1500)
                scrollOffset = Vector2.Lerp(scrollOffset, new Vector2(0), delta * 10f);

            if (scrollOffset.Y < -ItemSize.Y * itemCount && scrollInertia.Y > -1500)
                scrollOffset = Vector2.Lerp(scrollOffset, -new Vector2(0, ItemSize.Y * itemCount), delta * 10f);

            scrollOffset += scrollInertia * delta;

            scrollInertia = Vector2.Lerp(scrollInertia, new Vector2(0), delta * 8f);
        }

        public override void Render(Graphics g)
        {
            drawHeader(g);

            int itemCount = MaxItemsShown > Items.Count ? Items.Count : MaxItemsShown;
            //Wont actually resize every frame, just ensure it stays the correct size
            frameBuffer.EnsureSize(ItemSize.X, ItemSize.Y * itemCount);

            if (!dropdownAnimation.IsCompleted || isExpanded)
            {
                g.DrawInFrameBuffer(frameBuffer, () =>
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var currentItem = Items[i];

                        float offsetY = i * ItemSize.Y;

                        Vector2 currentPos = new Vector2(0, offsetY) + scrollOffset;

                        if (currentPos.Y < -ItemSize.Y)
                            continue;

                        if (currentPos.Y > frameBuffer.Height)
                            break;

                        if (new Rectangle(currentPos, ItemSize).IntersectsWith(Input.MousePosition - Position - new Vector2(0, Size.Y)))
                        {
                            g.DrawRectangle(currentPos, ItemSize, currentItem.HighlightColor);
                            if (clickEvent)
                            {
                                if (currentItem.CollapseOnClick)
                                {
                                    collapse();
                                    isExpanded = false;
                                }

                                currentItem.InvokeClick();
                                clickEvent = false;
                            }
                        }
                        else
                        {
                            g.DrawRectangle(currentPos, ItemSize, currentItem.Color);
                        }

                        if (!String.IsNullOrEmpty(Items[i].Text))
                        {
                            var scale = (ItemSize.Y / Font.DefaultFont.Size);

                            Vector2 textSize = Font.DefaultFont.MessureString(currentItem.Text, scale);

                            Vector2 textPos = new Vector2((ItemSize.X / 2f) - (textSize.X / 2f), offsetY + ItemSize.Y / 2f - textSize.Y / 2f) + scrollOffset;

                            g.DrawString(currentItem.Text, Font.DefaultFont, textPos, currentItem.TextColor, scale);
                        }
                    }
                });

                if (frameBuffer.Texture is not null && frameBuffer.IsInitialized)
                {
                    float height = MathUtils.Map(dropdownAnimation.Output, 0, 1f, 0, frameBuffer.Height);

                    Rectangle texRect = frameBuffer.Texture.GetTextureRect(new Rectangle(0, height, frameBuffer.Width, -height));

                    g.DrawRectangle(Position + new Vector2(0, Size.Y), new Vector2(ItemSize.X, height), Colors.White, frameBuffer.Texture, texRect, true);;
                }
            }

            if(clickEvent == true)
                collapse();

            clickEvent = false;
        }
    }
}
