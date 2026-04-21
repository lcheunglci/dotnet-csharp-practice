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
  }

}