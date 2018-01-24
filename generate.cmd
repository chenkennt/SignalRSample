setlocal

set src=%1
set dst=%2
set version=%3

for /f %%p in ('dir %dst% /b') do (
    pushd %src%\%%p
    dotnet build
    dotnet pack
    popd
    del /s /q %dst%\%%p
    nuget add %src%\%%p\bin\Debug\%%p.%version%.nupkg -Source %dst%
)
