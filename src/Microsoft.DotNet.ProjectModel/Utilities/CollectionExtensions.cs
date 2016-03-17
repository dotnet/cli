using Microsoft.DotNet.ProjectModel.Compilation;
using System.Linq;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static IEnumerable<LibraryAsset> GetGroup(this IEnumerable<LibraryAssetGroup> self, string runtime)
        {
            return self
                .Where(a => string.Equals(a.Runtime, runtime, StringComparison.Ordinal))
                .SelectMany(a => a.Assets);
        }
    }
}
