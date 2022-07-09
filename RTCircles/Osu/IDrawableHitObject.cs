using OsuParsers.Beatmaps.Objects;
using OpenTK.Mathematics;
using Easy2D;

namespace RTCircles
{
    public interface IDrawableHitObject
    {
        public bool IsHit { get; }
        public bool IsMissed { get; }

        public void MissIfNotHit();

        public int ObjectIndex { get; }

        public HitObject BaseObject { get; }

        public Vector4 CurrentColor { get; }
    }
}
