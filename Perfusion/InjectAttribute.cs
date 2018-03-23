namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : System.Attribute
    {
        public InjectAttribute() { }

        public bool HasBeenResolved;
    }
}