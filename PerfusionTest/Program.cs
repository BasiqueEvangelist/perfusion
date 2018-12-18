using System;
using Perfusion;

namespace PerfusionTest
{
    static class Program
    {
        static Container c;

        static void Main(string[] args)
        {
            c = new Container();
            c.GetInstance<App>().Run();
        }
    }
}
