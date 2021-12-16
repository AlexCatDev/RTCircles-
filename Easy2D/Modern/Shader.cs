using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGLES;
using OpenTK.Mathematics;

namespace Easy2D
{
    public class Shader : GLObject
    {
        private Dictionary<string, int> uniformLocationCache = new Dictionary<string, int>();

        private Dictionary<ShaderType, Stream> shaderFiles = new Dictionary<ShaderType, Stream>();

        private Dictionary<ShaderType, Dictionary<string, string>> preprocessors = new Dictionary<ShaderType, Dictionary<string, string>>();

        public void AttachShader(ShaderType shaderType, Stream shader)
        {
            if (shader is null)
                throw new Exception($"Shader stream was null type: {shaderType}");

            if(shaderFiles.ContainsKey(shaderType))
                throw new Exception($"Shader already have type: {shaderType} attached to it");

            shaderFiles.Add(shaderType, shader);
        }
        
        /// <summary>
        /// Attach a preprocessor to the respected shadertype.
        /// A preprocessor is just a string replacement mechanic
        /// Example: { "//replaceme", "FragColor = vec4(1.0); "}, { "ThisCouldBeAnything", "Same here" }
        /// </summary>
        /// <param name="shaderType"></param>
        /// <param name="preprocessor"></param>
        public void AttachPreprocessor(ShaderType shaderType, Dictionary<string, string> preprocessor)
        {
            if (preprocessors.ContainsKey(shaderType))
                throw new Exception($"Shader already have preprocessor for type: {shaderType} attached to it");

            preprocessors.Add(shaderType, preprocessor);
        }

        private void compileShaderFiles()
        {
            //Process and compile shader files
            foreach (KeyValuePair<ShaderType, Stream> shaderInfo in shaderFiles)
            {
                string shaderText = readShaderFromFile(shaderInfo.Value, shaderInfo.Key);

                uint shaderID = GL.Instance.CreateShader(shaderInfo.Key);
                if (shaderID == 0)
                {
                    Utils.Log($"Error creating {shaderInfo.Key}. Could not generate shader buffer.", LogLevel.Error);
                }
                
                GL.Instance.ShaderSource(shaderID, shaderText);
                GL.Instance.CompileShader(shaderID);

                int compileStatus;
                GL.Instance.GetShader(shaderID, ShaderParameterName.CompileStatus, out compileStatus);
                if (compileStatus == 0)
                {
                    Utils.Log($"Error compiling {shaderInfo.Key}. Could not compile shader.", LogLevel.Error);
                    Utils.Log(GL.Instance.GetShaderInfoLog(shaderID), LogLevel.Warning);
                }

                GL.Instance.AttachShader(Handle, shaderID);
            }

            //Link shader files
            GL.Instance.LinkProgram(Handle);
            int linkStatus;
            GL.Instance.GetProgram(Handle, ProgramPropertyARB.LinkStatus, out linkStatus);
            if (linkStatus == 0)
            {
                Utils.Log("Error linking shader program: Could not link shader program!", LogLevel.Error);
                Utils.Log(GL.Instance.GetProgramInfoLog(Handle), LogLevel.Warning);
            }

            GL.Instance.ValidateProgram(Handle);
            int validatationStatus;
            GL.Instance.GetProgram(Handle, ProgramPropertyARB.ValidateStatus, out validatationStatus);
            if (validatationStatus == 0)
            {
                Utils.Log("Error validating shader program: Could not validate shader program!", LogLevel.Error);
                Utils.Log(GL.Instance.GetProgramInfoLog(Handle), LogLevel.Warning);
            }

            GL.Instance.UseProgram(Handle);
        }

        public void Unbind()
        {
            GL.Instance.UseProgram(0);
        }

        private int GetUniformLocation(string uniformName)
        {
            if(uniformLocationCache.TryGetValue(uniformName, out int location))
            {
                return location;
            }
            else
            {
                int foundUniformLocation = GL.Instance.GetUniformLocation(Handle, uniformName);

                if (foundUniformLocation == -1)
                    Utils.Log($"Could not add uniform: '{uniformName}'", LogLevel.Warning);

                uniformLocationCache.Add(uniformName, foundUniformLocation);

                return foundUniformLocation;
            }
        }

        public void SetInt(string uniformName, int value)
        {
            GL.Instance.Uniform1(GetUniformLocation(uniformName), value);
        }

        public void SetFloat(string uniformName, float value)
        {
            GL.Instance.Uniform1(GetUniformLocation(uniformName), value);
        }

        public void SetDouble(string uniformName, double value)
        {
            GL.Instance.Uniform1(GetUniformLocation(uniformName), (float)value);
        }

        public void SetVector(string uniformName, Vector2 value)
        {
            GL.Instance.Uniform2(GetUniformLocation(uniformName), value.X, value.Y);
        }

        public void SetVector(string uniformName, Vector3 value)
        {
            GL.Instance.Uniform3(GetUniformLocation(uniformName), value.X, value.Y, value.Z);
        }

        public void SetVector(string uniformName, Vector4 value)
        {
            GL.Instance.Uniform4(GetUniformLocation(uniformName), value.X, value.Y, value.Z, value.W);
        }

        public void SetBoolean(string uniformName, bool value)
        {
            GL.Instance.Uniform1(GetUniformLocation(uniformName), value ? 1 : 0);
        }

        public void SetMatrix(string uniformName, Matrix4 value, bool transpose = true)
        {
            unsafe
            {
                GL.Instance.UniformMatrix4(GetUniformLocation(uniformName), 1, transpose, (float*)&value);
            }
        }

        public void SetIntArray(string uniformName, int[] values)
        {
            GL.Instance.Uniform1(GetUniformLocation(uniformName), new ReadOnlySpan<int>(values));
        }

        private string readShaderFromFile(Stream fileName, ShaderType shaderType)
        {
            StringBuilder shader = new StringBuilder();

            try
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if(preprocessors.TryGetValue(shaderType, out Dictionary<string, string> preprocessor))
                        {
                            if(preprocessor.TryGetValue(line.Trim('\n', ' ', '\0', '\t'), out string replaceWith))
                            {
                                line = replaceWith;
                            }
                        }

                        shader.Append(line).Append("\n");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return shader.ToString();
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.CreateProgram();

            if (Handle == 0)
            {
                Utils.Log("Error creating shader: Could not generate program buffer.", LogLevel.Error);
            }
            else
            {
                bind(null);
                compileShaderFiles();
            }
        }

        protected override void bind(int? slot)
        {
            GL.Instance.UseProgram(Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteProgram(Handle);
            Handle = uint.MaxValue;
        }
    }
}
