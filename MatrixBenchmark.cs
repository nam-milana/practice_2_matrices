using System.Diagnostics;

public record BenchmarkResult(string Name, double TimeMs);

public static class MatrixBenchmark
{
    public static async Task<List<BenchmarkResult>> Run(int size)
    {
        var rnd = new Random();

        return await Task.Run(() =>
        {
            var a = Matrix.CreateRandom(size, size, rnd);
            var b = Matrix.CreateRandom(size, size, rnd);

            var results = new List<BenchmarkResult>
            {
                Measure("Стандартная синхронная", () => ComputeStandard(a, b, false)),
                Measure("Стандартная параллельная", () => ComputeStandard(a, b, true)),
                Measure("Оптимизированная синхронная", () => ComputeSimplified(a, b, false)),
                Measure("Оптимизированная параллельная", () => ComputeSimplified(a, b, true)),
            };
            return results;
        });
    }

    private static BenchmarkResult Measure(string name, Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        return new BenchmarkResult(name, sw.Elapsed.TotalMilliseconds);
    }

    private static (Matrix A, Matrix B) PrepareData(int rows, int cols, Random rnd)
    {
        return (Matrix.CreateRandom(rows, cols, rnd), Matrix.CreateRandom(rows, cols, rnd));
    }

    static double ComputeStandard(Matrix a, Matrix b, bool parallel)
    {
        var bTransposed = Matrix.Transpose(b, parallel);
        var product = Matrix.Multiply(a, bTransposed, parallel);
        return Matrix.Trace(product);
    }

    static double ComputeSimplified(Matrix a, Matrix b, bool parallel)
    {
        double totalSum = 0;
        object lockObj = new();

        Execute(
            0,
            a.Rows,
            i =>
            {
                double rowSum = 0;

                for (int j = 0; j < a.Columns; j++)
                    rowSum += a[i, j] * b[i, j];

                lock (lockObj)
                    totalSum += rowSum;
            },
            parallel
        );

        return totalSum;
    }

    private static void Execute(int start, int end, Action<int> action, bool parallel)
    {
        if (parallel)
            Parallel.For(start, end, action);
        else
        {
            for (int i = start; i < end; i++)
                action(i);
        }
    }
}
