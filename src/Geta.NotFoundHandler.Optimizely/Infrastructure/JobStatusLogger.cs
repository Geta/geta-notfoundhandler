// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Text;

namespace Geta.NotFoundHandler.Optimizely.Infrastructure
{
    internal class JobStatusLogger
    {
        private readonly Action<string> _onStatusChanged;

        private readonly StringBuilder _stringBuilder = new();

        public JobStatusLogger(Action<string> onStatusChanged)
        {
            _onStatusChanged = onStatusChanged;
        }

        public void Log(string message)
        {
            _stringBuilder.AppendLine(message);
        }

        public void LogWithStatus(string message)
        {
            message = $"{DateTime.UtcNow:yyyy-MM-dd hh:mm:ss} - {message}";
            Status(message);
            Log(message);
        }

        public void Status(string message)
        {
            _onStatusChanged?.Invoke(message);
        }

        public string ToString(string separator = "<br />")
        {
            return _stringBuilder?.ToString().Replace(Environment.NewLine, separator);
        }
    }
}
