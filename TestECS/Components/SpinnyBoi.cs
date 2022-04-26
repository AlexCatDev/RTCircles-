using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestECS
{
    public class SpinnyBoi : Component
    {
        private Transform transform;

        public override void Setup(Entity Entity)
        {
            transform = Entity.GetOrAddComponent<Transform>(() =>
            {
                return transform = new Transform();
            });
        }

        public override void Update()
        {
            transform.Rotation += 180 * (float)Game.Instance.DeltaTime;
        }
    }
}
