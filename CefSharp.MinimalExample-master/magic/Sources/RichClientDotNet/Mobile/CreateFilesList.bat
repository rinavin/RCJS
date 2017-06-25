@echo off

setlocal EnableDelayedExpansion
setlocal EnableExtensions

if not defined loglevel set loglevel=1

if "%~5"=="" (
   echo usage: %~nx0 ^<list file^> ^<root dir^> ^<file patterns^> ^<base path property^> ^<exclusions file^> [^<link path base^>]
   echo.
   exit /b 1
)

rem ensure the list file exists, so that it will have an actual path.
set listFile=%~f1
set rootDirOrg=%~2
set rootDir=%~f2
set filePatterns=%~3
set basePathProperty=%~4
set exclusionsFile=%~f5

set linkPathBase=src\

if not "%~6"=="" set linkPathBase=%~6

set "INDENT=   "

set tempdir=%cd%\temp\%~n1
if exist %tempdir% rd %tempdir% /s /q
md %tempdir%

set templist=%tempdir%\templist.txt
set sortedlist=%tempdir%\sortedlist.txt
set filteredlist=%tempdir%\filteredlist.txt

@if %loglevel% geq 2 echo templist: %templist% >& 2

call :Run

exit /b

:Run
rem Create a plain list of the files
if exist "%templist%" del "%templist%"
pushd %rootDir%
call :ListFiles "%filepatterns%" "%templist%"
popd

rem Sort the files, so it will be possible to compare results after changes are being made.
sort %templist% /o %sortedlist%

rem Filter the list according to the exclusions file.
call :FilterList "%exclusionsFile%" "%sortedlist%" "%filteredlist%"

rem Generate the actual files list, which is included in the csproj file.
call :CreateListFile "%filteredlist%" > %listfile%

exit /b

:CreateListFile
call :EchoHeader

@if %loglevel% geq 1 echo Processing files from %1 >& 2
for /f "tokens=* usebackq" %%i in (%1) do (
   @if %loglevel% geq 3 echo Processing file %%i >& 2
   set relativePath="%%i"
   set relativePath=!relativePath:%rootDir%\=$^(%basePathProperty%^)\!
   set srcPath="%%i"
   set srcPath=!srcPath:%rootDir%\=%linkPathBase%!
   set name=%%~nxi
   for /f "tokens=1* delims=." %%a in ("!name!") do (
      if %loglevel% geq 5 echo Handling extension %%b >& 2
      call :Handle-%%b !relativePath! !srcPath!
   )
) 

call :EchoFooter

exit /b 0

:Handle-CS
:Handle-Scroll.cs
call :EchoCompileNode %1 %2
exit /b

:Handle-Designer.CS
set depfile=%~n1
set depfile=%depfile:designer=cs%
call :EchoCompileNode %1 %2 %depfile%
exit /b

:Handle-resx
:Handle-ico
:Handle-bmp
:Handle-gif
:Handle-jpg
:Handle-exe
call :EchoEmbeddedResourceNode %1 %2 %~n1.cs
exit /b

:EchoCompileNode
echo %INDENT%^<Compile Include="%~1"^>
echo %INDENT%%INDENT%^<Link^>%~2^</Link^>
echo %INDENT%%INDENT%^<Visible^>true^</Visible^>
if not "%~3"=="" (
   echo %INDENT%%INDENT%^<DependentUpon^>%~3^</DependentUpon^>
)
echo %INDENT%^</Compile^>
exit /b 0

:EchoEmbeddedResourceNode
echo %INDENT%^<EmbeddedResource Include="%~1"^>
echo %INDENT%%INDENT%^<Link^>%~2^</Link^>
echo %INDENT%%INDENT%^<DependentUpon^>%~3^</DependentUpon^>
echo %INDENT%%INDENT%^<SubType^>Designer^</SubType^>
echo %INDENT%%INDENT%^<Visible^>true^</Visible^>
echo %INDENT%^</EmbeddedResource^>
exit /b 0

:EchoHeader
echo ^<?xml version="1.0" encoding="utf-8"?^>
echo ^<Project ToolsVersion="3.5" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>
echo %INDENT%^<PropertyGroup^>
echo %INDENT%%INDENT%^<%basePathProperty%^>%rootDirOrg%^</%basePathProperty%^>
echo %INDENT%^</PropertyGroup^>
echo.
echo %INDENT%^<ItemGroup^>
exit /b 0

:EchoFooter
echo %INDENT%^</ItemGroup^>
echo ^</Project^>
exit /b 0

:ListFiles
rem This is a recursive procedure to list the files in a directory, based on a set of patterns.
rem The procedure uses for /f
rem to separate the head of the list from the rest of the patterns. Then it uses 'dir' to create
rem a list of files based on that pattern. Then, it calls itself recursively to process the remainder
rem of the list.
rem 
rem Parameters:
rem -----------
rem   %1 - List of patterns separated with comma. For example: "*.cs, *.resx, *.ico"
rem   %2 - Output file.

for /f "tokens=1* delims=," %%i in ("%~1") do (
   if %loglevel% geq 2 echo Listing pattern %%i
   dir /s /b %%i >> %2
   if not "%%j"=="" call :ListFiles "%%j" %2
)
exit /b

:FilterList
rem This procedure filters a list of files based on a list of exclusions, given as regular
rem expressions to the findstr method. Each expressions is matched against the file in its turn
rem and the output is written to a file. The output of the last match is used as input to the
rem next match cycle.
rem The procedure saves all intermediate files in the output file's directory, with a numerated
rem extension. So output.1 is the result of filtering the original file using the first filter
rem string, output.2 is the result of filtering output.1 using the second fitler string, etc.
rem 
rem Parameters:
rem -----------
rem   %1 - Exclusions file. A text file with a set of regular expressions. Each expression should
rem        be on a separate line.
rem   %2 - Initial input file.
rem   %3 - Output file. The fully filtered list will be written to that file.

setlocal
set /a workfileext=0
set exclusionsFile=%~1
set inputfile=%~2
for /f "delims=" %%i in (%exclusionsFile%) do (
   set /a workfileext=workfileext + 1
   set outputfile=%~dpn3.!workfileext!.txt
   
   if %loglevel% geq 5 echo checking pattern "%%i" >& 2
   findstr /i /v /R /C:"%%i" "!inputfile!" > "!outputfile!"
   if "%~z3"=="0" (
      call :PrintError "Size of filtered file is 0"
      endlocal
      exit /b 1
   )
   set inputfile=!outputfile!
)
copy "!outputfile!" %3 > NUL
endlocal
exit /b 0

:PrintError
echo. >& 2
echo Error: %1
echo.
exit /b
