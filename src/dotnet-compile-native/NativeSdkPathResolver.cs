using Microsoft.Extensions.DependencyModel;
using Microsoft.DotNet.ProjectModel.Resolution;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public class NativeSdkPathResolver
    {
        private string _packageRoot = PackageDependencyProvider.ResolvePackagesPath(null, null);
        private readonly string _ilcRootPath;
        private readonly string _ilcSdkRootPath;
        private readonly string _appDepsRootPath;
        
        public string IlcRootPath{ get { return _ilcRootPath; } }
        public string IlcSdkRootPath{ get { return _ilcSdkRootPath; } }
        public string AppDepsRootPath{ get { return _appDepsRootPath; } }
        
        public NativeSdkPathResolver()
        {
            using (var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "dotnet-compile-native.deps")))
            {
	           var depsReader = new DependencyContextCsvReader();
               var deps = depsReader.Read(stream);
               
               var ilcLibrary = deps.RuntimeLibraries.FirstOrDefault(l => l.PackageName.EndsWith("Microsoft.DotNet.ILCompiler"));
               var ilcExeAssembly = ilcLibrary.Assemblies.FirstOrDefault(a => a.Name.Name.Equals("ilc"));
               _ilcRootPath = Path.GetDirectoryName(Path.Combine(_packageRoot, ilcLibrary.PackageName, ilcLibrary.Version, ilcExeAssembly.Path));
               
               Console.WriteLine(_ilcRootPath);
               var appDepPkgName = ilcLibrary.PackageName.Substring(0, ilcLibrary.PackageName.Length - 10) + "AppDep";
               var version = ilcLibrary.Version;
               _appDepsRootPath = Path.Combine(_packageRoot, appDepPkgName, ilcLibrary.Version);
               
               Console.WriteLine(_appDepsRootPath);
               var ilcSdkPackageName = ilcLibrary.PackageName + ".SDK";
               var ilcSdkPackageRuntimesRootPath = Path.Combine(_packageRoot, ilcSdkPackageName, ilcLibrary.Version, "runtimes");
               var runtimeName = Directory.EnumerateDirectories(ilcSdkPackageRuntimesRootPath).First();
               _ilcSdkRootPath = Path.Combine(ilcSdkPackageRuntimesRootPath, runtimeName, "native");
               Console.WriteLine(_ilcSdkRootPath);
            }
        }
    }
}