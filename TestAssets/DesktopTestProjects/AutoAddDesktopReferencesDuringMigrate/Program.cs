using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace AutoAddDesktopReferencesDuringMigrate
{
    class Program
    {
        static void Main(string[] args)
        {
            var mscorlib_ref = new List<int>(new int[] { 4, 5, 6 });
            var system_core_ref = mscorlib_ref.ToArray().Average();
            Debug.Assert(system_core_ref == 5, "Test System assembly reference");
            if (system_core_ref != 5)
            {
                throw new RuntimeBinderException("Test Microsoft.CSharp assembly reference");
            }
        }
    }
}
