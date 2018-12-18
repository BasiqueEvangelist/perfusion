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

        public void Add<TContract>(Func<TContract> F, InjectionType type = InjectionType.Infer) where TContract : class
        {
            if (type == InjectionType.Infer) type = guessInjectionType(typeof(TContract));
            if (type != InjectionType.Singleton && type != InjectionType.Transient) throw new PerfusionException("Invalid injection type " + type);
            objects.Add(typeof(TContract), new ObjectInfo()
            {
                Factory = F,
                IsSingleton = type == InjectionType.Singleton,
                HasBeenInstantiated = false
            });
        }
        public void Add(Type t, Func<object> F, InjectionType type = InjectionType.Infer)
        {
            if (type == InjectionType.Infer) type = guessInjectionType(t);
            if (type != InjectionType.Singleton && type != InjectionType.Transient) throw new PerfusionException("Invalid injection type " + type);
            objects.Add(t, new ObjectInfo()
            {
                Factory = F,
                IsSingleton = type == InjectionType.Singleton,
                HasBeenInstantiated = false
            });
        }
        public void Add(Type t)
        {
            if (!tryAddGuessing(t))
            {
                throw new PerfusionException("Type not able to be guessed: " + t);
            }
        }

        public void AddInstance<TContract>(TContract f) where TContract : class => Add(() => f, InjectionType.Singleton);

        public T ResolveObject<T>(T o)
        {
            Type t = o.GetType();
            foreach (FieldInfo f in t.GetFields(ALL_INSTANCE))
            {
                if (f.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (f.FieldType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    if (!objects.ContainsKey(f.FieldType))
                        throw new PerfusionException("Object of type " + f.FieldType.FullName + " not found");
                    bool required = (bool)f.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                    f.SetValue(o, GetInstance(f.FieldType, required));
                }
            }
            foreach (PropertyInfo p in t.GetProperties(ALL_INSTANCE))
            {
                if (p.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (p.PropertyType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
                    if (!objects.ContainsKey(p.PropertyType))
                        throw new PerfusionException("Object of type " + p.PropertyType.FullName + " not found");
                    bool required = (bool)p.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                    p.SetValue(o, GetInstance(p.PropertyType, required));
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
                        if (!objects.ContainsKey(v.ParameterType))
                            throw new PerfusionException("Object of type " + v.ParameterType.FullName + " not found");
                        bool required = (bool)v.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                        param[i] = GetInstance(v.ParameterType, required);
                        i++;
                    }
                    m.Invoke(o, param);
                }
            }
            return o;
        }

        private object buildWithConstructor(Type t, ConstructorInfo c)
        {
            object[] paramlist = new object[c.GetParameters().Length];
            for (int i = 0; i < paramlist.Length; i++)
            {
                ParameterInfo p = c.GetParameters()[i];
                if (p.ParameterType == t)
                    throw new PerfusionException("Dependency loop in " + t.GetType());
                bool required = (bool)p.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
                paramlist[i] = GetInstance(p.ParameterType, required);
            }
            object created = Activator.CreateInstance(t, paramlist);
            ResolveObject(created);
            return created;
        }

        public T GetInstance<T>() where T : class => (T)GetInstance(typeof(T));
        private InjectionType guessInjectionType(Type t)
        {
            if (t.CustomAttributes.Any(x => x.AttributeType == typeof(SingletonAttribute)) &&
                t.CustomAttributes.Any(x => x.AttributeType == typeof(TransientAttribute)))
            {
                throw new PerfusionException("[Singleton] and [Transient] collide in " + t);
            }
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(SingletonAttribute)))
                return InjectionType.Singleton;
            else if (t.CustomAttributes.Any(x => x.AttributeType == typeof(TransientAttribute)))
                return InjectionType.Transient;
            else return InjectionType.Singleton;
        }
        private bool tryAddGuessing(Type t)
        {
            if (t.GetTypeInfo().DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
            {
                Add(t, () => Activator.CreateInstance(t));
                return true;
            }
            else
            {
                foreach (ConstructorInfo ci in t.GetTypeInfo().DeclaredConstructors)
                {
                    if (ci.GetParameters().All(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(InjectAttribute))))
                    {
                        Add(t, () => buildWithConstructor(t, ci));
                        return true;
                    }
                }
                return false;
            }
        }
        public object GetInstance(Type t, bool required = true)
        {
            if (!required)
            {
                try
                {
                    return GetInstance(t);
                }
                catch (PerfusionException)
                {
                    return null;
                }
            }
            var possibleImplementors = objects.Where(x => x.Key.GetInterfaces().Concat(GetHierarchy(x.Key)).Contains(t)).ToArray();

            if (possibleImplementors.Length > 1)
                throw new PerfusionException("Many possible implementors: " + string.Join(", ", possibleImplementors));
            if (possibleImplementors.Length == 0)
            {
                if (!t.IsAbstract && !t.IsInterface)
                {
                    if (!tryAddGuessing(t))
                    {
                        throw new PerfusionException("Type not able to be guessed: " + t);
                    }
                    else
                    {
                        return GetInstance(t); //use recursion
                    }
                }
                else
                {
                    throw new PerfusionException("Object implementing " + t.FullName + " not found");
                }
            }
            Type impl = possibleImplementors[0].Key;
            if (objects[impl].IsSingleton && objects[impl].HasBeenInstantiated) return objects[impl].Value;
            object o = objects[impl].Factory();
            ResolveObject(o);
            objects[impl].HasBeenInstantiated = true;
            objects[impl].Value = o;
            return o;
        }

        public Container()
        {
            AddInstance(this);
        }

        #region service 

        IEnumerable<Type> GetHierarchy(Type T)
        {
            for (; T != null; T = T.BaseType)
                yield return T;
        }

        #endregion
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
        Infer, Singleton, Transient
    }

}
