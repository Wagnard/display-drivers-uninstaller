@ECHO OFF
REM (C) 2015 
REM This file is part of Display Driver Uninstaller (DDU).
REM Help VS build version.
REM kongfl888(kongfl888@outlook.com) in 2015.
REM See the folder 'docs' for more information.

set "WORKDIR=%~dp0"
set "Common=%WORKDIR%Common"
set "gitver_file=%Common%\gitver.vb"

PUSHD "%~dp0"

IF EXIST "build.user.bat" CALL "build.user.bat"

IF NOT EXIST "%GIT%"    GOTO MissingVar

set "gitexe=%GIT%\bin\git.exe"

for /f "tokens=1,2,3 delims=-" %%a in ('"%gitexe%" describe --long') do ( set "MainVer=%%a" && set "GitBuildVer=%%b" && set "gitHash=%%c" )
echo Main Version: %MainVer%
echo Build Version: %GitBuildVer%
echo Hash: %gitHash%

echo %GitBuildVer%|findstr /be "[0-9]*" >nul && GOTO ChangVer || GOTO MissingVar 

:ChangVer
echo Class gitver > %gitver_file%
echo     Public Const Ver As String = "%GitBuildVer%" >> %gitver_file%
echo End Class >> %gitver_file%

:END
POPD

EXIT /B


:MissingVar
echo Class gitver > %gitver_file%
echo     Public Const Ver As String = "0" >> %gitver_file%
echo End Class >> %gitver_file%
POPD
EXIT /B 
