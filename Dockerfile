FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Устанавливаем необходимые пакеты
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    sudo \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Копируем файлы приложения
COPY . .

# Создаем директорию для данных
RUN mkdir -p /data

# Создаем пользователя и группу
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Устанавливаем права на директории
RUN chown -R appuser:appgroup /app /data && \
    chmod -R 755 /app && \
    chmod -R 775 /data

# Создаем скрипт инициализации базы данных
RUN echo '#!/bin/sh\n\
# Проверяем существование файла базы данных в томе\n\
if [ ! -f /data/database.db ]; then\n\
    echo "Initializing database from image..."\n\
    # Пытаемся скопировать базу из образа, если существует\n\
    if [ -f /app/database.db ]; then\n\
        cp /app/database.db /data/database.db\n\
        echo "Database copied from image to volume"\n\
    else\n\
        # Создаем новую базу данных SQLite\n\
        sqlite3 /data/database.db "VACUUM;"\n\
        echo "New empty database created in volume"\n\
    fi\n\
else\n\
    echo "Using existing database from volume: $(ls -la /data/database.db)"\n\
fi\n\
\n\
# Устанавливаем правильные права на файл базы данных\n\
chown appuser:appgroup /data/database.db 2>/dev/null || true\n\
chmod 664 /data/database.db 2>/dev/null || true\n\
\n\
# Запускаем основное приложение\n\
echo "Starting application with database: /data/database.db"\n\
exec dotnet TGBotLog.dll' > /init.sh && \
    chmod +x /init.sh

# Переключаемся на пользователя appuser
USER appuser

# Запускаем приложение через скрипт инициализации
ENTRYPOINT ["/init.sh"]