using Easy2D;

namespace RTCircles
{
    public class OsuTexture
    {
        public float Scale { get; private set; }

        public Texture Texture { get; private set; }

        public bool IsX2 { get; private set; }

        public static implicit operator Texture(OsuTexture ot) => ot.Texture;

        public OsuTexture(Texture texture, bool isX2, float X2Size)
        {
            Texture = texture;

            IsX2 = isX2;

            Scale = isX2 ? X2Size : X2Size / 2;
        }
    }
}
