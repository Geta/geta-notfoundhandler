// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using EPiServer.Core;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ChannelMovedContentRegistratorQueue : IMovedContentRegistratorQueue
    {
        private static readonly Channel<ContentReference> _buffer = Channel.CreateUnbounded<ContentReference>(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleWriter = false,
                SingleReader = true
            });

        public IAsyncEnumerable<ContentReference> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return _buffer.Reader.ReadAllAsync(cancellationToken);
        }

        public void Enqueue(ContentReference contentLink)
        {
            _buffer.Writer.TryWrite(contentLink);
        }
    }
}
