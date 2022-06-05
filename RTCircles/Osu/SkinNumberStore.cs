using Easy2D;

using OpenTK.Mathematics;
using System.Collections.Generic;

namespace RTCircles
{
    public class SkinNumberStore
    {
        private Dictionary<int, Texture> numbers = new Dictionary<int, Texture>();

        public IReadOnlyDictionary<int, Texture> Numbers => numbers;

        private Texture commaTexture;
        private Texture percentTexture;
        private Texture xTexture;

        public float Overlap = 0;

        public SkinNumberStore(string path, string name, string comma = null, string percent = null, string x = null)
        {
            for (int i = 0; i < 10; i++)
            {
                Texture texture = Skin.LoadTexture(path, $"{name}{i}");
                numbers.Add(i, texture);
            }

            if (comma is not null)
                commaTexture = Skin.LoadTexture(path, comma);

            if (percent is not null)
                percentTexture = Skin.LoadTexture(path, percent);

            if (x is not null)
                xTexture = Skin.LoadTexture(path, x);
        }

        public Vector2 Meassure(float ySize, string number)
        {
            Vector2 offset = Vector2.Zero;

            for (int i = 0; i < number.Length; i++)
            {
                Texture texture;

                if (number[i] == '.' || number[i] == ',')
                {
                    if (commaTexture is null)
                        continue;

                    texture = commaTexture;
                }
                else if (number[i] == 'x')
                {
                    if (xTexture is null)
                        continue;

                    texture = xTexture;
                }
                else if (number[i] == '%')
                {
                    if (percentTexture is null)
                        continue;

                    texture = percentTexture;
                }
                else
                {
                    if (int.TryParse(number[i].ToString(), out int num) == false)
                        continue;

                    texture = numbers[num];
                }

                float aspectRatio = (float)texture.Width / texture.Height;

                Vector2 digitSize = new Vector2(ySize * aspectRatio, ySize);

                offset.Y = digitSize.Y;

                float overlap = (digitSize.X / texture.Width) * Overlap;

                offset.X += digitSize.X;

                //Only offset the x if not on the last character.
                if (i < number.Length - 1)
                    offset.X -= overlap;
            }

            return offset;
        }

        public void DrawCentered(Graphics g, Vector2 position, float ySize, Vector4 color, string number)
        {
            Vector2 size = Meassure(ySize, number);
            Draw(g, position - size / 2f, ySize, color, number);
        }

        public void Draw(Graphics g, Vector2 position, float ySize, Vector4 color, string number)
        {
            for (int i = 0; i < number.Length; i++)
            {
                Texture texture;

                if (number[i] == '.' || number[i] == ',')
                {
                    if (commaTexture is null)
                        continue;

                    texture = commaTexture;
                }
                else if (number[i] == 'x')
                {
                    if (xTexture is null)
                        continue;

                    texture = xTexture;
                }
                else if (number[i] == '%')
                {
                    if (percentTexture is null)
                        continue;

                    texture = percentTexture;
                }
                else
                {
                    if (int.TryParse(number[i].ToString(), out int num) == false)
                        continue;

                    texture = numbers[num];
                }

                Vector2 digitSize = new Vector2(ySize * texture.Size.AspectRatio(), ySize);
                
                float overlap = (Overlap / texture.Width) * digitSize.X;

                g.DrawRectangle(position, digitSize, color, texture);
                //g.DrawRectangle(position + offset, digitSize, new Vector4(0f, 0f, 0f, 0.5f));

                position.X += digitSize.X - overlap;
            }
        }
    }
}
