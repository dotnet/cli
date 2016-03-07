using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.Cli.Utils
{
    public class DotNetProjectCache
    {
        public static LockFileReaderCache LockFileReaderCache { get; } = new LockFileReaderCache();
        public static ProjectContextCache ProjectContextCache { get; } = new ProjectContextCache(LockFileReaderCache);
    }
}
