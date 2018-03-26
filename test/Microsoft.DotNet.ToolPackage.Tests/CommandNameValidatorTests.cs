// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class CommandNameValidatorTests
    {
        [Fact]
        public void ItCanGenerateErrorWhenReservedWordIsPresentInAnywhere()
        {
            var commandValidator = new CommandNameValidator(true, false, false, "build");
            var errors = commandValidator.GenerateError("myBuildtool");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameContainsReservedString, "myBuildtool", "build"));
        }

        [Fact]
        public void ItCanGenerateErrorWhenReservedWordIsPresentAsStart()
        {
            var commandValidator = new CommandNameValidator(false, true, false, "build");
            var errors = commandValidator.GenerateError("Build-tool");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameStartsWithReservedString, "Build-tool", "build"));
        }

        [Fact]
        public void ItCanGenerateErrorWhenReservedWordIsPresentAsStart2()
        {
            var commandValidator = new CommandNameValidator(false, true, false, "build");
            var errors = commandValidator.GenerateError("Build");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameStartsWithReservedString, "Build", "build"));
        }

        [Fact]
        public void ItCanGenerateErrorWhenItMatchesReservedWord()
        {
            var commandValidator = new CommandNameValidator(false, false, true, "build");
            var errors = commandValidator.GenerateError("Build");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameMatchesReservedString, "Build", "build"));
        }

        [Fact]
        public void ItCanOnlyGenerateErrorForAnywhereEvenItMatchesTheWholeWord()
        {
            var commandValidator = new CommandNameValidator(true, false, true, "build");
            var errors = commandValidator.GenerateError("Build");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameContainsReservedString, "Build", "build"));
        }

        [Fact]
        public void ItCanOnlyGenerateErrorForAnywhereEvenItStartsWith()
        {
            var commandValidator = new CommandNameValidator(true, true, false, "build");
            var errors = commandValidator.GenerateError("Buildtool");
            errors.Single().Should().Be(string.Format(CommonLocalizableStrings.CommandNameContainsReservedString, "Buildtool", "build"));
        }

        [Fact]
        public void ItHasNoErrorWhenItDoesnotViolateAnyRules()
        {
            var commandValidator = new CommandNameValidator(true, true, true, "build");
            var errors = commandValidator.GenerateError("nomatch");
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ItHasNoErrorWhenItDoesnotViolateAnyRules2()
        {
            var commandValidator = new CommandNameValidator(false, true, true, "build");
            var errors = commandValidator.GenerateError("buildtool");
            errors.Should().BeEmpty();
        }
    }
}
