# Bruno Main Flow Test Guide

## 1) Start API
Run API before opening requests:

```powershell
dotnet run --project API/API.csproj -p RestoreIgnoreFailedSources=true
```

Health check must return `200`:
- `GET http://localhost:5042/health`

## 2) Open Collection in Bruno App
- Open folder: `E:\pbl3\flight\backend\API\bruno\API`
- Collection root file: `opencollection.yml`

## 3) Create Environment `local`
In Bruno App:
- `Environments` -> `New Environment` -> name `local`
- Add variables:
  - `baseUrl = http://localhost:5042`
  - `adminEmail = admin@flightbooking.vn`
  - `adminPassword = Admin@123456`
  - `adminToken = ` (leave empty, fill after login)

Select active environment: `local`.

## 4) Login And Set Admin Token
Run request:
- `Users/post -api-v1-Users-login.yml`

Body:
```json
{
  "email": "{{adminEmail}}",
  "password": "{{adminPassword}}"
}
```

Copy `token` from response and set `adminToken` environment variable.

## 5) Configure Admin Auth Once
For each `admin/*` request, set Authorization in Bruno request UI:
- Auth type: `Bearer Token`
- Token: `{{adminToken}}`

Important:
- Some generated requests include query param `JWT token=...`.
- Remove or disable that query param in request Params tab.
- Keep only functional query params (`page`, `pageSize`, ...).

## 6) Main Flows To Run (Order)

### Flow A: System Smoke
1. `API/get -health.yml`
2. `Users/post -api-v1-Users-login.yml`

### Flow B: Flight Admin Core
1. `FlightsAdmin/get -api-v1-admin-FlightsAdmin-routes.yml`
2. `FlightsAdmin/post -api-v1-admin-FlightsAdmin-routes.yml`
3. `FlightsAdmin/put -api-v1-admin-FlightsAdmin-routes--routeId.yml`
4. `FlightsAdmin/post -api-v1-admin-FlightsAdmin.yml`
5. `FlightsAdmin/get -api-v1-admin-FlightsAdmin.yml`
6. `Flights/get -api-v1-Flights--id.yml`

### Flow C: Airport CRUD (new)
Use endpoints in Bruno `New Request` (if not generated yet):
1. `GET {{baseUrl}}/api/v1/admin/airports`
2. `POST {{baseUrl}}/api/v1/admin/airports`
3. `PUT {{baseUrl}}/api/v1/admin/airports/{id}`
4. `DELETE {{baseUrl}}/api/v1/admin/airports/{id}`

Sample POST body:
```json
{
  "code": "DAD",
  "name": "Da Nang International Airport",
  "city": "Da Nang",
  "country": "Vietnam"
}
```

### Flow D: Customer Basics
1. `Flights/post -api-v1-Flights-search.yml`
2. `Users/post -api-v1-Users-register.yml`

## 7) Quick Troubleshooting
- `401 Unauthorized`: token missing/expired -> login again and update `adminToken`.
- `400 BadRequest`: check required IDs (`routeId`, `aircraftId`) exist in DB.
- `500`: check API console logs for stack trace.
- `ECONNREFUSED`: API is not running on `http://localhost:5042`.

## 8) Recommended Test Data
- Keep one fixed admin account.
- Create dedicated route codes for testing (ex: `DAD-SGN-T1`).
- Use future `departureTime/arrivalTime`.
