using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class SaveFile
    {
        DirectoryInfo Directory;

        public bool Exists => Directory.Exists;

        public SaveFile(DirectoryInfo directory)
        {
            Directory = directory;
        }

        private void WriteFileSafe(string dir, string name, JToken json)
        {
            string tempPath = Path.Combine(dir, $"_{name}");
            string path = Path.Combine(dir, $"{name}");
            FileInfo file = new FileInfo(tempPath);
            StreamWriter writer = file.CreateText();
            JsonTextWriter jsonWriter = new JsonTextWriter(writer) {
                Formatting = Formatting.Indented
            };
            json.WriteTo(jsonWriter);
            jsonWriter.Close();
            File.Delete(path);
            file.MoveTo(path);
        }

        private JObject ReadFile(FileInfo file)
        {
            StreamReader reader = file.OpenText();
            JsonTextReader jsonReader = new JsonTextReader(reader);
            JObject json = JObject.Load(jsonReader);
            jsonReader.Close();
            return json;
        }

        public void Save(SceneGame world)
        {
            if (!Directory.Exists)
                Directory.Create();
            WriteFileSafe(Directory.FullName, "main.json", world.WriteJson());
            foreach(var map in world.Maps.Values)
            {
                WriteFileSafe(Directory.FullName, $"map_{map.ID.ToString()}.json", map.WriteJson());
            }
        }
        
        public void Load(SceneGame world)
        {
            JObject main = null;
            List<JObject> maps = new List<JObject>();

            foreach (var file in Directory.GetFiles("*.json"))
            {
                if (file.Name.StartsWith("_")) //Failed to write this file, ignore it.
                    continue;
                else if (file.Name.StartsWith("map_")) //Read map
                {
                    JObject json = ReadFile(file);
                    maps.Add(json);
                }
                else if(file.Name.StartsWith("main")) //Read main file
                {
                    main = ReadFile(file);
                }
            }

            if (main != null)
                world.ReadJson(main);

            foreach(var json in maps)
            {
                Map map = new Map(world, 0, 0);
                map.ReadJson(json);
            }
        }
    }
}
