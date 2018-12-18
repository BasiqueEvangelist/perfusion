using Perfusion;
using Xunit;

namespace PerfusionTest
{
    public class PerfusionTests
    {
        [Fact]
        public void GuessType()
        {
            Container c = new Container();
            object gt = c.GetInstance(typeof(GuessableType));
            Assert.NotNull(gt);
            Assert.IsType<GuessableType>(gt);
        }
        [Fact]
        public void GuessTypeWithConstructor()
        {
            Container c = new Container();
            object gt = c.GetInstance(typeof(GuessableTypeWithConstructor));
            Assert.NotNull(gt);
            Assert.IsType<GuessableTypeWithConstructor>(gt);
        }
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
            public GuessableTypeWithConstructor([Inject]GuessableType gt)
            {
                Assert.NotNull(gt);
            }
        }
        [Fact]
        public void SingletonTest()
        {
            Container c = new Container();
            c.Add(() => new GuessableType(), InjectionType.Singleton);
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
            c.Add(() => new GuessableType(), InjectionType.Transient);
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
            c.Add(() => new GuessableType(), InjectionType.Singleton);
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
            c.Add(() => new GuessableType(), InjectionType.Singleton);
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(AGuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.Same(o, anothero);
        }
        [Fact]
        public void GuessInjectionTypeTest()
        {
            Container c = new Container();
            object o = c.GetInstance(typeof(TransientGuessableType));
            Assert.IsType<TransientGuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(TransientGuessableType));
            Assert.IsType<TransientGuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.NotSame(o, anothero);
        }
    }
}