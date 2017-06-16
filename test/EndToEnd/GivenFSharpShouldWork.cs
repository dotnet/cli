// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Tests.EndToEnd
{
    public class GivenFSharpShouldWork : TestBase
    {
        [Fact]
        public void AppWithLib()
        {
            using (DisposableDirectory directory = Temp.CreateDirectory())
            {
                string projectDirectory = directory.Path;

                string newArgs = "lib -n lib -lang f# -f netcoreapp2.0 --debug:ephemeral-hive --no-restore";
                new NewCommandShim()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute(newArgs)
                    .Should().Pass();

                File.WriteAllText(Path.Combine(directory, "lib", "Library.fs"), @"
namespace Lib
module Say =
    let hello name = printfn \"Hello World from %s!\" name
                ");

                string newArgs = "console -n app -lang f# -f netcoreapp2.0 --debug:ephemeral-hive --no-restore";
                new NewCommandShim()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute(newArgs)
                    .Should().Pass();

                File.WriteAllText(Path.Combine(directory, "app", "Program.fs"), @"
[<EntryPoint>]
let main argv =
    Lib.Say.hello \"F#\"
    0
                ");

                new DotnetCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("add app reference lib/lib.fsproj")
                    .Should().Pass();

                new RestoreCommand()
                    .WithWorkingDirectory(projectDirectory)
                    //.Execute("/p:SkipInvalidConfigurations=true")
                    .Execute("app")
                    .Should().Pass();

                new BuildCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("app")
                    .Should().Pass();

                new RunCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .ExecuteWithCapturedOutput("-p app")
                    .Should().Pass()
                         .And.HaveStdOutContaining("Hello World from F#!");
            }
        }

    }
}
