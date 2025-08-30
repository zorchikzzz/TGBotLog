# Скрипт для автоматической сборки и обновления Docker образа
# Запускается двойным кликом в Windows

# Параметры
$projectPath = "C:\TGBotLog\TGBotLog\TGBotLog.csproj"  # Полный путь к .csproj файлу
$solutionPath = "C:\TGBotLog\TGBotLog\TGBotLog.sln"  # Путь к решению (если есть)
$publishPath = "C:\DockerApps\BotsinServer\app"
$imageName = "zorovr/tgbotlog-family-budget-bot:latest"
$dockerfilePath = "C:\TGBotLog\TGBotLog\Dockerfile"  # Путь к Dockerfile

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
    
    # Проверка, запущен ли Docker
    $dockerStatus = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker не запущен. Запустите Docker Desktop и попробуйте снова."
    }
    
    # Проверка существования необходимых файлов
    if (-not (Test-Path $projectPath)) {
        throw "Файл проекта не найден: $projectPath"
    }
    
    if (-not (Test-Path $dockerfilePath)) {
        throw "Dockerfile не найден: $dockerfilePath"
    }
    
    # 1. Очистка предыдущей публикации
    if (Test-Path $publishPath) {
        Write-ColorOutput "Очистка предыдущей публикации..." "Yellow"
        Remove-Item -Path $publishPath -Recurse -Force
    }
    
    # Создание папки для публикации
    New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
    
    # 2. Публикация .NET приложения
    Write-ColorOutput "Публикация .NET приложения..." "Yellow"
    
    # Публикуем из папки решения, чтобы избежать предупреждений
    Set-Location (Split-Path $solutionPath -Parent)
    dotnet publish $solutionPath -c Release -o $publishPath --self-contained false
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Публикация завершена успешно!" "Green"
    } else {
        throw "Ошибка при публикации приложения"
    }
    
    # 3. Копирование Dockerfile в папку публикации
    Write-ColorOutput "Копирование Dockerfile..." "Yellow"
    Copy-Item -Path $dockerfilePath -Destination $publishPath -Force
    
    # 4. Сборка Docker образа
    Write-ColorOutput "Сборка Docker образа..." "Yellow"
    Set-Location $publishPath
    docker build -t $imageName .
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Сборка Docker образа завершена успешно!" "Green"
    } else {
        throw "Ошибка при сборке Docker образа"
    }
    
    # 5. Авторизация в Docker Hub (если не авторизованы)
    Write-ColorOutput "Проверка авторизации в Docker Hub..." "Yellow"
    $authCheck = docker info | Select-String "Username"
    if (-not $authCheck) {
        Write-ColorOutput "Требуется авторизация в Docker Hub..." "Yellow"
        docker login
        if ($LASTEXITCODE -ne 0) {
            throw "Ошибка авторизации в Docker Hub"
        }
    }
    
    # 6. Отправка образа на Docker Hub
    Write-ColorOutput "Отправка образа на Docker Hub..." "Yellow"
    docker push $imageName
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "Образ успешно отправлен на Docker Hub!" "Green"
    } else {
        throw "Ошибка при отправке образа на Docker Hub"
    }
    
    Write-ColorOutput "=== Процесс обновления завершен успешно! ===" "Green"
    
} catch {
    Write-ColorOutput "Ошибка: $($_.Exception.Message)" "Red"
    Write-ColorOutput "Процесс прерван." "Red"
    pause
    exit 1
}

# Пауза чтобы увидеть результат
pause