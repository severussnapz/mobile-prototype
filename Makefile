.PHONY: up down build restart logs api-logs db-shell seed regenerate-seed test-unit test-integration test-e2e test-all clean help migrate reset-db \
		s3-init localstack-restart health check-env restore publish

# ── Defaults ────────────────────────────────────────────────────────────────
DOTNET ?= dotnet
DOCKER_COMPOSE := docker compose

up: ## Start all services (postgres → flyway → localstack → seed → api)
	$(DOCKER_COMPOSE) up -d --build

down: ## Stop and remove all containers
	$(DOCKER_COMPOSE) down

restart: ## Rebuild and restart the API service
	$(DOCKER_COMPOSE) up -d --build api

logs: ## Follow all container logs
	$(DOCKER_COMPOSE) logs -f

api-logs: ## Follow API container logs only
	$(DOCKER_COMPOSE) logs -f api

# ── Database ────────────────────────────────────────────────────────────────
db-shell: ## Open PostgreSQL shell
	$(DOCKER_COMPOSE) exec postgres psql -U postgres -d genesis_ai_requirements

migrate: ## Run Flyway migrations manually
	$(DOCKER_COMPOSE) up flyway

reset-db: ## Drop and recreate the database (destroys all data)
	@echo "WARNING: This will destroy all data in the database."
	$(DOCKER_COMPOSE) exec -T postgres dropdb --force genesis_ai_requirements || true
	$(DOCKER_COMPOSE) up -d postgres
	@$(DOCKER_COMPOSE) exec -T postgres sh -c 'until pg_isready -U postgres; do sleep 1; done'
	$(DOCKER_COMPOSE) exec -T postgres createdb -U postgres genesis_ai_requirements
	$(DOCKER_COMPOSE) up flyway seed

# ── Seed Data ───────────────────────────────────────────────────────────────
seed: ## Run seed data manually (idempotent — safe to re-run)
	@echo "Checking for existing seed data..."
	$(DOCKER_COMPOSE) up seed 2>&1 | tail -5

regenerate-seed: ## Regenerate seed-local.sql from current database state
	@echo "Usage: make regenerate-seed PROJECT_ID=<project-id>"
	@echo "       Omit PROJECT_ID to list available projects."
	./db/generate-seed.sh $(PROJECT_ID)

# ── S3 / LocalStack ─────────────────────────────────────────────────────────
s3-init: ## Re-initialise the S3 bucket (creates genesis-ai-artefacts)
	docker exec localstack sh /etc/localstack/init/ready.d/init-s3.sh

localstack-restart: ## Restart LocalStack and re-initialise S3
	$(DOCKER_COMPOSE) restart localstack
	@sleep 3
	$(MAKE) s3-init

# ── Health Checks ───────────────────────────────────────────────────────────
health: ## Check that all services are healthy
	@echo "=== Service Health ==="
	@docker inspect --format='{{.Name}}: {{.State.Health.Status}}' $$(docker compose ps -q) 2>/dev/null || echo "Some containers not running"

check-env: ## Verify required .env variables are set
	@if [ ! -f .env ]; then echo "ERROR: .env file not found"; exit 1; fi
	@for var in IDENTITY_URL AUDIENCE JFROG_USER JFROG_TOKEN GIT_TOKEN; do \
		if ! grep -q "^$$var=" .env; then echo "MISSING: $$var"; else echo "OK: $$var"; fi; \
	done

# ── Dotnet Operations ───────────────────────────────────────────────────────
restore: ## Restore NuGet packages
	$(DOTNET) restore src/Genesis.AI.Api/Genesis.AI.Api.csproj

build: ## Build the solution
	$(DOTNET) build src/Genesis.AI.Api/Genesis.AI.Api.csproj

publish: ## Publish the API (Release)
	$(DOTNET) publish src/Genesis.AI.Api/Genesis.AI.Api.csproj -c Release -o out/publish

# ── Tests ───────────────────────────────────────────────────────────────────
test-unit: ## Run unit tests only
	$(DOTNET) test tests/Genesis.AI.Tests/ --no-restore

test-integration: ## Run integration tests only
	$(DOTNET) test tests/Genesis.AI.IntegrationTests/ --no-restore

test-e2e: ## Run E2E API tests (requires running API + identity service)
	$(DOTNET) test tests/Genesis.AI.ApiTests/ --no-restore

test-all: ## Run all test suites sequentially
	$(MAKE) test-unit && $(MAKE) test-integration && $(MAKE) test-e2e

# ── Cleanup ─────────────────────────────────────────────────────────────────
clean: ## Remove build artifacts and output directories
	rm -rf out/
	find src/ -type d -name 'bin' -exec rm -rf {} + 2>/dev/null || true
	find src/ -type d -name 'obj' -exec rm -rf {} + 2>/dev/null || true
	find tests/ -type d -name 'bin' -exec rm -rf {} + 2>/dev/null || true
	find tests/ -type d -name 'obj' -exec rm -rf {} + 2>/dev/null || true

# ── General ─────────────────────────────────────────────────────────────────
help: ## Show this help message
	@echo "Usage: make <target>"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2}'
