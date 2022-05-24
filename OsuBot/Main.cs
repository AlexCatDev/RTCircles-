using Easy2D;
using Easy2D.Game;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using FFMediaToolkit;
using FFMediaToolkit.Encoding;

namespace OsuBot
{
    /// <summary>
    /// Ignore this clusterfuck of no structure
    /// But basically just a test on how to take the RTCircles project and do something else with it.
    /// This will generate MP4 Files based on osu plays and post them to discord
    /// </summary>
    public class Main : RTCircles.MainGame
    {
        private FrameBuffer frameBuffer = new FrameBuffer(1, 1, pixelFormat: Silk.NET.OpenGLES.PixelFormat.Rgb, textureComponentCount: Silk.NET.OpenGLES.InternalFormat.Rgb);
        private DiscordSocketClient discordClient = new DiscordSocketClient();
        private Graphics graphics;

        private ConcurrentQueue<SocketMessage> playQueue = new();

        private RTCircles.OsuScreen osuScreen;

        private BanchoAPI banchoAPI;

        public Config Config { get; private set; }

        public override void OnLoad()
        {
            Utils.WriteToConsole = true;
            Utils.IgnoredLogLevels.Add(Easy2D.LogLevel.Debug);

            if (!File.Exists("Config.json"))
            {
                Utilities.Save(new Config(), "Config.json");

                Utils.Log("no Config.json, one was created, now go fill in your credentials", Easy2D.LogLevel.Success);

                View.Close();
                return;
            }

            Config = Utilities.Load<Config>("Config.json");

            if (Config == null)
            {
                Utils.Log("Config could not be parsed", Easy2D.LogLevel.Error);

                View.Close();
                return;
            }

            if (string.IsNullOrEmpty(Config.DISCORD_BOT_SECRET))
            {
                Utils.Log("Config does not have a valid bot secret", Easy2D.LogLevel.Error);
                View.Close();
                return;
            }

            if (string.IsNullOrEmpty(Config.OSU_API_SECRET))
            {
                Utils.Log("Config does not have a valid osu api secret", Easy2D.LogLevel.Error);
                View.Close();
                return;
            }

            discordClient.Ready += () =>
            {
                Utils.Log($"Logged into discord!", Easy2D.LogLevel.Success);
                return Task.CompletedTask;
            };

            discordClient.MessageReceived += (message) =>
            {
                Utils.Log($"[{message.Channel.Name}] {message.Author.Username}#{message.Author.Discriminator} -> {message.Content}", Easy2D.LogLevel.Info);

                if (!message.Author.IsBot)
                {
                    if (message.Content.ToLower().StartsWith(">video"))
                        playQueue.Enqueue(message);
                }

                return Task.CompletedTask;
            };

            banchoAPI = new BanchoAPI(Config.OSU_API_SECRET);

            discordClient.LoginAsync(TokenType.Bot, Config.DISCORD_BOT_SECRET);
            discordClient.StartAsync();

            VSync = true;

            graphics = new Graphics();

            frameBuffer.EnsureSize(OUTPUT_WIDTH, OUTPUT_HEIGHT);
            frameBuffer.Bind();
            Viewport.SetViewport(0, 0, OUTPUT_WIDTH, OUTPUT_HEIGHT);
            GL.Instance.ReadBuffer(Silk.NET.OpenGLES.ReadBufferMode.ColorAttachment0);

            //Obviously you have to provide your own path here
            FFmpegLoader.FFmpegPath = @"C:\Users\user\Desktop\TempOsuBot\ffmpeg";

            //Also here, but if it can't find a skin it will just use the default one
            RTCircles.Skin.Load(@"C:\Users\user\Desktop\osu!\Skins\- YUGEN FINAL WS -");
            WindowSize = new Vector2(OUTPUT_WIDTH, OUTPUT_HEIGHT);
            Scale = 0.5f;

            RTCircles.GlobalOptions.RenderBackground.Value = false;
            RTCircles.GlobalOptions.SliderSnakeExplode.Value = false;
            RTCircles.GlobalOptions.UseFancyCursorTrail.Value = false;

            //This is to not load all the beatmaps from it's database and set a map and all that jazz
            RTCircles.MenuScreen.IsRTCircles = false;
            RTCircles.OsuContainer.MuteHitsounds = true;
            osuScreen = RTCircles.ScreenManager.GetScreen<RTCircles.OsuScreen>();
            osuScreen.RenderHUD = false;
        }

        private ulong? BeatmapUrlToMapID(SocketMessage sMsg, CommandBuffer buffer)
        {
            string url = buffer.GetParameter("https://osu.ppy.sh/");

            if (url == "")
                return null;

            if (ulong.TryParse(url.Split('/').Last(), out ulong beatmapID))
                return beatmapID;
            else
            {
                sMsg.Channel.SendMessageAsync("Error parsing beatmap url.");
                return null;
            }
        }

        public override void OnOpenFile(string fullpath)
        {

        }

        private void renderTick(int frame, SocketMessage message)
        {
            double frameDelta = (1000d / OUTPUT_FPS) / 1000;
            double totalTime = frame * frameDelta;

            //RTCircles.OsuContainer.SongPosition += frameDelta*1000;

            float rate = RTCircles.OsuContainer.Beatmap.Mods.HasFlag(RTCircles.Mods.DT) ? 1.5f : RTCircles.OsuContainer.Beatmap.Mods.HasFlag(RTCircles.Mods.HT) ? 0.75f : 1;

            RTCircles.OsuContainer.Update((float)frameDelta * 1000 * rate);

            osuScreen.Update((float)frameDelta);
            osuScreen.Render(graphics);

            GPUSched.Instance.RunPendingTasks();
        }

        //private const int OUTPUT_WIDTH = 600;
        //private const int OUTPUT_HEIGHT = 360;

        private const int OUTPUT_WIDTH = 800;
        private const int OUTPUT_HEIGHT = 480;
        const int FRAMES_TO_RENDER = 2600;
        private const int OUTPUT_FPS = 120;

        private byte[] pixelBuffer;
        public override void OnRender()
        {
            if (pixelBuffer == null)
                pixelBuffer = new byte[OUTPUT_WIDTH * OUTPUT_HEIGHT * 3];

            if (playQueue.TryDequeue(out SocketMessage message))
            {
                var args = message.Content.Split(' ').ToList();
                args.RemoveAt(0);
                CommandBuffer commandBuffer = new CommandBuffer(args, "");

                if(commandBuffer.Count == 0)
                {
                    message.Channel.SendMessageAsync($"`How to use:`\n>video <**username** OR **map link**> <*optional* time eg **1:30** mm:ss>");
                    return;
                }

                TimeSpan? customStartTime = null;
                commandBuffer.Take((str) =>
                {
                    if (TimeSpan.TryParseExact(str, "m\\:s", System.Globalization.CultureInfo.InvariantCulture, out var time))
                    {
                        customStartTime = time;
                        return true;
                    }

                    return false;
                });

                var enabledMods = RTCircles.Mods.NM;

                ulong? beatmapID = BeatmapUrlToMapID(message, commandBuffer);

                if (!beatmapID.HasValue)
                {
                    string username = commandBuffer.GetRemaining(" ");

                    var recentPlay = banchoAPI.GetRecentPlays(username);

                    if (recentPlay.Count == 0)
                    {
                        message.Channel.SendMessageAsync($"`{username}` has no recent plays? :face_with_raised_eyebrow:");
                        return;
                    }

                    enabledMods = recentPlay[0].EnabledMods;

                    beatmapID = recentPlay[0].BeatmapID;
                }

                Utils.Log($"Dequeued a play: {message.Content}", Easy2D.LogLevel.Important);

                string outputFilename = $@"C:\Users\user\Desktop\TempOsuBot/{Guid.NewGuid().ToString()}.mp4";

                var videoSettings = new VideoEncoderSettings(width: OUTPUT_WIDTH, height: OUTPUT_HEIGHT, framerate: OUTPUT_FPS, codec: VideoCodec.H264);
                videoSettings.EncoderPreset = EncoderPreset.Fast;
                videoSettings.CRF = 24;

                var audioSettings = new AudioEncoderSettings(44100, 2, AudioCodec.Default);

                var videoFile = MediaBuilder.CreateContainer(outputFilename, ContainerFormat.MP4)
                    .WithVideo(videoSettings).Create();

                var beatmapText = BeatmapManager.GetBeatmap(beatmapID.Value);

                var rawBeatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(beatmapText.Split("\r\n"));

                    RTCircles.PlayableBeatmap beatmap = new RTCircles.PlayableBeatmap(
                        rawBeatmap, null, null, null);
                    beatmap.GenerateHitObjects(RTCircles.Mods.Auto | enabledMods);

                    RTCircles.OsuContainer.SetMap(beatmap);

                osuScreen.ResetState();

                if (!customStartTime.HasValue)
                {
                    var startTime = rawBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai)?.Offset ?? 0;

                    if (startTime <= 0)
                        startTime = rawBeatmap.GeneralSection.PreviewTime;

                    var firstObject = beatmap.HitObjects[0];

                    if (startTime < firstObject.BaseObject.StartTime)
                        startTime = firstObject.BaseObject.StartTime - (int)RTCircles.OsuContainer.Beatmap.Preempt;

                    RTCircles.OsuContainer.SongPosition = startTime;
                }
                else
                {
                    RTCircles.OsuContainer.SongPosition = customStartTime.Value.TotalMilliseconds;
                }

                osuScreen.EnsureObjectIndexSynchronization();

                string displayMods = "\n";

                foreach (RTCircles.Mods value in Enum.GetValues<RTCircles.Mods>())
                {
                    if (value == RTCircles.Mods.NM)
                        continue;

                    if (enabledMods.HasFlag(value))
                        displayMods += $"+{value.ToString()} ";
                }

                if (displayMods == "\n")
                    displayMods = string.Empty;

                var mapText = $"{rawBeatmap.MetadataSection.Artist} - {rawBeatmap.MetadataSection.Title} [{rawBeatmap.MetadataSection.Version}]{displayMods}";

                var endTimeSpan = TimeSpan.FromMilliseconds(beatmap.HitObjects[^1].BaseObject.EndTime - beatmap.HitObjects[0].BaseObject.StartTime);
                var endTimeSpanString = (Math.Floor(endTimeSpan.TotalMinutes) + ":" + endTimeSpan.ToString("ss"));
                var timeSpanScale = 0.5f;

                for (int i = 0; i < FRAMES_TO_RENDER; i++)
                {
                    GL.Instance.Clear(Silk.NET.OpenGLES.ClearBufferMask.ColorBufferBit);

                    renderTick(i, message);

                    var currentTimeSpan = TimeSpan.FromMilliseconds(RTCircles.OsuContainer.SongPosition);
                    string currentTimeSpanString = (Math.Floor(currentTimeSpan.TotalMinutes) + ":" + currentTimeSpan.ToString("ss"));

                    //graphics.DrawStringNoAlign($"{i+1}/{FRAMES_TO_RENDER}", Font.DefaultFont, new Vector2(3, 3), Colors.White, 0.25f);

                    graphics.DrawString(mapText, Font.DefaultFont, new Vector2(3), new Vector4(0.9f), 0.3f);

                    string timeSpanString = $"{currentTimeSpanString}/{endTimeSpanString}";

                    var timeSpanTextSize = Font.DefaultFont.MessureString(timeSpanString, timeSpanScale);
                    graphics.DrawString(timeSpanString, Font.DefaultFont, Viewport.Area.TopRight - new Vector2(timeSpanTextSize.X, 0), Colors.White, timeSpanScale);

                    graphics.Projection = Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, 0, frameBuffer.Height, -1, 1);
                    graphics.EndDraw();

                    GL.Instance.Finish();

                    unsafe
                    {
                        fixed (void* framePtr = pixelBuffer)
                        {
                            GL.Instance.ReadPixels(0, 0, (uint)OUTPUT_WIDTH, (uint)OUTPUT_HEIGHT,
                                Silk.NET.OpenGLES.PixelFormat.Rgb, Silk.NET.OpenGLES.PixelType.UnsignedByte, framePtr);
                        }

                        videoFile.Video.AddFrame(new FFMediaToolkit.Graphics.ImageData(
                                new Span<byte>(pixelBuffer),
                                FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24,
                                new System.Drawing.Size(OUTPUT_WIDTH, OUTPUT_HEIGHT)));
                    }
                }

                videoFile.Dispose();

                sendFileThenDelete(message, outputFilename);
            }
        }

        private async void sendFileThenDelete(SocketMessage message, string path)
        {
            await message.Channel.SendFileAsync(new FileAttachment(path, "osu.mp4", null, false));
            File.Delete(path);
        }

        public override void OnResize(int width, int height)
        {

        }

        public override void OnUpdate()
        {

        }
    }
}