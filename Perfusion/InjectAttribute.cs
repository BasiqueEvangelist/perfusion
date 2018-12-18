namespace Perfusion
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : System.Attribute
    {
        public InjectAttribute(bool required) { Required = required; }

        public bool Required { get; }
    }
}