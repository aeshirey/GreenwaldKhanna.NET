# GreenwaldKhanna.NET

This project implements the [Greenwald-Khanna quantile estimator](https://www.stevenengelhardt.com/2018/03/07/calculating-percentiles-on-streaming-data-part-3-visualizing-greenwald-khanna/) in .NET Core. This is a reimplementation from the version in [my Rust crate, `probably`](https://github.com/aeshirey/probably/) which in-turn comes from the [Postmates quantiles repository](https://github.com/postmates/quantiles#greenwald-khanna---%CE%B5-approximate-quantiles) (also available as the [Rust `quantiles` crate](https://crates.io/crates/quantiles)).

The current .NET Core version appears to validate against my Rust code, but no guarantees are made.

To use this code in C#, create a new `Stream<T>` object (`where T : IComparable` but is expected to be `float`, `double`, `int`, etc.). You must specify the value for &epsilon; -- 0.01 is a reasonable value. Then you may repeatedly invoke `stream.insert(t)` on the estimator. Finally, calculate your desired quantile: `stream.quantile(0.5)` to get the median, `stream.quantile(0.99)` to get the P99 value, etc.

For parallelized calculations, the `Stream` class overrides `operator +`, so multiple streams can be combined. For example:


```csharp
Stream<int> stream_a = new Stream<int>(0.01),
    stream_b = new Stream<int>(0.01);

foreach(int value in my_values)
{
    // Send the data to two different GK estimators
    if (value % 2 == 0)
        stream_a.insert(value);
    else
        stream_b.insert(value);
}

// Combine the estimators
Stream<int> stream = streamA + streamB;

// Alternately:
//streamA += streamB;
```