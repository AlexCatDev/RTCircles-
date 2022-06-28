using Easy2D;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public static class Settings
    {
        private static string SettingsDirectory;

        static Settings()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (!Directory.Exists(localAppData))
            {
                localAppData = ".";
                Utils.Log($"Could not find appdata directory, save directory is now path of executable", LogLevel.Warning);
            }

            SettingsDirectory = $"{localAppData}/Settings/{Assembly.GetCallingAssembly().GetName().Name}";

            new DirectoryInfo(SettingsDirectory).Create();
        }

        public static T GetValue<T>(string name, out bool exists, T defaultValue = default(T))
        {
            string filename = $"{SettingsDirectory}/{name}.json";

            if (new FileInfo(filename).Exists == false)
            {
                exists = false;
                return defaultValue;
            }

            string fileText = File.ReadAllText(filename);

            exists = true;
            return JsonConvert.DeserializeObject<T>(fileText);
        }

        public static void SetValue<T>(T value, string name)
        {
            string filename = $"{SettingsDirectory}/{name}.json";

            string jsonText = JsonConvert.SerializeObject(value);

            File.WriteAllText(filename, jsonText);

            Utils.Log($"Setting for {name} has been updated!", LogLevel.Important);
        }
    }
}
