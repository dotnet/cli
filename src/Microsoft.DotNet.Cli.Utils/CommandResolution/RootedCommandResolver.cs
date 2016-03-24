using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace Microsoft.DotNet.Cli.Utils
{
    public class AppBaseDllCommandResolver : ICommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == null)
            {
                return null;
            }
            if (commandResolverArguments.CommandName.EndsWith(FileNameSuffixes.DotNet.DynamicLib))
            {
                var localPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    commandResolverArguments.CommandName);
                if (File.Exists(localPath))
                {
                    var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(
                        new[] { localPath }
                        .Concat(commandResolverArguments.CommandArguments.OrEmptyIfNull()));
                    return new CommandSpec(
                        new Muxer().MuxerPath,
                        escapedArgs,
                        CommandResolutionStrategy.RootedPath);
                }
            }
            return null;
        }
    }

    public class MuxerCommandResolver : ICommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == "dotnet")
            {
                var muxer = new Muxer();
                var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(
                    commandResolverArguments.CommandArguments.OrEmptyIfNull());
                return new CommandSpec(muxer.MuxerPath, escapedArgs, CommandResolutionStrategy.RootedPath);
            }
            return null;
        }
    }

    public class RootedCommandResolver : ICommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments commandResolverArguments)
        {
            if (commandResolverArguments.CommandName == null)
            {
                return null;
            }

            if (Path.IsPathRooted(commandResolverArguments.CommandName))
            {
                var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(
                    commandResolverArguments.CommandArguments.OrEmptyIfNull());

                return new CommandSpec(commandResolverArguments.CommandName, escapedArgs, CommandResolutionStrategy.RootedPath);
            }

            return null;
        }
    }
}
