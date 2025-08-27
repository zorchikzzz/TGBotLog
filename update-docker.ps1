# Скрипт для автоматической сборки и обновления Docker образа
# Запускается двойным кликом в Windows

# Параметры
$publishPath = "C:\DockerApps\NewBot"
$imageName = "zorovr/tgbotlog-family-budget-bot"
$containerName = "family-budget-bot"

# Функция для вывода сообщений с цветом
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

try {
    Write-ColorOutput "=== Начало процесса обновления ===" "Green"
    
    # 1. Публикация .NET приложения
    Write-ColorOutput "Публикация .NET приложения..." "Yellow"
    dotnet publish -c Release -o $publishPath --self-contained false
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Публикация завершена успешно!" "Green"
    } else {
        throw "Ошибка при публикации приложения"
    }
    
    # 2. Переход в директорию с Docker проектом
    Set-Location $publishPath
    
    # 3. Проверка работы Docker
    Write-ColorOutput "Проверка состояния Docker..." "Yellow"
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker не запущен или работает некорректно. Запустите Docker Desktop и повторите попытку."
    }
    
    # 4. Остановка и удаление старого контейнера
    Write-ColorOutput "Проверка запущенных контейнеров..." "Yellow"
    $runningContainer = docker ps -aq -f "name=$containerName"
    if ($runningContainer) {
        Write-ColorOutput "Останавливаем и удаляем старый контейнер..." "Yellow"
        docker stop $containerName 2>&1 | Out-Null
        docker rm $containerName 2>&1 | Out-Null
        Write-ColorOutput "Старый контейнер удален" "Green"
    }
    
    # 5. Удаление старого образа
    Write-ColorOutput "Проверка существующих образов..." "Yellow"
    $existingImage = docker images -q "$imageName*"
    if ($existingImage) {
        Write-ColorOutput "Удаляем старый образ..." "Yellow"
        docker rmi -f $existingImage 2>&1 | Out-Null
        Write-ColorOutput "Старый образ удален" "Green"
    }
    
    # 6. Создание нового Docker образа
    Write-ColorOutput "Создание нового Docker образа..." "Yellow"
    docker-compose build
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Docker образ успешно создан!" "Green"
    } else {
        throw "Ошибка при создании Docker образа"
    }
    
    # 7. Запуск нового контейнера
    Write-ColorOutput "Запуск нового контейнера..." "Yellow"
    docker-compose up -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Новый контейнер успешно запущен!" "Green"
    } else {
        Write-ColorOutput "Образ создан, но контейнер не запущен" "Yellow"
    }
    
    Write-ColorOutput "=== Процесс обновления завершен ===" "Green"
    
} catch {
    Write-ColorOutput "Ошибка: $($_.Exception.Message)" "Red"
}

# Пауза чтобы увидеть результат
Write-ColorOutput "Нажмите любую клавишу для выхода..." "Gray"
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")