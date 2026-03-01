using SkiaSharp;
using FamilyBudgetBot.Data.Repositories;
using TGBotLog.Data.Models;

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
            var data = _repository.GetMonthlySummary(12);
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

            // Светло-серый фон
            canvas.Clear(SKColors.LightGray);

            // Определяем максимальное значение для масштабирования
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

            // Горизонтальные линии сетки и подписи по Y
            using (var linePaint = new SKPaint { Color = SKColors.DarkGray, StrokeWidth = 1, IsAntialias = true })
            using (var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true })
            {
                for (int i = 0; i <= 20; i++)
                {
                    float y = height - marginBottom - (i / 20f) * graphHeight;
                    decimal val = (maxValue / 20) * i;
                    canvas.DrawLine(marginLeft, y, width - marginRight, y, linePaint);
                    canvas.DrawText(val.ToString("N0"), 5, y - 5, textPaint);
                }
            }

            // Вычисляем координаты X для каждого месяца (центр интервала)
            float[] xPositions = data
                .Select((_, i) => marginLeft + (i + 0.5f) * (graphWidth / data.Count))
                .ToArray();

            // Строим график дохода (зелёная линия)
            using (var incomePath = new SKPath())
            using (var incomePaint = new SKPaint { Color = SKColors.Green, StrokeWidth = 3, IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                bool first = true;
                for (int i = 0; i < data.Count; i++)
                {
                    float y = height - marginBottom - (float)(data[i].Income / maxValue) * graphHeight;
                    if (first)
                    {
                        incomePath.MoveTo(xPositions[i], y);
                        first = false;
                    }
                    else
                    {
                        incomePath.LineTo(xPositions[i], y);
                    }
                }
                canvas.DrawPath(incomePath, incomePaint);
            }

            // Строим график расхода (красная линия)
            using (var expensePath = new SKPath())
            using (var expensePaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 3, IsAntialias = true, Style = SKPaintStyle.Stroke })
            {
                bool first = true;
                for (int i = 0; i < data.Count; i++)
                {
                    float y = height - marginBottom - (float)(data[i].Expense / maxValue) * graphHeight;
                    if (first)
                    {
                        expensePath.MoveTo(xPositions[i], y);
                        first = false;
                    }
                    else
                    {
                        expensePath.LineTo(xPositions[i], y);
                    }
                }
                canvas.DrawPath(expensePath, expensePaint);
            }

            // Рисуем маркеры для точек дохода (зелёные кружки)
            using (var markerPaint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                for (int i = 0; i < data.Count; i++)
                {
                    float y = height - marginBottom - (float)(data[i].Income / maxValue) * graphHeight;
                    canvas.DrawCircle(xPositions[i], y, 5, markerPaint);
                }
            }

            // Рисуем маркеры для точек расхода (красные кружки)
            using (var markerPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                for (int i = 0; i < data.Count; i++)
                {
                    float y = height - marginBottom - (float)(data[i].Expense / maxValue) * graphHeight;
                    canvas.DrawCircle(xPositions[i], y, 5, markerPaint);
                }
            }

            // Подписи месяцев под осью X
            using (var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 10, IsAntialias = true })
            {
                for (int i = 0; i < data.Count; i++)
                {
                    string monthLabel = $"{data[i].Month:00}/{data[i].Year}";
                    float textWidth = textPaint.MeasureText(monthLabel);
                    canvas.DrawText(monthLabel, xPositions[i] - textWidth / 2, height - marginBottom + 20, textPaint);
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

            // Сохраняем изображение
            using var image = surface.Snapshot();
            using var dataStream = image.Encode(SKEncodedImageFormat.Png, 100);
            return dataStream.ToArray();
        }
    

    

        public byte[] GenerateCategoryChart(List<CategoryChartData> data, string periodTitle)
        {
            if (data == null || data.Count == 0)
                return null;

            const int width = 1000;
            const int height = 600;
            const int marginLeft = 80;
            const int marginRight = 60;
            const int marginTop = 80;
            const int marginBottom = 80;

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Заголовок
            string title = $"Категории: {periodTitle}";
            using (var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 18,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                float textWidth = paint.MeasureText(title);
                canvas.DrawText(title, (width - textWidth) / 2, 30, paint);
            }

            // Определяем максимум для масштабирования
            decimal maxValue = data.SelectMany(x => new[] { x.Income, x.Expense }).Max();
            if (maxValue == 0) maxValue = 1;

            float graphHeight = height - marginTop - marginBottom;
            float graphWidth = width - marginLeft - marginRight;

            // Оси
            using (var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true })
            {
                canvas.DrawLine(marginLeft, marginTop, marginLeft, height - marginBottom, paint);
                canvas.DrawLine(marginLeft, height - marginBottom, width - marginRight, height - marginBottom, paint);
            }

            // Подписи по Y
            using (var paint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true })
            {
                for (int i = 0; i <= 15; i++)
                {
                    float y = height - marginBottom - (i / 15f) * graphHeight;
                    decimal val = (maxValue / 15) * i;
                    canvas.DrawText(val.ToString("N0"), 5, y - 5, paint);
                }
            }

            // Ширина одного столбца
            float barWidth = (graphWidth / data.Count) * 0.8f;
            float halfBar = barWidth / 2;

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float xBase = marginLeft + (i + 0.6f) * (graphWidth / data.Count);

                // Доход (зелёный)
                float incomeHeight = (float)(item.Income / maxValue) * graphHeight;
                if (incomeHeight > 0)
                {
                    using (var paint = new SKPaint { Color = SKColors.Green, Style = SKPaintStyle.Fill })
                    {
                        canvas.DrawRect(xBase - halfBar, height - marginBottom - incomeHeight, halfBar, incomeHeight, paint);
                    }
                }

                // Расход (красный)
                float expenseHeight = (float)(item.Expense / maxValue) * graphHeight;
                if (expenseHeight > 0)
                {
                    using (var paint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Fill })
                    {
                        canvas.DrawRect(xBase, height - marginBottom - expenseHeight, halfBar, expenseHeight, paint);
                    }
                }

                // Подпись категории
                string label = item.CategoryName.Length > 10 ? item.CategoryName.Substring(0, 8) + ".." : item.CategoryName;
                // Вместо обычного DrawText используем трансформацию canvas
                using (new SKAutoCanvasRestore(canvas)) // восстанавливаем состояние после трансформации
                {
                    // Сдвигаемся в точку привязки и поворачиваем
                    canvas.Translate(xBase, height - marginBottom + 50);
                    canvas.RotateDegrees(-30); // или -90 для вертикального текста

                    using (var paint = new SKPaint
                    {
                        Color = SKColors.Black,
                        TextSize = 10,
                        IsAntialias = true
                    })
                    {
                        canvas.DrawText(label, 0, 0, paint);
                    }
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