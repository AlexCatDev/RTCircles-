using Easy2D;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class HitsoundStore
    {
        private Dictionary<string, Sound> hitsounds = new Dictionary<string, Sound>();

        /// <summary>
        /// Get a hitsound of the respected type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="set"></param>
        /// <returns>A sound if found otherwise it returns null if the respected type does not exist</returns>
        public Sound this[HitSoundType type, SampleSet set]
        {
            get
            {
                //Convert type none to normal
                if (type == HitSoundType.None)
                    type = HitSoundType.Normal;

                if (set == SampleSet.None)
                    set = SampleSet.Normal;

                string name = $"{set}-hit{type}".ToLower();

                if (hitsounds.TryGetValue(name, out Sound hitsound))
                    return hitsound;

                return null;
            }
        }

        public HitsoundStore(string path, bool allowNull)
        {
            Utils.Log($"Loading hitsounds from: {path}", LogLevel.Important);

            Array allTypes = Enum.GetValues(typeof(HitSoundType));
            Array allSets = Enum.GetValues(typeof(SampleSet));

            foreach (HitSoundType hitSoundType in allTypes)
            {
                if (hitSoundType == HitSoundType.None)
                    continue;

                foreach (SampleSet sampleSet in allSets)
                {
                    if (sampleSet == SampleSet.None)
                        continue;

                    string name = $"{sampleSet}-hit{hitSoundType}".ToLower();

                    Sound s = Skin.LoadSound(path, name, allowNull);

                    if(s is not null)
                        hitsounds.Add(name, s);
                }
            }
            Utils.Log($"Loaded {hitsounds.Count} hitsounds", LogLevel.Important);
        }

        public void SetVolume(float volume)
        {
            foreach (var hitsound in hitsounds)
            {
                hitsound.Value.Volume = volume;
            }
        }
    }
}
