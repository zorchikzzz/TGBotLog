FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Устанавливаем необходимые пакеты
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    sudo \
    && rm -rf /var/lib/apt/lists/*

# Копируем файлы приложения
COPY . .

# Создаем пользователя и группу
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Создаем директорию для базы данных и устанавливаем права
RUN mkdir -p /app/database && \
    chown -R appuser:appgroup /app && \
    chmod -R 755 /app

# Переключаемся на пользователя appuser
USER appuser

# Запускаем приложение
ENTRYPOINT ["dotnet", "TGBotLog.dll"]