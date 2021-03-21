::Build NuGet packages step
@ECHO off
SETLOCAL

set BinaryOutputDirectory=%1
set Configuration=%1
set Platform=%2

if "%BinaryOutputDirectory%" EQU "" (
set BinaryOutputDirectory=.\bld\bin\
)

if "%Configuration%" EQU "" (
set Configuration=Release
)

if "%Platform%" EQU "" (
set Platform=AnyCpu
)

set BinaryOutputDirectory=%BinaryOutputDirectory%\%Platform%_%Configuration%\Publish
set LayoutForSigningDirectory=%BinaryOutputDirectory%\..\LayoutForSigning

:: Copy all multitargeted assemblies to their locations
call :CopyExeForSigning BinSkim.exe
call :CopyFilesForMultitargeting BinSkim.dll       || goto :ExitFailed
call :CopyFilesForMultitargeting BinaryParsers.dll || goto :ExitFailed
call :CopyFilesForMultitargeting BinSkim.Rules.dll || goto :ExitFailed
call :CopyFilesForMultitargeting BinSkim.Sdk.dll   || goto :ExitFailed

call SetCurrentVersion.cmd
set Version=%MAJOR%.%MINOR%.%PATCH%%PRERELEASE%
set NuGetOutputDirectory=..\..\bld\bin\nuget\
call BuildPackages.cmd %Configuration% %Platform% %NuGetOutputDirectory% %Version% || goto :ExitFailed

goto :Exit

:CopyExeForSigning
xcopy /Y %LayoutForSigningDirectory%\netcoreapp3.1\%~n1.exe %BinaryOutputDirectory%\netcoreapp3.1\ 
xcopy /Y %LayoutForSigningDirectory%\net5.0\%~n1.exe %BinaryOutputDirectory%\net5.0\ 
if "%ERRORLEVEL%" NEQ "0" (echo %1 assembly copy failed. && goto :ExeFilesExit)
:ExeFilesExit
Exit /B %ERRORLEVEL%

:CopyFilesForMultitargeting
xcopy /Y %LayoutForSigningDirectory%\netcoreapp3.1\%~n1.dll %BinaryOutputDirectory%\netcoreapp3.1\ 
xcopy /Y %LayoutForSigningDirectory%\net5.0\%~n1.dll %BinaryOutputDirectory%\net5.0\ 
if "%ERRORLEVEL%" NEQ "0" (echo %1 assembly copy failed. && goto :CopyFilesExit)
:CopyFilesExit
Exit /B %ERRORLEVEL%

:ExitFailed
@echo.
@echo Build NuGet packages from layout directory step failed.
exit /b 1

:Exit