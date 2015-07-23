using System;

namespace GitHubConsole.CachedGitHub
{
    public abstract class Fallback<T> where T : class
    {
        private T _fallback;
        private Func<T> getFallback;

        protected T fallback => _fallback ?? (_fallback = getFallback());

        public Fallback(Func<T> fallback)
        {
            if (fallback == null)
                throw new ArgumentNullException(nameof(fallback));

            this._fallback = null;
            this.getFallback = fallback;
        }
        public Fallback(T fallback)
        {
            if (fallback == null)
                throw new ArgumentNullException(nameof(fallback));

            this._fallback = fallback;
            this.getFallback = null;
        }
    }
}
