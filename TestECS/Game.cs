using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Windowing;
using System.Collections.Concurrent;

namespace TestECS
{
    public class Game : GameBase
    {
        public static Game Instance { get; private set; }

        public Game()
        {
            Instance = this;
        }

        public static Graphics Renderer { get; private set; }

        public Matrix4 Projection { get; private set; }

        private Entity player;
        private Entity player2;

        private Scene scene = new Scene();

        public override void OnLoad()
        {
            VSync = false;

            Renderer = new Graphics();

            player = scene.CreateEntity();
            player.AddComponent(new Transform() { Size = new Vector2(64) });
            player.AddComponent(new Sprite() { Texture = Texture.WhiteCircle, Color = Colors.White });
            player.AddComponent(new SpriteRenderer());
            player.AddComponent(new Controller());
            player.AddComponent(new BoundsCheck());
            player.AddComponent(new Rainbow());

            player2 = scene.CreateEntity();
            player2.AddComponent(new Transform() { Size = new Vector2(8) });
            player2.AddComponent(new Sprite() { Texture = Texture.WhiteCircle, Color = Colors.Red });
            player2.AddComponent(new SpriteRenderer());
            player2.AddComponent(new MouseController());
            player2.AddComponent(new BoundsCheck());

            
            for (int i = 0; i < 100_000; i++)
            {
                Entity entity = scene.CreateEntity();

                Transform transform = new Transform();
                transform.Position.X = RNG.Next(0, 1920);
                transform.Position.Y = RNG.Next(0, 1080);
                transform.Size = new Vector2(16, 16);

                entity.AddComponent(transform);
                entity.AddComponent(new Sprite() { Texture = Texture.WhiteSquare, Color = Colors.Yellow });
                entity.AddComponent(new SpriteRenderer());
                entity.AddComponent(new SpinnyBoi());
            }
            
            //IsMultiThreaded = true;
        }

        public override void OnUpdate()
        {
            scene.UpdateComponents<Controller>();
            scene.UpdateComponents<MouseController>();
            scene.UpdateComponents<BoundsCheck>();
            scene.UpdateComponents<Rainbow>();
            scene.UpdateComponents<SpinnyBoi>();
        }

        public override void OnRender()
        {
            //scene.UpdateComponents<SpriteRenderer>();
            Renderer.DrawString($"UPS: {UPS} FPS: {FPS} SpriteRenderes: {scene.GetCount<SpriteRenderer>()}", Font.DefaultFont, Vector2.Zero, Colors.White);
            Renderer.Projection = Projection;
            Renderer.EndDraw();
        }


        public override void OnResize(int width, int height)
        {
            GPUSched.Instance.Enqueue(() =>
            {
                Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
                Viewport.SetViewport(0, 0, width, height);
            });
        }

        public override void OnOpenFile(string fullpath)
        {
            
        }
    }
}