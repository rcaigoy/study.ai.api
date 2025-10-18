@echo off
echo ========================================
echo Starting study.ai.api on port 44335
echo ========================================
echo.
echo API will be available at:
echo   https://localhost:44335
echo.
echo Press Ctrl+C to stop the server
echo ========================================
echo.

cd study.ai.api
dotnet run

pause

