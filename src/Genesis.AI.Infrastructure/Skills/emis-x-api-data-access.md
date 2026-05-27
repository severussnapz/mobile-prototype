---
name: emis-x-api-data-access
description: >
  Use this skill when generating, reviewing, or auditing code that involves
  DbContext configuration, entity configuration, repository pattern, or
  query performance — regardless of the underlying data store. Covers
  DATA-001 through DATA-004.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X Data Access Guardrails (Shared)

Technology-agnostic data access guardrails that apply regardless of the underlying data store (PostgreSQL, SQL Server, DynamoDB, S3). Not every API requires a database — these guardrails apply only when the service has data access concerns.

**Target versions:** Entity Framework Core 10.0, MediatR 12.x. Use Fluent API entity configuration exclusively — never data annotations on domain entities.

For technology-specific guardrails, see the dedicated skills:
- **emis-x-api-postgres** — Owned PostgreSQL databases (Flyway migrations, snake_case naming, data types)
- **emis-x-api-legacy-sqlserver** — External/legacy SQL Server databases (dynamic connections, Kerberos, PascalCase)
- **emis-x-api-dynamodb** — Amazon DynamoDB (table models, caching, configuration)
- **emis-x-api-s3** — Amazon S3 (object storage, bucket access)

## Guardrails Index

| Guardrail | Name                       | Severity |
| --------- | -------------------------- | -------- |
| DATA-001  | DbContext Configuration    | High     |
| DATA-002  | Fluent API Entity Config   | High     |
| DATA-003  | Repository Pattern         | High     |
| DATA-004  | Query Performance          | Medium   |

---

## DATA-001: DbContext Configuration

**Type:** Guardrail

**Requirement:** For **owned databases** (PostgreSQL), each DbContext must inherit from the shared `DatabaseContext` base class (which implements `IUnitOfWork` and broadcasts domain events via `IMediator`). Register entity configurations using `ApplyConfiguration()` calls in `OnModelCreating`. Multiple DbContexts per service are valid.

**Severity:** High

**Exceptions:** Services with no database do not need a DbContext. For **external databases** the service does not own (e.g., legacy SQL Server), the DbContext may inherit from `DbContext` directly and may be created via a **factory pattern** for dynamic per-tenant connections. See the emis-x-api-legacy-sqlserver skill.

✅ **Good (owned database):**

```csharp
public class GreetingContext(DbContextOptions<GreetingContext> options, IMediator mediator)
    : DatabaseContext(options, mediator)
{
    public DbSet<Greeting> Greetings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GreetingEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
```

✅ **Good (external database with factory):**

```csharp
public class ExternalDbContext(DbContextOptions<ExternalDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public interface IExternalDatabaseContextFactory
{
    Task<ExternalDbContext> GetDatabaseContext(Guid organisationId, CancellationToken cancellationToken);
    Task<ExternalDbContext> GetDatabaseContext(string cdb, CancellationToken cancellationToken);
}
```

❌ **Bad:**

```csharp
// Inline entity configuration — should use separate IEntityTypeConfiguration classes
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Greeting>(e => { e.HasKey(x => x.Id); });
}
```

---

## DATA-002: Fluent API Entity Configuration

**Type:** Guardrail

**Requirement:** Entity configuration must use separate `IEntityTypeConfiguration<T>` classes in an `EntityConfigurations/` folder in the Infrastructure layer. Every property must be explicitly mapped to its column name via `.HasColumnName()`. Never use data annotations on domain entities. Never rely on convention-based naming (no `UseSnakeCaseNamingConvention()`).

**Severity:** High

**Exceptions:** None. This applies to both owned and external databases. For owned PostgreSQL databases, column names are `snake_case`. For external SQL Server databases, column names match the existing schema (typically PascalCase).

### Naming

Configuration classes follow the pattern `{Entity}EntityTypeConfiguration`.

✅ **Good (owned PostgreSQL):**

```csharp
public class GreetingEntityTypeConfiguration : IEntityTypeConfiguration<Greeting>
{
    public void Configure(EntityTypeBuilder<Greeting> builder)
    {
        builder.ToTable("greeting");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id")
            .HasConversion(id => id.Id, value => new GreetingIdentifier(value));
        builder.Property(g => g.Name).HasColumnName("name").IsRequired();
        builder.Property(g => g.Active).HasColumnName("active").IsRequired();
        builder.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
```

✅ **Good (external SQL Server):**

```csharp
public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", "dbo");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("UserId");
        builder.Property(u => u.EmailAddress).HasColumnName("EmailAddress");
        builder.Property(u => u.LoginName).HasColumnName("LoginName");
    }
}
```

❌ **Bad:**

```csharp
// Data annotations on domain entity — domain must not reference infrastructure concerns
public class Greeting : Entity, IAggregateRoot
{
    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; private set; }
}

// Missing explicit HasColumnName — relies on EF convention
builder.Property(g => g.Name).IsRequired();  // No HasColumnName!
```

---

## DATA-003: Repository Pattern

**Type:** Guardrail

**Requirement:** Repository interfaces must be defined in the **Domain layer**. Implementations reside in the **Infrastructure layer**. This applies to all data stores — EF Core (PostgreSQL, SQL Server), DynamoDB, S3, or any other technology. A single repository may span multiple data stores.

**Severity:** High

**Exceptions:** None.

### Standard Pattern (DI at startup)

For services with static database connections, repositories are injected via DI:

```csharp
// Domain interface
public interface IGreetingRepository : IRepository<Greeting>
{
    Task Add(Greeting greeting, CancellationToken cancellationToken);
    Task<Greeting> GetByID(GreetingIdentifier id, CancellationToken cancellationToken);
    void Delete(Greeting greeting, CancellationToken cancellationToken);
}

// Infrastructure implementation
public class GreetingRepository(GreetingContext context) : IGreetingRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<Greeting> GetByID(GreetingIdentifier id, CancellationToken cancellationToken)
        => await context.Greetings.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
}
```

### Factory Pattern (dynamic connections)

For services where the database connection is resolved at runtime (e.g., per-tenant external databases), use a **repository factory** defined in the Domain layer:

```csharp
// Domain interface
public interface IRepositoryFactory
{
    Task<IUserRepository> CreateUserRepository(Guid organisationId, CancellationToken cancellationToken);
    Task<IUserRepository> CreateUserRepository(string cdb, CancellationToken cancellationToken);
    Task<IAuditRepository> CreateAuditRepository(Guid organisationId, CancellationToken cancellationToken);
}

// Command handler usage
public class IdentityLinkCommandHandler(IRepositoryFactory repositoryFactory)
    : IRequestHandler<IdentityLinkCommand, int?>
{
    public async Task<int?> Handle(IdentityLinkCommand request, CancellationToken cancellationToken)
    {
        using var repository = await repositoryFactory.CreateUserRepository(
            request.OrganisationId, cancellationToken);
        return await repository.GetUserIdentityLink(
            request.OrganisationErn, request.UserErn, cancellationToken);
    }
}
```

### Multi-Store Repositories

A single repository may use multiple data stores. The domain interface remains technology-agnostic:

```csharp
// Infrastructure: UserRepository uses EF Core (SQL Server) + DynamoDB
public class UserRepository(ExternalDbContext context, IDynamoDBContext dynamoDbContext) : IUserRepository
{
    // EF Core for user queries
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        => await context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email, cancellationToken);

    // DynamoDB for identity link cache
    public async Task<int?> GetUserIdentityLink(string orgErn, string userErn, CancellationToken cancellationToken)
        => (await dynamoDbContext.LoadAsync<DynamoDBLinkedIdentity>(userErn, orgErn, cancellationToken))
            ?.UserInRoleId;
}
```

❌ **Bad:**

```csharp
// Repository interface defined in Infrastructure — violates dependency inversion
// Infrastructure/IGreetingRepository.cs  ← WRONG LAYER
public interface IGreetingRepository { }

// DynamoDB accessed directly in a command handler — no repository abstraction
public class SomeHandler(IDynamoDBContext dynamoDb) : IRequestHandler<SomeCommand, Unit>
{
    public async Task<Unit> Handle(SomeCommand request, CancellationToken ct)
    {
        await dynamoDb.SaveAsync(new SomeModel { ... }, ct);  // NO — go through a repository
        return Unit.Value;
    }
}
```

---

## DATA-004: Query Performance

**Type:** Steer

**Requirement:** Use `AsNoTracking()` for read-only EF Core queries. Avoid N+1 query patterns — use `Include()` for eager loading where needed. For DynamoDB, use batch operations where possible and design partition keys for your access patterns.

**Enumerable materialisation:** Materialise `IEnumerable<T>` to a concrete collection (e.g., `ToList()`, `ToArray()`) before iterating multiple times. Re-enumerating an `IEnumerable` can trigger multiple database round-trips or duplicate computation. Use `.Any()` rather than `.Count() > 0` to check for emptiness — `Any()` stops at the first element whereas `Count()` enumerates the entire collection.

**Severity:** Medium

**Exceptions:** Small lookup tables may not need `AsNoTracking()` overhead consideration.

**Evidence Required:** State which queries are read-only and confirm `AsNoTracking()` is applied. For any `Include()` usage, explain why eager loading is needed. For DynamoDB, state the partition key design and access patterns it supports.

✅ **Good:**

```csharp
// Read-only query with AsNoTracking
public async Task<IReadOnlyList<Greeting>> GetAllAsync(CancellationToken cancellationToken)
    => await context.Greetings
        .AsNoTracking()
        .OrderBy(g => g.Name)
        .ToListAsync(cancellationToken);

// Eager loading to avoid N+1
public async Task<Greeting?> GetWithResponsesAsync(GreetingIdentifier id, CancellationToken cancellationToken)
    => await context.Greetings
        .Include(g => g.Responses)
        .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
```

❌ **Bad:**

```csharp
// N+1 query — triggers a separate DB call for EACH greeting
var greetings = await context.Greetings.ToListAsync(ct);
foreach (var g in greetings)
{
    g.Responses = await context.Responses
        .Where(r => r.GreetingId == g.Id).ToListAsync(ct);
}
```

---

## Gotchas

- `UseSnakeCaseNamingConvention()` from the `EFCore.NamingConventions` package looks like a time-saver, but it is explicitly banned. Every column must be mapped with `.HasColumnName("snake_case")` individually in entity configuration. The convention-based approach hides the mapping and makes it impossible to audit column names without running the app.
- Inline entity configuration in `OnModelCreating` (e.g., `modelBuilder.Entity<T>(e => { ... })`) compiles fine but violates DATA-002. Always use separate `IEntityTypeConfiguration<T>` classes — one per entity — in the Infrastructure layer's `EntityConfigurations/` folder.
- For **owned** PostgreSQL databases, DbContext must inherit from `DatabaseContext` (which implements `IUnitOfWork` and dispatches domain events). For **external** databases the service does not own, DbContext inherits from `DbContext` directly. Mixing these up either breaks domain event dispatch or adds unwanted migration ownership.
- Data annotations (`[Required]`, `[MaxLength]`, `[Column]`) on domain entities compile and work, but they leak infrastructure concerns into the domain layer. Fluent API in entity configuration classes is the only accepted approach.
