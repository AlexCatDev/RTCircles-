namespace TestECS
{
    //Fuck det her er røv cancer

    public static class SceneManager
    {
        
    }

    //public class Scene
    //{
    //    private List<Entity> entitties = new List<Entity>();
    //    private Dictionary<Type, List<Component>> components = new Dictionary<Type, List<Component>>();

    //    public void Register<T>(T component) where T : Component
    //    {
    //        if(components.ContainsKey(typeof(T)) == false)
    //            components.Add(typeof(T), new List<Component>());

    //        components[typeof(T)].Add(component);
    //    }

    //    public void Unregister<T>(T t) where T : Component
    //    {
    //        if (components.TryGetValue(typeof(T), out var componentList))
    //            componentList.Remove(t);
    //    }

    //    public List<T>? GetComponents<T>() where T : Component
    //    {
    //        if (components.TryGetValue(typeof(T), out var componentList))
    //            return componentList as List<T>;

    //        return null;
    //    }

    //    public void UpdateComponents<T>()
    //    {
    //        if(components.TryGetValue(typeof(T), out var componentList))
    //        {
    //            for (int i = 0; i < componentList.Count; i++)
    //            {
    //                componentList[i].Update();
    //            }
    //        }
    //    }

    //    public Entity CreateEntity()
    //    {
    //        Entity entity = new Entity(this);
    //        entitties.Add(entity);
    //        return entity;
    //    }

    //    public void RemoveEntity(Entity entity)
    //    {
    //        entitties.Remove(entity);
    //        for (int i = 0; i < entity.components.Count; i++)
    //        {
    //            Unregister(entity.components[i]);
    //        }
    //    }

    //    public int GetCount<T>()
    //    {
    //        if (components.TryGetValue(typeof(T), out var ents))
    //            return ents.Count;

    //        return 0;
    //    }
    //}

    public interface IComponent
    {
        void Setup(Entity Entity);
        void Update();
    }

    struct cum : IComponent
    {
        public void Setup(Entity Entity)
        {
            
        }

        public void Update()
        {
            
        }
    }

    public class Scene
    {
        private List<Entity> entitties = new List<Entity>();
        private Dictionary<Type, System.Collections.IList> components = new Dictionary<Type, System.Collections.IList>();

        public void Register<T>(T component) where T : Component
        {
            if (components.ContainsKey(typeof(T)) == false)
                components.Add(typeof(T), new List<T>());

            components[typeof(T)].Add(component);
        }

        public void Unregister<T>(T t) where T : Component
        {
            if (components.TryGetValue(typeof(T), out var componentList))
                componentList.Remove(t);
        }

        public List<T>? GetComponents<T>() where T : Component
        {
            if (components.TryGetValue(typeof(T), out var componentList))
                return componentList as List<T>;

            return null;
        }

        public void UpdateComponents<T>()
        {
            if (components.TryGetValue(typeof(T), out var componentList))
            {
                for (int i = 0; i < componentList.Count; i++)
                {
                    //componentList[i].Update();
                }
            }
        }

        public Entity CreateEntity()
        {
            Entity entity = new Entity(this);
            entitties.Add(entity);
            return entity;
        }

        public void RemoveEntity(Entity entity)
        {
            entitties.Remove(entity);
            for (int i = 0; i < entity.components.Count; i++)
            {
                Unregister(entity.components[i]);
            }
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
        internal List<Component> components = new List<Component>();

        public Scene Scene { get; private set; }

        public Entity(Scene scene)
        {
            Scene = scene;
        }

        public void AddComponent<T>(T t) where T : Component
        {
            t.Setup(this);   
            Scene.Register(t);
            components.Add(t);
        }

        public void RemoveComponent<T>(T t) where T : Component
        {
            Scene.Unregister(t);
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
    }
}