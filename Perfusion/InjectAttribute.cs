namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : System.Attribute
    {
        public InjectAttribute() { }

        public bool HasBeenResolved;
    }
}