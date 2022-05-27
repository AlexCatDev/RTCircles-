using System;
using System.Collections.Generic;

namespace OsuBot
{
    /// <summary>
    /// This class acts kinda like queue, every parameter check removes the underlying string (if eligible) from the buffer.
    /// </summary>
    public class CommandBuffer
    {
        private List<string> buffer;
        public string TriggerText { get; private set; }

        public int Count => buffer.Count;

        public CommandBuffer(List<string> input, string triggerText)
        {
            buffer = input;
            TriggerText = triggerText;
        }

        public void Take(Func<string, bool> pred)
        {
            foreach (var str in buffer)
            {
                if (pred.Invoke(str))
                {
                    buffer.Remove(str);

                    return;
                }
            }
        }

        public string GetRemaining(string spaceString = "_")
        {
            string output = "";
            for (int i = 0; i < buffer.Count; i++)
            {
                bool isLast = i == buffer.Count - 1;

                output += buffer[i];

                if (!isLast)
                    output += spaceString;
            }

            return output;
        }

        public string TakeFirst()
        {
            if (buffer.Count == 0)
                return string.Empty;

            string first = buffer[0];
            buffer.RemoveAt(0);
            return first;
        }

        public int? GetInt()
        {
            foreach (var str in buffer)
            {
                if(int.TryParse(str, out int result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }

            return null;
        }

        public long? GetLong()
        {
            foreach (var str in buffer)
            {
                if (long.TryParse(str, out long result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }

            return null;
        }

        public ulong? GetULong()
        {
            foreach (var str in buffer)
            {
                if (ulong.TryParse(str, out ulong result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }

            return null;
        }

        public double? GetDouble()
        {
            foreach (var str in buffer)
            {
                if (double.TryParse(str, out double result))
                {
                    buffer.Remove(str);
                    return result;
                }
            }
            
            return null;
        }

        public string GetParameter(string param)
        {
            foreach (var str in buffer)
            {
                if (str.Contains(param))
                {
                    buffer.Remove(str);
                    return str.Remove(0, param.Length);
                }
            }

            return "";
        }

        public void Discard(string val)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i].Contains(val))
                {
                    buffer[i] = buffer[i].Trim(val.ToCharArray());
                }
            }
        }

        public bool HasParameter(string param)
        {
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i].ToLower() == param)
                {
                    buffer.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}
