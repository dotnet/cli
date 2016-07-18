// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class CleanPublishOutput : Task
    {
        [Required]
        public string Path { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool DeleteRuntimeConfigJson { get; set; }

        [Required]
        public bool DeleteDepsJson { get; set; }

        public override bool Execute()
        {
            PublishMutationUtilties.CleanPublishOutput(Path, Name, DeleteRuntimeConfigJson, DeleteDepsJson);

            return true;
        }
    }
}
