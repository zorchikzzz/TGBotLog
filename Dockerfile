# Стадия сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем весь код и собираем приложение
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Финальная стадия
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Устанавливаем необходимые пакеты
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    sudo \
    sqlite3 \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

# Устанавливаем часовой пояс Moscow
RUN ln -fs /usr/share/zoneinfo/Europe/Moscow /etc/localtime && \
    echo "Europe/Moscow" > /etc/timezone

# Создаем директорию для данных
RUN mkdir -p /app/data

# Создаем пользователя и группу
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Копируем собранное приложение из стадии сборки
COPY --from=build /app/publish .

# Устанавливаем права на директории
RUN chown -R appuser:appgroup /app && \
    chmod -R 755 /app

# Создаем скрипт инициализации базы данных
RUN echo '#!/bin/sh\n\
# Создаем директорию для базы данных\n\
mkdir -p /app/data\n\
\n\
# Проверяем существование файла базы данных\n\
if [ ! -f /app/data/budget.db ]; then\n\
    echo "Initializing database..."\n\
    # Создаем новую базу данных SQLite\n\
    sqlite3 /app/data/budget.db "VACUUM;"\n\
    echo "New database created: /app/data/budget.db"\n\
else\n\
    echo "Using existing database: /app/data/budget.db"\n\
fi\n\
\n\
# Устанавливаем правильные права на файл базы данных\n\
chown appuser:appgroup /app/data/budget.db 2>/dev/null || true\n\
chmod 664 /app/data/budget.db 2>/dev/null || true\n\
\n\
# Запускаем основное приложение\n\
echo "Starting application with database: /app/data/budget.db"\n\
exec dotnet TGBotLog.dll' > /init.sh && \
    chmod +x /init.sh
