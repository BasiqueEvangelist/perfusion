using System;
using System.Linq;
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
        public void ScopedTest()
        {
            Container c = new Container();
            c.AddScoped(() => new GuessableType());
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.Same(o, anothero);
        }
        [Fact]
        public void PoolableTest()
        {
            Container c = new Container();
            c.AddPoolable(() => new GuessableType(), 2);
            object o = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(o);
            Assert.NotNull(o);
            object anothero = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(anothero);
            Assert.NotNull(anothero);
            Assert.NotSame(o, anothero);
            object thirdo = c.GetInstance(typeof(GuessableType));
            Assert.IsType<GuessableType>(thirdo);
            Assert.NotNull(thirdo);
            Assert.True(object.ReferenceEquals(thirdo, o) || object.ReferenceEquals(thirdo, anothero));
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
        [Fact]
        public void AddInfoTest()
        {
            Container c = new Container();
            ObjectInfo oi = new SingletonInfo(() => null);
            c.AddInfo<GuessableType>(oi);
            Assert.Contains(oi, c.RegisteredObjects.Values);
        }
        public class PropInfo : ObjectInfo
        {
            public Type SavedValue;

            public override ObjectInfo Clone() => throw new NotImplementedException("What");

            public override object GetInstance(IContainer c, Type requester = null)
            {
                SavedValue = requester;
                return c.ResolveObject(new GuessableType());
            }
        }
        public class TrashClass { }
        [Fact]
        public void UserTypePropagationTest()
        {
            Container c = new Container();
            PropInfo oi = new PropInfo();
            Type t = typeof(TrashClass);
            c.AddInfo<GuessableType>(oi);
            c.GetInstance<GuessableType>(requester: t);
            Assert.Equal(t, oi.SavedValue);
        }
        [Fact]
        public void GetInstancesTest()
        {
            Container c = new Container();
            GuessableType gt = new GuessableType();
            TransientGuessableType tgt = new TransientGuessableType();
            c.AddInstance(gt);
            c.AddInstance<TransientGuessableType>(tgt);
            object[] arr = c.GetInstances<IGuessableType>().ToArray();
            Assert.NotEmpty(arr);
            Assert.Equal(2, arr.Length);
            Assert.Contains(gt, arr);
            Assert.Contains(tgt, arr);
        }
    }
}