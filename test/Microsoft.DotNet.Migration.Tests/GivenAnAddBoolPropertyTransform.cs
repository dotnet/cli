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
    public class GivenAnAddBoolPropertyTransform
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void It_adds_a_property_to_the_project_with_boolean_value(bool propertyValue)
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";
        
            var propertyTransform = new AddBoolPropertyTransform(propertyName, t => true);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(1);
            propertyGroup.Properties.First().Name.Should().Be(propertyName);
            propertyGroup.Properties.First().Value.Should().Be(propertyValue.ToString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void It_doesnt_add_a_property_when_condition_is_false(bool propertyValue)
        {
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();
            var propertyName = "Property1";

            var propertyTransform = new AddBoolPropertyTransform(propertyName, t => false);
            propertyTransform.Execute(propertyValue, mockProj, propertyGroup);

            propertyGroup.Properties.Count.Should().Be(0);
        }
    }
}
