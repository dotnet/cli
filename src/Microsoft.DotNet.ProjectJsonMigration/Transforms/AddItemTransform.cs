// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration.Models;

namespace Microsoft.DotNet.ProjectJsonMigration.Transforms
{
    internal class AddItemTransform<T> : ConditionalTransform<T, ProjectItemElement>
    {
        private readonly ProjectRootElement _itemObjectGenerator = ProjectRootElement.Create();

        private readonly string _itemName;
        private readonly string _includeValue;
        private readonly string _excludeValue;

        private readonly Func<T, string> _includeValueFunc;
        private readonly Func<T, string> _excludeValueFunc;
        private readonly Func<T, string> _updateValueFunc;

        private readonly List<ItemMetadataValue<T>> _metadata = new List<ItemMetadataValue<T>>();

        public AddItemTransform(
            string itemName,
            IEnumerable<string> includeValues,
            IEnumerable<string> excludeValues,
            Func<T, bool> condition)
            : this(
                itemName,
                string.Join(";", includeValues),
                string.Join(";", excludeValues),
                condition) { }

        public AddItemTransform(
            string itemName,
            Func<T, string> includeValueFunc,
            Func<T, string> excludeValueFunc,
            Func<T, string> updateValueFunc,
            Func<T, bool> condition)
            : base(condition)
        {
            _itemName = itemName;
            _includeValueFunc = includeValueFunc;
            _excludeValueFunc = excludeValueFunc;
            _updateValueFunc = updateValueFunc;
        }

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

        public AddItemTransform<T> WithMetadata(
            string metadataName,
            string metadataValue,
            bool expressedAsAttribute = false)
        {
            _metadata.Add(new ItemMetadataValue<T>(
                metadataName,
                metadataValue,
                expressedAsAttribute: expressedAsAttribute));
            return this;
        }

        public AddItemTransform<T> WithMetadata(
            string metadataName, 
            Func<T, string> metadataValueFunc, 
            Func<T, bool> writeMetadataConditionFunc = null,
            bool expressedAsAttribute = false)
        {
            _metadata.Add(new ItemMetadataValue<T>(
                metadataName,
                metadataValueFunc,
                writeMetadataConditionFunc,
                expressedAsAttribute: expressedAsAttribute));
            return this;
        }

        public AddItemTransform<T> WithMetadata(ItemMetadataValue<T> metadata)
        {
            _metadata.Add(metadata);
            return this;
        }

        public override ProjectItemElement ConditionallyTransform(T source)
        {
            string includeValue = _includeValue ?? _includeValueFunc(source);
            string excludeValue = _excludeValue ?? _excludeValueFunc(source);
            string updateValue = _updateValueFunc != null ? _updateValueFunc(source) : null;

            var item = _itemObjectGenerator.AddItem(_itemName, "placeholder");
            item.Include = includeValue;
            item.SetExcludeOnlyIfIncludeIsSet(excludeValue);
            item.Update = updateValue;

            foreach (var metadata in _metadata)
            {
                if (metadata.ShouldWriteMetadata(source))
                {
                    var metametadata = item.AddMetadata(metadata.MetadataName, metadata.GetMetadataValue(source));
                    metametadata.Condition = metadata.Condition;
                    metametadata.ExpressedAsAttribute = metadata.ExpressedAsAttribute;
                }
            }

            return item;
        }
    }
}
