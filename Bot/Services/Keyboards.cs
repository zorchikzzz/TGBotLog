using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace TGBotLog.Bot.Services
{
    public static class Keyboards
    {

        public static readonly ReplyKeyboardMarkup MainMenu = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "ОТЧЁТ" , "КАТЕГОРИИ" },
                new KeyboardButton[] { "СПРАВКА" , "ДОБАВИТЬ КАТЕГОРИЮ" },

            })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };


        public static readonly ReplyKeyboardMarkup SelectTypeOfCategorie = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] {"ДОХОД", "РАСХОД" },
            new KeyboardButton[] {"ОТМЕНА" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };


    }
                 

        

}

