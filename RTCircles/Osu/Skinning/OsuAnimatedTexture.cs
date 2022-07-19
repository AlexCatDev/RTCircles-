using System;
using System.Collections.Generic;

namespace RTCircles
{
    //TODO: Compile texture elements to an atlas.
    //TODO: Somehow fix when circle/combo/score numbers share the same texture, dont load them again
    //TODO: Support animated texture correctly
    //Basically redo this whole thing !

    public class OsuAnimatedTexture
    {
        public OsuAnimatedTexture(params OsuTexture[] textures) 
        {
            this.textures.AddRange(textures);
        }

        private OsuAnimatedTexture() { }

        private List<OsuTexture> textures = new List<OsuTexture>();

        public OsuTexture GetTexture(double time, bool ignoreSkinFramerate = false)
        {
            var timeScale = 1f;

            if(Skin.Config.Framerate > 0 && !ignoreSkinFramerate)
                timeScale = Skin.Config.Framerate / (float)textures.Count;

            int index = (int)Math.Floor(((time * timeScale) % 1) * textures.Count);

            return textures[Math.Max(index, 0)];
        }

        public IReadOnlyList<OsuTexture> Textures => textures;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="nameWithoutDash"></param>
        /// <param name="range">From and to are inclusive. If no range is specified it will keep going from 0 to 100 and only stop when it runs out of textures to load</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static OsuAnimatedTexture? FromPath(string path, string name, (int from, int to)? range = null)
        {
            OsuAnimatedTexture osuAnimTex = new OsuAnimatedTexture();
            if (range.HasValue)
            {
                if (range.Value.from > range.Value.to)
                    throw new ArgumentOutOfRangeException("From can't be bigger than to");
                if(range.Value.from < 0 || range.Value.to < 0)
                    throw new ArgumentOutOfRangeException("From or To can't be less than 0");

                for (int i = range.Value.from; i < range.Value.to + 1; i++)
                {
                    var filename = $"{name}{i}";
                    var osuTexture = Skin.LoadTexture(path, filename, true, false);

                    osuAnimTex.textures.Add(osuTexture);
                }
            }
            else
            {
                for (int i = 0; i < 1024; i++)
                {
                    var filename = $"{name}{i}";
                    var osuTexture = Skin.LoadTexture(path, filename, true, true);

                    if (osuTexture == null)
                        break;

                    osuAnimTex.textures.Add(osuTexture);
                }
            }

            if (osuAnimTex.textures.Count == 0)
                return null;

            return osuAnimTex;
        }


    }
}
