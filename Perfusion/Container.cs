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

        public void AddSingleton<TContract>(Func<TContract> F) where TContract : class
        {
            AddSingleton(typeof(TContract), F);
        }
        public void AddSingleton(Type t, Func<object> F)
        {
            objects.Add(t, new SingletonInfo(F));
        }
        public void AddTransient<TContract>(Func<TContract> F) where TContract : class
        {
            AddTransient(typeof(TContract), F);
        }
        public void AddTransient(Type t, Func<object> F)
        {
            objects.Add(t, new TransientInfo(F));
        }
        public void Add(Type t)
        {
            if (!tryAddGuessing(t))
            {
                throw new PerfusionException("Type not able to be guessed: " + t);
            }
        }

        public void Add<T>() => Add(typeof(T));

        public void AddInstance<TContract>(TContract f) where TContract : class => AddSingleton(() => f);

        public T ResolveObject<T>(T o)
        {
            Type t = o.GetType();
            foreach (FieldInfo f in t.GetFields(ALL_INSTANCE))
            {
                if (f.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                {
                    if (f.FieldType == t)
                        throw new PerfusionException("Dependency loop in " + o.GetType());
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
                        bool required = (bool)m.CustomAttributes.First(x => x.AttributeType == typeof(InjectAttribute)).ConstructorArguments[0].Value;
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
                paramlist[i] = GetInstance(p.ParameterType);
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
                InjectionType it = guessInjectionType(t);
                switch (it)
                {
                    case InjectionType.Singleton:
                        AddSingleton(t, () => Activator.CreateInstance(t));
                        break;
                    case InjectionType.Transient:
                        AddTransient(t, () => Activator.CreateInstance(t));
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return true;
            }
            else
            {
                foreach (ConstructorInfo ci in t.GetTypeInfo().DeclaredConstructors)
                {
                    if (ci.CustomAttributes.Any(x => x.AttributeType == typeof(InjectAttribute)))
                    {
                        InjectionType it = guessInjectionType(t);
                        switch (it)
                        {
                            case InjectionType.Singleton:
                                AddSingleton(t, () => buildWithConstructor(t, ci));
                                break;
                            case InjectionType.Transient:
                                AddTransient(t, () => buildWithConstructor(t, ci));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
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
            KeyValuePair<Type, ObjectInfo>[] possibleImplementors = objects.Where(x => x.Key.GetInterfaces().Concat(GetHierarchy(x.Key)).Contains(t)).ToArray();

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
            return ResolveObject(possibleImplementors[0].Value.GetInstance());
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

    public abstract class ObjectInfo
    {
        public Func<object> Factory;
        public abstract InjectionType Type { get; }
        public abstract object GetInstance();
    }
    public class SingletonInfo : ObjectInfo
    {
        public override InjectionType Type => InjectionType.Singleton;
        public bool IsInstantiated = false;
        public object Value;
        public override object GetInstance()
        {
            if (!IsInstantiated)
            {
                IsInstantiated = true;
                return Value = Factory();
            }
            else
                return Value;
        }
        public SingletonInfo(Func<Object> factory)
        {
            Factory = factory;
        }
    }
    public class TransientInfo : ObjectInfo
    {
        public override InjectionType Type => InjectionType.Transient;
        public override object GetInstance() => Factory();
        public TransientInfo(Func<Object> factory)
        {
            Factory = factory;
        }
    }

    public enum InjectionType
    {
        Infer, Singleton, Transient
    }

}
