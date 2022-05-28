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

        public Main()
        {
            Size = new Silk.NET.Maths.Vector2D<int>(640, 480);
        }

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

            //Ensure framebuffer is our default framebuffer, because otherwise, when framebuffer switches, it will try to bind the screen one ID: 0
            FrameBuffer.DefaultFrameBuffer.SetTarget(frameBuffer);

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

        public override void OnOpenFile(string fullpath)
        {

        }

        private void renderTick(int frame, SocketMessage message)
        {
            //Since we're rendering in fixed time mode, the deltatime will always be the 1 / TARGET_FPS
            double frameDelta = (1d / OUTPUT_FPS);
            double totalTime = frame * frameDelta;

            double rate = 1;

            if (RTCircles.OsuContainer.Beatmap.Mods.HasFlag(RTCircles.Mods.DT))
                rate = 1.5;
            else if (RTCircles.OsuContainer.Beatmap.Mods.HasFlag(RTCircles.Mods.HT))
                rate = 0.75;

            //Update the song position, and the timing point
            RTCircles.OsuContainer.Update(frameDelta * 1000 * rate);

            //Inside the osu screen the actual rendering of osu happens, very much code

            //Update, spawns objects etc
            osuScreen.Update((float)frameDelta);

            //Render, actually renders them
            osuScreen.Render(graphics);

            //GPU scheduler runs various tasks enqueued from likely other threads like the deletion of opengl objects, opengl is singlethreaded!
            GPUSched.Instance.RunPendingTasks();
        }

        /// <summary>
        /// The output resolution of the video (The gameplay gets drawn at this size too)
        /// </summary>
        private const int OUTPUT_WIDTH = 800;
        private const int OUTPUT_HEIGHT = 480;

        //More frames larger framesize, longer video generation time
        const int FRAMES_TO_RENDER = 2420;

        //More fps more smooth, less actual video time
        private const int OUTPUT_FPS = 120;

        private byte[] pixelBuffer;
        public override void OnRender()
        {
            if (pixelBuffer == null)
                pixelBuffer = new byte[OUTPUT_WIDTH * OUTPUT_HEIGHT * 3];

            if (playQueue.TryDequeue(out SocketMessage message))
            {
                var args = message.Content.Split(' ').ToList();

                //Remove >video from the list of strings
                args.RemoveAt(0);

                //Parse all the shit
                CommandBuffer commandBuffer = new CommandBuffer(args, "");

                if (commandBuffer.Count == 0)
                {
                    message.Channel.SendMessageAsync($"`How to use:`\n>video <**username** OR **map link**> <*optional* time eg **1:30** mm:ss> <*optional* **-dance** to cursor dance>");
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

                RTCircles.GlobalOptions.AutoCursorDance.Value = commandBuffer.HasParameter("-dance");

                var enabledMods = RTCircles.Mods.NM;

                ulong? beatmapID = Utilities.BeatmapUrlToMapID(message, commandBuffer);

                //check if the commands contains a beatmap url
                if (!beatmapID.HasValue)
                {
                    //If it doesn't then try to parse the remaining as a username

                    string username = commandBuffer.GetRemaining(" ");

                    var recentPlay = banchoAPI.GetRecentPlays(username);

                    if (recentPlay.Count == 0)
                    {
                        message.Channel.SendMessageAsync($"`{username}` has no recent plays. :face_with_raised_eyebrow:");
                        return;
                    }

                    enabledMods = recentPlay[0].EnabledMods;

                    beatmapID = recentPlay[0].BeatmapID;
                }
                else
                {
                    //Else if we did infact receive a beatmap url, try to parse the mods if any
                    enabledMods = Utilities.StringToMod(commandBuffer.GetRemaining());
                }

                Utils.Log($"Dequeued a play: {message.Content}", Easy2D.LogLevel.Important);

                //Setup video encoding

                string outputFilename = $@"C:\Users\user\Desktop\TempOsuBot/{Guid.NewGuid().ToString()}.mp4";

                var videoSettings = new VideoEncoderSettings(width: OUTPUT_WIDTH, height: OUTPUT_HEIGHT, framerate: OUTPUT_FPS, codec: VideoCodec.H264);
                videoSettings.EncoderPreset = EncoderPreset.Fast;
                videoSettings.CRF = 24;

                var audioSettings = new AudioEncoderSettings(44100, 2, AudioCodec.Default);

                var videoFile = MediaBuilder.CreateContainer(outputFilename, ContainerFormat.MP4)
                    .WithVideo(videoSettings).Create();

                //Now get or download the beatmap file (.osu) only, which contains all the beatmap text.
                var beatmapText = BeatmapManager.GetBeatmap(beatmapID.Value);

                //We split the beatmap text into lines which then get parsed to a osu beatmap
                var rawBeatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(beatmapText.Split("\r\n"));

                //Setup a 'playable' beatmap
                RTCircles.PlayableBeatmap beatmap = new RTCircles.PlayableBeatmap(
                    rawBeatmap, null, null, null);

                //Here we convert the hitobjects into actual renderable representations
                //Here we parse in the mods we want to use, (Mods doesn't actually do anything to the objects)
                //Í have a weird system for this, but the AR/OD/CS/HP does get adjusted based on the mods
                beatmap.GenerateHitObjects(RTCircles.Mods.Auto | enabledMods);

                //Now we notify the 'master container' to set map
                //I knows about the playfield size, and ensures timing points and shit
                RTCircles.OsuContainer.SetMap(beatmap);


                //Reset state, resets the score,accuracy, sets the object spawn index to 0, and other things
                osuScreen.ResetState();

                //Now check if a custom start time was included in our command
                if (!customStartTime.HasValue)
                {
                    //If it wasn't then we have to figure out a senseable start point ourselves

                    //Make start time the first kiai
                    var startTime = rawBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai)?.Offset ?? 0;

                    //If theres no kiai, use the preview time
                    if (startTime <= 0)
                        startTime = rawBeatmap.GeneralSection.PreviewTime;

                    var firstObject = beatmap.HitObjects[0];
                    
                    //if the preview time is less than the first object, then we just start at the first object
                    if (startTime < firstObject.BaseObject.StartTime)
                        startTime = firstObject.BaseObject.StartTime - (int)RTCircles.OsuContainer.Beatmap.Preempt;

                    RTCircles.OsuContainer.SongPosition = startTime;
                }
                else
                {
                    //If we had a custom time, use that
                    RTCircles.OsuContainer.SongPosition = customStartTime.Value.TotalMilliseconds;
                }

                //Ensure we are synchronized up to that point, so we dont start spawning all objects from 0
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
                    //Clear the image
                    GL.Instance.Clear(Silk.NET.OpenGLES.ClearBufferMask.ColorBufferBit);

                    //Render the osu gameplay
                    renderTick(i, message);

                    //Render the overlay with time and other info
                    var currentTimeSpan = TimeSpan.FromMilliseconds(RTCircles.OsuContainer.SongPosition - beatmap.HitObjects[0].BaseObject.StartTime);
                    string currentTimeSpanString = (Math.Floor(currentTimeSpan.TotalMinutes) + ":" + currentTimeSpan.ToString("ss"));

                    //graphics.DrawStringNoAlign($"{i+1}/{FRAMES_TO_RENDER}", Font.DefaultFont, new Vector2(3, 3), Colors.White, 0.25f);

                    graphics.DrawString(mapText, Font.DefaultFont, new Vector2(3), new Vector4(0.9f), 0.35f);

                    string timeSpanString = $"{currentTimeSpanString}/{endTimeSpanString}";

                    var timeSpanTextSize = Font.DefaultFont.MessureString(timeSpanString, timeSpanScale);
                    graphics.DrawString(timeSpanString, Font.DefaultFont, Viewport.Area.TopRight - new Vector2(timeSpanTextSize.X, 0), Colors.White, timeSpanScale);

                    //Update the projection (this never changes can be cached todo)
                    graphics.Projection = Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, 0, frameBuffer.Height, -1, 1);
                    //Now finally actually draw everything we have batched
                    graphics.EndDraw();

                    //OpenGL wants to be hot shit and do asynchronous stuff, we have to ensure all commands finish before capturing the off-screen rendered image
                    GL.Instance.Finish();

                    unsafe
                    {
                        //Pin the pixelBuffer
                        fixed (void* framePtr = pixelBuffer)
                        {
                            //Read the pixels into the pixelBuffer
                            GL.Instance.ReadPixels(0, 0, (uint)OUTPUT_WIDTH, (uint)OUTPUT_HEIGHT,
                                Silk.NET.OpenGLES.PixelFormat.Rgb, Silk.NET.OpenGLES.PixelType.UnsignedByte, framePtr);
                        }

                        //Add the video frame
                        videoFile.Video.AddFrame(new FFMediaToolkit.Graphics.ImageData(
                                new Span<byte>(pixelBuffer),
                                FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24,
                                new System.Drawing.Size(OUTPUT_WIDTH, OUTPUT_HEIGHT)));
                    }
                }

                //When we are done, dispose the video file, which then finializes it.
                videoFile.Dispose();

                //Send file asynchronously
                sendFileThenDelete(message, outputFilename);
            }
        }

        private async void sendFileThenDelete(SocketMessage message, string path)
        {
            try
            {
                await message.Channel.SendFileAsync(new FileAttachment(path, "osu.mp4", null, false));
                File.Delete(path);
            }catch (Exception ex)
            {
                Utils.Log($"Something happend while trying to send the video file\n{ex.Message}", Easy2D.LogLevel.Error);
            }
        }

        public override void OnResize(int width, int height)
        {

        }

        public override void OnUpdate()
        {

        }
    }
}