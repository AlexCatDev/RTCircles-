namespace TestECS
{
    //Fuck det her er røv cancer

    public static class SceneManager
    {
        
    }

    public class Scene
    {
        private List<Entity> entitties = new List<Entity>();
        private Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();

        public void Register<T>(T t) where T : Component
        {
            if(components.ContainsKey(typeof(T)) == false)
                components.Add(typeof(T), new List<Component>());

            components[typeof(T)].Add(t);
        }

        public void Unregister<T>(T t) where T : Component
        {
            if (components.TryGetValue(typeof(T), out var ents))
                ents.Remove(t);
        }

        public void UpdateComponents<T>()
        {
            var list = components[typeof(T)];

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Update();
            }
        }

        public Entity CreateEntity()
        {
            Entity entity = new Entity(this);
            entitties.Add(entity);
            return entity;
        }

        public void RemoveEntity()
        {

        }

        public int GetCount<T>()
        {
            if (components.TryGetValue(typeof(T), out var ents))
                return ents.Count;

            return 0;
        }
    }

    public class Entity
    {
        public long ID;

        private List<Component> components = new List<Component>();

        public Scene Scene { get; private set; }

        public Entity(Scene scene)
        {
            Scene = scene;
        }

        public void AddComponent<T>(T t) where T : Component
        {
            t.Setup(this);   
            Scene.Register<T>(t);
            components.Add(t);
        }

        public void RemoveComponent<T>(T t) where T : Component
        {
            Scene.Unregister<T>(t);
            components.Remove(t);
        }

        public T? GetComponent<T>() where T : Component
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T t)
                    return t;
            }

            return null;
        }

        public T GetOrAddComponent<T>(Func<T> noComponent) where T : Component
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T t)
                    return t;
            }

            T newComponent = noComponent();

            AddComponent(newComponent);

            return newComponent;
        }

        public void RemoveAllComponents()
        {
            /*
            for (int i = 0; i < components.Count; i++)
            {
                components[i].Unregister();
            }

            components.Clear();
            */
        }
    }
}