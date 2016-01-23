#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

loadTestList()
{
    return ( `cat "$REPOROOT/scripts/configuration/testProjects.csv" `)
}

loadTestPackageList()
{
    return ( `cat "$RepoRoot/scripts/configuration/testPackageProjects.csv" `)
}