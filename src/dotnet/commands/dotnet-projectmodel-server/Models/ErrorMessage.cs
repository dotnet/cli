// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectModel.Server.Models
{
    public sealed class ErrorMessage : IEquatable<ErrorMessage>
    {
        public string Message { get; set; }
        public string Path { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ErrorMessage);
        }

        public bool Equals(ErrorMessage payload)
        {
            return payload != null &&
                   string.Equals(Message, payload.Message, StringComparison.Ordinal) &&
                   string.Equals(Path, payload.Path, StringComparison.OrdinalIgnoreCase) &&
                   Line == payload.Line &&
                   Column == payload.Column;
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(Message,
                Hash.Combine(Line, Column));
        }
    }
}
