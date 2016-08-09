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

namespace Microsoft.DotNet.ProjectJsonMigration.Transforms
{
    public abstract class ConditionalTransform<T> : ITransform<T> 
    {
        private Func<T,bool> _condition;

        public ConditionalTransform(Func<T,bool> condition)
        {
            _condition = condition;
        }

        public void Execute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement=null)
        {
            if (_condition(source))
            {
                ConditionallyExecute(source, destinationProject, destinationElement);
            }
        }

        public abstract void ConditionallyExecute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement=null);
    }
}
