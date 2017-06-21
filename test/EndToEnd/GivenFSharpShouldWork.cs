// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests.EndToEnd
{
    public class GivenFSharpShouldWork : TestBase
    {
        [Fact]
        public void SlnRestore()
        {
            using (DisposableDirectory directory = Temp.CreateDirectory())
            {
                string projectDirectory = directory.Path;

                string newArgsApp = "console -n app -lang f# -f netcoreapp2.0 --debug:ephemeral-hive --no-restore";
                new NewCommandShim()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute(newArgsApp)
                    .Should().Pass();

                File.WriteAllText(Path.Combine(projectDirectory, "app", "Program.fs"), @"
[<EntryPoint>]
let main argv =
    printfn ""Hello.""
    0
                ");

                new DotnetCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("new sln -n my --debug:ephemeral-hive")
                    .Should().Pass();

                new DotnetCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute($"sln add {Path.Combine("app","app.fsproj")}")
                    .Should().Pass();

                new RestoreCommand()
                    .WithWorkingDirectory(projectDirectory)
                    //.Execute("/p:SkipInvalidConfigurations=true")
                    .Execute("my.sln")
                    .Should().Pass();

                new BuildCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("app")
                    .Should().Pass();

                new RunCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .ExecuteWithCapturedOutput("-p app")
                    .Should().Pass()
                         .And.HaveStdOutContaining("Hello.");
            }
        }

    }
}
