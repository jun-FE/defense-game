@echo off
rem Double-click to install everything needed for this project.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
