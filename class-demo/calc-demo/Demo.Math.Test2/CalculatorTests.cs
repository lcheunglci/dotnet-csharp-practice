using Demo.Math;

namespace Demo.Math.Test2
{
    public class Tests
    {
        [Test]
        public void Add2PositiveNumbers()
        {
            var result = Calculator.Add(40, 2);

            Assert.That(result, Is.EqualTo(42));
        }
    }
}
