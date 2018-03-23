using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perfusion
{
    public class Container
    {
        public const BindingFlags ALL_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        Dictionary<Type, ObjectInfo> objects = new Dictionary<Type, ObjectInfo>();

        public void Add<TContract>(Func<TContract> F, InjectionType type = InjectionType.Singleton) where TContract : class
        {
            if (type != InjectionType.Singleton && type != InjectionType.Transient) throw new PerfusionException("Invalid injection type " + type);
            objects.Add(typeof(TContract), new ObjectInfo()
            {
                Factory = F,
                IsSingleton = type == InjectionType.Singleton,
                HasBeenInstantiated = false
            });
        }

        public void AddInstance<TContract>(TContract f) where TContract : class => Add(() => f, InjectionType.Singleton);

        private void resolveObj(object o)
        {
            Type t = o.GetType();
            foreach (FieldInfo f in t.GetFields(ALL_INSTANCE))
            {
                if (f.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (f.FieldType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    f.SetValue(o, GetInstance(f.FieldType));
                }
            }
            foreach (PropertyInfo p in t.GetProperties(ALL_INSTANCE))
            {
                if (p.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (p.PropertyType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    p.SetValue(o, GetInstance(p.PropertyType));
                }
            }
            foreach (MethodInfo m in t.GetMethods(ALL_INSTANCE))
            {
                if (m.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    object[] param = new object[m.GetParameters().Count()];
                    int i = 0;
                    foreach (ParameterInfo v in m.GetParameters())
                    {
                        if (v.ParameterType == t)
                            throw new PerfusionException("Dependency loop in " + o.GetType());
                        param[i] = GetInstance(v.ParameterType);
                        i++;
                    }
                    m.Invoke(o, param);
                }
            }
        }

        public T GetInstance<T>() where T : class => (T)GetInstance(typeof(T));

        public object GetInstance(Type t)
        {
            if (objects.Any(x => x.Key.GetInterfaces().Contains(t)))
            {
                if (objects.Sum(x => (x.Key.GetInterfaces().Contains(t)) ? 1 : 0) > 1)
                    throw new PerfusionException("Many possible objects");
                t = objects.First(x => x.Key.GetInterfaces().Contains(t)).Key;
            }
            if (!objects.ContainsKey(t))
                throw new PerfusionException("Object of type " + t.FullName + " not found");
            if (objects[t].IsSingleton && objects[t].HasBeenInstantiated) return objects[t].Value;
            object o = objects[t].Factory();
            resolveObj(o);
            objects[t].HasBeenInstantiated = false;
            objects[t].Value = o;
            return o;
        }

        public Container()
        {
            AddInstance(this);
        }
    }

    public class ObjectInfo
    {
        public Func<object> Factory;
        public bool IsSingleton;
        public bool HasBeenInstantiated;
        public object Value;
    }

    public enum InjectionType
    {
        Singleton, Transient
    }
}
