@echo off
REM =======================================
REM  runbank.bat
REM  A shortcut script to run the BankerApp
REM =======================================

echo Running the BankerApp with arguments: %*

REM Call dotnet run, passing along all arguments.
dotnet run --project BankerApp\BankerApp.csproj -- %*

echo.
echo Done!