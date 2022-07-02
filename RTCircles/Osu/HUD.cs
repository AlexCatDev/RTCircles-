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

        public class HitPositionsContainer : DrawableContainer
        {
            public static Vector2 Position => new Vector2(MainGame.WindowWidth - Size.X/2 - 5, 200*MainGame.Scale);

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
                    g.DrawRectangleCentered(HitPositionsContainer.Position + position, Size / 6f * hitAnimation.Value, color, HitMarkerTexture);
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
                return;

                g.DrawRectangleCentered(Position, Size, new Vector4(0.7f, 0.7f, 0.7f, 0.2f), Texture.WhiteFlatCircle);
                g.DrawEllipse(Position, 0, 360, Size.Y / 2, Size.Y / 2 * 0.9f, new Vector4(0.9f, 0.9f, 0.9f, 1f), Texture.WhiteFlatCircle, 50, false);
                base.Render(g);
            }
        }

        private HitPositionsContainer hitPositions = new HitPositionsContainer();
        public HUD()
        {
            /*
            OsuContainer.OnHitObjectHit += (pos, result) => hitPositions.Add(
                new HitPositionsContainer.HitPositionDrawable(pos, result));
            */
        }

        private Vector3 colorFromHitResult(HitResult result)
        {
            switch (result)
            {
                case HitResult.Max:
                    return Colors.From255RGB(54, 187, 230);
                case HitResult.Good:
                    return Colors.From255RGB(92, 226, 22);
                case HitResult.Meh:
                    return Colors.From255RGB(216, 175, 73);
                case HitResult.Miss:
                    return new Vector3(1f, 0f, 0f);
                default:
                    return Vector3.One;
            }
        }

        private float comboScale = 1f;
        private float comboScaleTime = 0.2f;

        private float comboScale2 = 0;
        private float comboScaleTime2 = 0.4f;

        private float healthbarMarkerScale = 1;

        private float unstableRateBarWidth => (float)OsuContainer.Beatmap.Window50 * 4.4f * MainGame.Scale;
        private float unstableRateBarHeight => 14 * MainGame.Scale;

        private float currentHealth = 1;

        private Rectangle unstableRateBar => new Rectangle(new Vector2(MainGame.WindowCenter.X - unstableRateBarWidth / 2f, 1052 * MainGame.AbsoluteScale.Y), new Vector2(unstableRateBarWidth, unstableRateBarHeight));

        public override Rectangle Bounds => throw new NotImplementedException();

        private List<(double hitTime, double songTime, Vector3 color)> URJudgements = new List<(double hitTime, double songTime, Vector3 color)>();

        public void AddHit(double time, HitResult result, Vector2 position, bool displayJudgement = true)
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

                comboScaleTime = 0f;
                comboScaleTime2 = 0;

                if (GlobalOptions.EnableComboBursts.Value && Skin.ComboBurst is not null && OsuContainer.Combo % 50 == 0 && OsuContainer.Combo > 0)
                    ScreenManager.GetScreen<OsuScreen>().Add(new ComboBurst());
            }
            else
            {
                healthbarMarkerScale = 1;
            }


            if (displayJudgement && result != HitResult.Max)
                ScreenManager.GetScreen<OsuScreen>().Add(new HitJudgement(position, result));

            URJudgements.Add((hitTime: time, songTime: OsuContainer.SongPosition, colorFromHitResult(result)));

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

            if (currentHealth == 0)
                ScreenManager.GetScreen<OsuScreen>().reportDeath();
        }

        public void AddHP(float val)
        {
            if(val > 0)
                healthbarMarkerScale = 2;

            currentHealth += val;
            currentHealth.ClampRef(0, 1);
        }

        private void drawComboText(Graphics g)
        {
            string comboText = $"{OsuContainer.Combo}x";

            float comboScale = 70 * MainGame.Scale;

            float margin = 2;

            Vector2 comboSize = Skin.ComboNumbers.Meassure(comboScale * comboScale2, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(margin, MainGame.WindowHeight - comboSize.Y - margin), comboScale * comboScale2, new Vector4(1f, 1f, 1f, 0.7f), comboText);

            comboSize = Skin.ComboNumbers.Meassure(comboScale * this.comboScale, comboText);
            Skin.ComboNumbers.Draw(g, new Vector2(margin, MainGame.WindowHeight - comboSize.Y - margin), comboScale * this.comboScale, Colors.White, comboText);
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

            Vector2 urSize = new Vector2(4*MainGame.Scale, unstableRateBarHeight*4.5f);
            const double FadeOutDuration = 1000;
            const double FadeOutDelay = 2000;

            var fadeOutTime = OsuContainer.SongPosition - FadeOutDelay;
            
            URJudgements.RemoveAll((o) => (o.songTime > OsuContainer.SongPosition) || (fadeOutTime > o.songTime + FadeOutDuration));

            for (int i = URJudgements.Count - 1; i >= 0; i--)
            {
                float x = (float)URJudgements[i].hitTime.Map(OsuContainer.Beatmap.Window50, -OsuContainer.Beatmap.Window50, unstableRateBar.Left, unstableRateBar.Right);

                var fadeOutStart = URJudgements[i].songTime;

                float alpha = (float)Interpolation.ValueAt(fadeOutTime, 0.3, 0, fadeOutStart, fadeOutStart + FadeOutDuration, EasingTypes.None).Clamp(0, 0.3);
                //Console.WriteLine(URJudgements.Count);
                g.DrawRectangleCentered(new Vector2(x, unstableRateBar.Center.Y), urSize, new Vector4(URJudgements[i].color * 2, alpha));
            }
        }

        public override void Render(Graphics g)
        {
            drawPlayfieldBorder(g);

            drawComboText(g);

            drawScoreAccTime(g);

            drawHPBar(g);

            drawKeyOverlay(g);

            drawURBar(g);

            if (OsuContainer.CookieziMode && ScreenManager.ActiveScreen is OsuScreen)
                g.DrawStringCentered("Auto Play", ResultScreen.Font, new Vector2(MainGame.WindowCenter.X, 60*MainGame.Scale), new Vector4(0.6f, 0.6f, 0.6f, (float)Math.Cos(MainGame.Instance.TotalTime * 2).Map(-1, 1, 0.7f, 1f)), 1f * MainGame.Scale);

            drawCountDown(g);

            if(ScreenManager.ActiveScreen is OsuScreen)
            hitPositions.Render(g);
        }

        public bool RenderPlayfieldBorder = false;
        private void drawPlayfieldBorder(Graphics g)
        {
            if (!RenderPlayfieldBorder)
                return;

            Vector4 color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
            float lineThickness = 2f;

            g.DrawOneSidedLine(OsuContainer.FullPlayfield.TopLeft, OsuContainer.FullPlayfield.BottomLeft, color, color, lineThickness);
            g.DrawOneSidedLine(OsuContainer.FullPlayfield.BottomLeft, OsuContainer.FullPlayfield.BottomRight, color, color, lineThickness);
            g.DrawOneSidedLine(OsuContainer.FullPlayfield.BottomRight, OsuContainer.FullPlayfield.TopRight, color, color, lineThickness);
            g.DrawOneSidedLine(OsuContainer.FullPlayfield.TopRight, OsuContainer.FullPlayfield.TopLeft, color, color, lineThickness);
        }

        private double rollingScore;
        private double rollingAcc;

        private float rankScale = 1;
        private float rankAlpha = 1;
        private void drawScoreAccTime(Graphics g)
        {
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

            piePos.X -= radius * 2.8f;

            float beatProgress = (float)Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0f, 1f, 0f, 1f, EasingTypes.InOutSine);

            if (OsuContainer.IsKiaiTimeActive)
            {
                rankScale = (float)Interpolation.Damp(rankScale, 1, 0.001, MainGame.Instance.DeltaTime);
                rankAlpha = (float)Interpolation.Damp(rankAlpha, 1, 0.001, MainGame.Instance.DeltaTime);
            }
            else
            {
                rankScale = (float)Interpolation.Damp(rankScale, 0.75, 0.02, MainGame.Instance.DeltaTime);
                rankAlpha = (float)Interpolation.Damp(rankAlpha, 0.5, 0.02, MainGame.Instance.DeltaTime);
            }

            float rankSize = radius * 3.8f * rankScale;
            g.DrawRectangleCentered(piePos, new Vector2(rankSize * rankingLetterTex.Texture.Size.AspectRatio(), rankSize), new Vector4(1, 1, 1, rankAlpha), rankingLetterTex.Texture);
        }

        private void drawCountDown(Graphics g)
        {
            if (OsuContainer.Beatmap == null || OsuContainer.Beatmap.HitObjects.Count == 0 || OsuContainer.CurrentBeatTimingPoint == null)
                return;

            double beatLength = OsuContainer.CurrentBeatTimingPoint.BeatLength;

            //When this is 0 it will be 2 beats behind the first object
            double offsetFromStart = OsuContainer.SongPosition - OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime + beatLength * 2;

            //Console.WriteLine(offsetFromStart);

            double duration = 200;
            double pause = beatLength * 2;

            float textScale = MainGame.Scale;

            float letterGOAlpha = (float)(offsetFromStart > duration ? 
                offsetFromStart.Map(duration + pause, duration + pause + duration, 1, 0) : offsetFromStart.Map(0, duration, 0, 1)).Clamp(0, 1);

            float letterGOScale = (float)Interpolation.ValueAt(offsetFromStart.Clamp(0, duration), 2, 1, 0, duration, EasingTypes.Out);

            if (letterGOAlpha > 0)
                g.DrawStringCentered("GO!", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 1f, letterGOAlpha), letterGOScale * textScale);
        }

        private float interpolatedHP = 0;
        private void drawHPBar(Graphics g)
        {
            if (!ScreenManager.GetScreen<OsuScreen>().IsCurrentlyBreakTime && OsuContainer.Beatmap != null)
            {
                float drainRate = OsuContainer.Beatmap.HP.Map(0, 10, -0.1f, -0.4f);

                AddHP(drainRate * (float)(OsuContainer.DeltaSongPosition / 1000));
            }

            interpolatedHP = MathHelper.Lerp(interpolatedHP, currentHealth, (float)MainGame.Instance.DeltaTime * 30f);

            healthbarMarkerScale = (float)Interpolation.Damp(healthbarMarkerScale, 1, 0.35, MainGame.Instance.DeltaTime * 10);

            float hpScale = MainGame.Scale * 1.405f;

            if (Skin.HealthBar_BG != null)
            {
                var size = Skin.HealthBar_BG.Texture.Size;

                if (Skin.HealthBar_BG.IsX2)
                    size *= 0.5f;

                g.DrawRectangle(Vector2.Zero, size * hpScale, Colors.White, Skin.HealthBar_BG);
            }

            Vector2 fillOffset = Skin.HealthBar_Marker == null ? new Vector2(5, 16) : new Vector2(12, 12);
            Vector4 healthColor = new Vector4(1, 1, 1, 1);

            if (PostProcessing.Bloom)
                healthColor.Xyz += new Vector3(healthbarMarkerScale.Map(1, 2, 0, 0.5f));

            if (Skin.HealthBar_Fill != null)
            {
                Rectangle healthUV = new Rectangle(0, 0, interpolatedHP, 1);
                Vector2 healthSize = new Vector2(Skin.HealthBar_Fill.Texture.Size.X * interpolatedHP,
                    Skin.HealthBar_Fill.Texture.Size.Y) * hpScale;

                if (Skin.HealthBar_Fill.IsX2)
                    healthSize *= 0.5f;

                var healthFillPos = fillOffset * hpScale;

                g.DrawRectangle(healthFillPos, healthSize, healthColor, Skin.HealthBar_Fill, healthUV, true);

                if (Skin.HealthBar_Marker != null)
                {
                    Vector2 markerSize = Skin.HealthBar_Marker.Texture.Size * hpScale;

                    if (Skin.HealthBar_Marker.IsX2)
                        markerSize *= 0.5f;

                    g.DrawRectangleCentered(new Vector2(healthFillPos.X + healthSize.X, 16 * hpScale), markerSize * healthbarMarkerScale, healthColor, Skin.HealthBar_Marker);
                }
            }
            else
            {
                g.DrawRectangle(fillOffset * MainGame.Scale, new Vector2(500 * interpolatedHP, 32) * MainGame.Scale, new Vector4(0.85f, 0.85f, 0.85f, 1));
            }
        }

        private Vector2 key1Size = new Vector2(50);
        private Vector4 key1Color = (Vector4)Colors.LightGray;

        private Vector2 key2Size = new Vector2(50);
        private Vector4 key2Color = (Vector4)Colors.LightGray;
        public void drawKeyOverlay(Graphics g)
        {
            if (OsuContainer.CookieziMode)
                return;

            Vector2 key1Position = new Vector2(MainGame.WindowWidth - key1Size.X, MainGame.WindowHeight / 2.2f);
            float padding = 5f * MainGame.Scale;
            Vector2 key2Postion = new Vector2(MainGame.WindowWidth - key2Size.X, MainGame.WindowHeight / 2.2f + key1Size.Y + padding);
            float fontScale = 0.45f * MainGame.Scale;

            string key1Text = OsuContainer.Key1.ToString();
            string key2Text = OsuContainer.Key2.ToString();

            g.DrawRectangle(key1Position, key1Size, key1Color, Texture.WhiteFlatCircle);

            Vector2 textSize = Font.DefaultFont.MessureString(key1Text, fontScale);
            g.DrawString(key1Text, Font.DefaultFont, key1Position + key1Size / 2f - textSize / 2f, Colors.White, fontScale);

            g.DrawRectangle(key2Postion, key2Size, key2Color, Texture.WhiteFlatCircle);

            textSize = Font.DefaultFont.MessureString(key2Text, fontScale);
            g.DrawString(key2Text, Font.DefaultFont, key2Postion + key2Size / 2f - textSize / 2f, Colors.White, fontScale);

            //g.DrawString(OsuContainer.GetCurrentRankingLetter(), Font.DefaultFont, Easy2D.Game.Input.MousePosition, Colors.White);
        }

        public override void Update(float delta)
        {
            rollingScore = Interpolation.Damp(rollingScore, OsuContainer.Score, 0.05, delta * 10f);
            rollingAcc = Interpolation.Damp(rollingAcc, OsuContainer.Accuracy * 100, 0.05, delta * 10f);

            comboScaleTime += delta;
            comboScaleTime = comboScaleTime.Clamp(0f, 0.2f);

            comboScale = Interpolation.ValueAt(comboScaleTime, 1.8f, 1.5f, 0, 0.2f, EasingTypes.OutQuart);

            comboScaleTime2 += delta;
            comboScaleTime2 = comboScaleTime2.Clamp(0f, 0.4f);

            comboScale2 = Interpolation.ValueAt(comboScaleTime2, 2.7f, 1.5f, 0, 0.4f, EasingTypes.OutQuart);

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
                key1Color = Vector4.Lerp(key1Color, Colors.Black, delta * lerpSpeed);
            }

            if (OsuContainer.Key2Down)
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(pressSize), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.Pink, delta * lerpSpeed);
            }
            else
            {
                key2Size = Vector2.Lerp(key2Size, new Vector2(size), delta * lerpSpeed);
                key2Color = Vector4.Lerp(key2Color, Colors.Black, delta * lerpSpeed);
            }

            hitPositions.Update(delta);
        }
    }
}
