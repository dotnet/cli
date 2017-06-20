#!/bin/sh
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
echo "This software may collect information about you and your use of the software, and send that to Microsoft."
echo "Please visit http://aka.ms/dotnet-cli-eula for more information."

# Run 'dotnet new' as the user to trigger the first time experience to initialize the cache
echo "Welcome to .NET Core!
---------------------
Learn more about .NET Core @ https://aka.ms/dotnet-docs. Use dotnet --help to see available commands or go to https://aka.ms/dotnet-cli-docs.

.NET Core Tools Telemetry
--------------
The .NET Core Tools include a telemetry feature that collects usage information. It is important that the .NET Team understands how the tools are being used so that we can improve them.

The data collected is anonymous and will be published in an aggregated form for use by both Microsoft and community engineers under the Creative Commons Attribution License.

The .NET Core Tools telemetry feature is enabled by default. You can opt-out of the telemetry feature by setting an environment variable DOTNET_CLI_TELEMETRY_OPTOUT (for example, 'export' on macOS/Linux, 'set' on Windows) to true (for example, 'true', 1). You can read more about .NET Core tools telemetry at https://aka.ms/dotnet-cli-telemetry."

dotnet new > /dev/null 2>&1 || true
