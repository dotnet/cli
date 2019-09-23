#!/usr/bin/env sh

MANPAGE_TOOL_DIR=$(cd "$(dirname "$0")" || exit; pwd)

docker build -t dotnet-cli-manpage-tool "$MANPAGE_TOOL_DIR"

docker run --volume="$MANPAGE_TOOL_DIR"/..:/manpages dotnet-cli-manpage-tool
