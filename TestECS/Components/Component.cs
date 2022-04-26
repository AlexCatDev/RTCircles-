namespace TestECS
{
    public abstract class Component
    {
        public virtual void Setup(Entity Entity) { }

        public virtual void Update() { }
    }
}