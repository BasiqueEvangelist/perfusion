using System;
using PDI;

namespace PDITest
{
    static class Program
    {
        static Container c;

        static void Main(string[] args)
        {
            c = new Container();
            c.Add(new Random(), InjectionType.Transient);
            c.Add(() => new App(), InjectionType.Singleton);
            c.GetInstance<App>().Run();
        }
    }
}
