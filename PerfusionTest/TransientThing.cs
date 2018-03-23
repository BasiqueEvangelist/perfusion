using System;
using Perfusion;

namespace PerfusionTest
{
    public class TransientThing
    {
        [Inject]
        Random random;

        int? intint;
        public int GetInt()
        {
            if (!intint.HasValue) intint = random.Next();
            return intint.Value;
        }
    }
}