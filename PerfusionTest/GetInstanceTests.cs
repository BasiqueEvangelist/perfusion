using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class GetInstanceTests
    {
        class GuessableType : AGuessableType, IGuessableType
        {
            public GuessableType() { }
        }
        [Transient]
        class TransientGuessableType : GuessableType
        {
            public TransientGuessableType() { }
        }
        interface IGuessableType { }
        abstract class AGuessableType { }
        class GuessableTypeWithConstructor
        {
            [Inject]
            public GuessableTypeWithConstructor(GuessableType gt)
            {
                Assert.NotNull(gt);
            }
        }
        [Fact]
        public void SingletonTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new GuessableType());
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.Same(o, anothero);
        }
        [Fact]
        public void TransientTest()
        {
            Container c = new Container();
            c.AddTransient(() => new GuessableType());
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.NotSame(o, anothero);
        }
        [Fact]
        public void InterfaceTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new GuessableType());
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(IGuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.Same(o, anothero);
        }
        [Fact]
        public void SuperclassTest()
        {
            Container c = new Container();
            c.AddSingleton(() => new GuessableType());
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(AGuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.Same(o, anothero);
        }
    }
}