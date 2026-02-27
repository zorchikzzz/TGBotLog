using SkiaSharp;
using FamilyBudgetBot.Data.Repositories;
using System.IO;

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
            var data = _repository.GetMonthlySummary();
            if (data.Count == 0)
                return null;

            const int width = 900;
            const int height = 500;
            const int marginLeft = 70;
            const int marginRight = 40;
            const int marginTop = 40;
            const int marginBottom = 70;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Определение максимума для масштабирования
            decimal maxValue = data.SelectMany(x => new[] { x.Income, x.Expense }).Max();
            if (maxValue == 0) maxValue = 1;

            float graphHeight = height - marginTop - marginBottom;
            float graphWidth = width - marginLeft - marginRight;

            // Оси
            using (var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true })
            {
                canvas.DrawLine(marginLeft, marginTop, marginLeft, height - marginBottom, paint); // Y
                canvas.DrawLine(marginLeft, height - marginBottom, width - marginRight, height - marginBottom, paint); // X
            }

            // Подписи по оси Y (суммы)
            using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true, Typeface = SKTypeface.Default })
            {
                for (int i = 0; i <= 5; i++)
                {
                    float y = height - marginBottom - (i / 5f) * graphHeight;
                    decimal val = (maxValue / 5) * i;
                    canvas.DrawText(val.ToString("N0"), 5, y - 5, paint);
                }
            }

            // Ширина одного столбца (для двух столбцов на месяц)
            float barWidth = (graphWidth / data.Count) * 0.7f;
            float halfBar = barWidth / 2;

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

            // Легенда
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

            using var image = surface.Snapshot();
            using var dataStream = image.Encode(SKEncodedImageFormat.Png, 100);
            return dataStream.ToArray();
        }
    }
}