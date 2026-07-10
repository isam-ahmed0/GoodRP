@echo off
REM Minimal example: log each track to a file on your Desktop.
REM GoodRP passes metadata via environment variables (%%VAR%% in Batch).

echo %DATE% %TIME% - %GOODRP_TITLE% by %GOODRP_ARTIST% ^(%GOODRP_APP%^) >> "%USERPROFILE%\Desktop\track_history.log"
