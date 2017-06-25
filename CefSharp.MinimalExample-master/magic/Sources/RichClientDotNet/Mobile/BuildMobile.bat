setlocal
set action=Run
set pauseAtEnd=1

:NextParam
if "%~1"=="" goto :NoMoreParams
goto :Switch%~1

:Switch-nobuild
set action=CreateFiles
shift
goto :NextParam

:Switch-buildonly
set action=build
shift
goto :NextParam

:Switch-nopause
set pauseAtEnd=
shift
goto :NextParam

:NoMoreParams

pushd %~dp0
call :%action%
if defined pauseAtEnd pause
popd
endlocal
exit /b

:Run
call :CreateFiles || exit /b
call :Build
exit /b 

:CreateFiles
call CreateFilesList.bat _RC.files ..\RichClient\src "*.cs, *.resx" RCBasePath RC.excluded.txt 
call CreateFilesList.bat _Http.files ..\Http "*.cs, *.resx" HttpBasePath Http.excluded.txt
call CreateFilesList.bat _GatewayTypes.files ..\MgRIAGatewayTypes\src "*.cs, *.resx" GatewayTypesBasePath GatewayTypes.excluded.txt
call CreateFilesList.bat _uniUtils.files ..\util "*.cs, *.resx" UtilsBasePath util.excluded.txt
call CreateFilesList.bat _Gui.files ..\Gui "*.cs, *.resx" GuiBasePath Gui.excluded.txt
call CreateFilesList.bat _Controls.files ..\Controls "*.cs, *.resx" ControlsBasePath Controls.excluded.txt
call CreateFilesList.bat _NativeWrapper.files ..\NativeWrapper "*.cs, *.resx" NativeWrapperBasePath NativeWrapper.excluded.txt
call CreateFilesList.bat _LZMA.files "..\..\..\Addon For VC\Compression\LZMA\C#" "*.cs, *.resx" LZMABasePath LZMA.excluded.txt src\com\magicsoftware\richclient\util\compression\
exit /b

:Build
attrib -r "..\RichClient\Resources\MgxpaRIAMobile spawner.exe"
call "%VS90COMNTOOLS%\vsvars32.bat"
devenv "Magic Mobile.sln" /build || exit /b 1

exit /b 0