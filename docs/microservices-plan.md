# Microservices Migration Plan

## Target Folder Structure

```
microservices-new/
├── api-gateway/              # .NET 8 - YARP reverse proxy
├── user-service/             # .NET 8 + PostgreSQL
├── product-service/          # .NET 8 + MongoDB
├── inventory-service/        # .NET 8 + Redis
├── cart-service/             # .NET 8 + Redis
├── order-service/            # .NET 8 + PostgreSQL
├── frontend/                 # Next.js 14 (updated)
└── docker-compose.yml
```

---

## Services

### 1. API Gateway — Port 5000

- **Technology**: YARP (Yet Another Reverse Proxy) by Microsoft
- **Responsibilities**:
  - Route requests to the correct downstream services
  - Centralized JWT authentication — all other services trust it
  - Centralized CORS policy
- **Routing table**:

| Path | Downstream Service |
| :--- | :--- |
| `/api/auth/**` | user-service:5001 |
| `/api/products/**` | product-service:5002 |
| `/api/inventory/**` | inventory-service:5003 |
| `/api/cart/**` | cart-service:5004 |
| `/api/orders/**` | order-service:5005 |

---

### 2. User Service — Port 5001

- **Database**: PostgreSQL — table: `Users`
- **Migrated from monolith**: `AuthController`, `User` entity, `JwtService`
- **Endpoints**:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/auth/register` | Register a new customer |
| `POST` | `/auth/login` | Authenticate and receive JWT |
| `POST` | `/auth/admin-register` | Register an admin user |
| `GET` | `/users/{id}` | Internal use — fetch user by ID |

---

### 3. Product Service — Port 5002

- **Database**: MongoDB — collection: `products`
- **Migrated from monolith**: `ProductsController`, `Product` entity (adapted to MongoDB document model)
- **Change from monolith**: `StockQuantity` is removed — stock management moves to Inventory Service
- **Endpoints**:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/products` | List all products |
| `GET` | `/products/{id}` | Get product by ID |
| `POST` | `/products` | Create product (Admin) |
| `PUT` | `/products/{id}` | Update product (Admin) |
| `DELETE` | `/products/{id}` | Delete product (Admin) |

---

### 4. Inventory Service — Port 5003

- **Database**: Redis — key: `inventory:{productId}` → value: stock count
- **Migrated from monolith**: `StockQuantity` logic from `OrderService` and `CartController`
- **Endpoints**:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/inventory/{productId}` | Get current stock level |
| `POST` | `/inventory/{productId}/reserve` | Atomic stock deduction (on order) |
| `POST` | `/inventory/{productId}/release` | Return stock (on cancellation) |
| `PUT` | `/inventory/{productId}` | Manual stock update (Admin) |

---

### 5. Cart Service — Port 5004

- **Database**: Redis — key: `cart:{userId}` → Hash of cart items
- **Migrated from monolith**: `CartController` — all logic
- **Change from monolith**: removes direct dependency on Products DB — stock checks go through Inventory Service via HTTP
- **Endpoints**:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/cart` | Get current user's cart |
| `POST` | `/cart/add` | Add item to cart |
| `PUT` | `/cart/update/{itemId}` | Update item quantity |
| `DELETE` | `/cart/remove/{itemId}` | Remove item from cart |
| `DELETE` | `/cart/clear` | Clear entire cart |

---

### 6. Order Service — Port 5005

- **Database**: PostgreSQL — tables: `Orders`, `OrderItems`
- **Migrated from monolith**: `OrdersController`, `OrderService`, `Order` and `OrderItem` entities
- **Change from monolith**: instead of directly modifying `StockQuantity` → calls Inventory Service via HTTP to reserve stock
- **Endpoints**:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/orders` | Get orders (own for Customer, all for Admin) |
| `POST` | `/orders` | Place a manual order |
| `POST` | `/orders/from-cart` | Place order from cart |

---

## Inter-Service Communication

```
Frontend
    │
    │ HTTP
    ▼
API Gateway (JWT validation + routing)
    │
    ├──▶ User Service
    │
    ├──▶ Product Service
    │
    ├──▶ Cart Service ──────────▶ Inventory Service (stock check)
    │
    └──▶ Order Service ─────────▶ Inventory Service (reserve stock)
                       ─────────▶ Cart Service (clear cart after order)
```

---

## Database Decisions

| Service | Database | Reason |
| :--- | :--- | :--- |
| User | PostgreSQL | ACID compliance, unique email constraint |
| Product | MongoDB | Flexible schema, read-heavy catalog |
| Inventory | Redis | Atomic DECR operations, high-speed writes |
| Cart | Redis | Session-like data, natural TTL support |
| Order | PostgreSQL | ACID transactions, permanent audit trail |

> **Note**: Inventory and Cart share the same Redis instance using key prefixes (`inventory:` and `cart:`).
> User and Order share the same PostgreSQL instance using separate databases.

---

## Implementation Order

| Step | Service | Reason |
| :--- | :--- | :--- |
| 1 | User Service | Foundation — all services depend on JWT |
| 2 | Product Service | No dependencies on other services |
| 3 | Inventory Service | Depends only on product IDs |
| 4 | Cart Service | Depends on Inventory Service |
| 5 | Order Service | Depends on Inventory + Cart services |
| 6 | API Gateway | Connects everything together |
| 7 | Frontend | Update API URLs to route through Gateway |
