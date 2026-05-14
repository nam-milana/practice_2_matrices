using System;
using System.Text;
using System.Threading.Tasks;

public class Matrix
{
    private readonly double[,] _data;

    public int Rows { get; }
    public int Columns { get; }

    public double this[int r, int c]
    {
        get => _data[r, c];
        set => _data[r, c] = value;
    }

    public Matrix(int rows, int cols)
    {
        if (rows <= 0 || cols <= 0)
            throw new ArgumentException("Размеры матрицы должны быть положительными");

        (Rows, Columns) = (rows, cols);
        _data = new double[rows, cols];
    }

    public Matrix(double[,] source)
        : this(source.GetLength(0), source.GetLength(1))
    {
        Array.Copy(source, _data, source.Length);
    }

    public static Matrix operator +(Matrix a, Matrix b) => Add(a, b);

    public static Matrix Add(Matrix a, Matrix b, bool parallel = false)
    {
        ValidateSameSize(a, b);
        var result = new Matrix(a.Rows, a.Columns);

        Execute(
            0,
            a.Rows,
            i =>
            {
                for (int j = 0; j < a.Columns; j++)
                    result._data[i, j] = a._data[i, j] + b._data[i, j];
            },
            parallel
        );

        return result;
    }

    public static Matrix operator *(Matrix a, Matrix b) => Multiply(a, b);

    public static Matrix Multiply(Matrix a, Matrix b, bool parallel = false)
    {
        ValidateForMultiplication(a, b);
        var result = new Matrix(a.Rows, b.Columns);

        Execute(0, a.Rows, i => MultiplyRow(a, b, result, i), parallel);

        return result;
    }

    public static Matrix Transpose(Matrix target, bool parallel = false)
    {
        var result = new Matrix(target.Columns, target.Rows);

        Execute(
            0,
            target.Rows,
            i =>
            {
                for (int j = 0; j < target.Columns; j++)
                    result._data[j, i] = target._data[i, j];
            },
            parallel
        );

        return result;
    }

    public static double Trace(Matrix a)
    {
        if (a.Rows != a.Columns)
            throw new ArgumentException("Матрица должна быть квадратной");

        double sum = 0;
        for (int i = 0; i < a.Rows; i++)
            sum += a._data[i, i];
        return sum;
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

    private static void MultiplyRow(Matrix a, Matrix b, Matrix result, int i)
    {
        for (int k = 0; k < b.Columns; k++)
        {
            double sum = 0;
            for (int j = 0; j < a.Columns; j++)
                sum += a._data[i, j] * b._data[j, k];
            result._data[i, k] = sum;
        }
    }

    private static void ValidateSameSize(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Columns != b.Columns)
            throw new ArgumentException("Размеры матриц должны совпадать");
    }

    private static void ValidateForMultiplication(Matrix a, Matrix b)
    {
        if (a.Columns != b.Rows)
            throw new ArgumentException("Число столбцов A должно быть равно числу строк B");
    }

    public static Matrix CreateRandom(
        int rows,
        int cols,
        Random random,
        double min = 0,
        double max = 1
    )
    {
        var m = new Matrix(rows, cols);
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            m._data[i, j] = random.NextDouble() * (max - min) + min;
        return m;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
                sb.Append($"{_data[i, j]:F2}\t");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
