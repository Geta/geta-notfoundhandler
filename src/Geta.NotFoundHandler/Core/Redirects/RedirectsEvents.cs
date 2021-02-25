using System;

namespace Geta.NotFoundHandler.Core.Redirects
{
    public class RedirectsEvents
    {
        public event EventHandler OnUpdated;

        public void RedirectsUpdated()
        {
            OnUpdated?.Invoke(new EventArgs());
        }
    }

    public delegate void EventHandler(EventArgs e);
}
