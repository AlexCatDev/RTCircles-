using Easy2D;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    /*
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

        public void SetVolume(double volume)
        {
            foreach (var hitsound in hitsounds)
            {
                hitsound.Value.Volume = volume;
            }
        }
    }
    */

    public class HitsoundStore
    {
        private Dictionary<string, Sound> hitsounds = new Dictionary<string, Sound>();

        public void SetVolume(double volume)
        {
            foreach (var hitsound in hitsounds)
            {
                hitsound.Value.Volume = volume;
            }
        }

        /// <summary>
        /// Get a hitsound of the respected type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="set"></param>
        /// <returns>A sound if found otherwise it returns null if the respected type does not exist</returns>
        public Sound this[HitSoundType type, SampleSet set, int index = 0]
        {
            get
            {
                if (type == HitSoundType.None || set == SampleSet.None)
                    throw new Exception($"Tried to play hitsound with type <{set}-hit{type}>");

                string name = $"{set}-hit{type}{(index > 0 ? index.ToString() : "")}".ToLower();

                if (hitsounds.TryGetValue(name, out Sound hitsound))
                    return hitsound;

                return null;
            }
        }

        private void loadFromSkin(string path)
        {
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

                    Sound s = Skin.LoadSound(path, name, false);

                    if (s is not null)
                        hitsounds.Add(name, s);
                }
            }
            Utils.Log($"Loaded {hitsounds.Count} hitsounds from skin: {path}", LogLevel.Important);
        }

        public HitsoundStore(string path, bool isSkin)
        {
            if (isSkin)
            {
                loadFromSkin(path);
                return;
            }

            string[] files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                var lower = file.ToLower();

                int escapedPath = lower.LastIndexOf('\\');
                if(escapedPath == -1)
                    escapedPath = lower.LastIndexOf('/');

                string last = lower.Substring(escapedPath + 1);
                
                if(last.StartsWith("drum-hit") || last.StartsWith("normal-hit") || last.StartsWith("soft-hit"))
                {
                    string name = last;

                    int extension = name.IndexOf('.');

                    if (extension != -1)
                        name = name.Remove(extension);

                    using (FileStream fs = File.OpenRead(file))
                    {
                        Sound s = new Sound(fs, false, true);
                        hitsounds.Add(name, s);
                    }
                }
            }

            Utils.Log($"Loaded {hitsounds.Count} hitsounds from: {path}", LogLevel.Important);
        }
    }
}
