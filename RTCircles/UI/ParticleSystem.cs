using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System.Collections.Generic;

namespace RTCircles
{
    public abstract class Particle
    {
        public Vector4 Color;
        public Vector2 Position;

        public float Rotation;
        public Vector2? RotationOrigin;
        public Vector2 Size;

        public bool IsDead;

        /// <summary>
        /// Update particle
        /// </summary>
        /// <param name="delta"></param>
        public abstract void Update(float delta);
    }

    public class DefaultParticle : Particle
    {
        Vector2 velocity;
        float fadeSpeed;
        float rotationSpeed = 0;
        public DefaultParticle()
        {
            float speedX = RNG.Next(0, 50);
            float speedY = RNG.Next(0, 50);

            float directionX = RNG.TryChance() ? -1 : 1f;
            float directionY = RNG.TryChance() ? -1 : 1f;

            speedX *= directionX;
            speedY *= directionY;

            velocity = new Vector2(speedX, speedY);

            fadeSpeed = 0.5f;

            rotationSpeed = RNG.Next(-180, 180);

            Color = Colors.Pink;

            Size.X = RNG.Next(24, 48);
            Size.Y = Size.X;
        }

        public override void Update(float delta)
        {
            Position += (velocity * delta);
            Color.W -= fadeSpeed * delta;
            Rotation += rotationSpeed * delta;
            RotationOrigin = Position;
            
            if (Color.W <= 0)
                IsDead = true;
        }
    }

    class ParticleSystem<T> : Drawable where T : Particle, new()
    {
        private List<Particle> particles = new List<Particle>();

        public override Rectangle Bounds => new Rectangle(Position.X - 16, Position.Y - 16, 32, 32);

        public Vector2 Position;

        public Texture Texture { get; set; }

        public float EmitRate { get; set; }

        public int ParticleCount => particles.Count;

        private float emitTimer = 0;

        public ParticleSystem()
        {

        }

        public override void OnRemove()
        {

        }

        public override bool OnTextInput(char c)
        {
            return false;
        }

        public override bool OnKeyDown(Key key)
        {
            return false;
        }

        public override bool OnKeyUp(Key key)
        {
            return false;
        }

        public override bool OnMouseDown(MouseButton button)
        {
            return false;
        }

        public override bool OnMouseUp(MouseButton button)
        {
            return false;
        }

        public override bool OnMouseWheel(float delta)
        {
            return false;
        }

        public void Burst(int count, Vector2? pos = null)
        {
            Vector2 localPos = pos ?? Position;
            for (int i = 0; i < count; i++)
            {
                T particle = new T();
                particle.Position = localPos;
                particles.Insert(0, particle);
            }
        }

        public override void Update(float delta)
        {
            emitTimer += delta;

            if (emitTimer >= 1.0f / EmitRate)
            {
                emitTimer = 0;
                T particle = new T();
                particle.Position = Position;
                particles.Insert(0, particle);
            }

            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].Update(delta);
                if (particles[i].IsDead)
                    particles.RemoveAt(i);
            }
        }

        public override void Render(Graphics g)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                g.DrawRectangleCentered(particles[i].Position, particles[i].Size, particles[i].Color, Texture, null, false, particles[i].Rotation);
            }
        }
    }
}
