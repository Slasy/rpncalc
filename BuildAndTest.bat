@echo off

for %%C in (Debug Release) do (
    echo === Building %%C version ===
    dotnet clean -c %%C
    dotnet build -c %%C
    dotnet test -c %%C
)
