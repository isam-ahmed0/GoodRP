@echo off
REM Log when playback stops. GoodRP sets GOODRP_EVENT=stopped for this script.
echo %DATE% %TIME% - Playback stopped >> "%USERPROFILE%\Desktop\track_history.log"
