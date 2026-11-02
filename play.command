#!/bin/bash
# 在 Finder 里双击这个文件即可开始游戏。
cd "$(dirname "$0")" || exit 1

# 把终端窗口调到至少 100x30，避免游戏因为窗口太小直接退出。
printf '\033[8;30;100t'

DOTNET="$HOME/.dotnet/dotnet"
if [ ! -x "$DOTNET" ]; then
    DOTNET="$(command -v dotnet)"
fi

if [ -z "$DOTNET" ]; then
    echo "dotnet not found."
    echo "Install the .NET 10 SDK, then try again."
    read -r -p "Press Enter to close."
    exit 1
fi

"$DOTNET" run --project src/Rogue.Cli
status=$?

echo
if [ "$status" -ne 0 ]; then
    echo "Game exited with status $status."
fi

read -r -p "Game over. Press Enter to close this window."
