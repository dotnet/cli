// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Tools.List.ProjectToProjectReferences;

namespace Microsoft.DotNet.Tools.List
{
    public class ListCommand : DispatchCommand
    {
        protected override string HelpText => @".NET List Command

Usage: dotnet list [options] <object> <command> [[--] <arg>...]]

Options:
  -h|--help  Show help information

Arguments:
  <object>   The object of the operation. If a project file is not specified, it defaults to the current directory.
  <command>  Command to be executed on <object>.

Args:
  Any extra arguments passed to the command. Use `dotnet list <command> --help` to get help about these arguments.

Commands:
  p2ps       List project to project (p2p) references to a project";

        protected override Dictionary<string, Func<string[], int>> BuiltInCommands => new Dictionary<string, Func<string[], int>>
        {
            ["p2ps"] = ListProjectToProjectReferencesCommand.Run,
        };
    }
}
