using Easy2D.Game;
using Silk.NET.Input;

namespace TestECS
{
    public class MouseController : Component
    {
        private Transform transform;

        public override void Setup(Entity Entity)
        {
            transform = Entity.GetOrAddComponent(() =>
            {
                return transform = new Transform();
            });
        }

        public override void Update()
        {
            transform.Position = Input.MousePosition;
        }
    }

    public class Controller : Component
    {
        private Transform transform;

        public override void Setup(Entity Entity)
        {
            transform = Entity.GetOrAddComponent(() => 
            {
                return transform = new Transform();
            });
        }

        public override void Update()
        {
            if (Input.IsKeyDown(Key.W))
                transform.Position.Y -= 600 * (float)Game.Instance.DeltaTime;
            else if (Input.IsKeyDown(Key.S))
                transform.Position.Y += 600 * (float)Game.Instance.DeltaTime;
            
            if(Input.IsKeyDown(Key.D))
                transform.Position.X += 600 * (float)Game.Instance.DeltaTime;
            else if(Input.IsKeyDown(Key.A))
                transform.Position.X -= 600 * (float)Game.Instance.DeltaTime;

            //Console.WriteLine("Controller Transform: "+transform);
        }
    }
}