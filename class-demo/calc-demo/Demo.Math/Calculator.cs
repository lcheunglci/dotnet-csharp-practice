namespace Demo.Math
{
    public static class Calculator
    {
        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static string AsHex(int a)
        {

            var hex = a.ToString("X");
#if NET6_0_OR_GREATER
            return $"{hex} from .NET 6 or greater";
#elif NETFRAMEWORK
      return $"{hex} from .NET Framework";
#else
      return $"{hex} from .NET Standard 2.0";
#endif
        }

        public static void WriteAsHex(int a)
        {

            var hex = a.ToString("X");
#if NET6_0_OR_GREATER
            Console.WriteLine($"{hex} from .NET 6 or greater");
#elif NETFRAMEWORK
            System.Console.WriteLine($"{hex} from .NET Framework");
#elif NETSTANDARD2_0
            System.Console.WriteLine($"{hex} from .NET Standard 2.0");
#else
#error No TFM-specific code has been implemented
            // throw new System.PlatformNotSupportedException("This code should not be reached, as the preprocessor should prevent it.");
#endif
        }


        public static bool IsWriteAsHexSupported
        {
            get
            {
#if NET6_0_OR_GREATER || NETFRAMEWORK || NETSTANDARD2_0
                return true;
#else
                return false;
#endif

            }
        }

        internal static void SomeMethod()
        {

        }
    }

}