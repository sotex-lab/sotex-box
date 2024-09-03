.DEFAULT_GOAL := help
.ONESHELL:

mkfile_path := $(abspath $(lastword $(MAKEFILE_LIST)))
mkfile_dir := $(dir $(mkfile_path))

# Begin OS detection
ifeq ($(OS),Windows_NT) # is Windows_NT on XP, 2000, 7, Vista, 10...
    export OPERATING_SYSTEM := Windows
else
    export OPERATING_SYSTEM := $(shell uname)  # same as "uname -s"
endif

# Override the container tool. Tries docker first and then tries podman.
export CONTAINER_TOOL ?= auto
ifeq ($(CONTAINER_TOOL),auto)
	override CONTAINER_TOOL = $(shell docker version >/dev/null 2>&1 && echo docker || echo podman)
endif

export ANDROID_HOME ?= auto
ifeq ($(ANDROID_HOME),auto)
	$(shell echo "Setup ANDROID_HOME env variable")
endif

# Conditional assignment of COMPOSE_COMMAND
export COMPOSE_COMMAND ?= auto
ifeq ($(COMPOSE_COMMAND),auto)
	ifeq ($(CONTAINER_TOOL),podman)
		override COMPOSE_COMMAND := podman-compose
	else ifeq ($(CONTAINER_TOOL),docker)
		override COMPOSE_COMMAND := docker compose
	else
		$(error Unsupported value for CONTAINER_TOOL: $(CONTAINER_TOOL))
	endif
endif

# If we're using podman create pods else if we're using docker create networks.
export CURRENT_DIR = $(shell pwd)

COMMIT_SHA := $(if $(GITHUB_SHA),$(GITHUB_SHA),$(shell git log -1 --pretty=format:"%H"))

FORMATTING_BEGIN_YELLOW = \033[0;33m
FORMATTING_BEGIN_BLUE = \033[36m
FORMATTING_END = \033[0m

help:
	@awk 'BEGIN {FS = ":.*##"; printf "Usage: make ${FORMATTING_BEGIN_BLUE}<target>${FORMATTING_END}\nSelected container tool: ${FORMATTING_BEGIN_BLUE}${CONTAINER_TOOL}, ${COMPOSE_COMMAND}${FORMATTING_END}\n"} /^[a-zA-Z0-9_-]+:.*?##/ { printf "  ${FORMATTING_BEGIN_BLUE}%-46s${FORMATTING_END} %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)

##@ Misc actions
.PHONY: edit-docs
edit-docs: ## Run mkdocs local server for development
	pixi install
	pixi run mkdocs serve

ANDROID_IMAGE := "system-images;android-31;android-tv;x86"
BUILD_TOOLS := "build-tools;31.0.0"
PLATFORMS := "platforms;android-31"
.PHONY: setup-emulator
setup-emulator: ## Shorthand for setting up an emulator
	if [ -z $(ANDROID_HOME) ]; then echo "ANDROID_HOME is not set. Please set it and re-run this command."; exit 1; fi
	sdkmanager --sdk_root=$(ANDROID_HOME) $(BUILD_TOOLS) $(PLATFORMS) $(ANDROID_IMAGE) emulator platform-tools
	avdmanager create avd -n "android_tv" -k $(ANDROID_IMAGE) --force

UNWANTED_VOLUMES := $(shell $(CONTAINER_TOOL) volume list -q --filter name=sotex)
.PHONY: full-local-cleanup
full-local-cleanup: compose-down
full-local-cleanup: ## Run a full local cleanup of all volumes and dirs
	@echo $(UNWANTED_VOLUMES)
	@$(foreach vol,$(UNWANTED_VOLUMES), \
        docker volume rm $(vol); \
		)
	@sudo rm -rf ./volumes.local

##@ Dotnet Testing
.PHONY: dotnet-tests
dotnet-tests: dotnet-unit-tests
dotnet-tests: dotnet-integration-tests
dotnet-tests: ## Run all dotnet tests

.PHONY: dotnet-unit-tests
dotnet-unit-tests: ## Run dotnet unit tests
	cd dotnet/unit-tests && dotnet test

TOP_LEVEL := $(shell git rev-parse --show-toplevel)
.PHONY: dotnet-integration-tests
dotnet-integration-tests: compose-down
dotnet-integration-tests: ## Run dotnet unit tests
	cd dotnet/integration-tests && TOP_LEVEL=$(TOP_LEVEL) dotnet test

# Override the container tool. Tries docker first and then tries podman.
export PARALLELISM ?= auto
ifeq ($(PARALLELISM),auto)
	override PARALLELISM = 3
endif
export ABSOLUTE_PATH ?= auto
ifeq ($(ABSOLUTE_PATH),auto)
	override ABSOLUTE_PATH = $(shell git rev-parse --show-toplevel)
endif
.PHONY: dotnet-e2e-tests
dotnet-e2e-tests: ensure-setup
dotnet-e2e-tests: container-build-backend
dotnet-e2e-tests: container-build-local-pusher
dotnet-e2e-tests: ## Run dotnet e2e tests, excluded from dotnet-test
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml pull --policy missing
	$(CONTAINER_TOOL) build -t e2e -f distribution/docker/e2e.dockerfile .
	dotnet run --project dotnet/e2e-tester --parallelism $(PARALLELISM) --absolute-path $(ABSOLUTE_PATH)

##@ Flutter testing
.PHONY: flutter-test-launcher
flutter-test-launcher: ## Shorthand for running the launcher tests
	(cd launcher && flutter test -r expanded)

.PHONY: flutter-test
flutter-test: flutter-test-launcher
flutter-test: ## Shorthand for running all flutter tests

##@ Executing
.PHONY: run-backend
run-backend: ## Shorthand for running backend from cli
	dotnet run --project dotnet/backend

ENV_FILE_LAUNCHER := launcher/.env.json
.PHONY: run-launcher
run-launcher: ## Shorthand for running the launcher app locally
	@if [ -z "$(wildcard $(ENV_FILE_LAUNCHER))" ]; then \
        echo "$(ENV_FILE_LAUNCHER) does not exist. To run the stack you should create $(ENV_FILE_LAUNCHER). Use $(ENV_FILE_LAUNCHER).template to start"; \
        exit 1; \
    else \
        echo "$(ENV_FILE_LAUNCHER) exists"; \
    fi
	(cd launcher && flutter run --dart-define-from-file=.env.json -d emulator-5554)

.PHONY: run-emu
run-emu: ## Shorthand for running the android emulator
	emulator -avd "android_tv" -skin 1920x1080 -sysdir $(ANDROID_HOME)/system-images/android-31/android-tv/x86/

.PHONY: reverse-ports
reverse-ports: ## Reverse ports for backend and minio for local development, should be run after run-emu
	adb reverse tcp:8000 tcp:8000
	adb reverse tcp:9002 tcp:9002

.PHONY: build-launcher
build-launcher: ## Shorthand for building the launcher app locally
	(cd launcher && flutter build apk --dart-define-from-file=.env.json --release)

##@ Benchmarking
.PHONY: dotnet-benchmark
dotnet-benchmark: ## Shorthand for running dotnet benchmarks
	dotnet run -c Release --project dotnet/benchmarks
	cat BenchmarkDotNet.Artifacts/results/Benchmarks-report-github.md >> docs/benchmark-EventCoordinator.md

##@ Infrastructure actions
export AWS_ACCESS_KEY ?= auto
ifeq ($(AWS_ACCESS_KEY),auto)
	override AWS_ACCESS_KEY = $(shell taplo get -f ~/.aws/credentials 'service-account.aws_access_key_id')
endif
export AWS_SECRET_KEY ?= auto
ifeq ($(AWS_SECRET_KEY),auto)
	override AWS_SECRET_KEY = $(shell taplo get -f ~/.aws/credentials 'service-account.aws_secret_access_key')
endif
.PHONY: pulumi-up-staging
pulumi-up-staging: ## Command to deploy the staging infra
	@AWS_ACCESS_KEY=$(AWS_ACCESS_KEY) AWS_SECRET_KEY=$(AWS_SECRET_KEY) pulumi up --cwd infra/backend --stack staging

.PHONY: pulumi-destroy-staging
pulumi-destroy-staging: ## Command to destroy the staging infra
	pulumi destroy --cwd infra/backend --stack staging

.PHONY: pulumi-preview
pulumi-preview: ## Command to preview the staging infra
	pulumi preview --cwd infra/backend --stack staging --suppress-progress

##@ Container actions
.PHONY: container-build-backend
container-build-backend: ## Command to build the container for backend
	$(CONTAINER_TOOL) build -t ghcr.io/sotex-lab/sotex-box/backend:$(COMMIT_SHA) . -f distribution/docker/backend.dockerfile

.PHONY: container-build-local-pusher
container-build-local-pusher: ## Command to build the local pusher
	$(CONTAINER_TOOL) build -t ghcr.io/sotex-lab/sotex-box/local-pusher:$(COMMIT_SHA) . -f distribution/docker/local-pusher.dockerfile

.PHONY: container-push-backend
container-push-backend: ## Command to push the container for backend
	$(CONTAINER_TOOL) push ghcr.io/sotex-lab/sotex-box/backend:$(COMMIT_SHA)

##@ Compose actions

ENV_FILE := .env

ensure-setup:
	@if [ -z "$(wildcard $(ENV_FILE))" ]; then \
        echo "$(ENV_FILE) does not exist. To run the stack you should create $(ENV_FILE). Use $(ENV_FILE).template to start"; \
        exit 1; \
    else \
        echo "$(ENV_FILE) exists"; \
    fi

	mkdir -p volumes.local/minio

.PHONY: compose-up
compose-up: container-build-backend
compose-up: container-build-local-pusher
compose-up: compose-down
compose-up: ensure-setup
compose-up: ## Run local stack
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml up

.PHONY: compose-up-d
compose-up-d: compose-down
compose-up-d: ensure-setup
compose-up-d: ## Run local stack detached. Used for e2e tests
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml up -d

.PHONY: compose-up-aws
compose-up-non-local: compose-down
compose-up-non-local: ensure-setup
compose-up-non-local: container-build-backend
compose-up-non-local: ## Run stack on AWS EC2
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml up -d

.PHONY: compose-down
compose-down: ## Remove local stack
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml down
