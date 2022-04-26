using Easy2D;
using OpenTK.Mathematics;

namespace TestECS
{
    public class Rainbow : Component
    {
        private Sprite sprite;

        public override void Setup(Entity Entity)
        {
            sprite = Entity.GetOrAddComponent<Sprite>(() =>
            {
                return sprite = new Sprite();
            });
        }

        public override void Update()
        {
            sprite.Color = new Vector4(MathUtils.RainbowColor(Game.Instance.TotalTime), 1f);
        }
    }
}