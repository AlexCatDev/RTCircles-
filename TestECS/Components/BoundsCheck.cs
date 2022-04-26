using Easy2D;

namespace TestECS
{
    public class BoundsCheck : Component
    {
        private Transform transform;

        public override void Setup(Entity Entity)
        {
            ///Wouldnt it be better if Every component had a static Create Function
            ///transform = Transform.Create(Entity, ...);
            ///Will return the current transform or create a new one, add it, then return it
            transform = Entity.GetOrAddComponent(() =>
            {
                return transform = new Transform();
            });
        }

        public override void Update()
        {
            transform.Position.X.ClampRef(0, Game.Instance.View.Size.X - transform.Size.X);
            transform.Position.Y.ClampRef(0, Game.Instance.View.Size.Y - transform.Size.Y);
        }
    }
}