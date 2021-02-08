using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GreenwaldKhanna
{
    public class Stream<T>
    where T : IComparable
    {
        /// An ordered sequence of the selected observations
        public List<GkTuple<T>> summary;

        /// The error factor
        public double epsilon;

        /// The number of observations
        public uint n;

        /// Creates a new instance of a Stream
        public Stream(double epsilon)
        {
            this.epsilon = epsilon;
            this.summary = new List<GkTuple<T>>();
            this.n = 0;
        }


        /// Locates the correct position in the summary data set
        /// for the observation v, and inserts a new tuple (v,1,floor(2en))
        /// If v is the new minimum or maximum, then instead insert
        /// tuple (v,1,0).
        public void insert(T v)
        {
            GkTuple<T> t = new GreenwaldKhanna.GkTuple<T>
            {
                v = v,
                g = 1,
                delta = 0
            };

            uint position = this.find_insert_pos(t);

            if (position != 0 && position != this.summary.Count)
            {
                t.delta = (uint)(2.0 * this.epsilon * Math.Floor((double)this.n));
            }

            this.summary.Insert((int)position, t);

            this.n += 1;

            if (this.should_compress())
            {
                this.compress();
            }
        }



        /// Locates the proper position of v in a vector vs
        /// such that when v is inserted at position i,
        /// it is less then the element at i+1 if any,
        /// and greater than or equal to the element at i-1 if any.
        private uint find_insert_pos(GkTuple<T> v)
        {
            int position = this.summary.BinarySearch(v);

            if (position <= 0)
            {
                // Not found. We need to return the bitwise compliment
                return (uint)(~position);
            }

            return (uint)position;
        }

        /// Compute the epsilon-approximate phi-quantile
        /// from the summary data structure.
        public T quantile(double phi)
        {
            Debug.Assert(this.summary.Count > 0);
            Debug.Assert(phi >= 0.0 && phi <= 1.0);

            uint r = (uint)Math.Floor(phi * this.n);
            uint en = (uint)(this.epsilon * this.n);

            GkTuple<T> first = this.summary[0];

            T prev = first.v;
            uint prev_rmin = first.g;


            foreach (GkTuple<T> t in this.summary.Skip(1))
            {
                var rmax = prev_rmin + t.g + t.delta;

                if (rmax > r + en)
                {
                    return prev;
                }

                prev_rmin += t.g;
                prev = t.v;
            }

            return prev;
        }

        private bool should_compress()
        {
            uint period = (uint)Math.Floor(1.0 / (2.0 * this.epsilon));

            return this.n % period == 0;
        }

        private void compress()
        {
            int s = this.summary.Count;
            for (int i = s - 2; i >= 1; i--)
            {
                if (this.can_delete(i))
                {
                    this.delete(i);
                }
            }
        }

        private bool can_delete(int i)
        {
            Debug.Assert(this.summary.Count >= 2);
            Debug.Assert(i < this.summary.Count - 1);

            GkTuple<T> t = this.summary[i],
            tNext = this.summary[i + 1];
            var p = this.p();

            var safety_property = t.g + tNext.g + tNext.delta < p;

            var optimal = this.band(t.delta, p) <= this.band(tNext.delta, p);

            return safety_property && optimal;
        }

        /// Remove the ith tuple from the summary.
        /// Panics if i is not in the range [0,summary.len() - 1)
        /// Only permitted if g[i] + g[i+1] + delta[i+1] < 2 * epsilon * n
        private void delete(int i)
        {
            Debug.Assert(this.summary.Count >= 2);
            Debug.Assert(i < this.summary.Count - 1);

            GkTuple<T> t = this.summary[i];
            this.summary.RemoveAt(i);

            GkTuple<T> tnext = this.summary[i];

            tnext.g += t.g;
        }

        /// Compute which band a delta lies in.
        private uint band(uint delta, uint p)
        {
            Debug.Assert(p >= delta);

            var diff = p - delta + 1;

            return (uint)Math.Floor(Math.Log2(diff));
        }

        /// Calculate p = 2epsilon * n
        private uint p()
        {
            return (uint)Math.Floor(2.0 * this.epsilon * this.n);
        }


        // The GK algorithm is a bit unclear about it, but we need to adjust the statistics during the
        // merging. The main idea is that samples that come from one side will suffer from the lack of
        // precision of the other.
        // As a concrete example, take two QuantileSummaries whose samples (value, g, delta) are:
        // `a = [(0, 1, 0), (20, 99, 0)]` and `b = [(10, 1, 0), (30, 49, 0)]`
        // This means `a` has 100 values, whose minimum is 0 and maximum is 20,
        // while `b` has 50 values, between 10 and 30.
        // The resulting samples of the merge will be:
        // a+b = [(0, 1, 0), (10, 1, ??), (20, 99, ??), (30, 49, 0)]
        // The values of `g` do not change, as they represent the minimum number of values between two
        // consecutive samples. The values of `delta` should be adjusted, however.
        // Take the case of the sample `10` from `b`. In the original stream, it could have appeared
        // right after `0` (as expressed by `g=1`) or right before `20`, so `delta=99+0-1=98`.
        // In the GK algorithm's style of working in terms of maximum bounds, one can observe that the
        // maximum additional uncertainty over samples comming from `b` is `max(g_a + delta_a) =
        // floor(2 * eps_a * n_a)`. Likewise, additional uncertainty over samples from `a` is
        // `floor(2 * eps_b * n_b)`.
        // Only samples that interleave the other side are affected. That means that samples from
        // one side that are lesser (or greater) than all samples from the other side are just copied
        // unmodifed.
        // If the merging instances have different `relativeError`, the resulting instance will cary
        // the largest one: `eps_ab = max(eps_a, eps_b)`.
        // The main invariant of the GK algorithm is kept:
        // `max(g_ab + delta_ab) <= floor(2 * eps_ab * (n_a + n_b))` since
        // `max(g_ab + delta_ab) <= floor(2 * eps_a * n_a) + floor(2 * eps_b * n_b)`
        // Finally, one can see how the `insert(x)` operation can be expressed as `merge([(x, 1, 0])`
        public static Stream<T> operator +(Stream<T> a, Stream<T> b)
        {
            List<GkTuple<T>> merged_summary = new List<GkTuple<T>>(a.summary.Count + b.summary.Count);
            double merged_epsilon = Math.Max(a.epsilon, b.epsilon);
            uint merged_n = a.n + b.n;

            uint additional_a_delta = (uint)Math.Floor(2.0 * b.epsilon * b.n),
                additional_b_delta = (uint)Math.Floor(2.0 * a.epsilon * a.n);

            // Do a merge of two sorted lists until one of the lists is fully consumed
            bool started_a = false, started_b = false;
            while (a.summary.Count > 0 && b.summary.Count > 0)
            {
                // detect next 
                GkTuple<T> next_sample;

                if (a.summary[0].v.CompareTo(b.summary[0].v) < 0)
                {
                    started_a = true;
                    next_sample = a.summary[0];
                    if (started_b)
                    {
                        next_sample.delta += additional_a_delta;
                    }

                    a.summary.RemoveAt(0);
                }
                else
                {
                    started_b = true;
                    next_sample = b.summary[0];
                    if (started_a)
                    {
                        next_sample.delta += additional_b_delta;
                    }

                    b.summary.RemoveAt(0);
                }

                merged_summary.Add(next_sample);
            }


            // Copy the remaining samples from the rhs list
            // (by construction, at most one `while` loop will run)
            merged_summary.AddRange(a.summary);
            merged_summary.AddRange(b.summary);

            Stream<T> merged = new Stream<T>(merged_epsilon)
            {
                summary = merged_summary,
                n = merged_n
            };
            merged.compress();

            return merged;
        }
    }
}
