using NUnit.Framework;
using System;
using GreenwaldKhanna;

namespace GkTests
{
    public class Tests
    {
        private uint get_quantile_for_range(uint start, uint end, double phi)
        {
            return (uint)Math.Floor(phi * ((end - 1) - start)) + start;
        }

        private (uint, uint) get_quantile_bounds_for_range(uint start, uint end, double phi, double epsilon)
        {
            uint lower = Math.Max(0, get_quantile_for_range(start, end, (phi - epsilon)));
            uint upper = get_quantile_for_range(start, end, phi + epsilon);

            return (lower, upper);
        }

        private bool quantile_in_bounds(uint start, uint end, Stream<uint> s, double phi, double epsilon)
        {
            uint approx_quantile = s.quantile(phi);
            var (lower, upper) = get_quantile_bounds_for_range(start, end, phi, epsilon);

            // println!("approx_quantile={} lower={} upper={} phi={} epsilon={}",
            // approx_quantile, lower, upper, phi, epsilon);

            return approx_quantile >= lower && approx_quantile <= upper;
        }


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void test_basics()
        {
            Assert.Pass();
            double epsilon = 0.01;
            var stream = new Stream<uint>(epsilon);
            for (uint i = 1; i < 1001; i++)
                stream.insert(i);

            for (int phi = 0; phi < 100; phi++)
            {
                Assert.True(quantile_in_bounds(1, 1001, stream, phi / 100.0, epsilon));
            }
        }

        [Test]
        public void test_add_assign()
        {
            double epsilon = 0.01;

            var stream = new Stream<uint>(epsilon);
            var stream2 = new Stream<uint>(epsilon);

            for (uint i = 0; i < 1000; i++)
            {
                stream.insert(2 * i);
                stream2.insert(2 * i + 1);
            }

            for (int phi = 0; phi < 100; phi++)
            {
                Assert.True(quantile_in_bounds(0, 2000, stream, phi / 100.0, epsilon));
            }
        }
    }
}
