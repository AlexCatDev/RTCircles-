using Easy2D;
using OpenTK.Mathematics;

namespace PogGame
{
    public class DrawableExplosion2 : Drawable
    {
        private static Texture texture = new Texture(Utils.GetResource("Assets.Textures.explosion.png")) { };
        private static Tileset tileset = new Tileset(new Vector2(4096, 4096), new Vector2(512));

        public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

        private Vector2 pos;
        private Vector2 size = new Vector2(8, 8);
        private float rotation;

        public DrawableExplosion2(Vector2 pos, float rotation)
        {
            this.pos = pos;
            this.rotation = rotation;
        }

        public override void Render(Graphics g)
        {
            var texCoords = tileset.GetRightThenWrapDown((int)time);
            g.DrawRectangleCentered(pos, new Vector2(256), new Vector4(10f, 10f, 10f, 1f), texture, new Rectangle(texCoords.X, texCoords.Y, texCoords.Z, texCoords.W), true, rotation);
        }

        private double time;
        public override void OnUpdate()
        {
            time += Delta * tileset.Count * 2;
            if (time > tileset.Count + 10)
                IsDead = true;
        }
    }
}
