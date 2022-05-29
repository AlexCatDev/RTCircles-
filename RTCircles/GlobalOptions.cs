using Easy2D;

namespace RTCircles
{
    public static class GlobalOptions
    {
        #region booleans
        public readonly static Option<bool> Bloom
            = Option<bool>.CreateProxy("Bloom", (value) => { GPUSched.Instance.Enqueue(() => { PostProcessing.Bloom = value; }); }, false, "Looks like shit lmao");

        public readonly static Option<bool> MotionBlur
            = Option<bool>.CreateProxy("MotionBlur", (value) => { GPUSched.Instance.Enqueue(() => { PostProcessing.MotionBlur = value; }); }, false, "This only looks good if you get over 800 fps");

        public readonly static Option<bool> UseFancyCursorTrail = new Option<bool>("UseFancyCursorTrail", false);

        public readonly static Option<bool> SliderSnakeIn = new Option<bool>("SliderSnakeIn", true) { Description = "Negligible performance hit" };

        public readonly static Option<bool> SliderSnakeOut = new Option<bool>("SliderSnakeOut", true) { Description = "Significant performance hit" };

        public readonly static Option<bool> SliderSnakeExplode = new Option<bool>("SliderSnakeExplode", true) { Description = "No performance hit" };

        public readonly static Option<bool> AutoCursorDance = new Option<bool>("AutoCursorDance", false);

        public readonly static Option<bool> ShowRenderGraphOverlay = new Option<bool>("ShowRenderGraphOverlay", false);

        public readonly static Option<bool> ShowLogOverlay = new Option<bool>("ShowLogOverlay", false);

        public readonly static Option<bool> ShowFPS = new Option<bool>("ShowFPS", true);

        public readonly static Option<bool> KiaiCatJam = new Option<bool>("KiaiCatJam", false);

        public readonly static Option<bool> UseGameplayAsBackgroundSrc = new Option<bool>("UseGameplayAsBackgroundSrc", true) { Description = "Use current gameplay as background in the main menu?"};

        public readonly static Option<bool> AllowMapHitSounds = new Option<bool>("AllowMapHitSounds", true);

        public readonly static Option<bool> RenderBackground = new Option<bool>("RenderBackground", false);

        public readonly static Option<bool> RGBCircles = new Option<bool>("RGBCircles", false) { Description = "RGB ;) (Might not look good with all skins)" };

        public readonly static Option<bool> UseFastSliders = new Option<bool>("UseFastSliders", false) { Description = "Ugly sliders (Requires the current map to be reloaded, Please use slider snaking with this option)"};

        public readonly static Option<bool> EnableComboBursts = new Option<bool>("EnableComboBursts", true) { Description = "Want some anime girl to clutter your screen every 50 combo?" };

        public readonly static Option<bool> EnableMouseButtons = new Option<bool>("MouseButtons", false) { Description = "Enable Mouse Buttons?" };
        #endregion
        #region doubles
        public readonly static Option<double> GlobalVolume = Option<double>.CreateProxy("GlobalVolume", (volume) => Sound.GlobalVolume = volume, 0.3);

        public readonly static Option<double> SkinVolume = Option<double>.CreateProxy("SkinVolume", (volume) => {
            Skin.Hitsounds?.SetVolume(volume);

            if(Skin.ComboBreak != null)
                Skin.ComboBreak.Volume = volume;

            if (Skin.SliderSlide != null)
                Skin.SliderSlide.Volume = volume;

            if (Skin.SpinnerBonus != null)
                Skin.SpinnerBonus.Volume = volume;

            OsuContainer.Beatmap?.Hitsounds?.SetVolume(volume);
        }, 1);

        public readonly static Option<double> SongVolume = Option<double>.CreateProxy("SongVolume", (volume) => {
            if(OsuContainer.Beatmap != null)
                OsuContainer.Beatmap.Song.Volume = volume;
        }, 1);
        #endregion
        #region ints
        public readonly static Option<int> SongOffsetMS = new Option<int>("SongOffset", 0);
        #endregion

        #region strings
        public readonly static Option<string> OsuFolder = new Option<string>("OsuFolder", "");
        public readonly static Option<string> SkinFolder = new Option<string>("SkinFolder", "");
        #endregion
        public static void Init() 
        {
            Utils.Log($"Loaded Settings", LogLevel.Info);
            //We need to access a variable to instantiate every variable lol
            var ok = Bloom.Value;
        }
    }
}
