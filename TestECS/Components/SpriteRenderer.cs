namespace TestECS
{
    public class SpriteRenderer : Component
    {
        private Transform transform;
        private Sprite sprite;

        public override void Setup(Entity Entity)
        {
            if ((transform = Entity.GetComponent<Transform>()) == null)
            {
                transform = new Transform();
                Entity.AddComponent(transform);
            }

            if ((sprite = Entity.GetComponent<Sprite>()) == null)
            {
                sprite = new Sprite();
                Entity.AddComponent(sprite);
            }
        }

        public override void Update()
        {
            Game.Renderer.DrawRectangle(transform.Position, transform.Size, sprite.Color, sprite.Texture, null, false, transform.Rotation);

            //Console.WriteLine("SpriteRenderer Transform: "+transform);
        }
    }
}