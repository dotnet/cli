using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    internal static class CollectionExtensions
    {
        public static void MergeWith<T>(this ICollection<T> main, ICollection<T> incoming, Func<T, string> keyGenerator) where T : IMergeable<T>
        {
            var incomingDict = incoming.ToDictionary(keyGenerator);
            var mainDict = main.ToDictionary(keyGenerator);

            // merge with overlapping incoming items
            var commonKeys = incomingDict.Keys.Intersect(mainDict.Keys);
            foreach (var commonKey in commonKeys)
            {
                mainDict[commonKey].MergeWith(incomingDict[commonKey]);
            }

            // add new incoming items
            var newKeys = incomingDict.Keys.Except(mainDict.Keys);
            main.AddRange(newKeys.Select(k => incomingDict[k]));
        }
    }
}