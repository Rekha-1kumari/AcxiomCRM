# AcxiomCRM

A modern, role-based enterprise Customer Relationship Management (CRM) web application built with **ASP.NET Core MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, and **SQLite**.

AcxiomCRM manages the entire customer lifecycle and sales pipeline—from prospect acquisition and lead qualification to customer onboarding, deal progression, activity tracking, compliance auditing, and reporting.

---

## Features

### 1. Authentication & Security
- **ASP.NET Core Identity** integration for secure authentication and cookie session management.
- Standard adaptive password hashing (PBKDF2 with SHA-256).
- Strict password policy enforcement (minimum 8 characters with lowercase, uppercase, digit, and special characters).
- Automatic account lockout protection after repeated consecutive failed login attempts.
- Anti-forgery token protection (`ValidateAntiForgeryToken`) on all state-altering requests.

### 2. Role-Based Access Control (RBAC)
- **Admin**: Full administrative privileges including user management, role assignments, account unlock, password resets, system audit logs, and complete CRM data oversight.
- **Manager**: Team-wide sales pipeline visibility, lead monitoring, customer tracking, follow-ups, and business intelligence reporting.
- **Sales Executive**: Dedicated workspace scoped to personally assigned customers, leads, opportunities, activities, and follow-ups.

### 3. Sales Pipeline & CRM Modules
- **Customer Management**: Complete master data management with duplicate prevention, search, and activity history.
- **Lead Management**: Capture, qualify, track, and convert qualified leads into Customer and Opportunity records.
- **Opportunity Management**: Multi-stage sales pipeline (Qualification, Proposal, Negotiation, Won, Lost) with probability weighting and target close dates.
- **Follow-Up Management**: Schedule, track, and complete customer follow-ups and touchpoints with schedule validation.
- **Activity Management**: Log calls, meetings, emails, and tasks linked to leads or customer accounts.
- **Audit Logging**: Comprehensive, append-only audit trail capturing user ID, action, affected entity, record ID, old/new values, IP address, and timestamp.
- **Business Intelligence & Reporting**: 8 comprehensive operational reports (Pipeline, Customer, Lead, Opportunity, Follow-Up, Conversion, User Activity, Audit) with CSV export capability.

### 4. Interactive Dashboard
- Real-time KPI summaries: Total Customers, Total Leads, Open Leads, Total Deals, Open Deals, Won Deals, Lost Deals, Nominal Pipeline Value, and Weighted Pipeline Value.
- Interactive **Chart.js** visualizations:
  - Lead Status breakdown (Doughnut chart)
  - Opportunity Pipeline value by stage (Bar chart)
  - Monthly sales trends (Line chart)
- Quick-filter views by timeframe: Today, This Week, This Month, All Time.

### 5. RESTful Web API
Secured REST endpoints serving data transfer objects (DTOs):
- `GET /api/customers` - List customers (role-scoped)
- `GET /api/customers/{id}` - Retrieve customer by ID
- `POST /api/customers` - Create customer
- `PUT /api/customers/{id}` - Update customer
- `DELETE /api/customers/{id}` - Deactivate customer
- `GET /api/leads` - List leads
- `POST /api/leads` - Create lead
- `GET /api/opportunities` - List opportunities
- `POST /api/opportunities` - Create opportunity
- `GET /api/reports/pipeline` - Stage-wise pipeline metrics

---

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 10.0)
- **ORM**: Entity Framework Core
- **Database**: SQLite
- **Security**: ASP.NET Core Identity
- **Frontend**: Razor Views, Bootstrap 5, Bootstrap Icons, jQuery Validation & Unobtrusive Validation
- **Data Visualization**: Chart.js

---

## Project Structure

```text
AcxiomCRM/
├── Controllers/
│   ├── AccountController.cs       # Authentication, registration, logout, lockout
│   ├── DashboardController.cs     # Real-time metrics and Chart.js datasets
│   ├── CustomersController.cs     # Customer CRUD, duplicate validation, search
│   ├── LeadsController.cs         # Lead capture, qualification, conversion
│   ├── OpportunitiesController.cs # Deal stages, probability, pipeline values
│   ├── FollowUpsController.cs     # Scheduled interactions & completion workflows
│   ├── ActivitiesController.cs    # Interaction logging (Calls, Meetings, etc.)
│   ├── UsersController.cs         # User administration, role management, unlock
│   ├── AuditLogsController.cs     # Security & activity audit trail
│   ├── ReportsController.cs       # Analytical reports and CSV export
│   └── Api/                       # REST API endpoints
├── Models/                        # Domain entities
├── ViewModels/                    # Input models, view models, and DTOs
├── Services/                      # Cross-cutting services (AuditService)
├── Data/                          # DbContext configuration and initial seeder
├── Views/                         # Responsive Razor views
└── Program.cs                     # Application configuration and middleware pipeline
```

---

## Getting Started

### Prerequisites
- [.NET SDK 10.0](https://dotnet.microsoft.com/download)

### Installation & Run

1. Clone the repository:
   ```bash
   git clone https://github.com/Rekha-1kumari/AcxiomCRM.git
   cd AcxiomCRM
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser and navigate to:
   ```
   http://localhost:5000
   ```

*(On initial startup, the database schema is automatically created and seeded with baseline roles, accounts, and sample CRM data).*

---

## Default Accounts

| Role | Username / Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@123` |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123` |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@123` |

---

## License

This project is licensed under the MIT License.
