// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Geta.NotFoundHandler.Optimizely.Core.AutomaticRedirects
{
    public class ContentUrlHistory
    {
        public Guid Id { get; set; }
        public string ContentKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public ICollection<TypedUrl> Urls { get; set; }
    }
}
