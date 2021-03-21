@echo off
SETLOCAL
@REM Uncomment this line to update nuget.exe
@REM Doing so can break SLN build (which uses nuget.exe to
@REM create a nuget package for binskim) so must opt-in
@REM %~dp0.nuget\NuGet.exe update -self

set Configuration=%1
set Platform=AnyCPU

if "%Configuration%" EQU "" (
set Configuration=Release
)

@REM Remove existing build data
if exist bld (rd /s /q bld)

set NuGetConfigFile=%~dp0src\NuGet.config
set NuGetPackageDir=%~dp0src\packages
set NuGetOutputDirectory=%~dp0bld\bin\nuget\

call SetCurrentVersion.cmd

set VERSION_CONSTANTS=%~dp0src\BinaryParsers\VersionConstants.cs

@REM Rewrite VersionConstants.cs
echo // Copyright (c) Microsoft. All rights reserved. Licensed under the MIT         >  %VERSION_CONSTANTS%
echo // license. See LICENSE file in the project root for full license information.  >> %VERSION_CONSTANTS%
echo namespace Microsoft.CodeAnalysis.IL                                             >> %VERSION_CONSTANTS%
echo {                                                                               >> %VERSION_CONSTANTS%
echo     public static class VersionConstants                                        >> %VERSION_CONSTANTS%
echo     {                                                                           >> %VERSION_CONSTANTS%
echo         public const string Prerelease = "%PRERELEASE%";                        >> %VERSION_CONSTANTS%
echo         public const string AssemblyVersion = "%MAJOR%.%MINOR%.%PATCH%" + ".0"; >> %VERSION_CONSTANTS%
echo         public const string FileVersion = "%MAJOR%.%MINOR%.%PATCH%" + ".0";     >> %VERSION_CONSTANTS%
echo         public const string Version = AssemblyVersion + Prerelease;             >> %VERSION_CONSTANTS%
echo     }                                                                           >> %VERSION_CONSTANTS%
echo  }                                                                              >> %VERSION_CONSTANTS%

:: Restore packages
echo Restoring packages...
dotnet restore %~dp0src\BinSkim.sln /p:Configuration=%Configuration% --configfile "%NuGetConfigFile%" --packages "%NuGetPackageDir%

:: Build the solution 
echo Building solution...
dotnet build --no-restore /verbosity:minimal %~dp0src\BinSkim.sln /p:Configuration=%Configuration% /filelogger /fileloggerparameters:Verbosity=detailed || goto :ExitFailed

:: Run unit tests
echo Running tests...
dotnet test %~dp0src\BinSkim.sln /p:Configuration=%Configuration% --no-build

:: Create the BinSkim publish packages
echo Creating 'Publish' Packages...
call :CreatePublishPackage netcoreapp3.1 || goto :ExitFailed
call :CreatePublishPackage net5.0 || goto :ExitFailed

:: Build NuGet package
echo BuildPackages.cmd
call BuildPackages.cmd || goto :ExitFailed

:: Create layout directory of assemblies that need to be signed
echo CreateLayoutDirectory.cmd %~dp0bld\bin %Configuration% %Platform%
call CreateLayoutDirectory.cmd %~dp0bld\bin %Configuration% %Platform%

echo dotnet-format
dotnet tool update --global dotnet-format --version 4.1.131201
dotnet-format --folder --exclude .\src\sarif-sdk\

goto :Exit

:CreatePublishPackage
set Framework=%~1
dotnet publish %~dp0src\BinSkim.Driver\BinSkim.Driver.csproj --no-restore -c %Configuration% -f %Framework%
Exit /B %ERRORLEVEL%

:ExitFailed
@echo Build and test did not complete successfully.
Exit /B 1

:Exit