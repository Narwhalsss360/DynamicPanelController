@echo off
cd ..\

Set "ProjectDir=IncludedExtensions"
Set "InBin=IncludedExtensions\bin\Debug\net6.0"
Set "OutBin=DynamicPanelController\bin\Debug\net6.0-windows"

echo d | xcopy /d /y "%InBin%\IncludedExtensions.dll" "%OutBin%\Extensions"
echo d | xcopy /d /y "%ProjectDir%\AudioSwitcher.AudioApi.CoreAudio.dll" "%OutBin%\Extensions"
echo d | xcopy /d /y "%ProjectDir%\AudioSwitcher.AudioApi.dll" "%OutBin%\Extensions"
echo d | xcopy /d /y "%ProjectDir%\WindowsInput.dll" "%OutBin%\Extensions"
echo d | xcopy /d /y "%ProjectDir%\vJoyInterface.dll" "%OutBin%\Extensions"
echo d | xcopy /d /y "%ProjectDir%\vJoyInterfaceWrap.dll" "%OutBin%\Extensions"