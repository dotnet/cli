// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class AggregateTransform<T> : ITransform<T> 
    {
        protected ITransform<T>[] _transforms;

        public AggregateTransform(params ITransform<T>[] transforms)
        {
            _transforms = transforms;
        }

        public void Execute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement=null)
        {
            foreach (var transform in _transforms)
            {
                transform.Execute(source, destinationProject, destinationElement);
            }
        }
    }
}
