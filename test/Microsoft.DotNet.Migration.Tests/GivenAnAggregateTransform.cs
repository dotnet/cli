using Microsoft.DotNet.ProjectJsonMigration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Build.Construction;
using FluentAssertions;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenAnAggregateTransform
    {
        [Fact]
        public void It_executes_each_transform_passed_to_constructor()
        {
            var testTransforms = new TestTransform[]
            {
                new TestTransform("transform1"),
                new TestTransform("transform2"),
                new TestTransform("transform3")
            };

            var aggregateTransform = new AggregateTransform<string>(testTransforms);

            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();

            aggregateTransform.Execute("astring", mockProj, propertyGroup);
            
            propertyGroup.Children.Count.Should().Be(3);
            var foundTransforms = testTransforms.ToDictionary<TestTransform, string, bool>(transform => transform.Name, t => false);

            foreach (var property in propertyGroup.Properties)
            {
                var propertyName = property.Name;

                foundTransforms.Should().ContainKey(propertyName);
                foundTransforms[propertyName].Should().BeFalse();

                foundTransforms[propertyName] = true;
            }

            foundTransforms.All(entry => entry.Value).Should().BeTrue();
        }

        private class TestTransform : ITransform<string>
        {
            public string Name { get; }

            public TestTransform(string name)
            {
                Name = name;
            }

            public void Execute(string source, ProjectRootElement destinationProject, ProjectElement destinationElement = null)
            {
                ((ProjectPropertyGroupElement)destinationElement).AddProperty(Name, source);
            }
        }
    }
}
