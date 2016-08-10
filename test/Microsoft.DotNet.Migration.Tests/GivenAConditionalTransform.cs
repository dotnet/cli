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
    public class GivenAConditionalTransform
    {
        [Fact]
        public void It_does_nothing_when_condition_is_false()
        {
            var conditionalTransform = new TestConditionalTransform(t => false);
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();

            conditionalTransform.Execute("astring", mockProj, propertyGroup);

            propertyGroup.Children.Count.Should().Be(0);
        }

        [Fact]
        public void It_executes_when_condition_is_true()
        {
            var conditionalTransform = new TestConditionalTransform(t => true);
            var mockProj = ProjectRootElement.Create();
            var propertyGroup = mockProj.AddPropertyGroup();

            conditionalTransform.Execute("astring", mockProj, propertyGroup);

            propertyGroup.Children.Count.Should().Be(1);
        }

        private class TestConditionalTransform : ConditionalTransform<string>
        {
            public TestConditionalTransform(Func<string, bool> condition) : base(condition) { }

            public override void ConditionallyExecute(string source, ProjectRootElement destinationProject, ProjectElement destinationElement = null)
            {
                ((ProjectPropertyGroupElement)destinationElement).AddProperty(source, source);
            }
        }
    }
}
