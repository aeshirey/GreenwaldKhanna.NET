using System;
using System.IO;
using GreenwaldKhanna;

namespace gk_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var streamA = new Stream<int>(epsilon: 0.01);
            var streamB = new Stream<int>(epsilon: 0.04);

            // Generated some random numbers for testing in Python:
            // nums = [str(random.randint(0, 10_000)) for i in range(10000)]
            // with open('random_nums.txt', 'w') as fh: fh.write(','.join(nums))
            using (var sr = new StreamReader("random_nums.txt"))
            {
                int i = 0;
                foreach (string num in sr.ReadLine().Trim().Split(','))
                {
                    int n = int.Parse(num);

                    if (i % 2 == 0)
                        streamA.insert(n);
                    else
                        streamB.insert(n);

                    i += 1;
                }

            }

            Stream<int> stream = streamA + streamB;

            Console.WriteLine($"P90 = {stream.quantile(0.90)}");
            Console.WriteLine($"P95 = {stream.quantile(0.95)}");
            Console.ReadLine();
        }
    }
}
