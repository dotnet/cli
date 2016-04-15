// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.DotNet.Cli.Utils
{
    /// <summary>
    /// A TextWriter that can raises an event for each line that is written to it.
    /// </summary>
    public class LineNotificationTextWriter : TextWriter
    {
        private Encoding _encoding;
        private StringBuilder _currentString;
        private Action<string> _lineHandler;

        public LineNotificationTextWriter(IFormatProvider formatProvider, Encoding encoding)
            : base(formatProvider)
        {
            _encoding = encoding;

            // start with an average line length so the builder doesn't need to immediately grow
            _currentString = new StringBuilder(128);
        }

        public LineNotificationTextWriter OnWriteLine(Action<string> lineHandler)
        {
            if (lineHandler == null)
            {
                throw new ArgumentNullException(nameof(lineHandler));
            }
            if (_lineHandler != null)
            {
                throw new InvalidOperationException("OnWriteLine has already been set.");
            }

            _lineHandler = lineHandler;

            return this;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        // Write(char) gets called for all overloads of Write
        public override void Write(char value)
        {
            lock (_currentString)
            {
                _currentString.Append(value);

                if (value == '\n')
                {
                    _lineHandler?.Invoke(_currentString.ToString());
                    _currentString.Clear();
                }
            }
        }

        // optimize the common case - Write(string) - so we don't process char by char
        public override void Write(string value)
        {
            lock (_currentString)
            {
                List<int> newLineIndices = GetNewLines(value);

                if (newLineIndices == null || newLineIndices.Count == 0)
                {
                    // no newlines, just append
                    _currentString.Append(value);
                }
                else
                {
                    int start = 0;
                    for (int i = 0; i < newLineIndices.Count; i++)
                    {
                        int end = newLineIndices[i];

                        _currentString.Append(value, start, end - start + 1);

                        _lineHandler?.Invoke(_currentString.ToString());
                        _currentString.Clear();

                        start = end + 1;
                    }

                    _currentString.Append(value, start, value.Length - start);
                }
            }
        }

        private static List<int> GetNewLines(string value)
        {
            List<int> result = null;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n')
                {
                    if (result == null)
                    {
                        result = new List<int>();
                    }

                    result.Add(i);
                }
            }

            return result;
        }
    }
}
