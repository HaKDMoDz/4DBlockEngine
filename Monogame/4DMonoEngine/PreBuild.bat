cd "$(ProjectDir)..\Data\Assets\Effects\"
for %%F in (*.fx) do 2MGFX.exe %%F %%~nF.mgfxo
cd "$(ProjectDir)"
Xcopy "$(ProjectDir)..\ContentBuilderProject\bin\PSM\Content\*" "$(ProjectDir)\$(OutDir)\Content" /s /r /y /i 
Xcopy "$(ProjectDir)..\..\Data\*" "$(ProjectDir)\$(OutDir)\Content\Config" /s /r /y /i
