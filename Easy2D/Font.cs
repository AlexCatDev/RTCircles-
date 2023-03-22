using System.Numerics;
using SharpFNT;
using System;
using System.IO;

namespace Easy2D
{
    public class Font
    {
        public static readonly Font DefaultFont = new Font(Utils.GetInternalResource("Fonts.Default.fnt"), Utils.GetInternalResource("Fonts.Default.png"));
        public Texture Texture;
        public BitmapFont Info;

        public float Size => Info.Common.LineHeight;

        public float HeightOf(char c) => Info.GetCharacter(c).Height;
        public float YOffsetOf(char c) => Info.GetCharacter(c).YOffset;

        public Vector2 MessureString(ReadOnlySpan<char> text, float scale = 1f, bool includeLastCharAdvance = false)
        {
            if (text.Length == 0)
                return Vector2.Zero;

            scale = Math.Max(0, scale);

            float biggestWidth = 0;

            float totalWidth = 0;
            float totalHeight = 0;

            float biggestChar = 0;
            float smallestBearing = float.MaxValue;

            float newLines = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    newLines += (Info.Common.LineHeight * scale);
                    totalWidth = 0;
                    continue;
                }
                else if (c == '\t')
                    continue;
                else if (c == '\r')
                    continue;

                bool isValidCharacter = Info.Characters.TryGetValue(c, out Character character);

                if (isValidCharacter == false)
                    character = Info.Characters['?'];

                float width = character.Width * scale;
                float height = character.Height * scale;
                float bearing = character.YOffset * scale;

                bool lastCharacter = i == text.Length - 1;

                if (lastCharacter && includeLastCharAdvance == false)
                {
                    totalWidth += width;
                }
                else
                {
                    totalWidth += (character.XAdvance * scale);

                    if (lastCharacter == false)
                        totalWidth += Info.GetKerningAmount(c, text[i + 1]) * scale;

                    totalWidth -= character.XOffset * scale;
                }

                biggestWidth = totalWidth > biggestWidth ? totalWidth : biggestWidth;

                if (c == ' ')
                    continue;


                //Find the biggest char, which also includes the bearing
                biggestChar = biggestChar > height + bearing ? biggestChar : height + bearing;
                //Find the smallest bearing in the string, so we can offset the height by that
                smallestBearing = smallestBearing < bearing ? smallestBearing : bearing;
            }

            totalHeight = (biggestChar - smallestBearing) + newLines;
            return new Vector2(biggestWidth, totalHeight);
        }

        public Font(Stream fontFile, Stream fontImageFile) {
            Info = BitmapFont.FromStream(fontFile, FormatHint.Binary, false);
            Texture = new Texture(fontImageFile);
        }

    }
}
