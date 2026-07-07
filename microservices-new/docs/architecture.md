Architecture Document (Summary)

This project implements an e-commerce order system split into microservices:

- `api-gateway` (YARP) — single ingress point, routes to services, implements BFF endpoint for order details.
- `user-service` — user auth, PostgreSQL.
- `product-service` — product catalog, MongoDB + Redis cache.
- `inventory-service` — reserves inventory, communicates via RabbitMQ.
- `cart-service` — shopping cart, Redis.
- `order-service` — orders and saga orchestration (choreography), PostgreSQL.
- `notification-service` — sends emails on order events, RabbitMQ consumer.

Observability

- Serilog configured in each service, Seq added as a centralized log aggregator.
- Correlation ID propagated in HTTP headers and attached to RabbitMQ message headers.

Deployment

- Root `docker-compose.yml` brings up all services and data stores.
- GitHub Actions CI builds and integration tests; CD workflow builds/pushes images on tag.

Notes

- See `docs/ADRs` for architecture decision records.
