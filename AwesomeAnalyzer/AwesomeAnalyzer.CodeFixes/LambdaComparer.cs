using System;
using System.Collections.Generic;

namespace AwesomeAnalyzer
{
    internal sealed class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _func;

        public LambdaComparer(Func<T, T, bool> func)
        {
            _func = func;
        }

        public bool Equals(T x, T y) => _func(x, y);

        public int GetHashCode(T obj) => typeof(T).GetHashCode();
    }
}