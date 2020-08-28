using Newtonsoft.Json.Linq;
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

    class TypeInfo
    {
        public string ID;
        public Type Type;
        public Func<object> Construct;

        public TypeInfo(string iD, Type type, Func<object> construct)
        {
            ID = iD;
            Type = type;
            Construct = construct;
        }
    }

    class Context
    {
        int CurrentID = 0;
        Dictionary<IEffectHolder, int> EntityToID = new Dictionary<IEffectHolder, int>();
        Dictionary<int, IEffectHolder> IDToEntity = new Dictionary<int, IEffectHolder>();

        public int AddEntity(IEffectHolder entity)
        {
            int id = CurrentID++;
            AddEntity(entity, id);
            return id;
        }

        public void AddEntity(IEffectHolder entity, int id)
        {
            EntityToID.Add(entity, id);
            IDToEntity.Add(id, entity);
        }

        public int GetID(IEffectHolder holder)
        {
            return EntityToID[holder];
        }
    }

    interface IJsonSerializable
    {
        Guid GlobalID
        {
            get;
        }

        JToken WriteJson(Context context);

        void ReadJson(JToken json, Context context);
    }

    class Serializer
    {
        public static Dictionary<Type, string> TypeToName = new Dictionary<Type, string>();
        public static Dictionary<string, TypeInfo> Registry = new Dictionary<string, TypeInfo>();

        public static void Register(string id, Type type, Func<object> construct)
        {
            TypeToName.Add(type, id);
            Registry.Add(id, new TypeInfo(id, type, construct));
        }

        public static T Create<T>(string id) where T : class
        {
            TypeInfo typeInfo;
            if(id == null || !Registry.TryGetValue(id, out typeInfo))
            {
                //TODO: Find default replacement type
                typeInfo = null;
            }
            if (typeInfo != null && typeof(T).IsAssignableFrom(typeInfo.Type))
                return (T)typeInfo.Construct();
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
                return null; //TODO: Static id
            }
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
                Func<object> del = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), construct);

                Register(serializable.ID, type, del);

                //TODO: Register construct as delegate
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
