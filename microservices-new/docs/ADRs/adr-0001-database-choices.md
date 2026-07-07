# ADR-0001: Database Choices

Date: 2026-07-06

Context

We need to choose appropriate data stores for each microservice according to access patterns and consistency needs.

Decision

- `OrderService` — PostgreSQL (relational): money and transactional operations require ACID guarantees.
- `ProductCatalogService` — MongoDB (document): flexible schema for product attributes per category, fast reads for catalog browsing.
- `InventoryService` — Redis (key-value) + Postgres (optional): Redis for fast increments/decrements and reservation state; persistent store for long-term records if needed.

Consequences

- Pros: each service owns its data and can scale independently; technology choices map to access patterns.
- Cons: added operational complexity and need for eventual consistency handling across services.

Alternatives considered

- Cassandra for inventory: high write throughput but eventual consistency may complicate stock correctness.
- DynamoDB: managed alternative; chosen if moving to cloud.

Status: Accepted
