using System;
using System.Xml;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello World!");
#if !NETSTANDARDAPP1_5
            // Force XmlDocument to be used
            var doc = new XmlDocument();
#endif
        }
    }
}
