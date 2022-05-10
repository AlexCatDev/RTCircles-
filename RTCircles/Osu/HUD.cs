using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class HUD : Drawable
    {
        public class Firework : Drawable
        {
            public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

            private Vector2 pos;
            private Vector2 size = new Vector2(8,8);
            private SmoothFloat progress = new SmoothFloat();
            private Vector2 velocity;

            public Firework(Vector2 pos, float speed = 500f)
            {
                float theta = RNG.Next(0, MathF.PI * 2);

                float scale = RNG.Next(0.5f, 1f);

                velocity.X = MathF.Cos(theta) * speed * scale;
                velocity.Y = MathF.Sin(theta) * speed * scale;

                progress.Value = 1;
                progress.Wait(0.2f);
                progress.TransformTo(0f, 0.5f, EasingTypes.OutQuint);
                
                this.pos = pos;
            }

            public override void Render(Graphics g)
            {
                Vector4 color = new Vector4(1f, 1f, 1f, progress.Value);
                g.DrawRectangleCentered(pos, size, color, Texture.WhiteCircle);
            }

            public override void Update(float delta)
            {
                pos += velocity * delta * progress.Value;
                progress.Update(delta);

                if (Bounds.IntersectsWith(new Rectangle(0, 0, MainGame.WindowWidth, MainGame.WindowHeight)) == false || progress.HasCompleted)
                    IsDead = true;
            }
        }

        class HitURJudgement : Drawable
        {
            Vector2 pos;
            HitResult result;

            Vector4 color;

            Vector2 size = new Vector2(4, 60) * MainGame.Scale;

            public HitURJudgement(Vector2 pos, HitResult result)
            {
                this.pos = pos;
                this.result = result;

                switch (result)
                {
                    case HitResult.Max:
                        color = Colors.From255RGBA(54, 187, 230, 255);
                        break;
                    case HitResult.Good:
                        color = Colors.From255RGBA(92, 226, 22, 255);
                        break;
                    case HitResult.Meh:
                        color = Colors.From255RGBA(216, 175, 73, 255);
                        break;
                    case HitResult.Miss:
                        color = new Vector4(1f, 0f, 0f, 1f);
                        break;
                    default:
                        break;
                }

                color.Xyz *= 1.2f;
            }

            public override Rectangle Bounds => throw new NotImplementedException();

            public override void Render(Graphics g)
            {
                g.DrawRectangleCentered(pos - size / 4f, size, color);
            }

            public override void Update(float delta)
            {
                color.W -= delta * 0.3f;
                color.W = MathUtils.Clamp(color.W, 0, 1f);

                if (color.W == 0)
                    IsDead = true;
            }
        }

        public class HitPositionsContainer : DrawableContainer
        {
            public static Vector2 Position => new Vector2(MainGame.WindowWidth, MainGame.WindowHeight) - Size / 2 - new Vector2(10 * MainGame.Scale);

            public static Vector2 Size => new Vector2(175) * MainGame.Scale;

            public class HitPositionDrawable : Drawable
            {
                private static readonly Texture HitMarkerTexture = new Texture(Utils.GetResource("UI.Assets.hitmarker.png"));

                private Vector2 position;
                private Vector4 color;
                private SmoothFloat hitAnimation = new SmoothFloat() { Value = 0 };
                private SmoothFloat hitFadeout = new SmoothFloat() { Value = 1f };

                public HitPositionDrawable(Vector2 position, HitResult hitResult)
                {
                    this.position  = position;

                    switch (hitResult)
                    {
                        case HitResult.Max:
                            color = Colors.From255RGBA(54, 187, 230, 255);
                            break;
                        case HitResult.Good:
                            color = Colors.From255RGBA(92, 226, 22, 255);
                            break;
                        case HitResult.Meh:
                            color = Colors.From255RGBA(216, 175, 73, 255);
                            break;
                        case HitResult.Miss:
                            color = new Vector4(1f, 0f, 0f, 1f);
                            break;
                        default:
                            break;
                    }

                    hitAnimation.TransformTo(1f, 0.24f, EasingTypes.OutBack, () =>
                    {
                        hitFadeout.TransformTo(0, 1f);
                    });
                }

                public override void Render(Graphics g)
                {
                    color.W = hitFadeout.Value;
                    g.DrawRectangleCentered(HitPositionsContainer.Position + position, Size / 4f * hitAnimation.Value, color, HitMarkerTexture);
                }

                public override void Update(float delta)
                {
                    hitAnimation.Update(delta);
                    hitFadeout.Update(delta);

                    if (hitFadeout.Value == 0f)
                        IsDead = true;
                }
            }

            public override void Render(Graphics g)
            {
                g.DrawRectangleCentered(Position, Size, new Vector4(1f, 1f, 1f, 0.2f), Texture.WhiteFlatCircle);
                g.DrawEllipse(Position, 0, 360, Size.Y / 2, Size.Y / 2 * 0.9f, new Vector4(1f), Texture.WhiteFlatCircle, 50, false);
                base.Render(g);
            }
        }

        private HitPositionsContainer hitPositions = new HitPositionsContainer();
        public HUD()
        {
            OsuContainer.OnHitObjectHit += (pos, result) => hitPositions.Add(
                new HitPositionsContainer.HitPositionDrawable(pos, result));
        }

        private float scale = 1f;
        private float scaleTime = 0.2f;

        private float scale2 = 0;
        private float scaleTime2 = 0.4f;

        private float unstableRateBarWidth => (float)OsuContainer.Beatmap.Window50 * 4f * MainGame.Scale;
        private float unstableRateBarHeight => 15 * MainGame.Scale;

        private float hp = 1;

        private Rectangle unstableRateBar => new Rectangle(new Vector2(MainGame.WindowCenter.X - unstableRateBarWidth / 2f, 30 * MainGame.Scale), new Vector2(unstableRateBarWidth, unstableRateBarHeight));

        public override Rectangle Bounds => throw new NotImplementedException();

        private List<HitURJudgement> hitURJudgements = new List<HitURJudgement>();

        public void AddHit(float time, HitResult result, Vector2 position, bool displayJudgement = true)
        {
            if (result != HitResult.Miss)
            {
                //Watafuk ppy :<
                //The Difficulty multiplier equals the old star rating. It can be calculated via the following formula:
                //difficultyMultiplier = Round((HP Drain + Circle Size + Overall Difficulty + Clamp(Hit object count / Drain time in seconds * 8, 0, 16)) / 38 * 5)    

                double difficultyMultiplier = 1.0;
                
                double modMultiplier = 1.0;

                //LMAOOOOOOOO
                OsuContainer.Score += (uint)result + (uint)(((double)result) * (OsuContainer.Combo * difficultyMultiplier * modMultiplier / 25.0));

                OsuContainer.Combo++;

                if (OsuContainer.Combo > OsuContainer.MaxCombo)
                    OsuContainer.MaxCombo = OsuContainer.Combo;

                scaleTime = 0f;
                scaleTime2 = 0;
            }

            
            if(displayJudgement && result != HitResult.Max)
                ScreenManager.GetScreen<OsuScreen>().Add(new HitJudgement(position, result));

            //Console.WriteLine(result);
            //Map the whole width where 0 will be the center because its dead on time
            float x = MathUtils.Map(time, (float)OsuContainer.Beatmap.Window50, -(float)OsuContainer.Beatmap.Window50, unstableRateBar.Left, unstableRateBar.Right);

            hitURJudgements.Add(new HitURJudgement(new Vector2(x, unstableRateBar.Center.Y), result));

            switch (result)
            {
                case HitResult.Max:
                    OsuContainer.Count300++;
                    AddHP(0.125f);
                    break;
                case HitResult.Good:
                    OsuContainer.Count100++;
                    AddHP(0.025f);
                    break;
                case HitResult.Meh:
                    OsuContainer.Count50++;
                    break;
                case HitResult.Miss:
                    if (OsuContainer.Combo > 20 && OsuContainer.MuteHitsounds == false)
                        Skin.ComboBreak.Play(true);

                    OsuContainer.Combo = 0;
                    OsuContainer.CountMiss++;

                    float dmg = OsuContainer.Beatmap.HP.Map(0, 10, -0.05f, -0.25f);

                    AddHP(dmg);
                    break;
                default:
                    break;
            }
        }

        public void AddHP(float val)
        {
            hp += val;

            hp.ClampRef(0, 1);

            if (hp == 0)
                ScreenManager.GetScreen<OsuScreen>().reportDeath();
        }

        private void drawComboText(Graphics g)
        {
            string comboText = $"{OsuContainer.Combo}x";

            float comboScale = 80 * MainGame.Scale;

            Vector4 beatFlash = new Vector4(0.69f, 0.69f, 0.69f, 0f) * (float)OsuContainer.BeatProgressKiai;

            Vector2 comboSize = Skin.ComboNumbers.Meassure(comboScale * scale2, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(0, MainGame.WindowHeight - comboSize.Y), comboScale * scale2, new Vector4(1f, 1f, 1f, 0.7f) + beatFlash, comboText);

            comboSize = Skin.ComboNumbers.Meassure(comboScale * scale, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(0, MainGame.WindowHeight - comboSize.Y), comboScale * scale, Colors.White + beatFlash, comboText);
        }
        
        private void drawURBar(Graphics g)
        {
            //draw ur bar
            float width50 = (float)MathUtils.Map(OsuContainer.Beatmap.Window50, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);
            float width100 = (float)MathUtils.Map(OsuContainer.Beatmap.Window100, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);
            float width300 = (float)MathUtils.Map(OsuContainer.Beatmap.Window300, 0, OsuContainer.Beatmap.Window50, 0, unstableRateBar.Width);

            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width50, unstableRateBar.Height), Colors.From255RGBA(214, 175, 70, 255));
            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width100, unstableRateBar.Height), Colors.From255RGBA(88, 226, 16, 255));
            g.DrawRectangleCentered(unstableRateBar.Position + new Vector2(unstableRateBar.Width / 2f, 0), new Vector2(width300, unstableRateBar.Height), Colors.From255RGBA(51, 190, 223, 255));

            for (int i = 0; i < hitURJudgements.Count; i++)
            {
                hitURJudgements[i].Update((float)MainGame.Instance.DeltaTime);
                hitURJudgements[i].Render(g);
            }

            for (int i = hitURJudgements.Count - 1; i >= 0; i--)
            {
                if (hitURJudgements[i].IsDead)
                    hitURJudgements.RemoveAt(i);
            }
        }

        private double rollingScore;
        private double rollingAcc;
        public override void Render(Graphics g)
        {
            drawURBar(g);

            drawComboText(g);

            float scoreSizeScale = 66 * MainGame.Scale;

            string scoreText = $"{((int)Math.Round(rollingScore, 0, MidpointRounding.AwayFromZero)).ToString("00000000.##")}";
            Vector2 scoreSize = Skin.ScoreNumbers.Meassure(scoreSizeScale, scoreText);

            Skin.ScoreNumbers.Draw(g, new Vector2(MainGame.WindowWidth - scoreSize.X, 0), scoreSizeScale, Colors.White, scoreText);

            float accSizeScale = 38 * MainGame.Scale;

            string accText = $"{rollingAcc:F2}%";
            Vector2 accSize = Skin.ScoreNumbers.Meassure(accSizeScale, accText);
            Skin.ScoreNumbers.Draw(g, new Vector2(MainGame.WindowWidth - accSize.X, scoreSize.Y), accSizeScale, Colors.White, accText);

            float endAngle = (float)MathUtils.Map(OsuContainer.SongPosition, 0, OsuContainer.Beatmap.HitObjects.Count == 0 ? 0 : OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime, -90, 270).Clamp(-360, 360);

            var col = Colors.LightGray;

            float radius = 16f * MainGame.Scale;
            Vector2 piePos = new Vector2(MainGame.WindowWidth - accSize.X - radius - 4, scoreSize.Y + radius);

            g.DrawEllipse(piePos, -90, endAngle, radius, 0, col);
            g.DrawRectangleCentered(piePos, new Vector2(radius * 2.6f), Colors.White, Skin.CircularMetre);

            var rankingLetterTex = OsuContainer.CurrentRankingToTexture();

            piePos.X -= radius*2.8f;

            float beatProgress = (float)Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0f, 1f, 0f, 1f, EasingTypes.InOutSine);

            float brightness = 1f + 1f * beatProgress;

            float rankSize = radius * 3.8f;
            g.DrawRectangleCentered(piePos, new Vector2(rankSize * rankingLetterTex.Texture.Size.AspectRatio(), rankSize), new Vector4(brightness, brightness, brightness, 1f), rankingLetterTex.Texture);

            piePos.X -= rankSize * 0.8f;
            rankSize /= 1.4f - 0.25f * beatProgress;

            drawKeyOverlay(g);

            drawHPBar(g);

            if (OsuContainer.CookieziMode)
                g.DrawString("Auto", Font.DefaultFont, new Vector2(10) * MainGame.Scale, new Vector4(1f, 1f, 0f, (float)Math.Cos(MainGame.Instance.TotalTime * 1.6).Map(-1, 1, 0.25f, 0.5f)), 1f * MainGame.Scale);

            drawCountDown(g);

            hitPositions.Render(g);
        }

        private void drawCountDown(Graphics g)
        {
            if (OsuContainer.Beatmap == null || OsuContainer.Beatmap.HitObjects.Count == 0 || OsuContainer.CurrentBeatTimingPoint == null)
                return;

            double beat = OsuContainer.GetBeatCountFrom(OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime, 0.5);

            float functionLol(float from, float to, float pause)
            {
                if (beat >= from && beat <= to)
                    return (float)beat.Map(from, to, 0, 1).Clamp(0, 1);

                if (beat >= to && beat <= to + pause)
                    return 1;

                if (beat >= to + pause)
                    return (float)beat.Map(to + pause, to + pause + (to - from), 1, 0).Clamp(0, 1);

                return 0;
            }

            float textScale = MainGame.Scale * 2;

            /*
            float letterReadyAlpha = (float)beat.Map(-4, -3, 1, 0).Clamp(0, 1);

            float letter3Alpha = functionLol(-5, -4.5f, 0.5f);
            float letter3Scale = (float)Interpolation.ValueAt(beat.Clamp(-5, -4.75), 1.5, 1, -5, -4.75, EasingTypes.Out);

            float letter2Alpha = functionLol(-4, -3.5f, 0.5f);
            float letter2Scale = (float)Interpolation.ValueAt(beat.Clamp(-4, -3.75), 1.5, 1, -4, -3.75, EasingTypes.Out);

            float letter1Alpha = functionLol(-3, -2.5f, 0.5f);
            float letter1Scale = (float)Interpolation.ValueAt(beat.Clamp(-3, -2.75), 1.5, 1, -3, -2.75, EasingTypes.Out);
            */

            float letterGOAlpha = functionLol(-2, -1.5f, 0.5f);
            float letterGOScale = (float)Interpolation.ValueAt(beat.Clamp(-2, -1.5), 1.5, 1, -2, -1.5, EasingTypes.Out);

            if(letterGOAlpha > 0)
            g.DrawStringCentered("GO!", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letterGOAlpha), letterGOScale * textScale);

            //g.DrawStringCentered("Ready?", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letterReadyAlpha), textScale);

            //g.DrawStringCentered("3", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letter3Alpha), letter3Scale * textScale);
            //g.DrawStringCentered("2", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letter2Alpha), letter2Scale * textScale);
            //g.DrawStringCentered("1", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letter1Alpha), letter1Scale * textScale);
        }

        private float interpolatedHP = 0;
        private void drawHPBar(Graphics g)
        {
            if (!ScreenManager.GetScreen<OsuScreen>().IsCurrentlyBreakTime && OsuContainer.Beatmap != null)
            {
                AddHP((-0.25f * (float)OsuContainer.DeltaSongPosition / 1000));
            }

            Vector2 pos = new Vector2(20) * MainGame.Scale;

            interpolatedHP = MathHelper.Lerp(interpolatedHP, hp, (float)MainGame.Instance.DeltaTime * 30f);

            Vector2 size = new Vector2(650 * interpolatedHP, 32) * MainGame.Scale;
            Rectangle uv = new Rectangle(0, 0, interpolatedHP, 0);
            g.DrawRectangle(new Vector2(0, 12 * MainGame.Scale), size, new Vector4(new Vector3(0.9f), 1), Texture.WhiteSquare, uv, true);
            //g.DrawRectangle(new Vector2(10) * MainGame.Scale, new Vector2(400, 32) * MainGame.Scale, Colors.Red);
        }

        private Vector2 key1Size = new Vector2(50);
        private Vector4 key1Color = Colors.White;

        private Vector2 key2Size = new Vector2(50);
        private Vector4 key2Color = Colors.White;
        public void drawKeyOverlay(Graphics g)
        {
            Vector2 key1Position = new Vector2(MainGame.WindowWidth - key1Size.X, MainGame.WindowHeight / 2f);
            float padding = 4f * MainGame.Scale;
            Vector2 key2Postion = new Vector2(MainGame.WindowWidth - key2Size.X, MainGame.WindowHeight / 2f + key1Size.Y + padding);
            float fontScale = 0.5f * MainGame.Scale;

            string key1Text = OsuContainer.Key1.ToString();
            string key2Text = OsuContainer.Key2.ToString();

            g.DrawRectangle(key1Position, key1Size, key1Color);

            Vector2 textSize = Font.DefaultFont.MessureString(key1Text, fontScale);
            g.DrawString(key1Text, Font.DefaultFont, key1Position + key1Size / 2f - textSize / 2f, Colors.Black, fontScale);

            g.DrawRectangle(key2Postion, key2Size, key2Color);

            textSize = Font.DefaultFont.MessureString(key2Text, fontScale);
            g.DrawString(key2Text, Font.DefaultFont, key2Postion + key2Size / 2f - textSize / 2f, Colors.Black, fontScale);

            //g.DrawString(OsuContainer.GetCurrentRankingLetter(), Font.DefaultFont, Easy2D.Game.Input.MousePosition, Colors.White);
        }

        public override void Update(float delta)
        {
            rollingScore = Interpolation.Damp(rollingScore, OsuContainer.Score, 0.05, delta * 10f);
            rollingAcc = Interpolation.Damp(rollingAcc, OsuContainer.Accuracy * 100, 0.05, delta * 10f);

            scaleTime += delta;
            scaleTime = scaleTime.Clamp(0f, 0.2f);

            scale = Interpolation.ValueAt(scaleTime, 1.8f, 1.5f, 0, 0.2f, EasingTypes.OutQuart);

            scaleTime2 += delta;
            scaleTime2 = scaleTime2.Clamp(0f, 0.4f);

            scale2 = Interpolation.ValueAt(scaleTime2, 2.7f, 1.5f, 0, 0.4f, EasingTypes.OutQuart);

            float pressSize = 50f * MainGame.Scale;
            float size = 65f * MainGame.Scale;
            const float lerpSpeed = 32f;

            if (OsuContainer.Key1Down)
            {
                key1Size = Vector2.Lerp(key1Size, new Vector2(pressSize), delta * lerpSpeed);
                key1Color = Vector4.Lerp(key1Color, Colors.Pink, delta * lerpSpeed);
            }
            else
            {
                key1Size = Vector2.Lerp(key1Size, new Vector2(size), delta * lerpSpeed);
                key1Color = Vector4.Lerp(key1Color, Colors.White, delta * lerpSpeed);
            }

            if (OsuContainer.Key2Down)
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(pressSize), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.Pink, delta * lerpSpeed);
            }
            else
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(size), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.White, delta * lerpSpeed);
            }

            hitPositions.Update(delta);
        }
    }
}
