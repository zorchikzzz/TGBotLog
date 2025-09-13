using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using Microsoft.VisualBasic;

namespace TGBotLog.Data.Models
{
    public static class Keyboards
    {

        public static readonly ReplyKeyboardMarkup MainMenu =
            new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "ОТЧЁТ" , "НЕДАВНИЕ ОПЕРАЦИИ" },
                new KeyboardButton[] { "КАТЕГОРИИ" , "ДОБАВИТЬ КАТЕГОРИЮ" },

            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false

            };


        public static readonly ReplyKeyboardMarkup SelectTypeOfCategorie =
            new ReplyKeyboardMarkup(new[]
            {
            //new KeyboardButton[] {"ДОХОД", "РАСХОД" },
            new KeyboardButton[] {"ОТМЕНА" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false

            };

        public static readonly InlineKeyboardMarkup showIncomeCategoriesButton = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("КАТЕГОРИИ ДОХОДОВ", "income_categories")
            }
        });

        public static readonly InlineKeyboardMarkup SelectTypeOFCategorie = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ДОХОД", "income_categorie_selected"),
                InlineKeyboardButton.WithCallbackData("РАСХОД", "expance_categorie_selected")

            }
            
        });

        /// Создаёт инлайн-клавиатуру с кнопками месяцев и "Выбрать год"

        public static InlineKeyboardMarkup CreateYearMonthSelectionKB(int year, List<int> months)
        {
            var buttons = new List<InlineKeyboardButton[]>();

            bool onecolumn = false; // Переменная для переключения между двумя вариантами клавиатуры, в одну или три колонки

            if (onecolumn == false)
            {
                // В одну колонку

                for (int i = 0; i < months.Count; i += 3)
                {
                    var row = new List<InlineKeyboardButton>();

                    for (int j = 0; j < 3 && i + j < months.Count; j++)
                    {
                        var month = months[i + j];
                        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                        row.Add(InlineKeyboardButton.WithCallbackData(
                            $"{monthName.ToUpper()}",
                            $"report_month_{year}_{month}"));
                    }

                    buttons.Add(row.ToArray());
                }
            }
            else
            {
                //В три колонки

                foreach (var month in months)
                {
                    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                    buttons.Add(new[]
                    {
                    InlineKeyboardButton.WithCallbackData(
                        $"{monthName.ToUpper()}",
                        $"report_month_{year}_{month}")
                });
                }
            }
            // Кнопка "Выбрать год"
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("Выбрать год", "select_report_year")
            });

            return new InlineKeyboardMarkup(buttons);
        }
        public static InlineKeyboardMarkup CreateYearsSelectionKB(List<int> years)
        {
            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var year in years)
            {
                buttons.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData(
                year.ToString(),
                $"select_month_{year}")
                });
            }

            return new InlineKeyboardMarkup(buttons);

        }

         public static readonly InlineKeyboardMarkup SelectReportPeriod = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Выбрать период", "select_report_period")
            }
        });


    }

}

