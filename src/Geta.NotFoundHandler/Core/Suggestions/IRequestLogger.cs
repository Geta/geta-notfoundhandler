// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

namespace Geta.NotFoundHandler.Core.Suggestions
{
    public interface IRequestLogger
    {
        void LogRequest(string oldUrl, string referer);
        bool ShouldLogRequest(string oldUrl);
    }
}
