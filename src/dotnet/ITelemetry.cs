// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Cli
{
    public interface ITelemetry
    {
        void TrackEvent(string eventName, IList<string> properties, IDictionary<string, double> measurements);
    }
}
