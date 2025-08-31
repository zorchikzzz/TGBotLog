# Скрипт для автоматической сборки и обновления Docker образа
# Запускается двойным кликом в Windows

# Определяем директорию скрипта
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path -Parent $scriptPath

# Параметры (абсолютные пути от расположения скрипта)
$projectPath = Join-Path $scriptDir "TGBotLog.csproj"
$solutionPath = Join-Path $scriptDir "TGBotLog.sln"
$publishPath = "C:\DockerApps\BotsinServer"
$imageName = "zorovr/tgbotlog-family-budget-bot:latest"
$dockerfilePath = Join-Path $scriptDir "Dockerfile"

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
    
    # Публикуем из директории скрипта
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
    # Сохраняем текущую директорию
    $currentLocation = Get-Location
    Set-Location $publishPath
    docker build -t $imageName .
    # Возвращаемся обратно
    Set-Location $currentLocation
    
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