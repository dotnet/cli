using System.Linq;

namespace AutoAddDesktopReferencesDuringMigrate
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Collections.Generic.List<int> mscorlib_ref = new System.Collections.Generic.List<int>(new int[] { 4, 5, 6 });
            double system_core_ref = mscorlib_ref.ToArray().Average();
            System.Diagnostics.Debug.Assert(system_core_ref == 5, "Test System assembly reference");
            if (system_core_ref != 5)
            {
                throw new Microsoft.CSharp.RuntimeBinder.RuntimeBinderException("Test Microsoft.CSharp assembly reference");
            }
        }
    }
}
