using System;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.Tools.DependencyInvoker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if(DependencyContext.Default != null)
            {
                Console.WriteLine("DependencyContext.Default is set!");
            }
            else
            {
                Console.Error.WriteLine("DependencyContext.Default is NULL!");
                return 1;
            }

            if(DependencyContext.Default.RuntimeGraph.Any())
            {
                Console.WriteLine("DependencyContext.Default.RuntimeGraph has items!");
            }
            else
            {
                Console.WriteLine("DependencyContext.Default.RuntimeGraph is empty!");
                return 1;
            }
        }
    }
}
