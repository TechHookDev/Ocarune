@echo off
rem Builds hs_dlss.dll with MSVC (NGX libs are MSVC-ABI). Usage: build_dlss.bat <outdir>
call "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul || exit /b 1
set SDK=%~dp0..\..\..\..\third_party\dlss-sdk
set VKH=%~dp0..\..\..\cpm\vulkanheaders\v1.4.345\include
cl /nologo /O2 /MD /LD /EHsc /I"%SDK%\include" /I"%VKH%" "%~dp0hs_dlss.cpp" /Fe:"%~1\hs_dlss.dll" /Fo:"%TEMP%\hs_dlss.obj" /link /LIBPATH:"%SDK%\lib\Windows_x86_64\x64" nvsdk_ngx_d.lib advapi32.lib user32.lib setupapi.lib || exit /b 1
