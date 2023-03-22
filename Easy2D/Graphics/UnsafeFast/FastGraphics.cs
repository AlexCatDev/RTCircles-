using Silk.NET.OpenGLES;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy2D
{
    /// <summary>
    /// Slightly faster version of Graphics
    ///</summary>
    public class FastGraphics
    {
        private static readonly Dictionary<string, string> fragmentPreprocessor = new Dictionary<string, string>();
        private static readonly int[] textureSlots;

        private static int MaxTextureSlots;

        static FastGraphics()
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

        public Matrix4x4 Projection;

        public readonly Shader Shader = new Shader();
        public readonly UnsafePrimitiveBatch<Vertex> VertexBatch;

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

        public FastGraphics(int vertexCount = 500000, int indexCount = 2000000)
        {
            VertexBatch = new UnsafePrimitiveBatch<Vertex>(vertexCount, indexCount);

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
            if (texture == null)
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

            perpen = Vector2.Normalize(perpen);

            Vector2 topRight = new Vector2(startPosition.X + perpen.X * thickness,
                startPosition.Y + perpen.Y * thickness);

            Vector2 bottomRight = startPosition;

            Vector2 topLeft = new Vector2(endPosition.X + perpen.X * thickness,
                endPosition.Y + perpen.Y * thickness);

            Vector2 bottomLeft = endPosition;

            unsafe
            {
                var quad = VertexBatch.GetQuad();

                int slot = GetTextureSlot(texture);

                quad->Color = color;
                quad->Position = topRight;
                quad->TexCoord = new Vector2(0, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = bottomRight;
                quad->TexCoord = new Vector2(1, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = bottomLeft;
                quad->TexCoord = new Vector2(1, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = topLeft;
                quad->TexCoord = new Vector2(0, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;

                topRight = startPosition;

                bottomRight = new Vector2(startPosition.X - perpen.X * thickness,
                                                 startPosition.Y - perpen.Y * thickness);

                topLeft = endPosition;

                bottomLeft = new Vector2(endPosition.X - perpen.X * thickness,
                                                 endPosition.Y - perpen.Y * thickness);

                quad = VertexBatch.GetQuad();

                quad->Color = color;
                quad->Position = topRight;
                quad->TexCoord = new Vector2(1, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = bottomRight;
                quad->TexCoord = new Vector2(0, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = bottomLeft;
                quad->TexCoord = new Vector2(0, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Color = color;
                quad->Position = topLeft;
                quad->TexCoord = new Vector2(1, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
            }
        }

        public void DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color, float thickness, Texture texture = null)
        {
            DrawLine(startPosition, endPosition, color, color, thickness, texture);
        }

        public void DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color1, Vector4 color2, float thickness, Texture texture = null)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen = Vector2.Normalize(perpen);

            Vector2 topLeft = new Vector2(startPosition.X + perpen.X * thickness / 2f,
                startPosition.Y + perpen.Y * thickness / 2f);

            Vector2 topRight = new Vector2(startPosition.X - perpen.X * thickness / 2f,
                startPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomLeft = new Vector2(endPosition.X - perpen.X * thickness / 2f,
                endPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomRight = new Vector2(endPosition.X + perpen.X * thickness / 2f,
                endPosition.Y + perpen.Y * thickness / 2f);

            unsafe
            {
                var quad = VertexBatch.GetQuad();

                int slot = GetTextureSlot(texture);

                quad->Rotation = 0;
                quad->Color = color1;
                quad->Position = topLeft;
                quad->TexCoord = new Vector2(0, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color1;
                quad->Position = topRight;
                quad->TexCoord = new Vector2(0, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color2;
                quad->Position = bottomLeft;
                quad->TexCoord = new Vector2(1, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color2;
                quad->Position = bottomRight;
                quad->TexCoord = new Vector2(1, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
            }
        }

        public void DrawDottedLine(Vector2 startPosition, Vector2 endPosition, Texture texture, Vector4 color, Vector2 dotSize, float spacing, bool centeredDots = true, bool alwaysDotEnd = false, Rectangle? bounds = null)
        {
            float angle = MathF.Atan2(endPosition.Y - startPosition.Y, endPosition.X - startPosition.X);

            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            Vector2 step = new Vector2(cos, sin) * spacing;

            float degrees = MathUtils.ToDegrees(angle);

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
        public void DrawString(string text, Font font, Vector2 position, Vector4 color, float scale = 1f)
        {
            Vector2 startPosition = position;

            scale = Math.Max(0, scale);

            float biggestChar = 0;
            float smallestBearing = float.MaxValue;

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

                //30%
                DrawRectangle(position, size, color, font.Texture, new Rectangle(character.X, character.Y, character.Width, character.Height), false);

                position.Y -= bearing - smallestBearing;

                position.X += character.XAdvance * scale;

                //10%
                if (i < text.Length - 1)
                    position.X += font.Info.GetKerningAmount(c, text[i + 1]) * scale;

                position.X -= character.XOffset * scale;
            }
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

            var newProj = Matrix4x4.CreateOrthographicOffCenter(0, frameBuffer.Width, frameBuffer.Height, 0, -10, 10);

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

            unsafe
            {
                int slot = GetTextureSlot(texture);
                var quad = VertexBatch.GetQuad();

                quad->Position = position;
                quad->TexCoord = new Vector2(texX, texY);
                quad->Color = color;
                quad->TextureSlot = slot;
                quad->RotationOrigin = center;
                quad->Rotation = rotation;
                ++quad;

                quad->Position = new Vector2(position.X + size.X, position.Y);
                quad->TexCoord = new Vector2(texX + texWidth, texY);
                quad->Color = color;
                quad->TextureSlot = slot;
                quad->RotationOrigin = center;
                quad->Rotation = rotation;
                ++quad;

                quad->Position = position + size;
                quad->TexCoord = new Vector2(texX + texWidth, texY + texHeight);
                quad->Color = color;
                quad->TextureSlot = slot;
                quad->RotationOrigin = center;
                quad->Rotation = rotation;
                ++quad;

                quad->Position = new Vector2(position.X, position.Y + size.Y);
                quad->TexCoord = new Vector2(texX, texY + texHeight);
                quad->Color = color;
                quad->TextureSlot = slot;
                quad->RotationOrigin = center;
                quad->Rotation = rotation;
            }
        }

        public void DrawRectangleCentered(Vector2 position, Vector2 size, Vector4 color, Texture texture = null, Rectangle? textureRectangle = null, bool uvNormalized = false, float rotDegrees = 0)
        {
            DrawRectangle(position - size / 2f, size, color, texture, textureRectangle, uvNormalized, rotDegrees);
        }

        public void DrawEllipse(Vector2 position, float startAngle, float endAngle, float outerRadius, float innerRadius, Vector4 color, Texture texture = null, uint segments = 50, bool wrapUV = true, Rectangle? textureCoords = null)
        {
            startAngle = MathUtils.ToRadians(startAngle);
            endAngle = MathUtils.ToRadians(endAngle);

            int slot = GetTextureSlot(texture);

            Vector2 first, second;

            Vector2 firstUV = Vector2.Zero, secondUV = Vector2.One;

            float theta = startAngle;
            float stepTheta = (endAngle - startAngle) / (segments - 1);

            float cos, sin;

            uint vertexCount = segments * 2;

            unsafe
            {
                var vertices = VertexBatch.GetTriangleStrip(vertexCount);

                for (int i = 0; i < vertexCount; i += 2)
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

                    vertices->Position = first;
                    vertices->Color = color;
                    vertices->TextureSlot = slot;
                    vertices->Rotation = 0;
                    vertices->TexCoord = firstUV;
                    ++vertices;

                    vertices->Position = second;
                    vertices->Color = color;
                    vertices->TextureSlot = slot;
                    vertices->Rotation = 0;
                    vertices->TexCoord = secondUV;
                    ++vertices;

                    theta += stepTheta;
                }
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

        private bool slotArrayFlag = true;

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
            if (slotArrayFlag)
            {
                Shader.SetIntArray("u_Textures", textureSlots);
                slotArrayFlag = false;
            }

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
