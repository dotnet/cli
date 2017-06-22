﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Tools.Test;
using Microsoft.Extensions.DependencyModel.Tests;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Configurer.UnitTests
{
    public class GivenADotnetFirstTimeUseConfigurer
    {
        private const string CliFallbackFolderPath = "some path";

        private Mock<INuGetCachePrimer> _nugetCachePrimerMock;
        private Mock<INuGetCacheSentinel> _nugetCacheSentinelMock;
        private Mock<IFirstTimeUseNoticeSentinel> _firstTimeUseNoticeSentinelMock;
        private Mock<IEnvironmentProvider> _environmentProviderMock;
        private Mock<IReporter> _reporterMock;

        public GivenADotnetFirstTimeUseConfigurer()
        {
            _nugetCachePrimerMock = new Mock<INuGetCachePrimer>();
            _nugetCacheSentinelMock = new Mock<INuGetCacheSentinel>();
            _firstTimeUseNoticeSentinelMock = new Mock<IFirstTimeUseNoticeSentinel>();
            _environmentProviderMock = new Mock<IEnvironmentProvider>();
            _reporterMock = new Mock<IReporter>();

            _environmentProviderMock
                .Setup(e => e.GetEnvironmentVariableAsBool("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", false))
                .Returns(false);
            _environmentProviderMock
                .Setup(e => e.GetEnvironmentVariableAsBool("DOTNET_PRINT_TELEMETRY_MESSAGE", true))
                .Returns(true);
        }

        [Fact]
        public void It_does_not_print_the_first_time_use_notice_if_the_sentinel_exists()
        {
            _firstTimeUseNoticeSentinelMock.Setup(n => n.Exists()).Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.FirstTimeWelcomeMessage)), Times.Never);
            _reporterMock.Verify(r => r.Write(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void It_does_not_print_the_first_time_use_notice_when_the_user_has_set_the_DOTNET_SKIP_FIRST_TIME_EXPERIENCE_environemnt_variable()
        {
            _firstTimeUseNoticeSentinelMock.Setup(n => n.Exists()).Returns(false);
            _environmentProviderMock
                .Setup(e => e.GetEnvironmentVariableAsBool("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", false))
                .Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.FirstTimeWelcomeMessage)), Times.Never);
            _reporterMock.Verify(r => r.Write(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void It_does_not_print_the_first_time_use_notice_when_the_user_has_set_the_DOTNET_PRINT_TELEMETRY_MESSAGE_environemnt_variable()
        {
            _firstTimeUseNoticeSentinelMock.Setup(n => n.Exists()).Returns(false);
            _environmentProviderMock
                .Setup(e => e.GetEnvironmentVariableAsBool("DOTNET_PRINT_TELEMETRY_MESSAGE", true))
                .Returns(false);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.FirstTimeWelcomeMessage)), Times.Never);
            _reporterMock.Verify(r => r.Write(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void It_prints_the_telemetry_if_the_sentinel_does_not_exist()
        {
            _firstTimeUseNoticeSentinelMock.Setup(n => n.Exists()).Returns(false);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.FirstTimeWelcomeMessage)));
            _reporterMock.Verify(r => r.Write(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void It_does_not_prime_the_cache_if_the_sentinel_exists()
        {
            _nugetCacheSentinelMock.Setup(n => n.Exists()).Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Never);
        }

        [Fact]
        public void It_does_not_prime_the_cache_if_first_run_experience_is_already_happening()
        {
            _nugetCacheSentinelMock.Setup(n => n.InProgressSentinelAlreadyExists()).Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Never);
        }

        [Fact]
        public void It_does_not_prime_the_cache_if_cache_is_missing()
        {
            _nugetCachePrimerMock.Setup(n => n.SkipPrimingTheCache()).Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Never);
        }

        [Fact]
        public void It_does_not_prime_the_cache_if_the_sentinel_exists_but_the_user_has_set_the_DOTNET_SKIP_FIRST_TIME_EXPERIENCE_environemnt_variable()
        {
            _nugetCacheSentinelMock.Setup(n => n.Exists()).Returns(false);
            _environmentProviderMock
                .Setup(e => e.GetEnvironmentVariableAsBool("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", false))
                .Returns(true);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Never);
        }

        [Fact]
        public void It_primes_the_cache_if_the_sentinel_does_not_exist()
        {
            _nugetCacheSentinelMock.Setup(n => n.Exists()).Returns(false);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Once);
        }        

        [Fact]
        public void It_prints_first_use_notice_and_primes_the_cache_if_the_sentinels_do_not_exist()
        {
            _nugetCacheSentinelMock.Setup(n => n.Exists()).Returns(false);
            _firstTimeUseNoticeSentinelMock.Setup(n => n.Exists()).Returns(false);

            var dotnetFirstTimeUseConfigurer = new DotnetFirstTimeUseConfigurer(
                _nugetCachePrimerMock.Object,
                _nugetCacheSentinelMock.Object,
                _firstTimeUseNoticeSentinelMock.Object,
                _environmentProviderMock.Object,
                _reporterMock.Object,
                CliFallbackFolderPath);

            dotnetFirstTimeUseConfigurer.Configure();

            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.FirstTimeWelcomeMessage)));
            _reporterMock.Verify(r => r.WriteLine(It.Is<string>(str => str == LocalizableStrings.NugetCachePrimeMessage)));
            _nugetCachePrimerMock.Verify(r => r.PrimeCache(), Times.Once);
            _reporterMock.Verify(r => r.Write(It.IsAny<string>()), Times.Never);
        }
    }
}
