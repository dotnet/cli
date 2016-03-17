using Microsoft.Extensions.DependencyModel;
using System.Linq;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static IEnumerable<RuntimeAsset> GetGroup(this IEnumerable<RuntimeAssetGroup> self, string runtime)
        {
            return self
                .Where(a => string.Equals(a.Runtime, runtime, StringComparison.Ordinal))
                .SelectMany(a => a.Assets);
        }
    }
}
