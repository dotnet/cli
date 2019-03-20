// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;

namespace Microsoft.DotNet.ToolManifest
{
    internal static class JsonElementExtension
    {
        // this is needed due to https://github.com/dotnet/corefx/issues/36109
        internal static bool TryGetPropertyValue<T>(this JsonElement element, string propertyName, out T result)
        {
            Type t = typeof(T);
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                result = default;
                return false;
            }
            try
            {
                if (t == typeof(bool))
                {
                    result = (T)(object)value.GetBoolean();
                    return true;
                }
                if (t == typeof(int))
                {
                    result =(T)(object)value.GetInt32();
                    return true;
                }
                if (t == typeof(string))
                {
                    result = (T)(object)value.GetString();
                    return true;
                }
                throw new ArgumentOutOfRangeException(
                    nameof(T),
                    string.Format("Destination type {0} is not supported.", t.FullName));
            }
            catch (InvalidOperationException e)
            {
                throw new ToolManifestException(string.Format(LocalizableStrings.FailedToReadProperty, propertyName, e.Message));
            }
        }
    }
}
