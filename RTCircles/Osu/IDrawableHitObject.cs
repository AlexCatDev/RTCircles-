using OsuParsers.Beatmaps.Objects;
using OpenTK.Mathematics;
using Easy2D;

namespace RTCircles
{
    //TODO: fuck interfaces
    public abstract class DrawableHitObject : Drawable
    {
        public HitObject BaseObject { get; private set; }
        public Vector3 BaseColor { get; private set; }
        public int Combo { get; private set; }

        public DrawableHitObject(HitObject baseObject, Vector3 baseColor, int combo)
        {
            BaseObject = baseObject;
            BaseColor = baseColor;
            Combo = combo;
        }

        public float TimeElapsed => 
            (float)(OsuContainer.SongPosition - BaseObject.StartTime + OsuContainer.Beatmap.Preempt);

        float Alpha
        {
            get
            {
                if(fadeOutStart != -1)
                    return MathUtils.Map((float)OsuContainer.SongPosition, fadeOutStart,
                        fadeOutStart + (float)OsuContainer.Fadeout, fadeOutStartAlpha, 0f).Clamp(0, 1f);

                return MathUtils.Map(TimeElapsed, 0, (float)OsuContainer.Beatmap.Fadein, 0, 1f).Clamp(0, 1f);
            }
        }

        public Vector4 Color => new Vector4(BaseColor, Alpha);

        public Vector4 White => new Vector4(1f, 1f, 1f, Alpha);

        private float fadeOutStart;
        private float fadeOutStartAlpha;

        public override void OnAdd()
        {
            fadeOutStart = -1;
        }

        protected void beginFadeout()
        {
            fadeOutStartAlpha = Alpha;
            fadeOutStart = (float)OsuContainer.SongPosition;
        }

        public override void Update(float delta)
        {
            
        }
    }

    public interface IDrawableHitObject
    {
        HitObject BaseObject { get; }

        Vector4 CurrentColor { get; }

        float TimeElapsed => (float)(OsuContainer.SongPosition - BaseObject.StartTime + OsuContainer.Beatmap.Preempt);
    }
}
