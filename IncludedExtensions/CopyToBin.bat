@echo off
cd ..\

Set "InBin=IncludedExtensions\bin\Debug\net6.0"
Set "OutBin=DynamicPanelController\bin\Debug\net6.0-windows"

echo "Copying %InBin%\IncludedExtensions.dll to %OutBin%\Extensions"
echo d | xcopy /d /y "%InBin%\IncludedExtensions.dll" "%OutBin%\Extensions"