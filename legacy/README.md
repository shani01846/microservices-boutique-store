# ECommerce Monolith - בסיס למיקרוסרביסים

מערכת אי-קומס מונוליטית לחנות בגדים, בנויה עם .NET 8 WebAPI, PostgreSQL ו-Next.js. מערכת זו משמשת כנקודת מוצא למעבר עתידי לארכיטקטורת מיקרוסרביסים.

## סטאק טכנולוגי

- **Backend**: .NET 8 WebAPI עם JWT Authentication
- **Database**: PostgreSQL עם Entity Framework Core
- **Frontend**: Next.js 14 עם App Router, TypeScript, ו-Tailwind CSS
- **Infrastructure**: Docker Compose
- **Security**: BCrypt לסיסמאות, JWT Tokens לאימות

## מבנה הפרויקט

```
microservices/
├── docker-compose.yml
├── backend/
│   ├── src/
│   │   ├── ECommerce.Domain/          # ישויות עסקיות (User, Product, Order, Cart)
│   │   ├── ECommerce.Application/     # DTOs ו-Interfaces
│   │   ├── ECommerce.Infrastructure/  # EF Core + JWT Service
│   │   └── ECommerce.API/            # Controllers עם Authorization
│   └── Dockerfile
├── frontend/
│   ├── app/
│   │   ├── auth/                     # דפי התחברות והרשמה
│   │   ├── admin/                    # פאנל ניהול (Admin בלבד)
│   │   └── cart/                     # סל קניות
│   ├── components/                   # Header עם Navigation
│   ├── lib/                         # Auth Context + API Utils
│   └── Dockerfile
└── README.md
```

## תכונות

### ניהול משתמשים ואימות
- **הרשמה והתחברות**: מערכת אימות מלאה עם JWT
- **הרשאות**: Customer (לקוח רגיל) ו-Admin (מנהל)
- **אבטחה**: הצפנת סיסמאות עם BCrypt
- **Session Management**: JWT Tokens עם localStorage

### קטלוג מוצרים
- **לקוחות**: צפייה בכל המוצרים והוספה לסל
- **מנהלים**: CRUD מלא (יצירה, עריכה, מחיקה) של מוצרים
- **מאפייני לבוש**: שם, תיאור, מחיר, קטגוריה, מידה, מלאי

### סל קניות
- **סל אישי**: כל משתמש רשום מקבל סל קניות אוטומטי
- **ניהול פריטים**: הוספה, עדכון כמות, הסרה
- **חישוב מחיר**: מעדכן סכומים בזמן אמת
- **אינטגרציה עם מלאי**: בדיקת זמינות לפני הוספה

### ביצוע הזמנות
- **הזמנה מהסל**: המרת סל להזמנה בקליק אחד
- **הזמנה ידנית**: אפשרות ליצור הזמנה ישירות
- **ניהול מלאי אוטומטי**: הפחתת כמויות בעת ביצוע הזמנה
- **טיפול בטרנזקציות**: Rollback אוטומטי במקרה של כשל

### פאנל ניהול (Admin)
- **גישה מוגבלת**: רק למשתמשים עם הרשאת Admin
- **ניהול מוצרים**: הוספה, עריכה ומחיקה של מוצרים
- **ממשק ידידותי**: טפסים מתקדמים עם validation
- **צפייה בהזמנות**: מנהלים יכולים לראות את כל ההזמנות

### מעקב בריאות המערכת
- **Health Check**: נקודת קצה לבדיקת תקינות API ובסיס הנתונים

## התחלה מהירה

### דרישות מוקדמות
- Docker ו-Docker Compose
- Git

### הפעלת המערכת

1. **שכפול הפרויקט**:
```bash
git clone <repository-url>
cd microservices
```

2. **הפעלת כל השירותים**:
```bash
docker compose up --build
```

זה יפעיל:
- PostgreSQL על פורט 5432
- .NET API על פורט 5000
- Next.js Frontend על פורט 3000

3. **גישה למערכת**:
- **Frontend**: http://localhost:3000
- **API Documentation (Swagger)**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/health

### חשבונות ברירת מחדל

לצורך בדיקה, תוכל ליצור חשבון Admin באמצעות:
```bash
POST http://localhost:5000/api/auth/admin-register
{
  "email": "admin@example.com",
  "password": "Admin123!",
  "firstName": "Admin",
  "lastName": "User"
}
```

## שימוש במערכת

### לקוחות רגילים
1. **הרשמה**: צור חשבון חדש בדף /auth/register
2. **התחברות**: התחבר בדף /auth/login
3. **קנייה**: עיין במוצרים והוסף לסל
4. **תשלום**: עבור לסל ובצע הזמנה

### מנהלי מערכת
1. **יצירת חשבון אדמין**: השתמש ב-admin-register endpoint
2. **גישה לפאנל**: לחץ על "Admin Panel" בתפריט
3. **ניהול מוצרים**: הוסף, ערוך או מחק מוצרים
4. **מעקב הזמנות**: צפה בכל ההזמנות במערכת

## API Endpoints

### Authentication
```bash
# הרשמה
POST /api/auth/register

# התחברות
POST /api/auth/login

# הרשמה כמנהל
POST /api/auth/admin-register
```

### Products
```bash
# צפייה במוצרים (כולם)
GET /api/products

# יצירת מוצר (Admin בלבד)
POST /api/products

# עדכון מוצר (Admin בלבד)
PUT /api/products/{id}

# מחיקת מוצר (Admin בלבד)
DELETE /api/products/{id}
```

### Cart
```bash
# צפייה בסל (משתמש מחובר)
GET /api/cart

# הוספה לסל
POST /api/cart/add

# עדכון כמות
PUT /api/cart/update/{itemId}

# הסרה מהסל
DELETE /api/cart/remove/{itemId}
```

### Orders
```bash
# צפייה בהזמנות (משתמש: שלו, Admin: כולם)
GET /api/orders

# יצירת הזמנה מהסל
POST /api/orders/from-cart

# יצירת הזמנה ידנית
POST /api/orders
```

## פיתוח

### הרצה מקומית ללא Docker

#### Backend
```bash
cd backend/src/ECommerce.API
dotnet restore
dotnet run
```

#### Frontend
```bash
cd frontend
npm install
npm run dev
```

### מיגרציות בסיס נתונים

הוספת מיגרציה חדשה:
```bash
cd backend/src/ECommerce.API
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## ארכיטקטורה

המונוליט בנוי על פי עקרונות Clean Architecture:

1. **Domain Layer**: ישויות עסקיות (User, Product, Order, Cart)
2. **Application Layer**: DTOs ו-Interfaces
3. **Infrastructure Layer**: EF Core + JWT Service + BCrypt
4. **API Layer**: Controllers עם Authorization

### תכונות אבטחה
- **JWT Authentication**: אימות מבוסס tokens
- **Role-Based Authorization**: הפרדה בין Customer ל-Admin
- **Password Hashing**: BCrypt לאחסון מאובטח של סיסמאות
- **CORS Configuration**: הגנה על cross-origin requests

המבנה הזה מקל על פירוק עתידי למיקרוסרביסים על ידי הפרדה ברורה של אחריויות.

## מעבר עתידי למיקרוסרביסים

המונוליט הזה מוכן לפירוק על פי התחומים הבאים:
1. **User Service**: ניהול משתמשים ואימות
2. **Product Service**: ניהול קטלוג מוצרים
3. **Cart Service**: ניהול סלי קניות
4. **Order Service**: עיבוד הזמנות
5. **Inventory Service**: ניהול מלאי

## רישיון

פרויקט לימודי בלבד.

---

## API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/auth/login` | User authentication |
| `GET` | `/api/products` | Browse products |
| `POST` | `/api/orders` | Place an order (including inventory reservation) |
| `GET` | `/api/cart` | View shopping cart |
| `GET` | `/api/health` | System health check |

---

## Monolith Scalability Challenges

As this system grows toward a microservices architecture, the current monolithic design presents the following structural limitations:

### 1. Data Contention & Performance Bottlenecks

All functional modules — Orders, Products, and Cart — share a single `ApplicationDbContext` and a single PostgreSQL database instance. Under high-traffic conditions, write-intensive operations such as inventory reservation during concurrent order placements introduce database-level locking. This directly contends with read-heavy workloads (e.g., product catalog browsing), degrading overall system responsiveness and throughput.

### 2. Deployment Coupling

The entire application is packaged and deployed as a single unit. As a result, any isolated change — such as a modification to inventory reservation logic — necessitates a full redeployment of the API. This tight coupling increases the blast radius of each release, raises the risk of unintended side effects across unrelated modules, and significantly slows down the delivery pipeline.

### 3. Scaling Limitations

The monolithic architecture constrains scaling to a vertical model — increasing the capacity of a single server. It is not possible to scale individual modules independently based on demand. For example, during a high-traffic product browsing event, the entire application must be scaled rather than the Product Catalog module alone. This results in inefficient resource utilization and higher infrastructure costs.

---

## Architecture Decisions

### PostgreSQL as the Database Engine

PostgreSQL was selected as the primary data store for several reasons. Its support for ACID-compliant transactions is critical for an e-commerce system where operations such as order placement and inventory deduction must be atomic and consistent. PostgreSQL also handles relational data models naturally — the relationships between Users, Orders, Products, and Carts map cleanly to its table structure with enforced foreign key constraints. Additionally, PostgreSQL is well-supported by the .NET ecosystem via Npgsql, and its maturity and performance under concurrent workloads make it a reliable foundation for a system that is intended to scale into microservices.

### Entity Framework Core as the ORM

EF Core was chosen to abstract database interactions through a strongly-typed, code-first model. This aligns with the Clean Architecture approach of the project — domain entities are defined in the Domain layer and EF Core configuration lives in the Infrastructure layer, keeping concerns separated. EF Core's `DbContext` also provides a natural unit-of-work boundary, which is important for transactional consistency during order processing. The code-first approach makes the schema easy to evolve alongside the domain model, and the Npgsql EF Core provider integrates seamlessly with PostgreSQL.

### Next.js 14 as the Frontend Framework

Next.js 14 with the App Router was selected for the frontend to take advantage of React Server Components, file-based routing, and built-in TypeScript support. The App Router structure maps naturally to the application's access model — route segments such as `app/admin/`, `app/cart/`, and `app/auth/` align directly with the three user flows (Admin, Customer, Guest). Tailwind CSS was included for utility-first styling that keeps component markup lean and consistent. Next.js also simplifies future improvements such as server-side rendering for product pages, which would benefit SEO and initial load performance.

### .NET 8 WebAPI as the Backend Framework

.NET 8 provides a high-performance, cross-platform runtime well-suited for building RESTful APIs. Its built-in dependency injection, middleware pipeline, and tight integration with EF Core and JWT authentication reduced the amount of boilerplate required to implement a secure, layered API. The LTS status of .NET 8 also ensures long-term stability for a project that is intended to serve as a baseline for a microservices migration.
