using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    [AttributeUsage(AttributeTargets.Method)]
    class Construct : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    class SerializeInfo : Attribute
    {
        public string ID;

        public SerializeInfo(string id)
        {
            ID = id;
        }
    }

    delegate object ConstructDelegate(Context context);

    class TypeInfo
    {
        public string ID;
        public Type Type;
        public ConstructDelegate Construct;

        public TypeInfo(string iD, Type type, ConstructDelegate construct)
        {
            ID = iD;
            Type = type;
            Construct = construct;
        }
    }

    class Context
    {
        public Map Map;

        public SceneGame World => Map.World;

        public Context(Map map)
        {
            Map = map;
        }

        public string GetID(JToken json)
        {
            string id = null;
            if (json is JValue)
                id = json.Value<string>();
            else if (json is JObject)
                id = json["id"].Value<string>();

            return id;
        }

        public Tile CreateTile(JToken json)
        {
            string id = GetID(json);
            var entity = Serializer.Create<Tile>(id, this);
            if (entity != null)
            {
                entity.ReadJson(json, this);
                return entity;
            }
            return null;
        }

        public IJsonSerializable CreateEntity(JToken json)
        {
            return CreateEntity<IJsonSerializable>(json);
        }

        public T CreateEntity<T>(JToken json) where T : class, IJsonSerializable
        {
            string id = GetID(json);
            var entity = Serializer.Create<T>(id, this);
            if (entity != null)
            {
                entity.ReadJson(json, this);
                return entity;
            }
            return null;
        }

        public Effect CreateEffect(JToken json)
        {
            string id = GetID(json);
            var entity = Serializer.Create<Effect>(id, this);
            if (entity != null)
            {
                entity.ReadJson(json, this);
                return entity;
            }
            return null;
        }
    }

    interface IJsonSerializable
    {
        Guid GlobalID
        {
            get;
        }

        Map Map
        {
            get;
            set;
        }

        JToken WriteJson(Context context);

        void ReadJson(JToken json, Context context);
    }

    class Serializer
    {
        public static Dictionary<Type, string> TypeToName = new Dictionary<Type, string>();
        public static Dictionary<string, TypeInfo> Registry = new Dictionary<string, TypeInfo>();

        public static void Register(string id, Type type, ConstructDelegate construct)
        {
            TypeToName.Add(type, id);
            Registry.Add(id, new TypeInfo(id, type, construct));
        }

        public static T Create<T>(string id, Context context) where T : class
        {
            TypeInfo typeInfo;
            if(id == null || !Registry.TryGetValue(id, out typeInfo))
            {
                //TODO: Find default replacement type
                typeInfo = null;
            }
            if (typeInfo != null && typeof(T).IsAssignableFrom(typeInfo.Type))
                return (T)typeInfo.Construct(context);
            return null;
        }

        public static string GetID(object obj)
        {
            return TypeToName.GetOrDefault(obj.GetType(), null);
        }

        public static JToken GetHolderID(IEffectHolder holder)
        {
            if (holder is MapTile mapTile)
            {
                return WriteCoordinate("mapTile", mapTile.X, mapTile.Y);
            }
            else if (holder is Tile tile)
            {
                if (tile == tile.Under)
                    return WriteCoordinate("under", tile.X, tile.Y);
                else
                    return WriteCoordinate("tile", tile.X, tile.Y);
            }
            else if (holder is IJsonSerializable jsonSerializable)
            {
                return jsonSerializable.GlobalID.ToString();
            }
            else
            {
                return null;
            }
        }

        public static IEffectHolder GetHolder(JToken json, Context context)
        {
            //TODO: Extract this somehow
            if (json is JObject) //It's a coord
            {
                string type = json["type"].Value<string>();
                int x = json["x"].Value<int>();
                int y = json["y"].Value<int>();

                MapTile mapTile = context.Map.Tiles[x, y];

                switch (type)
                {
                    case ("mapTile"):
                        return mapTile;
                    case ("tile"):
                        return mapTile.Tile;
                    case ("under"):
                        return mapTile.UnderTile;
                    default:
                        return null;
                }
            }
            else if (json is JValue value && value.Type == JTokenType.Integer) //It's a global id
            {
                Guid globalId = Guid.Parse(value.Value<string>());
                return EffectManager.GetHolder(globalId);
            }
            else
            {
                return null;
            }
        }

        public static T GetHolder<T>(JToken json, Context context) where T : class, IEffectHolder
        {
            IEffectHolder holder = GetHolder(json, context);
            if (holder is T)
                return (T)holder;
            return null;
        }

        //TODO: Support cross-map coords
        public static JToken WriteCoordinate(string type, int x, int y)
        {
            JObject json = new JObject();
            json["type"] = type;
            json["x"] = x;
            json["y"] = y;
            return json;
        }

        public static void Init()
        {
            //TODO: Get id from construct instead of serializeinfo
            foreach(var type in GetSerializableTypes())
            {
                var serializable = type.GetCustomAttribute<SerializeInfo>();
                MethodInfo construct = null;
                foreach (var method in type.GetMethods())
                {
                    if (Attribute.IsDefined(method, typeof(Construct)))
                    {
                        construct = method;
                    }
                }
                if(construct == null)
                {
                    throw new Exception($"Type {type} is marked serializable, but doesn't contain a method marked as construct.");
                }

                try
                {
                    ConstructDelegate del = (ConstructDelegate)Delegate.CreateDelegate(typeof(ConstructDelegate), construct);
                    Register(serializable.ID, type, del);
                }
                catch(ArgumentException e)
                {
                    throw new Exception($"Type {type} is marked serializable, but construct method could not be bound.");
                }
            }
        }

        static IEnumerable<Type> GetSerializableTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (Attribute.IsDefined(type, typeof(SerializeInfo), false))
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
