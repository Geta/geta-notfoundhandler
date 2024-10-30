// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Geta.NotFoundHandler.Core;
using Geta.NotFoundHandler.Core.Redirects;

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
        public string BootstrapJsUrl { get; set; } = "https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/js/bootstrap.bundle.min.js";
        public string BootstrapJsIntegrity { get; set; } = "sha384-kenU1KFdBIe4zVF0s0G1M5b4hcpxyD9F7jL+jjXkk+Q2h455rYXK/7HAuoJl+0I4";
        public string BootstrapCssUrl { get; set; } = "https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css";
        public string BootstrapCssIntegrity { get; set; } = "sha384-rbsA2VBKQhggwzxH7pPCaAqO46MgnOM80zW1RWuH61DGLwZJEdK2Kadq2F9CUG65";
        public string FeatherJsUrl { get; set; } = "https://cdn.jsdelivr.net/npm/feather-icons@4.29.0/dist/feather.min.js";
        public string FeatherJsIntegrity { get; set; } = "sha256-7kKJWwCLNN8n5rT1MNUpVPkeLxbwe1EZU73jiLdssrI=";

        private readonly List<Type> _providers = new();
        public IEnumerable<Type> Providers => _providers;

        public RedirectType DefaultRedirectType { get; set; } = RedirectType.Temporary;

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
