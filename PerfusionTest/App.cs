using System;
using Perfusion;

namespace PerfusionTest
{
    public class App
    {
        [Inject]
        Random random;
        [Inject]
        Container c;

        [Inject]
        Container cc
        {
            set
            {
                Console.WriteLine("set, " + value);
            }
        }

        public void Run()
        {
            Console.WriteLine("This is after dependency injection, as I can use Random: " + random.Next());
            Random r = c.GetInstance<Random>();
            Console.WriteLine("I got a random, does it work? " + r.Next());
            Console.WriteLine("And is it equal to the previous one? " + (r == random).ToString());
            c.Add(() => new TransientThing(), InjectionType.Transient);
            TransientThing t = c.GetInstance<TransientThing>();
            Console.WriteLine(t.GetInt());
            Console.WriteLine(t.GetInt());
            t = c.GetInstance<TransientThing>();
            Console.WriteLine(t.GetInt());
            Console.WriteLine(t.GetInt());
            t = c.GetInstance<TransientThing>();
            Console.WriteLine(t.GetInt());
            Console.WriteLine(t.GetInt());
        }
        [Inject]
        void injectmethod(Container c, Random r)
        {
            Console.WriteLine("Here's a container: " + c);
            Console.WriteLine("And a random: " + r);
        }
    }
}