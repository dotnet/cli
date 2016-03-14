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
        private readonly string _copyDestPath;
 
        public string FscExePath { get { return _fscExePath; } }
        public string DepsFilePath { get { return _depsFilePath; } }

        public DepsCommandResolver()  
        {  
            var depsName = "dotnet-compile-fsc.deps";
            var originalDepsFilePath = Path.Combine(AppContext.BaseDirectory, depsName);

            using (var stream = File.OpenRead(originalDepsFilePath))  
            {  
                var depsReader = new DependencyContextCsvReader();  
                var deps = depsReader.Read(stream);  

                var fscPackageLibrary = deps.RuntimeLibraries.FirstOrDefault(l => l.Name.EndsWith("Microsoft.FSharp.Compiler.netcore"));
                
                var fscCopyLocalAssets = fscPackageLibrary.NativeAssets;

                var temp = Path.GetTempPath();
                _copyDestPath = Path.Combine(temp, Guid.NewGuid().ToString());
                Directory.CreateDirectory(_copyDestPath);
                

                foreach (var asset in fscCopyLocalAssets)
                {   
                    var assetPath = Path.Combine(_packageRoot, fscPackageLibrary.Name, fscPackageLibrary.Version, asset);
                    var destPath = Path.Combine(_copyDestPath, Path.GetFileName(asset));
                    
                    File.Copy(assetPath,destPath);
                }

                _depsFilePath = Path.Combine(_copyDestPath, Path.GetFileName(originalDepsFilePath));
                File.Copy(originalDepsFilePath, _depsFilePath);

                var fscFileName = Path.GetFileName(
                    fscCopyLocalAssets.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "fsc"));
            
                _fscExePath = Path.Combine(_copyDestPath, fscFileName);
            }
        }

        public void Cleanup()
        {
            try
            {
                Directory.Delete(_copyDestPath, true);
            }
            catch { }
        }
    }
}
