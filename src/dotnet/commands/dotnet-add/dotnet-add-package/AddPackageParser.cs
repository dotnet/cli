// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.DotNet.Cli.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LocalizableStrings = Microsoft.DotNet.Tools.Add.PackageReference.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class AddPackageParser
    {
        public static Command AddPackage()
        {
            return Create.Command(
                "package",
                LocalizableStrings.AppFullName,
                Accept.ExactlyOneArgument(errorMessage: o => LocalizableStrings.SpecifyExactlyOnePackageReference)
                      .WithSuggestionsFrom(QueryNuGet)
                      .With(name: LocalizableStrings.CmdPackage,
                            description: LocalizableStrings.CmdPackageDescription),
                CommonOptions.HelpOption(),
                Create.Option("-v|--version",
                              LocalizableStrings.CmdVersionDescription,
                              Accept.ExactlyOneArgument()
                                    .With(name: LocalizableStrings.CmdVersion)
                                    .ForwardAsSingle(o => $"--version {o.Arguments.Single()}")),
                Create.Option("-f|--framework",
                              LocalizableStrings.CmdFrameworkDescription,
                              Accept.ExactlyOneArgument()
                                    .With(name: LocalizableStrings.CmdFramework)
                                    .ForwardAsSingle(o => $"--framework {o.Arguments.Single()}")),
                Create.Option("-n|--no-restore",
                              LocalizableStrings.CmdNoRestoreDescription),
                Create.Option("-s|--source",
                              LocalizableStrings.CmdSourceDescription,
                              Accept.ExactlyOneArgument()
                                    .With(name: LocalizableStrings.CmdSource)
                                    .ForwardAsSingle(o => $"--source {o.Arguments.Single()}")),
                Create.Option("--package-directory",
                              LocalizableStrings.CmdPackageDirectoryDescription,
                              Accept.ExactlyOneArgument()
                                    .With(name: LocalizableStrings.CmdPackageDirectory)
                                    .ForwardAsSingle(o => $"--package-directory {o.Arguments.Single()}")));
        }

        public static IEnumerable<string> QueryNuGet(string match)
        {
            var httpClient = new HttpClient();

            Stream result;

            try
            {
                var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = httpClient.GetAsync($"https://api-v2v3search-0.nuget.org/autocomplete?q={match}&skip=0&take=100", cancellation.Token)
                                         .Result;

                result = response.Content.ReadAsStreamAsync().Result;
            }
            catch (Exception)
            {
                yield break;
            }

            JObject json;
            using (var reader = new JsonTextReader(new StreamReader(result)))
            {
                json = JObject.Load(reader);
            }

            foreach (var id in json["data"])
            {
                yield return id.Value<string>();
            }
        }
    }
}
