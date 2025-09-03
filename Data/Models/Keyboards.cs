using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

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

    }
                 

        

}

