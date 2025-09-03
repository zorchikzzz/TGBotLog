using FamilyBudgetBot.Bot.Handlers;
using FamilyBudgetBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TGBotLog.Bot.Handlers
{
    public class CallbackHandler
    {
        private readonly ITelegramBotClient _bot;
        private readonly PendingActionHandler _pendingActionHandler;
        private readonly CommandHandler _commandHandler;

        public CallbackHandler(ITelegramBotClient bot, PendingActionHandler pendingActionHandler, CommandHandler commandHandler)
        {
            _bot = bot;
            _pendingActionHandler = pendingActionHandler;
            _commandHandler = commandHandler;
        }


        public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);

            switch (data)
            {
                case "income_categories":
                    await _commandHandler.ShowIncomeCategories(chatId);
                    break;

                case "income_categorie_selected":
                    await _pendingActionHandler.HandleCategoryTypeSelection(chatId, "ДОХОД");
                    break;

                case "expance_categorie_selected":
                    await _pendingActionHandler.HandleCategoryTypeSelection(chatId, "РАСХОД");
                    break;


            }

        }

    }
}
