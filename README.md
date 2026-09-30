# ShoppyAI

A full-stack order management app with two sides: a **customer portal** for browsing products, ordering and reviewing, and an **admin dashboard** for managing products, customers, orders and reviews. Stock is updated automatically and both sides get live notifications.

| Part | Stack |
|---|---|
| [`backend/`](backend) | ASP.NET Core Web API (.NET 10), Entity Framework Core 10, SQL Server LocalDB |
| [`frontend/`](frontend) | Angular 22 (standalone components, signals), Tailwind CSS 4 |

## Features

### Customer portal
- **Dashboard:** totals paid, orders, items and savings, plus two charts (spend per month, and spend per product) and recent orders.
- **Products:** search the catalogue, add to a cart and check out as one order.
- **Orders:** full history with status filter, order details, and cancel-your-own-order.
- **Reviews:** rate and review a product after ordering it. One review per product.
- **Notifications:** a live bell and toasts, backed by SignalR.
- **Account:** password change and two-factor authentication.

### Admin dashboard
- **Orders:** every order, with status and customer filters and paging; open one to see items and cancel it. Admins no longer create orders - customers do.
- **Products:** list, search and create. SKUs must be unique and are stored in upper case.
- **Customers:** list customers and jump to their orders.
- **Reviews:** every review, filterable by rating, deletable, and new ones arrive live.
- Totals are calculated on the server; cancelling an order returns its items to stock.
- **Stock safety:** stock is reserved with a single conditional database update, so orders placed at the same time can't oversell a product. A cancelled order returns its stock only once.
- **Accounts (ASP.NET Core Identity):** everything is behind a sign-in, with an `Admin` and a `Customer` role.
  - Customers sign up freely, with an email and password or **with Google**.
  - Admin sign-up needs a registration key, so nobody can make themselves an admin.
  - Email confirmation, password reset and change password, sent by email.
  - Optional two-factor authentication with an authenticator app, plus 10 recovery codes.
  - "Remember me" keeps you signed in for exactly 7 days; without it, a session lasts 8 hours of activity and ends when the browser closes.
  - Accounts lock for 15 minutes after 5 wrong passwords, and sign-in is rate limited per IP address.
- **Consistent errors:** validation errors return `400` and missing records return `404`, both as `{ "message": "..." }`. The frontend shows these messages to the user.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio, or on its own)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Node.js `^22.22.3`, `^24.15.0` or `>=26` (required by Angular 22), and npm

## Getting started

### 1. Backend

```bash
cd backend
cp .env.example .env        # then fill in the values (see below)
dotnet restore
dotnet ef database update   # creates AdvancedOrderDb and seeds sample data
dotnet run                  # http://localhost:5264
```

Settings live in `backend/.env`, which git ignores. [`backend/.env.example`](backend/.env.example) lists them all. The ones you must set:

| Setting | Purpose |
|---|---|
| `Auth__RegistrationKey` | Needed on the sign-up page. **Sign-up is switched off while this is empty.** Generate one with `openssl rand -base64 36`. |
| `Email__*` | SMTP account for confirmation and reset emails. Leave `Email__Password` empty and the emails are written to the terminal instead, which is fine for development. Gmail needs a 16-character App Password, not the account password. |
| `Frontend__BaseUrl` | Where email links point (`http://localhost:4200`). |
| `Google__ClientId` / `Google__ClientSecret` | Optional. Google sign-in for customers. Create an OAuth client in Google Cloud with redirect URI `http://localhost:5264/api/auth/google/callback`. Without these the Google button explains that it is not set up. |

The first admin account is confirmed automatically, so an email problem can't lock you out. Later accounts must confirm their email before signing in.

The connection string is in [`backend/appsettings.json`](backend/appsettings.json):

```
Server=(localdb)\MSSQLLocalDB;Database=AdvancedOrderDb;Trusted_Connection=True;TrustServerCertificate=True;
```

The database is seeded with 5 products (Laptop, Mouse, Keyboard, Monitor, Headphones) and 3 customers.

### 2. Frontend

```bash
cd frontend
npm install
npm start                   # http://localhost:4200
```

Open http://localhost:4200. The frontend calls the API at `http://localhost:5264/api`, which is set in [`frontend/src/app/core/config.ts`](frontend/src/app/core/config.ts).

> Run the backend with the default **http** profile. If you use a different port, update `API_BASE_URL` in `config.ts`. If you serve the frontend from a different origin, add it to `Cors:AllowedOrigins` in `appsettings.json`.

## API

Base URL: `http://localhost:5264/api`

All endpoints below need the auth cookie; without it they return `401`, and with the wrong role `403`. Rows marked **Admin** or **Customer** are limited to that role.

**Auth** (`/auth/*`), which does not require a session unless noted:

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/auth/register` | Create an admin account (needs the registration key) |
| `POST` | `/auth/register-customer` | Create a customer account |
| `GET` | `/auth/google/start`, `/auth/google/callback` | Google sign-in for customers |
| `POST` | `/auth/login` | Sign in. `portal` must be `Customer` or `Admin`; an account signing in through the wrong page gets `403`. Answers `requiresTwoFactor` when a code is needed |
| `POST` | `/auth/2fa/verify` | Second step: authenticator or recovery code |
| `POST` | `/auth/logout` | Clear the session |
| `GET` | `/auth/me` | The signed-in account and its role (used to restore the session) |
| `POST` | `/auth/confirm-email`, `/auth/resend-confirmation` | Email confirmation |
| `POST` | `/auth/forgot-password`, `/auth/reset-password` | Password reset by email |
| `POST` | `/auth/change-password` | Change password (signed in) |
| `GET`/`POST` | `/auth/2fa/setup`, `/2fa/enable`, `/2fa/disable`, `/2fa/recovery-codes` | Manage two-factor authentication (signed in) |

**Data:**

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/products?search=` | Active products, filtered by name or SKU |
| `GET` | `/products/{id}` | One active product |
| `POST` | `/products` | **Admin.** Create a product |
| `GET` | `/customers`, `/customers/{id}` | **Admin.** Customer list and details |
| `GET` | `/orders?status=&customerId=&page=1&pageSize=10` | **Admin.** Paged list of every order (`pageSize` max 100) |
| `GET` | `/orders/my?status=&page=1` | **Customer.** Their own orders |
| `GET` | `/orders/{id}` | Order with its items (customers only their own) |
| `POST` | `/orders` | **Customer.** Place an order for the signed-in customer |
| `PATCH` | `/orders/{id}/cancel` | Cancel an order and return its stock |
| `GET` | `/dashboard/customer` | **Customer.** Totals, chart data and recent orders |
| `GET` | `/reviews/product/{id}` | Reviews for a product, and whether you may add one |
| `POST` | `/reviews` | **Customer.** Review a product you ordered |
| `GET` | `/reviews?rating=&page=1` | **Admin.** Every review |
| `DELETE` | `/reviews/{id}` | **Admin.** Remove a review |
| `GET` | `/notifications` | Your notifications and unread count |
| `POST` | `/notifications/{id}/read`, `/notifications/read-all` | Mark as read |

Live notifications use a SignalR hub at `/hubs/notifications` (sign-in required).

Example requests for every endpoint are in [`backend/backend.http`](backend/backend.http). In Development, the OpenAPI document is at `/openapi/v1.json`.

**Create order request**

```json
{
  "discountPercent": 10,
  "items": [
    { "productId": 2, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

### Order rules

- The order belongs to the signed-in customer and needs at least one item.
- The discount must be between 0 and 100 (%).
- Each product can appear only once in an order, must be active, and must have enough stock.
- Quantities must be at least 1.
- Line totals, subtotal, discount and total are rounded to 2 decimal places. The price at the time of purchase is stored on each order item.
- Order status is `Confirmed` when the order is created and `Cancelled` after it is cancelled. An order can't be cancelled twice.

## Project structure

```
backend/
├── Controllers/        API endpoints (Order, Product, Customer)
├── Services/           Business rules and validation
├── Repositories/       Data access (EF Core)
├── Data/               ApplicationDbContext, seed data, unit of work
├── Middleware/         Global exception handler (400 / 401 / 403 / 404 / 409 / 500)
├── Hubs/               SignalR notification hub
├── Models/
│   ├── Entities/       AppUser, Product, Customer, Order, OrderItem, Review, Notification
│   └── DTOs/           Request and response models
└── Migrations/         EF Core migrations

frontend/src/app/
├── core/               API services, auth, models, config, error helper
├── shared/             Badges, alerts, pipes, validators, chart wrapper and theme
├── layout/             Admin and customer shells (sidebar, notification bell)
└── features/
    ├── auth/           login, register, admin-register, confirm/reset password
    ├── admin/          orders, products, customers, reviews
    ├── shop/           dashboard, catalog, cart, my-orders
    ├── orders/         order-detail (shared by both roles)
    ├── notifications/  notification list
    ├── account/        password and two-factor settings
    └── not-found/
```

**Routes:** the landing page is at `/`; customers live at `/dashboard`, `/products`, `/cart`, `/orders`, `/notifications`, `/account`; admins at `/admin/...`.

**Two sign-in pages.** Customers use `/login` (with Google) and staff use `/admin/login`. Each page refuses the other role's credentials, even when the password is correct, and offers a link to the right page.

## Useful commands

| Where | Command | Purpose |
|---|---|---|
| `backend/` | `dotnet build` | Compile the API |
| `backend/` | `dotnet ef migrations add <Name>` | Add a migration after changing the model |
| `frontend/` | `npm run build` | Production build to `dist/` |
| `frontend/` | `npm test` | Run unit tests (Vitest) |
