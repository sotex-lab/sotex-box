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

# Conditional assignment of COMPOSE_COMMAND
ifeq ($(CONTAINER_TOOL),podman)
    COMPOSE_COMMAND := podman-compose
else ifeq ($(CONTAINER_TOOL),docker)
    COMPOSE_COMMAND := docker compose
else
    $(error Unsupported value for CONTAINER_TOOL: $(CONTAINER_TOOL))
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
.PHONY: py-export
py-export: ## Export poetry into requirements
	poetry export > requirements.txt

.PHONY: edit-docs
edit-docs: ## Run mkdocs local server for development
	poetry install
	poetry run mkdocs serve

ANDROID_IMAGE := "system-images;android-31;android-tv;x86"

.PHONY: flutter-create-emulator
flutter-create-emulator: ## Shorthand for setting up an emulator
	sdkmanager $(ANDROID_IMAGE)
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
dotnet-e2e-tests: container-build-backend
dotnet-e2e-tests: container-build-local-pusher
dotnet-e2e-tests: ## Run dotnet e2e tests, excluded from dotnet-test
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml pull --policy missing
	$(CONTAINER_TOOL) build -t e2e -f distribution/docker/e2e.dockerfile .
	dotnet run --project dotnet/e2e-tester --parallelism $(PARALLELISM) --absolute-path $(ABSOLUTE_PATH)

##@ Flutter testing
.PHONY: flutter-test-launcher
flutter-test-launcher: ## Shorthand for running the launcher tests
	(cd android/launcher && flutter test -r expanded)

.PHONY: flutter-test-box
flutter-test-box: ## Shorthand for running the sotex_box tests
	(cd android/sotex_box && flutter test -r expanded)

.PHONY: flutter-test
flutter-test: flutter-test-launcher
flutter-test: flutter-test-box
flutter-test: ## Shorthand for running all flutter tests

##@ Executing
.PHONY: run-backend
run-backend: ## Shorthand for running backend from cli
	dotnet run --project dotnet/backend

.PHONY: run-emu
run-emu: ## Shorthand for running the android emulator
	emulator -avd "android_tv" -skin 1920x1080

.PHONY: rotate-emu
rotate-emu: ## Shorthand for rotating the android emulator
	adb emu rotate

.PHONY: run-launcher
run-launcher: ## Shorthand for running the launcher app locally
	(cd android/launcher && flutter run -d emulator-5554)

.PHONY: run-box
run-box: ## Shorthand for running the sotex_box app locally
	emulator -avd "android_tv" -skin 1280x720
	(cd android/sotex_box && flutter run -d emulator-5554)

##@ Benchmarking
.PHONY: dotnet-benchmark
dotnet-benchmark: ## Shorthand for running dotnet benchmarks
	dotnet run -c Release --project dotnet/benchmarks
	cp BenchmarkDotNet.Artifacts/results/Benchmarks-report-github.md docs/benchmark-EventCoordinator.md

##@ Infrastructure actions
.PHONY: pulumi-up-staging
pulumi-up-staging: ## Command to deploy the staging infra
	pulumi up --cwd infra/backend --stack staging

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

.PHONY: compose-down
compose-down: ## Remove local stack
	COMMIT_SHA=$(COMMIT_SHA) $(COMPOSE_COMMAND) -f docker-compose.yaml -f distribution/local/docker-compose.dev.yaml down
