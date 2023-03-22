using System.Numerics;
using System;

namespace Easy2D
{
    public class Tileset
    {
        public Vector4 this[int i] => this[i % RowCount, i / RowCount];
        public Vector4 this[int x, int y] => new Vector4(x * uvSize.X, y * uvSize.Y, uvSize.X, uvSize.Y);
        public Vector4 this[int x, int y, int width, int height] => new Vector4(x * uvSize.X, y * uvSize.Y, width * uvSize.X, height * uvSize.Y);

        public Vector2 TileSize { get; private set; }

        public int Count { get; private set; }

        /// <summary>
        /// This is X Count
        /// </summary>
        public int RowCount { get; private set; }
        /// <summary>
        /// This is Y Count
        /// </summary>
        public int ColumnCount { get; private set; }

        public Vector2 TextureSize { get; private set; }

        private Vector2 uvSize;

        /// <summary>
        /// i → X ... ↓
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Vector4 GetRightThenWrapDown(int i) => this[i % RowCount, i / RowCount];
        /// <summary>
        /// i ↓ Y ... →
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Vector4 GetDownThenWrapRight(int i) => this[i % ColumnCount, i / ColumnCount];

        public Tileset(Vector2 textureSize, Vector2 tileSize)
        {
            if (!IsEven(textureSize.X / tileSize.X))
                throw new ArgumentException("TextureSize.Width and TileSize.Width divided is not even");

            if (!IsEven(textureSize.Y / tileSize.Y))
                throw new ArgumentException("TextureSize.Height and TileSize.Height divided is not even");

            TextureSize = textureSize;
            TileSize = tileSize;

            ColumnCount = (int)(textureSize.Y / TileSize.Y);
            RowCount = (int)(textureSize.X / TileSize.X);
            Count = ColumnCount * RowCount;

            uvSize = new Vector2(tileSize.X / textureSize.X, tileSize.Y / textureSize.Y);
        }

        static bool IsEven(float d) => MathF.Abs(d % 1f) <= (float.Epsilon * 100);
    }
}
