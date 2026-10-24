# CampusBite — New PC Setup & Continuation Guide

This guide contains everything you need to set up and continue developing **CampusBite** on your own computer.

---

## 1. What Was Completed Before PC Transfer
- **Phase 1 (Data Layer & SQL Server):** Entities, Fluent API configurations, EF Core migrations (`InitialCreate`), and initial seeding (`DbInitializer.cs`).
- **Phase 2 (Auth & Security):** Pure JWT authentication (no ASP.NET Identity bloat), refresh token rotation, password hashing with BCrypt, rate limiting, and exception middleware.
- **Phase 3 (Catalog, Orders & Ownership Policies):** Universities, Cafés, Products CRUD, atomic order creation, and resource-based ownership policies (`OrderOwnerOrAdmin`, `CafeOwnerOrAdmin`) to prevent Horizontal Privilege Escalation.
- **Git State:** All code is pushed and synchronized to GitHub: `https://github.com/bilmoh287/CampusBite` (branch `master`).

---

## 2. Prerequisites for Your New PC

Install the following software on your new machine:
1. **Git for Windows:** [git-scm.com](https://git-scm.com/download/win)
2. **.NET 8 SDK:** [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Visual Studio 2022** (Community Edition is free):
   - Make sure to check these two workloads during installation:
     - **.NET Multi-platform App UI development** (for the MAUI client)
     - **ASP.NET and web development** (for the backend API)
4. **Microsoft SQL Server:**
   - Install **SQL Server 2022 Express** or **Developer Edition**.
   - Install **SQL Server Management Studio (SSMS)**.
5. **EF Core CLI Tools (Open PowerShell/Terminal on new PC):**
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

---

## 3. Step-by-Step Setup on Your New PC

### Step 1: Clone the Repository
Open PowerShell or your preferred terminal in your desired folder (e.g. `C:\Projects`):
```powershell
git clone https://github.com/bilmoh287/CampusBite.git
cd CampusBite
```

### Step 2: Configure Your Local SQL Server Connection
Open `CampusBite.Api/appsettings.json` and change the `Server` name from `IGRIS` (the cousin's machine name) to your local SQL Server instance:

* If using default SQL Server:
  ```json
  "DefaultConnection": "Server=localhost;Database=CampusBiteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  ```
* If using SQL Express:
  ```json
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CampusBiteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  ```
* If using LocalDB:
  ```json
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CampusBiteDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  ```

### Step 3: Create the Database & Apply Migrations
Run this command from the repository root:
```powershell
dotnet ef database update --project CampusBite.Api
```
> [!NOTE]
> This single command recreates the entire SQL Server schema on your new machine including all tables, constraints, and relationships.

### Step 4: Run the API & Seed Data
```powershell
dotnet run --project CampusBite.Api/CampusBite.Api.csproj
```
When the API starts, `DbInitializer.cs` will automatically detect the fresh database and seed:
- 3 Universities (AAU 4 Kilo, AAU 6 Kilo, ASTU)
- 4 Test Accounts (`admin@campusbite.com`, `manager@centralcafe.com`, `student@aau.edu.et`, `courier@campusbite.com` — all with password `Password123!`)
- Central Campus Diner & 4 menu items with inventory
- Meal subscription plans

Open `http://localhost:5278/swagger` in your browser to verify Swagger is running!

---

## 4. How to Continue Pair Programming on Your New PC

When you install and open **Antigravity** (or your AI coding assistant) on your new PC, open the `CampusBite` workspace folder and send this prompt:

```text
Hi! We are continuing development of CampusBite. 
Here is where we left off:
- Pragmatic Clean Architecture monorepo with CampusBite.Api, CampusBite.Shared, and CampusBite.Client.
- Phase 1 (Data Layer & Migrations), Phase 2 (JWT Auth & Refresh Tokens), and Phase 3 (Catalog, Order Lifecycle, and Policy-Based Ownership Authorization) are complete and tested.
- We are ready to begin Phase 4: Client Integration (.NET MAUI Transition).
Please inspect the repository and let's start Phase 4!
```

---

## 5. Important Reminder Before Returning the PC
If you have any practice, interview, or personal files stored outside this repository (such as files in `C:\Users\remzi\Practice`), remember to copy them to a USB drive or email them to yourself before returning your cousin's computer.
