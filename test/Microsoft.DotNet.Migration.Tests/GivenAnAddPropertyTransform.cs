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
    public class GivenAnAddPropertyTransform
    {
        [Fact]
        public void It_adds_a_property_to_the_project_with_specified_value()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "Value1";

            var propertyTransform = new AddPropertyTransform<string>(propertyName, propertyValue, t=>true);
            propertyTransform.Execute("_", mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(1);
            propertyGroup.Properties.First().Name.Should().Be(propertyName);
            propertyGroup.Properties.First().Value.Should().Be(propertyValue);
        }

        [Fact]
        public void It_adds_duplicate_properties_to_the_project_with_specified_value_when_the_property_exists()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "Value1";

            var propertyTransform = new AddPropertyTransform<string>(propertyName, propertyValue, t => true);
            propertyTransform.Execute("_", mockProj, propertyGroup);
            propertyTransform.Execute("_", mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(2);

            foreach (var property in propertyGroup.Properties)
            {
                property.Name.Should().Be(propertyName);
                property.Value.Should().Be(propertyValue);
            }
        }

        [Fact]
        public void It_adds_a_property_to_the_project_with_computed_value()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "Value1";

            var propertyTransform = new AddPropertyTransform<string>(propertyName, t => t.ToUpper(), t => true);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(1);
            propertyGroup.Properties.First().Name.Should().Be(propertyName);
            propertyGroup.Properties.First().Value.Should().Be(propertyValue.ToUpper());
        }

        [Fact]
        public void It_doesnt_add_a_property_when_condition_is_false()
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
            var propertyValue = "Value1";

            var propertyTransform = new AddPropertyTransform<string>(propertyName, propertyValue, t => false);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(0);
        }
    }
}
