using Microsoft.Xna.Framework;
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
        public static DirectoryInfo SaveDirectory = new DirectoryInfo("saves");

        DirectoryInfo Directory;
        DirectoryInfo TempDirectory;

        public bool Cached;
        public string Name;
        public DateTime CreateTime;
        public DateTime LastPlayedTime;

        public string FileName => Directory.Name;
        public bool Exists => Directory.Exists;

        public SaveFile(DirectoryInfo directory)
        {
            Directory = directory;
            TempDirectory = new DirectoryInfo(Path.Combine(directory.Parent.FullName, $"_{directory.Name}"));
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

        private JObject ReadFile(string dir, string name)
        {
            string path = Path.Combine(dir, $"{name}");
            FileInfo file = new FileInfo(path);
            return ReadFile(file);
        }

        private JObject ReadFile(FileInfo file)
        {
            StreamReader reader = file.OpenText();
            JsonTextReader jsonReader = new JsonTextReader(reader);
            JObject json = JObject.Load(jsonReader);
            jsonReader.Close();
            return json;
        }

        private void ClearSafe(DirectoryInfo directory)
        {
            foreach(var file in directory.GetFiles())
            {
                if (file.Extension == ".json")
                    file.Delete();
            }
        }

        private void MoveSafe(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
                destination.Create();
            else
                ClearSafe(destination);
            destination.Refresh();
            foreach (var file in source.GetFiles())
            {
                file.MoveTo(Path.Combine(destination.FullName, file.Name));
            }
            try
            {
                source.Delete();
            }
            catch(Exception e) //Might fail because of directory locks
            {

            }
        }

        public void Preload()
        {
            if (!Cached)
            {
                ReadMeta(ReadFile(Directory.FullName, "meta.json"));
                Cached = true;
            }
        }

        public void AddDescription(ref string description)
        {
            description += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            description += $"{Game.FormatColor(Color.Gray)}{Path.DirectorySeparatorChar}{FileName}{Game.FormatColor(Color.White)}\n";
            description += $"Created {Game.FORMAT_BOLD}{CreateTime}{Game.FORMAT_BOLD}\n";
            description += $"Last played {Game.FORMAT_BOLD}{LastPlayedTime}{Game.FORMAT_BOLD}\n";
        }

        private JToken WriteMeta()
        {
            JObject json = new JObject();

            json["name"] = Name;
            json["createTime"] = CreateTime;
            json["lastPlayedTime"] = LastPlayedTime;
            return json;
        }

        private void ReadMeta(JToken json)
        {
            Name = json["name"].Value<string>();
            CreateTime = json["createTime"].Value<DateTime>();
            LastPlayedTime = json["lastPlayedTime"].Value<DateTime>();
        }

        public void Save(SceneGame world)
        {
            if (!TempDirectory.Exists)
                TempDirectory.Create();
            else
                ClearSafe(TempDirectory);
            WriteFileSafe(TempDirectory.FullName, "main.json", world.WriteJson());
            WriteFileSafe(TempDirectory.FullName, "meta.json", WriteMeta());
            foreach (var map in world.Maps.Values)
            {
                WriteFileSafe(TempDirectory.FullName, $"map_{map.ID.ToString()}.json", map.WriteJson());
            }
            MoveSafe(TempDirectory, Directory);
        }
        
        public void Load(SceneGame world)
        {
            JObject main = null;
            JObject meta = null;
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
                else if (file.Name.StartsWith("meta")) //Read main file
                {
                    meta = ReadFile(file);
                }
            }

            if (meta != null)
                ReadMeta(meta);
            if (main != null)
                world.ReadJson(main);

            foreach (var json in maps)
            {
                Map map = new Map(world, 0, 0);
                map.ReadJson(json);
            }
        }
    }
}
