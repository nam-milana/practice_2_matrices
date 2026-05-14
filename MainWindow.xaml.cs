using System.Collections.Generic;
using System.Windows;
using ScottPlot;

namespace practice_2;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnRunClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(SizeInput.Text, out int size))
            return;

        Loader.Visibility = Visibility.Visible;
        RunButton.IsEnabled = false;

        var results = await MatrixBenchmark.Run(size);

        // 1. Фильтруем и рисуем верхние графики
        var stdResults = results.Where(r => r.Name.Contains("Стандартная")).ToList();
        var optResults = results.Where(r => r.Name.Contains("Оптимизированная")).ToList();

        DrawVerticalPlot(StandardPlot, stdResults, $"trace(A * B^T)", Colors.IndianRed);
        DrawVerticalPlot(
            OptimizedPlot,
            optResults,
            $"сумма по i,j A[i,j]*B[i,j]",
            Colors.ForestGreen
        );

        // 2. Рисуем нижний общий горизонтальный график
        DrawHorizontalSummaryPlot(SummaryPlot, results, $"Все результаты (N={size})");

        Loader.Visibility = Visibility.Collapsed;
        RunButton.IsEnabled = true;
    }

    // Метод для верхних вертикальных графиков (с наклоном текста)
    private void DrawVerticalPlot(
        ScottPlot.WPF.WpfPlot plot,
        List<BenchmarkResult> results,
        string title,
        Color barColor
    )
    {
        plot.Plot.Clear();
        if (results.Count == 0)
            return;

        double[] values = results.Select(r => r.TimeMs).ToArray();

        var bars = plot.Plot.Add.Bars(values);
        foreach (var bar in bars.Bars)
        {
            bar.FillColor = barColor;
            bar.Size = 0.4;
        }

        var ticks = results.Select((r, i) => new ScottPlot.Tick(i, r.Name)).ToArray();
        plot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;

        plot.Plot.Title(title);
        plot.Plot.YLabel("Время (мс)");

        plot.Plot.Axes.SetLimitsX(-0.5, values.Length - 0.5);
        plot.Plot.Axes.SetLimitsY(0, values.Max() * 1.2);

        plot.Refresh();
    }

    private void DrawHorizontalSummaryPlot(
        ScottPlot.WPF.WpfPlot plot,
        List<BenchmarkResult> results,
        string title
    )
    {
        plot.Plot.Clear();
        if (results.Count == 0)
            return;

        // Сортируем результаты по убыванию времени, чтобы самый медленный был сверху
        var sortedResults = results.OrderByDescending(r => r.TimeMs).ToList();

        double maxValue = sortedResults[0].TimeMs; // Это наши 100%

        // Вместо сырых миллисекунд передаем в бары проценты (от 0 до 100)
        double[] percentages = sortedResults.Select(r => (r.TimeMs / maxValue) * 100).ToArray();

        var bars = plot.Plot.Add.Bars(percentages);
        bars.Horizontal = true;

        for (int i = 0; i < bars.Bars.Count; i++)
        {
            bars.Bars[i].FillColor = sortedResults[i].Name.Contains("Стандартная")
                ? Colors.CornflowerBlue
                : Colors.LemonChiffon;
            bars.Bars[i].Size = 0.5;

            double ms = sortedResults[i].TimeMs;
            double pct = percentages[i];

            // Формируем красивую и точную строку
            string labelText;
            if (pct == 100)
            {
                labelText = $" 100% ({ms:F0} мс)";
            }
            else if (pct < 0.01)
            {
                labelText = $" {pct:F4}% ({ms:F2} мс)";
            }
            else
            {
                labelText = $" {pct:F2}% ({ms:F1} мс)";
            }

            // Добавляем текст на графике. Координата X — это процентная длина полосы.
            var text = plot.Plot.Add.Text(labelText, pct, i);
            text.LabelFontColor = Colors.Black;
            text.LabelBold = true;
            text.LabelFontSize = 16;
            text.LabelAlignment = i == 0 ? Alignment.MiddleRight : Alignment.MiddleLeft;
        }

        // Привязываем названия тестов к левой оси Y
        var ticks = sortedResults.Select((r, i) => new ScottPlot.Tick(i, r.Name)).ToArray();
        plot.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

        plot.Plot.Title(title);
        plot.Plot.XLabel("Процент от времени самого долгого решения (%)");

        // Настраиваем лимиты по оси X ровно от 0 до 135 (чтобы влез текст справа)
        plot.Plot.Axes.SetLimitsX(0, 100);
        plot.Plot.Axes.SetLimitsY(-0.6, percentages.Length - 0.4);

        plot.Refresh();
    }
}
