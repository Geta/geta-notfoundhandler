// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class CustomRedirectEqualityComparer: IEqualityComparer<CustomRedirect>
    {
        public bool Equals(CustomRedirect x, CustomRedirect y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.WildCardSkipAppend == y.WildCardSkipAppend && x.NewUrl == y.NewUrl && x.State == y.State && x.RedirectType == y.RedirectType;
        }

        public int GetHashCode(CustomRedirect obj)
        {
            return HashCode.Combine(obj.WildCardSkipAppend, obj.NewUrl, obj.State, (int)obj.RedirectType);
        }
    }
}
