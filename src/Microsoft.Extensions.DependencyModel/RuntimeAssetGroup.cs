// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeAssetGroup
    {
        public RuntimeAssetGroup(string runtime, params RuntimeAsset[] assets) : this(runtime, (IEnumerable<RuntimeAsset>)assets) { }
        public RuntimeAssetGroup(params RuntimeAsset[] assets) : this(string.Empty, (IEnumerable<RuntimeAsset>)assets) { }
        public RuntimeAssetGroup(IEnumerable<RuntimeAsset> assets) : this(string.Empty, assets) { }

        public RuntimeAssetGroup(string runtime,
            IEnumerable<RuntimeAsset> assets)
        {
            Runtime = runtime;
            Assets = assets.ToArray();
        }

        /// <summary>
        /// The runtime ID associated with this group (may be empty if the group is runtime-agnostic)
        /// </summary>
        public string Runtime { get; }

        /// <summary>
        /// Gets a list of assets provided in this runtime group
        /// </summary>
        public IReadOnlyList<RuntimeAsset> Assets { get; }
    }
}