namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : System.Attribute
    {
        public InjectAttribute() { }

    }
}