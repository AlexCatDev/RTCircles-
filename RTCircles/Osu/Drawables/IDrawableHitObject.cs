using OsuParsers.Beatmaps.Objects;
using System.Numerics;
using Easy2D;

namespace RTCircles
{
    //Some day i will have to make this an abstract class, and cleanup everything because lots of duplicated code / dodgy code
    public interface IDrawableHitObject
    {
        public bool IsHit { get; }
        public bool IsMissed { get; }

        public bool IsHittable => !(IsHit || IsMissed);

        public void MissIfNotHit();

        public int ObjectIndex { get; }

        public HitObject BaseObject { get; }

        public Vector4 CurrentColor { get; }
    }
}
