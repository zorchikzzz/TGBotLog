using SkiaSharp;
using FamilyBudgetBot.Data.Repositories;
using System.IO;
using System.Linq;

namespace FamilyBudgetBot.Services
{
    public class ChartService
    {
        private readonly BudgetRepository _repository;

        public ChartService(BudgetRepository repository)
        {
            _repository = repository;
        }

        public byte[] GenerateMonthlyChart()
        {
            var data = _repository.GetMonthlySummary(24);
            if (data.Count == 0)
                return null;

            const int width = 1800;
            const int height = 1000;
            const int marginLeft = 70;
            const int marginRight = 40;
            const int marginTop = 40;
            const int marginBottom = 70;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            // --- Светло-серый фон всего изображения ---
            canvas.Clear(SKColors.LightGray);

            // Определение максимума для масштабирования
            decimal maxValue = data.SelectMany(x => new[] { x.Income, x.Expense }).Max();
            if (maxValue == 0) maxValue = 1;

            float graphHeight = height - marginTop - marginBottom;
            float graphWidth = width - marginLeft - marginRight;

            // --- Рисуем оси (чёрные линии) ---
            using (var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true })
            {
                canvas.DrawLine(marginLeft, marginTop, marginLeft, height - marginBottom, paint); // Y
                canvas.DrawLine(marginLeft, height - marginBottom, width - marginRight, height - marginBottom, paint); // X
            }

            // --- Горизонтальные линии сетки и подписи по оси Y ---
            using (var linePaint = new SKPaint { Color = SKColors.DarkGray, StrokeWidth = 1, IsAntialias = true })
            using (var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true, Typeface = SKTypeface.Default })
            {
                for (int i = 0; i <= 20; i++)
                {
                    float y = height - marginBottom - (i / 20f) * graphHeight;
                    decimal val = (maxValue / 20) * i;

                    // Горизонтальная линия от левого до правого края области графика
                    canvas.DrawLine(marginLeft, y, width - marginRight, y, linePaint);

                    // Текст подписи значения слева от графика
                    canvas.DrawText(val.ToString("N0"), 5, y - 5, textPaint);
                }
            }

            // Ширина одного столбца (для двух столбцов на месяц)
            float barWidth = (graphWidth / data.Count) * 0.8f;
            float halfBar = barWidth / 2;

            // --- Рисуем столбцы доходов и расходов ---
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float xBase = marginLeft + (i + 0.5f) * (graphWidth / data.Count);

                // Столбец дохода (зелёный)
                float incomeHeight = (float)(item.Income / maxValue) * graphHeight;
                using (var paint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill })
                {
                    canvas.DrawRect(xBase - halfBar, height - marginBottom - incomeHeight, halfBar, incomeHeight, paint);
                }

                // Столбец расхода (красный)
                float expenseHeight = (float)(item.Expense / maxValue) * graphHeight;
                using (var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill })
                {
                    canvas.DrawRect(xBase, height - marginBottom - expenseHeight, halfBar, expenseHeight, paint);
                }

                // Подпись месяца
                string monthLabel = $"{item.Month:00}/{item.Year}";
                using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 10, IsAntialias = true })
                {
                    float textWidth = paint.MeasureText(monthLabel);
                    canvas.DrawText(monthLabel, xBase - textWidth / 2, height - marginBottom + 20, paint);
                }
            }

            // --- Легенда ---
            using (var paint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(width - 150, marginTop + 10, 15, 15, paint);
            }
            using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 14 })
            {
                canvas.DrawText("Доход", width - 130, marginTop + 23, paint);
            }
            using (var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(width - 150, marginTop + 35, 15, 15, paint);
            }
            using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 14 })
            {
                canvas.DrawText("Расход", width - 130, marginTop + 48, paint);
            }

            // --- Сохраняем результат в PNG ---
            using var image = surface.Snapshot();
            using var dataStream = image.Encode(SKEncodedImageFormat.Png, 100);
            return dataStream.ToArray();
        }
    }
}