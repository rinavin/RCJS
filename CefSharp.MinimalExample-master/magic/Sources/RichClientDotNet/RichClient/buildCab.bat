
rem *------------------------------------------------
rem *------------------------------------------------

set PATH=%PATH%;d:\_Trunk\Addon for VC\JavaUtils

set CAB_NAME=MGRIA%1_%2_%3CS.cab

%windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe  MgxpaRIA.dll /unregister
rem del cabinf.inf /F
rem createcabinf.exe %1 %2 %3
cabarc n %CAB_NAME% cabINF.inf MgxpaRIA.dll MgRegAsm2.exe MgRegAsm2.exe.config MgHttpClient.dll MgGui.dll MgRuntimeDesigner.dll MgRIAGatewayTypes.dll MgControls.dll MgUtils.dll MgNative.dll
signtool sign /f C:\DigitalSignature\MagicCert.pfx /p APaaS2008 /d "Magic xpa RIA IE Plugin" /t http://timestamp.verisign.com/scripts/timstamp.dll /du http://www.magicsoftware.com %CAB_NAME%

copy %CAB_NAME% c:\inetpub\wwwroot\