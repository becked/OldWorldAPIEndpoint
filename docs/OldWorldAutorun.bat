@echo off
set /p loadfile="Filename: "
set /p maxturns="Turns: "
START /B OldWorld.exe %loadfile% -batchmode -headless -autorunturns %maxturns%
