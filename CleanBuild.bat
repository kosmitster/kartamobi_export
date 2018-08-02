for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s/q "%%d"

del /q %cd%\Build\*
for /d %%x in (%cd%\Build\*) do @rd /s /q "%%x"