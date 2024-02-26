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
# If we're using podman create pods else if we're using docker create networks.
export CURRENT_DIR = $(shell pwd)

FORMATTING_BEGIN_YELLOW = \033[0;33m
FORMATTING_BEGIN_BLUE = \033[36m
FORMATTING_END = \033[0m

help:
	@awk 'BEGIN {FS = ":.*##"; printf "Usage: make ${FORMATTING_BEGIN_BLUE}<target>${FORMATTING_END}\nSelected container tool: ${FORMATTING_BEGIN_BLUE}${CONTAINER_TOOL}${FORMATTING_END}\n"} /^[a-zA-Z0-9_-]+:.*?##/ { printf "  ${FORMATTING_BEGIN_BLUE}%-46s${FORMATTING_END} %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)

.PHONY: py-export
py-export: ## Export poetry into requirements
	poetry export > requirements.txt

.PHONY: edit-docs
edit-docs: ## Run mkdocs local server for development
	poetry install
	poetry run mkdocs serve

.PHONY: dotnet-tests
dotnet-tests: dotnet-unit-tests
dotnet-tests: dotnet-integration-tests
dotnet-tests: ## Run all dotnet tests

.PHONY: dotnet-unit-tests
dotnet-unit-tests: ## Run dotnet unit tests
	cd dotnet/unit-tests && dotnet test

.PHONY: dotnet-integration-tests
dotnet-integration-tests: ## Run dotnet unit tests
	cd dotnet/integration-tests && dotnet test

.PHONY: run-backend
run-backend: ## Shorthand for running backend from cli
	dotnet run --project dotnet/backend

.PHONY: dotnet-benchmark
dotnet-benchmark: ## Shorthand for running dotnet benchmarks
	dotnet run -c Release --project dotnet/benchmarks
	cp BenchmarkDotNet.Artifacts/results/Benchmarks-report-github.md docs/benchmark-EventCoordinator.md

.PHONY: pulumi-up-staging
pulumi-up-staging: ## Command to deploy the staging infra
	pulumi up --cwd infra/backend --stack staging

.PHONY: pulumi-destroy-staging
pulumi-destroy-staging: ## Command to destroy the staging infra
	pulumi destroy --cwd infra/backend --stack staging

.PHONY: pulumi-preview
pulumi-preview: ## Command to preview the staging infra
	pulumi preview --cwd infra/backend --stack staging --suppress-progress
