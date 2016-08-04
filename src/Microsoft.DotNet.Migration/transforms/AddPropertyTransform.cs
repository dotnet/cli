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

namespace Microsoft.DotNet.Migration.Transforms
{
    public abstract class AddPropertyTransform<T> : ConditionalTransform<T> 
    {
        private string _propertyName;
        private string _propertyValue;
        private Func<T,string> _propertyValueFunc;

        public AddPropertyTransform(string propertyName, string propertyValue, Func<T,Bool> condition)
            : base(condition)
        {
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        public AddPropertyTransform(string propertyName, Func<T, string> propertyValueFunc, Func<T,Bool> condition)
            : base(condition)
        {
            _propertyName = propertyName;
            _propertyValueFunc = propertyValueFunc;
        }

        public override void ConditionallyExecute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement=null)
        {
            ProjectPropertyGroupElement propertyGroup = (ProjectPropertyGroupElement) destinationElement 
                ?? destinationProject.AddPropertyGroup();

            string propertyValue = _propertyValue ?? _propertyValueFunc(source);

            propertyGroup.AddProperty(_propertyName, propertyValue);
        }
    }
}
