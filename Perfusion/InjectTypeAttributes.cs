namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class SingletonAttribute : System.Attribute
    {
        public SingletonAttribute() { }
    }
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class TransientAttribute : System.Attribute
    {
        public TransientAttribute() { }
    }
}