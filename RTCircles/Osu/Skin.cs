using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTCircles
{
    //TODO: Compile texture elements to an atlas.
    //TODO: Somehow fix when circle/combo/score numbers share the same texture, dont load them again
    //TODO: Support animated texture correctly
    //Basically redo this whole thing !

    public class OsuAnimatedTexture
    {
        private OsuAnimatedTexture() { }

        private List<OsuTexture> textures;

        public IReadOnlyList<OsuTexture> Textures => textures;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="nameWithoutDash"></param>
        /// <param name="range">From and to are inclusive. If no range is specified it will keep going from 0 to 100 and only stop when it runs out of textures to load</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public OsuAnimatedTexture FromPath(string path, string nameWithoutDash, (int from, int to)? range = null)
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
                    var filename = $"{nameWithoutDash}-{i}";
                    var osuTexture = Skin.LoadTexture(path, filename, true, false);

                    textures.Add(osuTexture);
                }
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    var filename = $"{nameWithoutDash}-{i}";
                    var osuTexture = Skin.LoadTexture(path, filename, true, true);

                    if (osuTexture == null)
                        break;

                    textures.Add(osuTexture);
                }
            }

            return osuAnimTex;
        }


    }

    public class OsuTexture
    {
        public float Scale { get; private set; }

        public Texture Texture { get; private set; }

        public bool IsX2 { get; private set; }

        public static implicit operator Texture(OsuTexture ot) => ot.Texture;

        public OsuTexture(Texture texture, bool isX2, float X2Size)
        {
            Texture = texture;

            IsX2 = isX2;

            Scale = isX2 ? X2Size : X2Size / 2;
        }
    }

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
                col = MathUtils.RainbowColor(OsuContainer.SongPosition/1000, 0.5f, 1.1f);
            
            if (OsuContainer.IsKiaiTimeActive)
                col *= 1 + (float)(OsuContainer.BeatProgress * 0.25);

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

        public bool HitCircleOverlayAboveNumber = true;

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
                }else if(option.StartsWith("hitcircleoverlayabovenumer") || option.StartsWith("hitcircleoverlayabovenumber"))
                {
                    HitCircleOverlayAboveNumber = Convert.ToBoolean(int.Parse(value));
                }
            }
        }
    }

    public static class Skin
    {
        public static string[] SupportedAudioExtensions { get; private set; } = new string[] { ".mp3", ".wav", ".ogg" };

        public static string[] SupportedImageExtensions { get; private set; } = new string[] { ".png", ".jpg", ".gif", ".bmp", ".tga", ".tiff" };

        /// <summary>
        /// TODO: Compile textures into an atlas, so drawcalls can be cut down.
        /// </summary>

        public static OsuTexture ApproachCircle { get; private set; }

        public static OsuTexture HitCircleOverlay { get; private set; }
        public static OsuTexture HitCircle { get; private set; }

        public static OsuTexture Cursor { get; private set; }

        public static OsuTexture CursorTrail { get; private set; }

        public static OsuTexture Smoke { get; private set; }

        public static OsuTexture SliderFollowCircle { get; private set; }
        public static OsuTexture SliderBall { get; private set; }

        public static OsuTexture SliderReverse { get; private set; }

        public static OsuTexture FollowPoint { get; private set; }

        
        public static SkinNumberStore CircleNumbers { get; private set; }

        public static SkinNumberStore ComboNumbers { get; private set; }

        public static SkinNumberStore ScoreNumbers { get; private set; }
        
        public static HitsoundStore Hitsounds { get; private set; }

        public static Texture Arrow { get; private set; } = new Texture(Utils.GetResource("UI.Assets.arrowexpand.png"));

        public static Texture Checkmark { get; private set; }

        public static Sound Hover { get; private set; }
        public static Sound Click { get; private set; }

        public static OsuTexture Star { get; private set; }

        public static OsuTexture CircularMetre { get; private set; }

        public static Sound ComboBreak { get; private set; }

        public static OsuTexture Hit300 { get; private set; }
        public static OsuTexture Hit100 { get; private set; }
        public static OsuTexture Hit50 { get; private set; }
        public static OsuTexture HitMiss { get; private set; }

        public static OsuTexture SpinnerCircle { get; private set; }
        public static Sound SpinnerBonus { get; private set; }

        public static Sound SliderSlide { get; private set; }

        public static OsuTexture SpinnerApproachCircle { get; private set; }

        public static Texture LoadingSpinner { get; private set; }

        public static SkinConfiguration Config { get; private set; }

        public static OsuTexture SliderStartCircle { get; private set; }
        public static OsuTexture SliderStartCircleOverlay { get; private set; }

        public static Texture DefaultBackground { get; private set; } = new Texture(Utils.GetResource("Skin.defaultbackground.png"));

        public static OsuTexture CursorMiddle { get; private set; }

        public static OsuTexture WarningArrow { get; private set; }

        public static OsuTexture ComboBurst { get; private set; }

        public static OsuTexture HRModIcon { get; private set; }
        public static OsuTexture DTModIcon { get; private set; }
        public static OsuTexture EZModIcon { get;private set; }

        public static OsuTexture RankingXH { get; private set; }
        public static OsuTexture RankingX { get; private set; }
        public static OsuTexture RankingSH { get; private set; }
        public static OsuTexture RankingS { get; private set; }
        public static OsuTexture RankingA { get; private set; }
        public static OsuTexture RankingB { get; private set; }
        public static OsuTexture RankingC { get; private set; }
        public static OsuTexture RankingD { get; private set; }

        public static OsuTexture HealthBar_BG { get; private set; }
        public static OsuTexture HealthBar_Fill { get; private set; }
        public static OsuTexture HealthBar_Marker { get; private set; }

        public static Texture FlashlightOverlay = new Texture(Utils.GetResource("Skin.FlashlightOverlay.png"));

        public static string CurrentPath { get; private set; }

        public static void Reload() => Load(CurrentPath);

        public static void Load(string path)
        {
            Utils.BeginProfiling("SkinLoad");

            bool invalidPath = !Directory.Exists(path);

            CurrentPath = path;

            Config = new SkinConfiguration(File.Exists($"{path}/skin.ini") ? File.OpenRead($"{path}/skin.ini") : Utils.GetResource("Skin.skin.ini"));

            WarningArrow = LoadTexture(path, "play-warningarrow");

            LoadingSpinner = LoadTexture(path, "loading");

            SpinnerApproachCircle = LoadTexture(path, "spinner-approachcircle");

            SliderSlide = LoadSound(path, "sliderslide");
            SliderSlide.Volume = GlobalOptions.SkinVolume.Value;

            SpinnerBonus = LoadSound(path, "spinnerbonus");
            SpinnerBonus.Volume = GlobalOptions.SkinVolume.Value;

            SpinnerCircle = LoadTexture(path, "spinner-circle");

            Hit300 = LoadTexture(path, "hit300");
            Hit100 = LoadTexture(path, "hit100");
            Hit50 = LoadTexture(path, "hit50");
            HitMiss = LoadTexture(path, "hit0");

            ComboBreak = LoadSound(path, "combobreak");
            ComboBreak.Volume = GlobalOptions.SkinVolume.Value;

            CircularMetre = LoadTexture(path, "circular-metre");

            var cursorTex = LoadTexture(path, "cursor");

            if (cursorTex.Texture.Size.X == 1 || cursorTex.Texture.Size.Y == 1)
            {
                var cursorMiddle = LoadTexture(path, "cursormiddle", true, true);
                if(cursorMiddle is null)
                {
                    Utils.Log("CursorTexture was nothing so default element is used", LogLevel.Error);
                    Cursor = LoadTexture("", "cursor");
                }else if (cursorMiddle.Texture.Size.X == 1 || cursorMiddle.Texture.Size.Y == 1)
                {
                    Utils.Log("CursorTexture was nothing so default element is used", LogLevel.Error);
                    Cursor = LoadTexture("", "cursor");
                }
                else
                {
                    Cursor = cursorMiddle;
                }
            }
            else
            {
                Cursor = cursorTex;
            }

            CursorTrail = LoadTexture(path, "cursortrail");
            CursorMiddle = LoadTexture(path, "cursormiddle", true, true);

            HitCircle = LoadTexture(path, "hitcircle");
            HitCircleOverlay = LoadTexture(path, "hitcircleoverlay");
            ApproachCircle = LoadTexture(path,"approachcircle");
            Smoke = LoadTexture(path,"smoke");
            SliderFollowCircle = LoadTexture(path, "sliderfollowcircle");
            //Use hitcircle if sliderstartcircle wasnt found
            SliderStartCircle = LoadTexture(path, "sliderstartcircle", true, true) ?? HitCircle;
            SliderStartCircleOverlay = LoadTexture(path, "sliderstartcircleoverlay", true, true) ?? HitCircleOverlay;

            SliderBall = LoadTexture(path, "sliderb0");
            SliderReverse = LoadTexture(path, "reversearrow");

            FollowPoint = LoadTexture(path, "followpoint");
            if (FollowPoint.Texture.Size.X == 1 || FollowPoint.Texture.Size.Y == 1)
                FollowPoint = new OsuTexture(new Texture(Utils.GetResource($"Skin.followpoint.png")), true, 0);

            CircleNumbers = new SkinNumberStore(path, $"{Config.HitCirclePrefix}-");
            CircleNumbers.Overlap = Config.HitCircleOverlap;

            ComboNumbers = new SkinNumberStore(path, $"{Config.ComboPrefix}-", "dot", "percent", "combo-x");
            ComboNumbers.Overlap = Config.ComboOverlap;

            ScoreNumbers = new SkinNumberStore(path, $"{Config.ScorePrefix}-", "dot", "percent", "score-x");
            ScoreNumbers.Overlap = Config.ScoreOverlap;

            Hitsounds = new HitsoundStore(path, true);
            Hitsounds.SetVolume(GlobalOptions.SkinVolume.Value);

            Checkmark = LoadTexture(path, "checkmark");

            Hover = LoadSound(path, "hover");
            Click = LoadSound(path, "click");

            Star = LoadTexture(path, "star");

            ComboBurst = LoadTexture(path, "comboburst-0", true, true);

            HRModIcon = LoadTexture(path, "selection-mod-hardrock");
            DTModIcon = LoadTexture(path, "selection-mod-doubletime");
            EZModIcon = LoadTexture(path, "selection-mod-easy");

            HealthBar_Marker = LoadTexture(path, "scorebar-marker", false, allowNull: !invalidPath);

            HealthBar_BG = LoadTexture(path, "scorebar-bg", false, false);
            HealthBar_Fill = LoadTexture(path, "scorebar-colour-0", false, true);
            if(HealthBar_Fill == null)
                HealthBar_Fill = LoadTexture(path, "scorebar-colour", false, false);

            RankingXH = LoadTexture(path, "ranking-XH-small");
            RankingX = LoadTexture(path, "ranking-X-small");
            RankingSH = LoadTexture(path, "ranking-SH-small");
            RankingS = LoadTexture(path, "ranking-S-small");
            RankingA = LoadTexture(path, "ranking-A-small");
            RankingB = LoadTexture(path, "ranking-B-small");
            RankingC = LoadTexture(path, "ranking-C-small");
            RankingD = LoadTexture(path, "ranking-D-small");

            double loadTime = Utils.EndProfiling("SkinLoad");

            NotificationManager.ShowMessage($"\"{path}\" loaded in {loadTime:F0} ms", new Vector3(0.8f, 0.8f, 1f), 4f);
        }
        
        //Better version
        /*
        public static void LoadTexture(ref Texture texture, string path, string name, bool genMipMaps = true, bool allowNull = false)
        {
            Texture loadTexture(string path, string name)
            {
                foreach (var extension in SupportedImageExtensions)
                {
                    string file = $"{path}/{name}{extension}";

                    if (File.Exists(file))
                    {
                        Texture tex = new Texture(file);
                        tex.GenerateMipmaps = genMipMaps;
                        tex.UseAsyncLoading = false;
                        //Bind to preload it.
                        tex.Bind();
                        return tex;
                    }
                }

                return null;
            }

            //First look for the x2 version
            var tex = loadTexture(path, $"{name}@2x");

            //if that fails look for the normal version
            if (tex is null)
                tex = loadTexture(path, name);

            if (allowNull && tex is null)
            {
                texture = null;
                return;
            }

            //if that also fails, load it from our default skin but only if it's not already loaded from there
            if (tex is null)
            {
                if (texture?.Path.Contains("./DefaultSkin") == true)
                    return;

                Utils.Log($"Could not load {name} so default element is used", LogLevel.Warning);
                texture = loadTexture("./DefaultSkin", name);

                return;
            }
            else
            {
                texture?.
        ();
                texture = tex;

                return;
            }

            if (tex is null)
                throw new FileNotFoundException($"No file found {path}/{name}.?");
        }
        */

        
        public static OsuTexture LoadTexture(string path, string name, bool genMipMaps = true, bool allowNull = false)
        {
            Texture loadTexture(string path, string name)
            {
                foreach (var extension in SupportedImageExtensions)
                {
                    string file = $"{path}/{name}{extension}";

                    if (File.Exists(file))
                    {
                        Texture tex = new Texture(File.OpenRead(file));
                        tex.GenerateMipmaps = genMipMaps;
                        tex.UseAsyncLoading = false;

                        //Bind to preload it.
                        tex.Bind();
                        return tex;
                    }
                }

                return null;
            }

            //First look for the x2 version
            var tex = loadTexture(path, $"{name}@2x");

            //if that fails look for the normal version
            if (tex is null)
                tex = loadTexture(path, name);
            else
                return new OsuTexture(tex, true, 0);

            if (tex is null && allowNull)
                return null;

            //if that also fails, load it from our default skin
            if (tex is null)
            {
                Utils.Log($"Could not load {name} so default element is used", LogLevel.Warning);
                return new OsuTexture(new Texture(Utils.GetResource($"Skin.{name}.png")), true, 0);
            }
            else
                return new OsuTexture(tex, false, 0);
        }

        public static float GetScale(OsuTexture texture, float normal = 128f, float x2 = 256f)
        {
            float scaleNominator = texture.IsX2 ? x2 : normal;
            return texture.Texture.Size.Y / scaleNominator;
        }

        public static Sound LoadSound(string path, string name, bool allowNull = false)
        {
            Sound loadSound(string path, string name)
            {
                foreach (var extension in SupportedAudioExtensions)
                {
                    string file = $"{path}/{name}{extension}";

                    if (File.Exists(file))
                    {
                        return new Sound(File.OpenRead(file), false, true);
                    }
                }

                return null;
            }

            //load sound from file
            var sound = loadSound(path, name);

            if (allowNull)
                return sound;

            //if that fails load from our default skin
            if (sound is null)
            {
                sound = new Sound(Utils.GetResource($"Skin.{name}.wav"), false, true);
                Utils.Log($"Could not load {name} so default element is used", LogLevel.Warning);
            }

            return sound;

            throw new FileNotFoundException($"No file found {path}/{name}.?");
        }
    }
}
