﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.ProjectJsonMigration.Transforms
{
    internal interface ITransform<T, U>
    {
        U Transform(T source);
    }
}