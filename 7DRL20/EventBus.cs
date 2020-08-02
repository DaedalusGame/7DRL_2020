using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace RoguelikeEngine
{
    [AttributeUsage(AttributeTargets.Method)]
    class EventSubscribe : Attribute
    {
    }

    class Event
    {

    }

    class MethodTransformer
    {
        MethodInfo Method;
        Type Type;
        Type Action;

        public MethodTransformer(MethodInfo method, Type type)
        {
            Method = method;
            Type = type;
            Action = GetAction(type);
        }
    
        public EventHandler Transform()
        {
            Delegate del = Delegate.CreateDelegate(Action, Method);
            return new EventHandler(null, Type, del);
        }

        public EventHandler Transform(object o)
        {
            Delegate del = Delegate.CreateDelegate(Action, o, Method);
            return new EventHandler(o, Type, del);
        }

        private Type GetAction(Type type)
        {
            return typeof(Action<object>).GetGenericTypeDefinition().MakeGenericType(new[] { type });
        }
    }

    class ClassTransformer
    {
        List<MethodTransformer> Methods = new List<MethodTransformer>();
        List<MethodTransformer> MethodsStatic = new List<MethodTransformer>();

        public ClassTransformer(Type type)
        {
            var methods = type.GetRuntimeMethods();
            foreach (var method in methods)
            {
                if (!method.IsPrivate && method.GetCustomAttribute<EventSubscribe>() != null)
                {
                    var parameters = method.GetParameters();
                    if (MatchesParameters(type, parameters))
                    {
                        Type parameterType = parameters[0].ParameterType;
                        if (method.IsStatic)
                            MethodsStatic.Add(new MethodTransformer(method,parameterType));
                        else
                            Methods.Add(new MethodTransformer(method, parameterType));
                    }
                }
            }
        }

        public IEnumerable<EventHandler> Transform()
        {
            foreach(var method in MethodsStatic)
            {
                yield return method.Transform();
            }
        }

        public IEnumerable<EventHandler> Transform(object o)
        {
            foreach (var method in Methods)
            {
                yield return method.Transform(o);
            }
        }

        private bool MatchesParameters(Type objectType, ParameterInfo[] parameters)
        {
            if (parameters.Length == 1)
                return typeof(Event).IsAssignableFrom(parameters[0].ParameterType);
            return false;
        }
    }

    class EventHandler
    {
        public object Object;
        Type Type;
        Delegate Delegate;

        public EventHandler(object obj, Type type, Delegate del)
        {
            Object = obj;
            Type = type;
            Delegate = del;
        }

        public void Push(Event evt)
        {
            if(Type == evt.GetType())
                Delegate.DynamicInvoke(evt);
        }
    }
    
    class EventBus
    {
        static List<EventHandler> Handlers = new List<EventHandler>();

        public delegate void EventDelegate(Event evt);

        static Dictionary<Type, ClassTransformer> ClassTransformers = new Dictionary<Type, ClassTransformer>();

        public static void Register(Type type)
        {
            ClassTransformer classTransformer = ClassTransformers.GetOrDefault(type, null);
            if (classTransformer == null)
            {
                classTransformer = new ClassTransformer(type);
                ClassTransformers.Add(type, classTransformer);
            }

            Handlers.AddRange(classTransformer.Transform());
        }

        public static void Register(object o)
        {
            Type type = o.GetType();
            ClassTransformer classTransformer = ClassTransformers.GetOrDefault(type, null);
            if(classTransformer == null)
            {
                classTransformer = new ClassTransformer(type);
                ClassTransformers.Add(type, classTransformer);
            }

            Handlers.AddRange(classTransformer.Transform(o));
        }

        public static void Unregister(object o)
        {
            Handlers.RemoveAll(handler => handler.Object == o);
        }

        public static void PushEvent(Event evt)
        {
            foreach(var handler in Handlers)
            {
                handler.Push(evt);
            }
        }
    }
}
