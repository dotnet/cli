using System;
using System.IO;
using System.Diagnostics;

namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(TestLibrary.Helper.GetMessage());

            var filePath = Path.Combine(AppContext.BaseDirectory, "TestFile.txt");
            if (File.Exists(filePath))
            {
                Console.WriteLine(File.ReadAllText(filePath));
            }
        }
    }
}
