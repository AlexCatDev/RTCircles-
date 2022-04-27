using Easy2D;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using System.Runtime.CompilerServices;
using DiscordRPC;

namespace RTCircles.Desktop
{
    class Program
    {
        static DiscordRpcClient rpcClient = new DiscordRpcClient("834056835786604554", autoEvents: true, logger: null);
        static MainGame game;

        static RichPresence presence = new RichPresence();

        static void Main(string[] args)
        {
            game = new MainGame();

            Assets assets = new Assets() { LargeImageKey = "asset", LargeImageText = $"Version: {game.Version}" };
#if DEBUG
            assets.SmallImageKey = "debug";
            assets.SmallImageText = $"Debugging";
#endif

            presence.Assets = assets;


            rpcClient.Initialize();

            if (!rpcClient.RegisterUriScheme(null, null))
                Utils.Log($"Could not register Discord RPC Uri Scheme!", LogLevel.Error);

            OsuContainer.BeatmapChanged += OsuContainer_BeatmapChanged;

            game.Run();
        }


        private static void OsuContainer_BeatmapChanged()
        {
            /*
                Secrets = new Secrets() { JoinSecret = "FuckAnime727" },
                Party = new Party() { ID = Guid.NewGuid().ToString(), Size = 1, Max = 5, Privacy = Party.PrivacySetting.Private },
            */
            presence.Details = $"FPS: {game.FPS}";
            presence.Timestamps = Timestamps.Now;
            presence.State = $"Listening to: {OsuContainer.Beatmap.SongName} [{OsuContainer.Beatmap.DifficultyName}]";

            rpcClient.SetPresence(presence);
        }
    }
}
