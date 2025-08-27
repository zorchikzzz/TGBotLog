@echo off
echo Запуск скрипта обновления Docker образа...
powershell -ExecutionPolicy Bypass -File "%~dp0update-docker.ps1"
pause