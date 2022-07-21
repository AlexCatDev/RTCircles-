using Easy2D;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public static class DynamicTexureCache
    {
        private static Dictionary<string, (Texture, List<Guid>)> textureCache = new Dictionary<string, (Texture, List<Guid>)>();

        public static Texture AquireCache(Guid id, string path)
        {
            if (textureCache.TryGetValue(path, out var value))
            {
                if (value.Item2.Contains(id))
                    throw new Exception("This id already has a cache???");

                value.Item2.Add(id);

                return value.Item1;
            }
            else
            {
                var tex = File.Exists(path) ?
                    new Texture(File.OpenRead(path)) { AutoDisposeStream = true, GenerateMipmaps = false } : Skin.DefaultBackground;

                var toAdd = (tex, new List<Guid>() { id });
                textureCache.Add(path, toAdd);

                return toAdd.Item1;
            }
        }

        public static void ReleaseCache(Guid guid, string path)
        {
            if (textureCache.TryGetValue(path, out var subscribers))
            {
                var texture = subscribers.Item1;
                var listSubscribers = subscribers.Item2;

                var index = listSubscribers.IndexOf(guid);

                if (index != -1)
                {
                    listSubscribers.RemoveAt(index);

                    //When theres no more subscribers
                    if (listSubscribers.Count == 0)
                    {
                        //Remove the texture cache item
                        textureCache.Remove(path);

                        GPUSched.Instance.Enqueue(() =>
                        {
                            //And delete the texture
                            texture.Delete();
                        });
                    }
                }
            }
        }
    }
}

