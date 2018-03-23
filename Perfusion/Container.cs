using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perfusion
{
    public class Container
    {
        Dictionary<Type, ObjectInfo> objects = new Dictionary<Type, ObjectInfo>();


        public void Add<T>(Func<T> F, InjectionType type) where T : class
        {
            if (type != InjectionType.Singleton && type != InjectionType.Transient) throw new PerfusionException("Invalid injection type " + type);
            objects.Add(typeof(T), new ObjectInfo()
            {
                Factory = F,
                IsSingleton = type == InjectionType.Singleton,
                HasBeenInstantiated = false
            });
        }

        public void Add<T>(T f, InjectionType type) where T : class => Add(() => f, type);

        private void resolveObj(object o)
        {
            foreach (FieldInfo f in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (f.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (f.FieldType == o.GetType())
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    f.SetValue(o, GetInstance(f.FieldType));
                }
            }
            foreach (PropertyInfo p in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (p.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (p.PropertyType == o.GetType())
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    p.SetValue(o, GetInstance(p.PropertyType));
                }
            }
            foreach (MethodInfo m in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (m.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    object[] param = new object[m.GetParameters().Count()];
                    int i = 0;
                    foreach (ParameterInfo v in m.GetParameters())
                    {
                        if (v.ParameterType == o.GetType())
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
            Add(this, InjectionType.Singleton);
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
