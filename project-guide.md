## A. Existing Database Environment

> The PostgreSQL instance is already provisioned (default port 5432) and all tables (organisation, users, template, assignment, audit, report, log) are in place.

1. **Connection Details**

   * **Host:** `localhost` (or container name in Docker Compose)
   * **Port:** `5432`
   * **Database Name:** (e.g.) `audit_system`
   * **User / Password:**

     * Should be provided via environment variables (see below)

2. **Required Environment Variables**

   ```bash
   # Database
   POSTGRES_HOST=localhost
   POSTGRES_PORT=5432
   POSTGRES_DB=audit_system
   POSTGRES_USER=<db_user>
   POSTGRES_PASSWORD=<db_password>

   # Redis (caching & sessions)
   REDIS_HOST=localhost
   REDIS_PORT=6379

   # RabbitMQ (messaging)
   RABBITMQ_HOST=localhost
   RABBITMQ_PORT=5672
   RABBITMQ_USER=<rabbit_user>
   RABBITMQ_PASSWORD=<rabbit_password>

   # JWT Auth
   JWT_ISSUER=<your-issuer>
   JWT_AUDIENCE=<your-audience>
   JWT_SECRET=<strong-secret>

   # Logging / Monitoring
   SENTRY_DSN=<optional>
   ELASTICSEARCH_URL=<optional>
   ```

3. **Verify Tables**
   Run a quick `\d` in `psql` or:

   ```sql
   SELECT table_name 
     FROM information_schema.tables 
    WHERE table_schema = 'public';
   ```

   to confirm the seven tables are all present.

4. **Migrations**

   * No initial migrations are needed—you can scaffold a **baseline** migration if you’d like EF Core to “know” this schema as v1.
   * Example:

     ```bash
     dotnet ef migrations add Baseline --output-dir Infrastructure/Migrations
     ```
   * Afterwards, use `dotnet ef database update` to mark it applied.

---

## B. Greenfield Onboarding (If Starting from Scratch)

If a new developer were setting up **entirely from zero**, here’s the **minimum info** they’d need:

1. **Project & Repo Access**

   * Git repository URL
   * Branching strategy (e.g. `main`/`develop`/feature branches)
   * Code style & linting rules (EditorConfig, Roslyn analyzers)

2. **Solution Structure & Framework Versions**

   * .NET SDK version (e.g. .NET 8.0)
   * Project layout (Common, Domain, Infrastructure, API, Services.\*)
   * Package references (EF Core, Npgsql, AutoMapper, FluentValidation, Serilog, etc.)

3. **Database Schema**

   * SQL scripts (the ones you shared) or a SQL file in `/db`
   * Instructions to enable `uuid-ossp` extension
   * Seed data (if any demo users or templates are required)

4. **Cache & Messaging**

   * Redis version & access details
   * RabbitMQ version, virtual host, credentials
   * Docker Compose snippet or Kubernetes manifests for all three services

5. **Authentication / Authorization**

   * JWT settings (issuer, audience, secret, token lifetime)
   * Role definitions: `auditor`, `manager`, `supervisor`, `admin`
   * If using external SSO: OIDC/SAML metadata endpoints

6. **Configuration & Secrets Management**

   * `.env.example` with all required variables
   * Vault/Key Vault paths for production secrets (if used)

7. **CI/CD Pipeline**

   * Build & test commands
   * Docker build & push steps (registry URL, credentials)
   * Kubernetes/Helm deployment instructions
   * Health-check endpoints for liveness/readiness probes

8. **API Contract**

   * OpenAPI/Swagger spec file or URL
   * Any client-SDK generation instructions

9. **Reporting & File Storage**

   * PDF/XLSX library choices (e.g. DinkToPdf, EPPlus)
   * Blob storage or S3 bucket details (for report files)

10. **Logging & Monitoring**

    * Serilog sink configuration (console, file, Elasticsearch)
    * Tracing (OpenTelemetry, Application Insights)
    * Grafana/Prometheus endpoints and dashboards

11. **Performance & Non-Functional Targets**

    * API latency (P95 < 200 ms)
    * Sync SLA (≤ 60 s for 100 audits)
    * Report gen SLA (≤ 30 s for 10k records)
    * Backup/restore procedures

12. **Testing Requirements**

    * Unit test frameworks & coverage target (e.g. xUnit, Moq, ≥80%)
    * Integration tests (local Docker Compose or Testcontainers)
    * End-to-end test guidance (if any)