// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return _buffer.Reader.WaitToReadAsync(cancellationToken);
        }

        public bool TryRead(out ContentReference contentLink)
        {
            return _buffer.Reader.TryRead(out contentLink);
        }

        public void Enqueue(ContentReference contentLink)
        {
            _buffer.Writer.TryWrite(contentLink);
        }
    }
}
