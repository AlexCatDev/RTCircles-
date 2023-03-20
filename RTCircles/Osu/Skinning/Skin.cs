using Easy2D;
using OpenTK.Mathematics;
using System.IO;
using System.Linq;

namespace RTCircles
{
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
        public static OsuAnimatedTexture SliderBall { get; private set; }
        public static OsuTexture SliderBallSpecular { get; private set; }

        public static OsuTexture SliderReverse { get; private set; }

        public static OsuAnimatedTexture FollowPoint { get; private set; }

        
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
        public static OsuAnimatedTexture HealthBar_Fill { get; private set; }
        public static OsuTexture HealthBar_Marker { get; private set; }

        public static Texture FlashlightOverlay = new Texture(Utils.GetResource("Skin.FlashlightOverlay.png"));

        public static Sound SelectDifficulty { get; private set; }

        public static Sound Applause { get; private set; }

        public static Texture CarouselButton { get; private set; }

        public static string CurrentPath { get; private set; }

        public static void Reload() => Load(CurrentPath);

        public static void Load(string path)
        {
            Utils.BeginProfiling("SkinLoad");

            bool invalidPath = !Directory.Exists(path);

            CurrentPath = path;

            Config = new SkinConfiguration(File.Exists($"{path}/skin.ini") ? File.OpenRead($"{path}/skin.ini") : Utils.GetResource("Skin.skin.ini"));

            CarouselButton = LoadTexture(path, "menu-button-background");

            WarningArrow = LoadTexture(path, "play-warningarrow");

            LoadingSpinner = LoadTexture(path, "loading");

            SpinnerApproachCircle = LoadTexture(path, "spinner-approachcircle");

            SliderSlide = LoadSound(path, "sliderslide");
            SliderSlide.AddFlag(ManagedBass.BassFlags.Loop);
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
            SliderFollowCircle = LoadTexture(path, "sliderfollowcircle", false);
            //Use hitcircle if sliderstartcircle wasnt found
            SliderStartCircle = LoadTexture(path, "sliderstartcircle", true, true) ?? HitCircle;
            SliderStartCircleOverlay = LoadTexture(path, "sliderstartcircleoverlay", true, true) ?? HitCircleOverlay;

            SliderBall = OsuAnimatedTexture.FromPath(path, "sliderb");
            if (SliderBall == null)
                SliderBall = new OsuAnimatedTexture(LoadTexture(path, "sliderb0", true, false));

            //SliderBallSpecular = LoadTexture(path, "sliderb-spec", true, true);

            SliderReverse = LoadTexture(path, "reversearrow");

            FollowPoint = OsuAnimatedTexture.FromPath(path, "followpoint-");
            if (FollowPoint == null)
                FollowPoint = new OsuAnimatedTexture(LoadTexture(path, "followpoint", true, false));

            CircleNumbers = new SkinNumberStore(path, $"{Config.HitCirclePrefix}-");
            CircleNumbers.Overlap = Config.HitCircleOverlap;

            ComboNumbers = new SkinNumberStore(path, $"{Config.ComboPrefix}-", null, null, "x");
            ComboNumbers.Overlap = Config.ComboOverlap;

            ScoreNumbers = new SkinNumberStore(path, $"{Config.ScorePrefix}-", "dot", "percent", "x");
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
            HealthBar_Fill = OsuAnimatedTexture.FromPath(path, "scorebar-colour-"); //LoadTexture(path, "scorebar-colour-0", false, true);
            if (HealthBar_Fill == null)
                HealthBar_Fill = new OsuAnimatedTexture(LoadTexture(path, "scorebar-colour", false, false));

            RankingXH = LoadTexture(path, "ranking-XH-small");
            RankingX = LoadTexture(path, "ranking-X-small");
            RankingSH = LoadTexture(path, "ranking-SH-small");
            RankingS = LoadTexture(path, "ranking-S-small");
            RankingA = LoadTexture(path, "ranking-A-small");
            RankingB = LoadTexture(path, "ranking-B-small");
            RankingC = LoadTexture(path, "ranking-C-small");
            RankingD = LoadTexture(path, "ranking-D-small");

            SelectDifficulty = LoadSound(path, "select-difficulty");

            Applause = LoadSound(path, "applause", true);

            double loadTime = Utils.EndProfiling("SkinLoad");

            string name = string.IsNullOrEmpty(path) ? "Skin" : $"\"{path}\"";

            NotificationManager.ShowMessage($"{name} loaded in {loadTime:F0} ms", new Vector3(0.8f, 0.8f, 1f), 2f);
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
                        using (FileStream fs = File.OpenRead(file))
                        {
                            Texture tex = new Texture(fs);
                            tex.GenerateMipmaps = genMipMaps;
                            tex.AutoDisposeStream = true;
                            tex.UseAsyncLoading = false;
                            tex.MipmapFilter = Silk.NET.OpenGLES.TextureMinFilter.LinearMipmapLinear;

                            //Bind to preload it.
                            tex.Bind();
                            return tex;
                        }
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

                var defaultTexture = new Texture(Utils.GetResource($"Skin.{name}.png"));
                defaultTexture.GenerateMipmaps = genMipMaps;
                defaultTexture.AutoDisposeStream = true;
                defaultTexture.MipmapFilter = Silk.NET.OpenGLES.TextureMinFilter.LinearMipmapLinear;
                defaultTexture.Bind();

                return new OsuTexture(defaultTexture, true, 0);
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
