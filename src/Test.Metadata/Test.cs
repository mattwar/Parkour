#nullable disable

namespace Test.Metadata
{
    public class Test
    {
        public Generic<int>.Nested<string> Field;
    }

    public class Generic<T>
    {
        public class Nested<S>
        {
        }
    }
}

