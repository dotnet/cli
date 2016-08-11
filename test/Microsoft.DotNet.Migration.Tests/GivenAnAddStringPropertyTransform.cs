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
    public class GivenAnAddStringPropertyTransform
    {
        [Fact]
        public void It_adds_a_property_to_the_project_with_string_value()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "TestValue1";

            var propertyTransform = new AddStringPropertyTransform(propertyName, t => true);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(1);
            propertyGroup.Properties.First().Name.Should().Be(propertyName);
            propertyGroup.Properties.First().Value.Should().Be(propertyValue);
        }

        [Fact]
        public void It_doesnt_add_a_property_when_condition_is_false()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "TestValue1";

            var propertyTransform = new AddStringPropertyTransform(propertyName, t => false);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(0);
        }
    }
}
