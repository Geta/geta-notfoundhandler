// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Core;

namespace Geta.NotFoundHandler.Infrastructure.Configuration
{
    public class NotFoundHandlerOptions
    {
        public const int CurrentDbVersion = 2;
        public int BufferSize { get; set; } = 30;
        public int ThreshHold { get; set; } = 5;
        public FileNotFoundMode HandlerMode { get; set; } = FileNotFoundMode.On;
        public TimeSpan RegexTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

        public string[] IgnoredResourceExtensions { get; set; } =
            {"jpg", "gif", "png", "css", "js", "ico", "swf", "woff"};

        public LoggerMode Logging { get; set; } = LoggerMode.On;
        public bool LogWithHostname { get; set; } = false;
        public string ConnectionString { get; private set; }

        private readonly List<Type> _providers = new();
        public IEnumerable<Type> Providers => _providers;

        public NotFoundHandlerOptions AddProvider<T>()
            where T : INotFoundHandler
        {
            var t = typeof(T);
            _providers.Add(t);
            return this;
        }

        public NotFoundHandlerOptions UseSqlServer(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }
    }
}
