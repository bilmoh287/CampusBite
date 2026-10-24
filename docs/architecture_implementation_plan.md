# CampusBite - Enterprise Backend & Multi-Tier Architecture Plan (Version 5)

## 🎯 Executive Summary
CampusBite is transitioning from an offline-first single-device prototype into a multi-tier, enterprise-grade campus food-ordering, courier dispatch, and meal subscription platform. 

This plan details the phased roadmap to scale the platform to **50,000+ concurrent active users** across multiple universities, cafés, and courier runners using **Pragmatic Clean Architecture**, **ASP.NET Core (.NET 8 LTS)**, **Entity Framework Core 8**, and **Microsoft SQL Server**.

---

## 🏛️ Architectural Architecture & Project Layout

```text
CampusBite (Enterprise Solution)
│
├── 📱 CampusBite.Client (.NET MAUI 10)
│   ├── ViewModels/ (Observable MVVM with CommunityToolkit)
│   ├── Views/ (XAML Presentation Layer)
│   └── Services/Http/ (Typed HttpClient delegates consuming Shared DTOs)
│
├── 📦 CampusBite.Shared (.NET 8 LTS Class Library)
│   ├── Enums/ (UserRole, OrderStatus, PaymentMethod, ContractStatus, ExpenseCategory)
│   ├── DTOs/ (UserDto, CafeDto, ProductDto, OrderDto, TopSellingItemDto, ContractDto)
│   ├── Requests/ (LoginRequest, RegisterRequest, CreateOrderRequest, ExpenseRequest)
│   └── Responses/ (ApiResponse<T>, AuthResponse, PagedResult<T>)
│
└── 🌐 CampusBite.Api (ASP.NET Core Web API .NET 8 LTS)
    ├── Controllers/ (Thin, RESTful API Controllers inheriting BaseApiController)
    ├── Services/ (Business logic, calculations, transaction boundaries)
    ├── Domain/Entities/ (11 core relational domain models)
    └── Infrastructure/
        ├── Data/ (CampusBiteDbContext, Fluent API, Model configurations)
        ├── Migrations/ (EF Core SQL Server migrations)
        └── Seeders/ (Initial university, café, and menu test datasets)
```

---

## 📋 Phased Execution Roadmap

### Phase 1: Persistence & Relational Data Layer (Complete - Commit de2c2b6)
> **Goal:** Establish a robust SQL Server database with referential integrity, indexing, snapshot patterns, and seed data.

1. **`CampusBiteDbContext` Implementation:**
   * Register `DbSet<T>` for all 11 entities (`Universities`, `Users`, `Cafes`, `Categories`, `Products`, `Orders`, `OrderItems`, `ContractPlans`, `StudentContracts`, `ContractUsageLogs`, `Expenses`).
2. **Enterprise Fluent API Configurations (`OnModelCreating`):**
   * **Monetary Precision:** Enforce `.HasPrecision(10, 2)` across all decimal columns (prices, delivery fees, order totals, and expenses).
   * **Unique Constraints:** Unique indexes on `User.Email` and `Order.OrderNumber`.
   * **Referential Integrity & Delete Behaviors:**
     * `DeleteBehavior.Restrict` on `Order.CustomerId`, `Order.CourierId`, and `Cafe.OwnerId` to prevent accidental loss of historical transaction ledgers.
     * `DeleteBehavior.Cascade` on `Order.OrderItems`, `Cafe.Categories`, and `Cafe.Products`.
     * `DeleteBehavior.SetNull` on `User.UniversityId` and `Cafe.UniversityId`.
3. **Database Seeding (`DbInitializer`):**
   * Pre-seed universities (e.g. *Addis Ababa University*, *ASTU*), default campus cafés, categorised menus, and student meal plans.
4. **EF Core Migrations & SQL Server Deployment:**
   * Applied `20261021135224_InitialCreate` to database `CampusBiteDb`.

---

### Phase 2: Security, Authentication & Base Framework (Complete - Commit 8f84ec0)
> **Goal:** Secure the platform with token-based authentication, role-based authorization, and standardized API envelopes.

1. **Password Security:**
   * Password hashing using `BCrypt.Net-Next` with work factor 12.
2. **JWT Authentication & Token Lifecycle:**
   * Pure JWT Bearer tokens with claims: `sub`, `email`, `role` (`Student`, `CafeOwner`, `Courier`, `Admin`), and `university_id`.
   * Sliding session rotation via `RefreshTokens` table in SQL Server.
3. **Global Error Handling & API Response Envelope:**
   * Standardized `ApiResponse<T>` envelope for all endpoints.
   * `ExceptionMiddleware` transforming unhandled exceptions into structured responses without leaking internal stack traces.

---

### Phase 3: Catalog, Order Lifecycle & Ownership Policies (Complete - Commit bccd2f9)
> **Goal:** Implement the business engines as isolated, testable services injected into thin API controllers with strict policy-based security.

1. **Catalog & Location Engine:**
   * `UniversitiesController`: Fetch active campuses.
   * `CafesController`: Multi-vendor directory with university filtering and search.
   * `ProductsController`: Real-time inventory queries and manager menu CRUD.
2. **Order Lifecycle & KDS Engine:**
   * `OrdersController` & `OrderService`: Atomic order placement with price/item snapshotting and stock deduction.
   * Courier claim ticket workflow (`/api/orders/{id}/claim`) and status transitions.
3. **Resource-Based Ownership Policies (Preventing Horizontal Privilege Escalation):**
   * `OrderOwnerOrAdminHandler`: Enforces that only the ordering student, the assigned courier, the café manager, or an Admin can access an order.
   * `CafeOwnerOrAdminHandler`: Enforces that only the café owner or Admin can manage that café's catalog and orders.
4. **Rate Limiting & Security Auditing:**
   * `AuthLimiter` (5 req/min) on `/api/auth/login`.
   * `ApiLimiter` (100 req/min) on public endpoints.
   * Structured security alert logging on unauthorized access attempts.

---

### Phase 4: .NET MAUI Client Modernization (Next Step)
> **Goal:** Convert the MAUI client from direct SQLite access into an API-driven frontend.

1. **Network Infrastructure:**
   * Create `DelegatingHandler` for automatic Bearer token injection from `SecureStorage` and automatic refresh token rotation on 401.
   * Centralized `IApiService` with base URL configuration (`localhost` on Windows, `10.0.2.2` on Android emulator).
2. **Service Delegation:**
   * Swap local SQLite queries in existing MAUI services (`ProductService`, `OrderService`, `CafeAdminService`) to call HTTP REST endpoints.
3. **UI Preservation:**
   * All existing XAML Views and MVVM ViewModels remain intact, binding seamlessly to the updated service layer.

---

### Phase 5: High-Scale & Enterprise Hardening (50,000+ Users)
> **Goal:** Guarantee high throughput, low latency, and zero race conditions under campus peak hours.

1. **Database Indexing:**
   * Composite index on `Orders(CafeId, Status, CreatedAt)`.
   * Index on `Products(CafeId, IsAvailable)`.
   * Index on `StudentContracts(StudentId, Status)`.
2. **Concurrency Control:**
   * Optimistic concurrency tokens (`RowVersion` / `byte[]`) on `Products.StockQuantity` and `StudentContracts.RemainingCredits`.
3. **Performance Optimization:**
   * Enforce `.AsNoTracking()` on all read-only catalog queries.
