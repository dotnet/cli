using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenAnAddItemTransform
    {
        [Fact]
        public void It_adds_items_with_Include_Exclude_and_Metadata_to_project_when_condition_is_true()
        {
            var mockProj = ProjectRootElement.Create();
            var itemGroup = mockProj.AddItemGroup();

            var itemTransforms = GetFullItemTransformSet(true);

            foreach (var transform in itemTransforms)
            {
                transform.Execute("_", mockProj, itemGroup);
            }

            for (int i = 1; i <= itemTransforms.Length; ++i)
            {
                var itemName = FullItemTransformSetItemNamePrefix + i.ToString();
                var item = itemGroup.Items.Where(it => it.ItemType == itemName).First();

                item.Should().NotBeNull();
                item.Include.Should().Be(FullItemTransformSetIncludeValue);
                item.Exclude.Should().Be(FullItemTransformSetExcludeValue);

                item.HasMetadata.Should().BeTrue();

                var metadata = item.Metadata.First();
                metadata.Name.Should().Be(FullItemTransformSetMetadataName);
                metadata.Value.Should().Be(FullItemTransformSetMetadataValue);
            }
        }

        [Fact]
        public void It_doesnt_add_items_when_condition_is_false()
        {
            var mockProj = ProjectRootElement.Create();
            var itemGroup = mockProj.AddItemGroup();

            var itemTransforms = GetFullItemTransformSet(false);

            foreach (var transform in itemTransforms)
            {
                transform.Execute("_", mockProj, itemGroup);
            }

            itemGroup.Items.Count().Should().Be(0);
        }

        [Fact]
        public void It_merges_Metadata_and_Exclude_with_items_with_same_ItemType_and_Include_when_mergeExisting_is_true()
        {
            var metadata = new ItemMetadataValue<string>[]
            {
                 new ItemMetadataValue<string>("metadata1", "value1"),
                 new ItemMetadataValue<string>("metadata2", "value2")
            };

            var transform1 = new AddItemTransform<string>("item",
                    FullItemTransformSetIncludeValue,
                    "exclude1",
                    t => true,
                    mergeExisting: true)
                    .WithMetadata(metadata[0]);

            var transform2 = new AddItemTransform<string>("item",
                    FullItemTransformSetIncludeValue,
                    "exclude2",
                    t => true,
                    mergeExisting: true)
                    .WithMetadata(metadata[1]);

            var mockProj = ProjectRootElement.Create();
            var itemGroup = mockProj.AddItemGroup();

            transform1.Execute("_", mockProj, itemGroup);
            transform2.Execute("_", mockProj, itemGroup);

            itemGroup.Items.Count.Should().Be(1);

            var item = itemGroup.Items.First();
            item.Exclude.Should().Be("exclude1;exclude2");

            item.Metadata.Count.Should().Be(2);
            var foundMetadata = metadata.ToDictionary<ItemMetadataValue<string>, string, bool>(m => m.MetadataName, m => false);

            foreach (var metadataEntry in item.Metadata)
            {
                foundMetadata.Should().ContainKey(metadataEntry.Name);
                foundMetadata[metadataEntry.Name].Should().BeFalse();
                foundMetadata[metadataEntry.Name] = true;
            }

            foundMetadata.All(kv => kv.Value).Should().BeTrue();
        }

        [Fact]
        public void It_throws_with_existing_items_when_mergeExisting_is_false()
        {
            var metadata = new ItemMetadataValue<string>[]
            {
                 new ItemMetadataValue<string>("metadata1", "value1"),
                 new ItemMetadataValue<string>("metadata2", "value2")
            };

            var transform1 = new AddItemTransform<string>("item",
                    FullItemTransformSetIncludeValue,
                    "exclude1",
                    t => true,
                    mergeExisting: false)
                    .WithMetadata(metadata[0]);

            var transform2 = new AddItemTransform<string>("item",
                    FullItemTransformSetIncludeValue,
                    "exclude2",
                    t => true,
                    mergeExisting: false)
                    .WithMetadata(metadata[1]);

            var mockProj = ProjectRootElement.Create();
            var itemGroup = mockProj.AddItemGroup();

            transform1.Execute("_", mockProj, itemGroup);
            Action action = () => transform2.Execute("_", mockProj, itemGroup);

            action.ShouldThrow<Exception>();
        }

        private static string FullItemTransformSetItemNamePrefix => "item";
        private static string FullItemTransformSetIncludeValue => "include1;include2";
        private static string FullItemTransformSetExcludeValue => "exclude1;exclude2";
        private static string FullItemTransformSetMetadataName => "SomeName";
        private static string FullItemTransformSetMetadataValue => "SomeValue";

        private AddItemTransform<string>[] GetFullItemTransformSet(bool condition)
        {
            return new AddItemTransform<string>[]
            {
                new AddItemTransform<string>(FullItemTransformSetItemNamePrefix + "1",
                    FullItemTransformSetIncludeValue.Split(';'),
                    FullItemTransformSetExcludeValue.Split(';'),
                    t => condition)
                    .WithMetadata(FullItemTransformSetMetadataName, FullItemTransformSetMetadataValue),
                new AddItemTransform<string>(FullItemTransformSetItemNamePrefix + "2",
                    t => FullItemTransformSetIncludeValue,
                    t => FullItemTransformSetExcludeValue,
                    t => condition)
                    .WithMetadata(FullItemTransformSetMetadataName, t => FullItemTransformSetMetadataValue),
                new AddItemTransform<string>(FullItemTransformSetItemNamePrefix + "3",
                    FullItemTransformSetIncludeValue,
                    t => FullItemTransformSetExcludeValue,
                    t => condition)
                    .WithMetadata(new ItemMetadataValue<string>(FullItemTransformSetMetadataName, FullItemTransformSetMetadataValue)),
                new AddItemTransform<string>(FullItemTransformSetItemNamePrefix + "4",
                    t => FullItemTransformSetIncludeValue,
                    FullItemTransformSetExcludeValue,
                    t => condition)
                    .WithMetadata(new ItemMetadataValue<string>(FullItemTransformSetMetadataName, t => FullItemTransformSetMetadataValue)),
                new AddItemTransform<string>(FullItemTransformSetItemNamePrefix + "5",
                    FullItemTransformSetIncludeValue,
                    FullItemTransformSetExcludeValue,
                    t => condition)
                    .WithMetadata(FullItemTransformSetMetadataName, FullItemTransformSetMetadataValue)
            };
        }
    }
}
