# SleepGuard — Unix/Linux build targets
# Requires: dotnet 10 SDK, Node 22, npm 10
# Tested on: Ubuntu 22.04+ (Jellyfin running as a systemd service)

.PHONY: build test client-build publish-local deploy-local clean format check-format

PLUGIN_VERSION ?= 0.1.0.0
JELLYFIN_PLUGIN_DIR ?= /var/lib/jellyfin/plugins/SleepGuard_$(PLUGIN_VERSION)
PUBLISH_DIR := src/Jellyfin.Plugin.SleepGuard/bin/Release/net10.0/publish

## Build the full solution (compiles TypeScript overlay first via BeforeBuild target)
build:
	dotnet build Jellyfin.Plugin.SleepGuard.sln -c Release

## Run all tests
test:
	dotnet test Jellyfin.Plugin.SleepGuard.sln -c Release

## Compile only the TypeScript overlay (without dotnet build)
client-build:
	cd client && npm ci && npm run build

## Publish the DLL without deploying
publish-local:
	dotnet publish src/Jellyfin.Plugin.SleepGuard/Jellyfin.Plugin.SleepGuard.csproj -c Release

## Build, publish, copy DLL, and restart Jellyfin (requires sudo)
## Usage: make deploy-local PLUGIN_VERSION=0.1.0.0
deploy-local: publish-local
	sudo mkdir -p $(JELLYFIN_PLUGIN_DIR)
	sudo cp $(PUBLISH_DIR)/Jellyfin.Plugin.SleepGuard.dll $(JELLYFIN_PLUGIN_DIR)/
	sudo systemctl restart jellyfin
	@echo "Deployed SleepGuard $(PLUGIN_VERSION). Tailing logs — Ctrl+C to stop."
	journalctl -u jellyfin -f --no-pager

## Format code
format:
	dotnet format Jellyfin.Plugin.SleepGuard.sln

## Check formatting without making changes (mirrors CI)
check-format:
	dotnet format Jellyfin.Plugin.SleepGuard.sln --verify-no-changes

## Remove build artifacts
clean:
	dotnet clean Jellyfin.Plugin.SleepGuard.sln
	rm -rf client/dist client/node_modules
