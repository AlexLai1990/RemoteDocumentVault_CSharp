:run.bat
@echo off
cd ./Server/bin/Debug 
start Server.exe
cd ../../../
cd ./Client_WPF/bin/Debug
start Client_WPF.exe 
cd ../../../