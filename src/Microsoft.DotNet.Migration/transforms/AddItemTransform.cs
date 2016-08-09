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
using Microsoft.DotNet.Migration.Models;

namespace Microsoft.DotNet.Migration.Transforms
{
    public class AddItemTransform<T> : ConditionalTransform<T>
    {
        private string _itemName;
        private string _includeValue;
        private string _excludeValue;

        private Func<T, string> _includeValueFunc;
        private Func<T, string> _excludeValueFunc;

        private List<ItemMetadataValue<T>> _metadata = new List<ItemMetadataValue<T>>();

        public AddItemTransform(
            string itemName,
            IEnumerable<string> includeValues,
            IEnumerable<string> excludeValues,
            Func<T, bool> condition)
            : this(itemName, string.Join(";", includeValues), string.Join(";", excludeValues), condition) { }

        public AddItemTransform(
            string itemName,
            Func<T, string> includeValueFunc,
            Func<T, string> excludeValueFunc,
            Func<T, bool> condition)
            : base(condition)
        {
            _itemName = itemName;
            _includeValueFunc = includeValueFunc;
            _excludeValueFunc = excludeValueFunc;
        }

        public AddItemTransform(
            string itemName,
            string includeValue,
            Func<T, string> excludeValueFunc,
            Func<T, bool> condition)
            : base(condition)
        {
            _itemName = itemName;
            _includeValue = includeValue;
            _excludeValueFunc = excludeValueFunc;
        }

        public AddItemTransform(
            string itemName,
            Func<T, string> includeValueFunc,
            string excludeValue,
            Func<T, bool> condition)
            : base(condition)
        {
            _itemName = itemName;
            _includeValueFunc = includeValueFunc;
            _excludeValue = excludeValue;
        }

        public AddItemTransform(
            string itemName,
            string includeValue,
            string excludeValue,
            Func<T, bool> condition)
            : base(condition)
        {
            _itemName = itemName;
            _includeValue = includeValue;
            _excludeValue = excludeValue;
        }

        public AddItemTransform<T> WithMetadata(string metadataName, string metadataValue)
        {
            _metadata.Add(new ItemMetadataValue<T>(metadataName, metadataValue));
            return this;
        }

        public AddItemTransform<T> WithMetadata(string metadataName, Func<T, string> metadataValueFunc)
        {
            _metadata.Add(new ItemMetadataValue<T>(metadataName, metadataValueFunc));
            return this;
        }

        public AddItemTransform<T> WithMetadata(ItemMetadataValue<T> metadata)
        {
            _metadata.Add(metadata);
            return this;
        }

        public override void ConditionallyExecute(T source, ProjectRootElement destinationProject, ProjectElement destinationElement = null)
        {
            ProjectItemGroupElement itemGroup = (ProjectItemGroupElement)destinationElement
                ?? destinationProject.AddItemGroup();

            string includeValue = _includeValue ?? _includeValueFunc(source);
            string excludeValue = _excludeValue ?? _excludeValueFunc(source);

            var item = itemGroup.AddItem(_itemName, includeValue);
            item.Exclude = excludeValue;

            foreach (var metadata in _metadata)
            {
                item.AddMetadata(metadata.MetadataName, metadata.GetMetadataValue(source));
            }
        }
    }
}
