using System;

/// 3-tuple of a value v[i], g[i] and delta[i].
namespace GreenwaldKhanna
{
    public class GkTuple<T> : IComparable
    where T : IComparable
    {
        /// v[i], an observation in the set of observations
        public T v;
        /// the difference between the rank lowerbounds of t[i] and t[i-1]
        /// g = r_min(v[i]) - r_min(v[i - 1])
        public uint g;

        /// the difference betweeh the rank upper and lower bounds for this tuple
        public uint delta;

        public int CompareTo(object obj)
        {
            GkTuple<T> other = (GkTuple<T>)obj;
            return this.v.CompareTo(other.v);
        }
    }
}