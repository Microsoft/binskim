::Build NuGet packages step
@ECHO off
SETLOCAL

set BinaryOutputDirectory=%1
set Configuration=%2
set Platform=%3

if "%BinaryOutputDirectory%" EQU "" (
set BinaryOutputDirectory=%~dp0bld\bin
)

if "%Configuration%" EQU "" (
set Configuration=Release
)

if "%Platform%" EQU "" (
set Platform=AnyCPU
)

set BinaryOutputDirectory=%BinaryOutputDirectory%\%Platform%_%Configuration%\Publish
set LayoutForSigningDirectory=%BinaryOutputDirectory%\..\LayoutForSigning

call :CreateDirIfNotExist %LayoutForSigningDirectory%
call :CreateDirIfNotExist %LayoutForSigningDirectory%\netcoreapp3.1
call :CreateDirIfNotExist %LayoutForSigningDirectory%\net5.0

call :CopyExeForSigning BinSkim.exe                || goto :ExitFailed
call :CopyFilesForMultitargeting BinSkim.dll      || goto :ExitFailed
call :CopyFilesForMultitargeting BinaryParsers.dll || goto :ExitFailed
call :CopyFilesForMultitargeting BinSkim.Rules.dll || goto :ExitFailed
call :CopyFilesForMultitargeting BinSkim.Sdk.dll   || goto :ExitFailed

goto :Exit

:CopyExeForSigning
xcopy /Y %BinaryOutputDirectory%\netcoreapp3.1\%~n1.exe  %LayoutForSigningDirectory%\netcoreapp3.1\
xcopy /Y %BinaryOutputDirectory%\net5.0\%~n1.exe  %LayoutForSigningDirectory%\net5.0\
if "%ERRORLEVEL%" NEQ "0" (echo %1 assembly copy failed. && goto :ExeFilesExit)
:ExeFilesExit
Exit /B %ERRORLEVEL%

:CopyFilesForMultitargeting
xcopy /Y %BinaryOutputDirectory%\netcoreapp3.1\%~n1.dll  %LayoutForSigningDirectory%\netcoreapp3.1\
xcopy /Y %BinaryOutputDirectory%\net5.0\%~n1.dll  %LayoutForSigningDirectory%\net5.0\
if "%ERRORLEVEL%" NEQ "0" (echo %1 assembly copy failed. && goto :CopyFilesExit)
:CopyFilesExit
Exit /B %ERRORLEVEL%

:CreateDirIfNotExist
set dir=%~1
if not exist %dir% (md %dir%)
Exit /B %ERRORLEVEL%

:ExitFailed
@echo.
@echo Create layout directory step failed.
exit /b 1

:Exit