// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.CommandFactory;
using Microsoft.DotNet.Tools.Test.Utilities;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenACompositeCommandResolver
    {
        [Fact]
        public void It_iterates_through_all_added_resolvers_in_order_when_they_return_null()
        {
            var compositeCommandResolver = new CompositeCommandResolver();

            var resolverCalls = new List<int>();

            var mockResolver1 = new Mock<ICommandResolver>();
            mockResolver1.Setup(r => r
                .Resolve(It.IsAny<CommandResolverArguments>()))
                .Returns(default(CommandSpec))
                .Callback(() => resolverCalls.Add(1));

            var mockResolver2 = new Mock<ICommandResolver>();
            mockResolver2.Setup(r => r
                .Resolve(It.IsAny<CommandResolverArguments>()))
                .Returns(default(CommandSpec))
                .Callback(() => resolverCalls.Add(2));

            compositeCommandResolver.AddCommandResolver(mockResolver1.Object);
            compositeCommandResolver.AddCommandResolver(mockResolver2.Object);

            compositeCommandResolver.Resolve(default(CommandResolverArguments));

            resolverCalls.Should()
                .HaveCount(2)
                .And
                .ContainInOrder(new [] {1, 2});

        }

        [Fact]
        public void It_stops_iterating_through_added_resolvers_when_one_returns_nonnull()
        {
            var compositeCommandResolver = new CompositeCommandResolver();

            var resolverCalls = new List<int>();

            var mockResolver1 = new Mock<ICommandResolver>();
            mockResolver1.Setup(r => r
                .Resolve(It.IsAny<CommandResolverArguments>()))
                .Returns(new CommandSpec(null, null))
                .Callback(() => resolverCalls.Add(1));

            var mockResolver2 = new Mock<ICommandResolver>();
            mockResolver2.Setup(r => r
                .Resolve(It.IsAny<CommandResolverArguments>()))
                .Returns(default(CommandSpec))
                .Callback(() => resolverCalls.Add(2));

            compositeCommandResolver.AddCommandResolver(mockResolver1.Object);
            compositeCommandResolver.AddCommandResolver(mockResolver2.Object);

            compositeCommandResolver.Resolve(default(CommandResolverArguments));

            resolverCalls.Should()
                .HaveCount(1)
                .And
                .ContainInOrder(new [] {1});

        }
    }
}
