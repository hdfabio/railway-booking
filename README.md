# Railway Booking App

.NET 8 + Angular railway reservation app. One modular-monolith API with in-process events after booking. Angular talks to the API over REST.

## Architecture

- One host: `src/services/Railway.Gateway` on port 5000
- Modules in `Railway.Application`: Identity, Catalog, Inventory, Booking, Payment, Notification, Reporting
- Booking is transactional. After commit, `booking.created` / `booking.cancelled` handlers refresh seats, log payment, send simulated notifications, and fill the event stream
- Frontend: `RailwayApiService` uses `HttpClient` against `/api/...`

## Run

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet"
dotnet build RailwayBooking.sln
.\scripts\run-api.ps1
```

```powershell
cd railway-web
npm install
npm start
```

Open `http://localhost:4200`. Demo login: `demo@example.com` / `Demo123!`

```http
GET  http://localhost:5000/api/health
GET  http://localhost:5000/api/trains/search?source=NDLS&destination=MMCT&date=2026-09-15&class=3AC&quota=GENERAL
POST http://localhost:5000/api/auth/login
GET  http://localhost:5000/api/events
```
