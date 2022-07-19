using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class SkinConfiguration
    {
        public List<Vector3> ComboColors = new List<Vector3>() {  Colors.From255RGBA(139, 233, 253, 255).Xyz,
                                                                  Colors.From255RGBA(80, 250, 123, 255).Xyz,
                                                                  Colors.From255RGBA(255, 121, 198, 255).Xyz,
                                                                  Colors.From255RGBA(189, 147, 249, 255).Xyz,
                                                                  Colors.From255RGBA(241, 250, 140, 255).Xyz,
                                                                  Colors.From255RGBA(255, 255, 255, 255).Xyz
        };

        public Vector3 ColorFromIndex(int index) {
            var col = ComboColors[index % ComboColors.Count];

            if (GlobalOptions.RGBCircles.Value && OsuContainer.IsKiaiTimeActive)
                col = MathUtils.RainbowColor(OsuContainer.SongPosition / 1000, 0.5f, 1.1f);
            
            if (OsuContainer.IsKiaiTimeActive)
                col *= new Vector3(1 + 0.33f * (float)OsuContainer.BeatProgress);

                return col;
        }

        public Vector3 MenuGlow = new Vector3(1f,0.8f,0f);

        public Vector3? SliderBorder = null;

        public Vector3? SliderTrackOverride = null;

        public string HitCirclePrefix = "default";
        public float HitCircleOverlap = 0;

        public string ScorePrefix = "score";
        public float ScoreOverlap = 0;

        public string ComboPrefix = "score";
        public float ComboOverlap = 0;

        public int Framerate = -1;

        public bool HitCircleOverlayAboveNumber = true;

        public Vector3 SongSelectActiveTextColor = Colors.From255RGB(255, 255, 255);
        public Vector3 SongSelectInactiveTextColor = Colors.From255RGB(200, 200, 200);

        public SkinConfiguration(Stream stream)
        {
            if (stream is not null)
            {
                ComboColors.Clear();

                var reader = new StreamReader(stream);

                while (reader.EndOfStream == false)
                {
                    string line = reader.ReadLine();

                    if (line.StartsWith("//"))
                        continue;

                    parse(line);
                }

                Utils.Log($"Loaded Skin.ini SliderBorder: {SliderBorder} SliderTrack: {SliderTrackOverride}", LogLevel.Important);
            }
            else
            {
                Utils.Log($"Skin.ini was not found!! using default values!", LogLevel.Error);
            }

            if (ComboColors.Count == 0)
            {
                ComboColors.Add(Colors.From255RGBA(255, 150, 0, 255).Xyz);
                ComboColors.Add(Colors.From255RGBA(5, 240, 5, 255).Xyz);
                ComboColors.Add(Colors.From255RGBA(5, 5, 240, 255).Xyz);
                ComboColors.Add(Colors.From255RGBA(240, 5, 5, 255).Xyz);
                Utils.Log($"Skin.ini parsing completed with 0 combo colors ???", LogLevel.Error);
            }
        }

        private Vector3 parseColor(string text)
        {
            string[] colors = text.Split(',');

            byte r = byte.Parse(colors[0]);
            byte g = byte.Parse(colors[1]);
            byte b = byte.Parse(colors[2]);

            return new Vector3(r / 255f, g / 255f, b / 255f);
        }

        // i fucking hate making parsers
        private void parse(string line)
        {
            //Remove comments
            int indexOfComment = line.IndexOf("//");

            if(indexOfComment != -1)
                line = line.Remove(indexOfComment);

            var options = line.Replace(" ", "").Split(':');
            if(options.Length == 2)
            {
                var option = options[0].ToLower();
                var value = options[1];

                if (option.StartsWith("menuglow"))
                {
                    MenuGlow = parseColor(value);
                }else if (option.StartsWith("sliderborder"))
                {
                    SliderBorder = parseColor(value);
                }else if (option.StartsWith("slidertrackoverride"))
                {
                    SliderTrackOverride = parseColor(value);
                }else if (option.StartsWith("hitcircleprefix"))
                {
                    HitCirclePrefix = value;
                }
                else if (option.StartsWith("hitcircleoverlap"))
                {
                    HitCircleOverlap = float.Parse(value);
                }
                else if (option.StartsWith("scoreprefix"))
                {
                    ScorePrefix = value;
                }
                else if (option.StartsWith("scoreoverlap"))
                {
                    ScoreOverlap = float.Parse(value);
                }
                else if (option.StartsWith("comboprefix"))
                {
                    ComboPrefix = value;
                }
                else if (option.StartsWith("combooverlap"))
                {
                    ComboOverlap = float.Parse(value);
                }
                else if(option.StartsWith("combo") && option.Length == 6)
                {
                    try
                    {
                        var col = parseColor(value);
                        ComboColors.Add(col);
                    }
                    catch
                    {
                        Utils.Log($"Error parsing combo color Option: {option} Value: {value}", LogLevel.Error);
                    }
                }
                else if(option.StartsWith("hitcircleoverlayabovenumer") || option.StartsWith("hitcircleoverlayabovenumber"))
                {
                    HitCircleOverlayAboveNumber = Convert.ToBoolean(int.Parse(value));
                }
                else if (option.StartsWith("songselectactivetext"))
                {
                    SongSelectActiveTextColor = parseColor(value);
                }
                else if (option.StartsWith("songselectinactivetext"))
                {
                    SongSelectInactiveTextColor = parseColor(value);
                }
                else if (option.StartsWith("animationframerate"))
                {
                    Framerate = Convert.ToInt32(value);
                }
            }
        }
    }
}
