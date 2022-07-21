using Silk.NET.OpenGLES;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Easy2D
{
    /// <summary>
    /// Draw stuff
    /// TODO: fix shitty texture bind logic
    ///</summary>
    public class Graphics
    {
        private static readonly Dictionary<string, string> fragmentPreprocessor = new Dictionary<string, string>();
        private static readonly int[] textureSlots;

        private static int MaxTextureSlots;

        static Graphics()
        {
            MaxTextureSlots = GL.MaxTextureSlots;
            /*
            if (MaxTextureSlots == 0)
                MaxTextureSlots = 8;

            MaxTextureSlots = 16;
            */
            Utils.Log($"Max Available Texture Slots: {MaxTextureSlots}", LogLevel.Important);

            textureSlots = new int[MaxTextureSlots];

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("switch(v_TextureSlot) {");
            for (int i = 0; i < MaxTextureSlots; i++)
            {
                textureSlots[i] = i;

                sb.AppendLine($"case {i}: texColor = texture(u_Textures[{i}], v_TexCoordinate); break;");
            }
            sb.AppendLine("}");

            fragmentPreprocessor = new()
            {
                { "//#SWITCH", sb.ToString() },
                { "//#uniform sampler2D u_Textures[];", $"uniform sampler2D u_Textures[{MaxTextureSlots}];" }
            };
        }

        private int bindTextureIndex = 0;

        private Dictionary<Texture, int> texturesToBind = new Dictionary<Texture, int>();

        public Matrix4 Projection;

        public readonly Shader Shader = new Shader();
        public readonly PrimitiveBatch<Vertex> VertexBatch;

        public ulong VerticesDrawn { get; private set; }
        public ulong IndicesDrawn { get; private set; }

        public ulong TrianglesDrawn { get; private set; }

        public ulong DrawCalls { get; private set; }

        public ulong TexturesBound { get; private set; }

        public void ResetStatistics()
        {
            VerticesDrawn = 0;
            IndicesDrawn = 0;
            TrianglesDrawn = 0;
            DrawCalls = 0;
            TexturesBound = 0;
        }

        public Graphics(int vertexCount = 40_000, int indexCount = 60_000)
        {
            VertexBatch = new PrimitiveBatch<Vertex>(vertexCount, indexCount);

            Shader.AttachShader(ShaderType.VertexShader, Utils.GetInternalResource("Shaders.Default.vert"));
            Shader.AttachShader(ShaderType.FragmentShader, Utils.GetInternalResource("Shaders.Default.frag"));

            Shader.AttachPreprocessor(ShaderType.FragmentShader, fragmentPreprocessor);
        }

        public void Recompile()
        {
            Shader.Delete();
        }

        public int GetTextureSlot(Texture texture)
        {
            if (texture is null)
                texture = Texture.WhiteSquare;

            if (texturesToBind.TryGetValue(texture, out int slot))
            {
                return slot;
            }
            else
            {
                if (texturesToBind.Count == MaxTextureSlots)
                {
                    EndDraw();
                    Utils.Log($"Renderer flushed because of texture limit: {MaxTextureSlots}", LogLevel.Debug);
                }

                int slotToAdd = bindTextureIndex;
                texturesToBind.Add(texture, slotToAdd);
                bindTextureIndex++;
                return slotToAdd;
            }
        }

        #region Lines
        public void DrawTwoSidedLine(Vector2 startPosition, Vector2 endPosition, Vector4 color, float thickness, Texture texture = null)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topRight = new Vector2(startPosition.X + perpen.X * thickness,
                startPosition.Y + perpen.Y * thickness);

            Vector2 bottomRight = startPosition;

            Vector2 topLeft = new Vector2(endPosition.X + perpen.X * thickness,
                endPosition.Y + perpen.Y * thickness);

            Vector2 bottomLeft = endPosition;

            var quad = VertexBatch.GetQuad();

            int slot = GetTextureSlot(texture);

            quad[0].Color = color;
            quad[0].Position = topRight;
            quad[0].TexCoord = new Vector2(0, 0);
            quad[0].TextureSlot = slot;

            quad[1].Color = color;
            quad[1].Position = bottomRight;
            quad[1].TexCoord = new Vector2(1, 0);
            quad[1].TextureSlot = slot;

            quad[2].Color = color;
            quad[2].Position = bottomLeft;
            quad[2].TexCoord = new Vector2(1, 1);
            quad[2].TextureSlot = slot;

            quad[3].Color = color;
            quad[3].Position = topLeft;
            quad[3].TexCoord = new Vector2(0, 1);
            quad[3].TextureSlot = slot;

            topRight = startPosition;

            bottomRight = new Vector2(startPosition.X - perpen.X * thickness,
                                             startPosition.Y - perpen.Y * thickness);

            topLeft = endPosition;

            bottomLeft = new Vector2(endPosition.X - perpen.X * thickness,
                                             endPosition.Y - perpen.Y * thickness);

            quad = VertexBatch.GetQuad();

            quad[0].Color = color;
            quad[0].Position = topRight;
            quad[0].TexCoord = new Vector2(1, 1);
            quad[0].TextureSlot = slot;

            quad[1].Color = color;
            quad[1].Position = bottomRight;
            quad[1].TexCoord = new Vector2(0, 0);
            quad[1].TextureSlot = slot;

            quad[2].Color = color;
            quad[2].Position = bottomLeft;
            quad[2].TexCoord = new Vector2(0, 1);
            quad[2].TextureSlot = slot;

            quad[3].Color = color;
            quad[3].Position = topLeft;
            quad[3].TexCoord = new Vector2(1, 0);
            quad[3].TextureSlot = slot;
        }

        public void DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color, float thickness, Texture texture = null)
        {
            DrawLine(startPosition, endPosition, color, color, thickness, texture);
        }

        public void DrawOneSidedLine(Vector2 startPosition, Vector2 endPosition, Vector4 color1, Vector4 color2, float thickness, Texture texture = null, Rectangle? textureRect = null)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topLeft = new Vector2(endPosition.X + perpen.X * thickness,
                endPosition.Y + perpen.Y * thickness);

            Vector2 topRight = endPosition;

            Vector2 bottomLeft = new Vector2(startPosition.X + perpen.X * thickness,
                startPosition.Y + perpen.Y * thickness);

            Vector2 bottomRight = startPosition;

            var quad = VertexBatch.GetQuad();

            int slot = GetTextureSlot(texture);

            quad[0].Rotation = 0;
            quad[0].Color = color1;
            quad[0].Position = topLeft;
            quad[0].TexCoord = textureRect?.TopLeft ?? Vector2.Zero;
            quad[0].TextureSlot = slot;

            quad[1].Rotation = 0;
            quad[1].Color = color1;
            quad[1].Position = topRight;
            quad[1].TexCoord = textureRect?.TopRight ?? Vector2.Zero;
            quad[1].TextureSlot = slot;

            quad[2].Rotation = 0;
            quad[2].Color = color2;
            quad[2].Position = bottomRight;
            quad[2].TexCoord = textureRect?.BottomRight ?? Vector2.Zero;
            quad[2].TextureSlot = slot;

            quad[3].Rotation = 0;
            quad[3].Color = color2;
            quad[3].Position = bottomLeft;
            quad[3].TexCoord = textureRect?.BottomLeft ?? Vector2.Zero;
            quad[3].TextureSlot = slot;
        }

        public void DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color1, Vector4 color2, float thickness, Texture texture = null)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topLeft = new Vector2(startPosition.X + perpen.X * thickness / 2f,
                startPosition.Y + perpen.Y * thickness / 2f);

            Vector2 topRight = new Vector2(startPosition.X - perpen.X * thickness / 2f,
                startPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomLeft = new Vector2(endPosition.X - perpen.X * thickness / 2f,
                endPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomRight = new Vector2(endPosition.X + perpen.X * thickness / 2f,
                endPosition.Y + perpen.Y * thickness / 2f);

            var quad = VertexBatch.GetQuad();

            int slot = GetTextureSlot(texture);

            quad[0].Rotation = 0;
            quad[0].Color = color1;
            quad[0].Position = topLeft;
            quad[0].TexCoord = new Vector2(0, 0);
            quad[0].TextureSlot = slot;

            quad[1].Rotation = 0;
            quad[1].Color = color1;
            quad[1].Position = topRight;
            quad[1].TexCoord = new Vector2(0, 1);
            quad[1].TextureSlot = slot;

            quad[2].Rotation = 0;
            quad[2].Color = color2;
            quad[2].Position = bottomLeft;
            quad[2].TexCoord = new Vector2(1, 1);
            quad[2].TextureSlot = slot;

            quad[3].Rotation = 0;
            quad[3].Color = color2;
            quad[3].Position = bottomRight;
            quad[3].TexCoord = new Vector2(1, 0);
            quad[3].TextureSlot = slot;
        }

        public void DrawDottedLine(Vector2 startPosition, Vector2 endPosition, Texture texture, Vector4 color, Vector2 dotSize, float spacing, bool centeredDots = true, bool alwaysDotEnd = false, Rectangle? bounds = null)
        {
            float angle = MathF.Atan2(endPosition.Y - startPosition.Y, endPosition.X - startPosition.X);

            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            Vector2 step = new Vector2(cos, sin) * spacing;

            float degrees = MathHelper.RadiansToDegrees(angle);

            if (centeredDots == false)
            {
                startPosition += step / 2f;
                endPosition -= step / 2f;
            }

            if (bounds.HasValue && bounds.Value.IntersectsWith(new Rectangle(startPosition, Vector2.Zero)) == false)
                return;

            while (startPosition != endPosition)
            {
                DrawRectangleCentered(startPosition, dotSize, color, texture, rotDegrees: degrees);

                if (step.X < 0)
                    startPosition.X = (startPosition.X + step.X).Clamp(endPosition.X, startPosition.X);
                else
                    startPosition.X = (startPosition.X + step.X).Clamp(startPosition.X, endPosition.X);

                if (step.Y < 0)
                    startPosition.Y = (startPosition.Y + step.Y).Clamp(endPosition.Y, startPosition.Y);
                else
                    startPosition.Y = (startPosition.Y + step.Y).Clamp(startPosition.Y, endPosition.Y);
            }

            if (alwaysDotEnd)
                DrawRectangleCentered(endPosition, dotSize, color, texture, rotDegrees: degrees);
        }
        #endregion

        #region Text
        public void DrawString(string text, Font font, Vector2 position, Vector4 color, float scale = 1f, float? spacing = null)
        {
            Vector2 startPosition = position;

            scale = Math.Max(0, scale);

            float biggestChar = 0;
            float smallestBearing = float.MaxValue;

            foreach (char c in text)
            {
                if (c == '\n')
                    continue;
                else if (c == '\t')
                    continue;
                else if (c == '\r')
                    continue;

                bool isValidCharacter = font.Info.Characters.TryGetValue(c, out SharpFNT.Character character);

                if (isValidCharacter == false)
                    character = font.Info.Characters['?'];

                float height = character.Height * scale;

                float bearing = (character.YOffset * scale);

                smallestBearing = smallestBearing > bearing ? bearing : smallestBearing;

                biggestChar = height > biggestChar ? height : biggestChar;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    position.Y += font.Info.Common.LineHeight * scale;
                    position.X = startPosition.X;
                    continue;
                }
                else if (c == '\t')
                    continue;
                else if (c == '\r')
                    continue;

                bool isValidCharacter = font.Info.Characters.TryGetValue(c, out SharpFNT.Character character);

                if (isValidCharacter == false)
                    character = font.Info.Characters['?'];

                float bearing = (character.YOffset * scale);

                position.Y += bearing - smallestBearing;

                Vector2 size = new Vector2(character.Width, character.Height) * scale;

                DrawRectangle(position, size, color, font.Texture, new Rectangle(character.X, character.Y, character.Width, character.Height), false);

                position.Y -= bearing - smallestBearing;

                if (spacing.HasValue)
                {
                    position.X += spacing.Value * scale;
                    position.X += character.Width * scale;

                    continue;
                }

                position.X += character.XAdvance * scale;

                if (i < text.Length - 1)
                    position.X += font.Info.GetKerningAmount(c, text[i + 1]) * scale;

                position.X -= character.XOffset * scale;
            }
        }

        public void DrawStringCentered(string text, Font font, Vector2 position, Vector4 color, float scale = 1f)
        {
            var size = font.MessureString(text, scale);

            DrawString(text, font, position - size / 2f, color, scale);
        }

        public void DrawStringNoAlign(string text, Font font, Vector2 position, Vector4 color, float scale = 1f)
        {
            scale = Math.Max(0, scale);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                bool valid = font.Info.Characters.TryGetValue(c, out SharpFNT.Character character);

                if (valid == false)
                    character = font.Info.Characters['?'];

                float bearing = character.YOffset * scale;
                position.Y += bearing;

                Vector2 size = new Vector2(character.Width, character.Height) * scale;

                DrawRectangle(position, size, color, font.Texture, new Rectangle(character.X, character.Y, character.Width, character.Height), false);

                position.Y -= bearing;

                position.X += (character.XAdvance) * scale;

                if (i < text.Length - 1)
                    position.X += font.Info.GetKerningAmount(c, text[i + 1]) * scale;

                position.X -= character.XOffset * scale;
            }
        }

        public void DrawClippedString(string text, Font font, Vector2 position, Vector4 color, Rectangle boundingBox, float scale = 1f, bool alignText = true)
        {
            Vector2 startPosition = position;

            scale = Math.Max(0, scale);

            float biggestChar = 0;
            float smallestBearing = float.MaxValue;

            if (alignText)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    if (c == '\n')
                        continue;
                    else if (c == '\t')
                        continue;
                    else if (c == '\r')
                        continue;

                    //3.6%
                    bool isValidCharacter = font.Info.Characters.TryGetValue(c, out SharpFNT.Character character);

                    if (isValidCharacter == false)
                        character = font.Info.Characters['?'];

                    float height = character.Height * scale;

                    float bearing = (character.YOffset * scale);

                    smallestBearing = smallestBearing > bearing ? bearing : smallestBearing;

                    biggestChar = height > biggestChar ? height : biggestChar;
                }
            }
            else
            {
                smallestBearing = 0;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    position.Y += font.Info.Common.LineHeight * scale;
                    position.X = startPosition.X;
                    continue;
                }
                else if (c == '\t')
                    continue;
                else if (c == '\r')
                    continue;

                //3.6%
                bool isValidCharacter = font.Info.Characters.TryGetValue(c, out SharpFNT.Character character);

                if (isValidCharacter == false)
                    character = font.Info.Characters['?'];

                float bearing = (character.YOffset * scale);

                position.Y += bearing - smallestBearing;

                Vector2 size = new Vector2(character.Width, character.Height) * scale;

                Vector2 clipPush = Vector2.Zero;

                Rectangle texRect = new Rectangle(character.X, character.Y, character.Width, character.Height);

                if (position.X + size.X > boundingBox.Right)
                {
                    var diff = position.X + size.X - boundingBox.Right;
                    texRect.Width -= texRect.Width * (diff / size.X);

                    size.X -= diff;
                }
                else if (position.X < boundingBox.Left)
                {
                    var diff = boundingBox.Left - position.X;
                    texRect.X += texRect.Width * (diff / size.X);
                    texRect.Width -= texRect.Width * (diff / size.X);

                    position.X += diff;
                    size.X -= diff;
                    clipPush.X = diff;
                }

                if (position.Y + size.Y > boundingBox.Bottom)
                {
                    var diff = position.Y + size.Y - boundingBox.Bottom;
                    texRect.Height -= texRect.Height * (diff / size.Y);

                    size.Y -= diff;
                }
                else if (position.Y < boundingBox.Top)
                {
                    var diff = boundingBox.Top - position.Y;

                    texRect.Y += texRect.Height * (diff / size.Y);
                    texRect.Height -= texRect.Height * (diff / size.Y);

                    position.Y += diff;
                    size.Y -= diff;
                    clipPush.Y = diff;
                }

                if (size.X < 0)
                    size.X = 0;

                if (size.Y < 0)
                    size.Y = 0;

                //30%
                DrawRectangle(position, size, color, font.Texture, texRect, false);

                position -= clipPush;

                position.Y -= bearing - smallestBearing;

                position.X += character.XAdvance * scale;

                //10%
                if (i < text.Length - 1)
                    position.X += font.Info.GetKerningAmount(c, text[i + 1]) * scale;

                position.X -= character.XOffset * scale;
            }
        }

        #endregion

        public void DrawInFrameBuffer(FrameBuffer frameBuffer, params Action[] drawActions)
        {
            EndDraw();

            var prevProj = Projection;
            var prevViewport = Viewport.CurrentViewport;
            FrameBuffer.DefaultFrameBuffer.TryGetTarget(out FrameBuffer prevTarget);
            FrameBuffer.DefaultFrameBuffer.SetTarget(frameBuffer);

            frameBuffer.Bind();

            var newProj = Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, frameBuffer.Height, 0, -10, 10);

            Projection = newProj;

            Viewport.SetViewport(0, 0, frameBuffer.Width, frameBuffer.Height);

            GL.Instance.Clear(ClearBufferMask.ColorBufferBit);

            foreach (var action in drawActions)
            {
                action?.Invoke();
                EndDraw();
            }

            FrameBuffer.DefaultFrameBuffer.SetTarget(prevTarget);
            frameBuffer.Unbind();

            Projection = prevProj;
            Viewport.SetViewport(prevViewport);
        }

        public void DrawFrameBuffer(Vector2 position, Vector4 color, FrameBuffer frameBuffer)
        {
            DrawRectangle(position, new Vector2(frameBuffer.Width, frameBuffer.Height), color, frameBuffer.Texture,
                new Rectangle(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height), false);
        }


        public void DrawRoundedRect(Vector2 position, Vector2 size, Vector4 color, float cornerRadius)
        {
            size.X -= cornerRadius * 2;
            size.Y -= cornerRadius * 2;

            Rectangle rect = new Rectangle(position - size * 0.5f, size);

            Vector2 cornerSize = new Vector2(cornerRadius) * 1f;

            var cornerTex = Texture.WhiteFlatCircle2;

            //Corner pieces
            DrawRectangle(rect.TopLeft - cornerSize, cornerSize, color, cornerTex, new Rectangle(0, 0, 0.5f, 0.5f), true);

            DrawRectangle(rect.TopRight - new Vector2(0, cornerRadius), cornerSize, color, cornerTex, new Rectangle(0.5f, 0f, 0.5f, 0.5f), true);

            DrawRectangle(rect.BottomRight, cornerSize, color, cornerTex, new Rectangle(0.5f, 0.5f, 0.5f, 0.5f), true);

            DrawRectangle(rect.BottomLeft - new Vector2(cornerRadius, 0), cornerSize, color, cornerTex, new Rectangle(0, 0.5f, 0.5f, 0.5f), true);

            //Center
            DrawRectangle(rect.TopLeft, size, color);

            //Top
            DrawRectangle(rect.TopLeft - new Vector2(0, cornerRadius), new Vector2(size.X, cornerRadius), color);

            //Right
            DrawRectangle(rect.TopRight, new Vector2(cornerRadius, size.Y), color);

            //Bottom
            DrawRectangle(rect.BottomLeft, new Vector2(size.X, cornerRadius), color);

            //Left
            DrawRectangle(rect.TopLeft - new Vector2(cornerRadius, 0), new Vector2(cornerRadius, size.Y), color);
        }

        public void DrawQuadrilateral(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Vector4 color)
        {
            int slot = GetTextureSlot(null);
            var quad = VertexBatch.GetQuad();

            quad[0].Position = topLeft;
            quad[0].TextureSlot = slot;
            quad[0].TexCoord = Vector2.Zero;
            quad[0].Rotation = 0;
            quad[0].Color = color;

            quad[1].Position = topRight;
            quad[1].TextureSlot = slot;
            quad[1].TexCoord = Vector2.Zero;
            quad[1].Rotation = 0;
            quad[1].Color = color;

            quad[2].Position = bottomLeft;
            quad[2].TextureSlot = slot;
            quad[2].TexCoord = Vector2.Zero;
            quad[2].Rotation = 0;
            quad[2].Color = color;

            quad[3].Position = bottomRight;
            quad[3].TextureSlot = slot;
            quad[3].TexCoord = Vector2.Zero;
            quad[3].Rotation = 0;
            quad[3].Color = color;
        }

        public void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector4 color)
        {
            int slot = GetTextureSlot(null);
            var tri = VertexBatch.GetTriangle();

            tri[0].TextureSlot = slot;
            tri[0].Position = p1;
            tri[0].TexCoord = Vector2.Zero;
            tri[0].Rotation = 0;
            tri[0].Color = color;

            tri[1].TextureSlot = slot;
            tri[1].Position = p2;
            tri[1].TexCoord = Vector2.Zero;
            tri[1].Rotation = 0;
            tri[1].Color = color;

            tri[2].TextureSlot = slot;
            tri[2].Position = p3;
            tri[2].TexCoord = Vector2.Zero;
            tri[2].Rotation = 0;
            tri[2].Color = color;
        }

        public void DrawRectangle(Vector2 position, Vector2 size, Vector4 color, Texture texture = null, Rectangle? textureRectangle = null, bool uvNormalized = false, float rotDegrees = 0)
        {
            float texX = 0;
            float texY = 0;
            float texWidth = 1;
            float texHeight = 1;

            if (textureRectangle.HasValue)
            {
                if (uvNormalized)
                {
                    texX = textureRectangle.Value.X;
                    texY = textureRectangle.Value.Y;
                    texWidth = textureRectangle.Value.Width;
                    texHeight = textureRectangle.Value.Height;
                }
                else
                {
                    texX = textureRectangle.Value.X / texture.Width;
                    texY = textureRectangle.Value.Y / texture.Height;
                    texWidth = textureRectangle.Value.Width / texture.Width;
                    texHeight = textureRectangle.Value.Height / texture.Height;
                }
            }

            float rotation = (float)MathUtils.ToRadians(rotDegrees);

            Vector2 center = position + size / 2f;

            int slot = GetTextureSlot(texture);

            var quad = VertexBatch.GetQuad();

            quad[0].Position = position;
            quad[0].TexCoord = new Vector2(texX, texY);
            quad[0].Color = color;
            quad[0].TextureSlot = slot;
            quad[0].RotationOrigin = center;
            quad[0].Rotation = rotation;

            quad[1].Position = new Vector2(position.X + size.X, position.Y);
            quad[1].TexCoord = new Vector2(texX + texWidth, texY);
            quad[1].Color = color;
            quad[1].TextureSlot = slot;
            quad[1].RotationOrigin = center;
            quad[1].Rotation = rotation;

            quad[2].Position = position + size;
            quad[2].TexCoord = new Vector2(texX + texWidth, texY + texHeight);
            quad[2].Color = color;
            quad[2].TextureSlot = slot;
            quad[2].RotationOrigin = center;
            quad[2].Rotation = rotation;

            quad[3].Position = new Vector2(position.X, position.Y + size.Y);
            quad[3].TexCoord = new Vector2(texX, texY + texHeight);
            quad[3].Color = color;
            quad[3].TextureSlot = slot;
            quad[3].RotationOrigin = center;
            quad[3].Rotation = rotation;
        }

        public void DrawRectangleCentered(Vector2 position, Vector2 size, Vector4 color, Texture texture = null, Rectangle? textureRectangle = null, bool uvNormalized = false, float rotDegrees = 0)
        {
            DrawRectangle(position - size / 2f, size, color, texture, textureRectangle, uvNormalized, rotDegrees);
        }

        public void DrawRectangle(Vector2 position, Vector2 size, Vector4 color, Texture texture, Rectangle textureRectangle, Vector2 rotationOrigin, float rotation)
        {
            int slot = GetTextureSlot(texture);

            var quad = VertexBatch.GetQuad();

            quad[0].Position = position;
            quad[0].TexCoord = textureRectangle.TopLeft;
            quad[0].Color = color;
            quad[0].TextureSlot = slot;
            quad[0].RotationOrigin = rotationOrigin;
            quad[0].Rotation = rotation;

            quad[1].Position = new Vector2(position.X + size.X, position.Y);
            quad[1].TexCoord = textureRectangle.TopRight;
            quad[1].Color = color;
            quad[1].TextureSlot = slot;
            quad[1].RotationOrigin = rotationOrigin;
            quad[1].Rotation = rotation;

            quad[2].Position = position + size;
            quad[2].TexCoord = textureRectangle.BottomRight;
            quad[2].Color = color;
            quad[2].TextureSlot = slot;
            quad[2].RotationOrigin = rotationOrigin;
            quad[2].Rotation = rotation;

            quad[3].Position = new Vector2(position.X, position.Y + size.Y);
            quad[3].TexCoord = textureRectangle.BottomLeft;
            quad[3].Color = color;
            quad[3].TextureSlot = slot;
            quad[3].RotationOrigin = rotationOrigin;
            quad[3].Rotation = rotation;
        }

        public void DrawEllipse(Vector2 position, float startAngle, float endAngle, float outerRadius, float innerRadius, Vector4 color, Texture texture = null, int segments = 50, bool wrapUV = true, Rectangle? textureCoords = null)
        {
            startAngle = MathHelper.DegreesToRadians(startAngle);
            endAngle = MathHelper.DegreesToRadians(endAngle);

            int slot = GetTextureSlot(texture);

            Vector2 first, second;

            Vector2 firstUV = Vector2.Zero, secondUV = Vector2.One;

            float theta = startAngle;
            float stepTheta = (endAngle - startAngle) / (segments - 1);

            float cos, sin;

            var vertices = VertexBatch.GetTriangleStrip(segments * 2);

            for (int i = 0; i < vertices.Length; i++)
            {
                cos = MathF.Cos(theta);
                sin = MathF.Sin(theta);

                first.X = position.X + cos * outerRadius;
                first.Y = position.Y + sin * outerRadius;

                second.X = position.X + cos * innerRadius;
                second.Y = position.Y + sin * innerRadius;

                if (wrapUV)
                {
                    if (textureCoords.HasValue)
                    {
                        var tex = textureCoords.Value;

                        firstUV.X = MathUtils.Map(cos * outerRadius, -outerRadius, outerRadius, tex.X, tex.X + tex.Width);
                        firstUV.Y = MathUtils.Map(sin * outerRadius, -outerRadius, outerRadius, tex.Y, tex.Y + tex.Height);

                        secondUV.X = MathUtils.Map(cos * innerRadius, -outerRadius, outerRadius, tex.X, tex.X + tex.Width);
                        secondUV.Y = MathUtils.Map(sin * innerRadius, -outerRadius, outerRadius, tex.Y, tex.Y + tex.Height);
                    }
                    else
                    {
                        firstUV.X = MathUtils.Map(cos * outerRadius, 0, outerRadius, 0.5f, 1);
                        firstUV.Y = MathUtils.Map(sin * outerRadius, 0, outerRadius, 0.5f, 1);

                        secondUV.X = MathUtils.Map(cos * innerRadius, 0, outerRadius, 0.5f, 1);
                        secondUV.Y = MathUtils.Map(sin * innerRadius, 0, outerRadius, 0.5f, 1);
                    }
                }

                vertices[i].Position = first;
                vertices[i].Color = color;
                vertices[i].TextureSlot = slot;
                vertices[i].Rotation = 0;
                vertices[i].TexCoord = firstUV;

                ++i;

                vertices[i].Position = second;
                vertices[i].Color = color;
                vertices[i].TextureSlot = slot;
                vertices[i].Rotation = 0;
                vertices[i].TexCoord = secondUV;

                theta += stepTheta;
            }
        }

        public Vector3 BorderColorOuter;
        public Vector3 BorderColorInner;

        public Vector3 TrackColorOuter;
        public Vector3 TrackColorInner;

        public Vector4 ShadowColor;

        public Vector4 FinalColorMult = Vector4.One;
        public Vector3 FinalColorAdd = Vector3.Zero;

        public float BorderWidth = 1.0f;

        /// <summary>
        /// Ends and draws the batch currently pending
        /// </summary>
        public void EndDraw()
        {
            if (texturesToBind.Count == 0)
                return;

            foreach (var item in texturesToBind)
            {
                //Key is the actual texture, and value is the desired slot to bind it to
                //so go through all textures to bind and bind them to the desired slot?
                item.Key.Bind(item.Value);
            }

            Shader.Bind();
            Shader.SetIntArray("u_Textures", textureSlots);

            Shader.SetVector("u_FinalColorMult", FinalColorMult);
            Shader.SetVector("u_FinalColorAdd", FinalColorAdd);

            #region SliderUniforms
            Shader.SetFloat("u_BorderWidth", BorderWidth);

            Shader.SetVector("u_BorderColorOuter", BorderColorOuter);
            Shader.SetVector("u_BorderColorInner", BorderColorInner);

            Shader.SetVector("u_TrackColorOuter", TrackColorOuter);
            Shader.SetVector("u_TrackColorInner", TrackColorInner);

            Shader.SetVector("u_ShadowColor", ShadowColor);
            #endregion

            Shader.SetMatrix("u_Projection", Projection);

            TexturesBound += (ulong)texturesToBind.Count;
            VerticesDrawn += (ulong)VertexBatch.VertexRenderCount;
            IndicesDrawn += (ulong)VertexBatch.IndexRenderCount;
            TrianglesDrawn += (ulong)VertexBatch.TriangleRenderCount;
            DrawCalls++;

            VertexBatch.Draw();

            //Todo dont rebind textures every frame, needs some kind of texture manager
            texturesToBind.Clear();

            bindTextureIndex = 0;
        }
    }
}