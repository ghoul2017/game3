@echo off
REM dev.cmd - Run single automated dev cycle
REM Usage: dev.cmd

echo [Dev] Running single dev cycle...
claude -p "Run /dev" --dangerously-skip-permissions
echo [Dev] Done
