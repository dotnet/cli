using Microsoft.Extensions.DependencyModel;  
using Microsoft.DotNet.ProjectModel.Resolution;  
using System;  
using System.IO;  
using System.Linq;  

namespace Microsoft.DotNet.Tools.Compiler.Fsc
{
    public class DepsCommandResolver  
    {  
        private string _packageRoot = PackageDependencyProvider.ResolvePackagesPath(null, null);  
        private readonly string _fscExePath;
        private readonly string _depsFilePath;
 
        public string FscExePath { get { return _fscExePath; } }
        public string DepsFilePath { get { return _depsFilePath; } }

        public DepsCommandResolver()  
        {  
            var depsName = "dotnet-compile-fsc.deps";
            var _depsFilePath = Path.Combine(AppContext.BaseDirectory, depsName);

            using (var stream = File.OpenRead(_depsFilePath))  
            {  
                var depsReader = new DependencyContextCsvReader();  
                var deps = depsReader.Read(stream);  

                var fscPackageLibrary = deps.RuntimeLibraries.FirstOrDefault(l => l.Name.EndsWith("Microsoft.FSharp.Compiler.netcore"));
                
                var fscCopyLocalAssets = fscPackageLibrary.NativeAssets;

                foreach (var asset in fscCopyLocalAssets)
                {   
                    var assetPath = Path.Combine(_packageRoot, fscPackageLibrary.Name, fscPackageLibrary.Version, asset);
                    var destPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(asset));
                    try 
                    {
                        File.Copy(assetPath,destPath);
                    }
                    catch {}
                }

                var fscFileName = Path.GetFileName(
                        fscCopyLocalAssets
                            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "fsc"));

                _fscExePath = Path.Combine(AppContext.BaseDirectory, fscFileName);
            }
        }
    }
}
