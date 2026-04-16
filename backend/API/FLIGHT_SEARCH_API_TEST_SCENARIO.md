# 🔍 Flight Search API - Complete Test Scenario

## 📋 Overview

This guide provides **step-by-step test scenarios** for the Flight Search API endpoint, including various search parameters, expected responses, and edge cases.

---

## 🎯 Prerequisites

Before testing, ensure:
- ✅ App is running: `dotnet run`
- ✅ Database migrations completed
- ✅ Sample data seeded
- ✅ Listening on `http://localhost:5000`

---

## 📊 Sample Data Available

### Airports
```
ID: 1 - SGN (Tân Sơn Nhất - TP.HCM)
ID: 2 - HAN (Nội Bài - Hà Nội)
ID: 3 - DAD (Đà Nẵng)
ID: 4 - CTS (Cần Thơ)
```

### Flights
```
VN001: SGN → HAN (Tomorrow 08:00) - Available
VN002: HAN → SGN (Tomorrow 14:00) - Available
VN003: SGN → DAD (Next week 09:00) - Available
```

### Seat Classes
```
ID: 1 - Economy (ECO) - 1,650,000 VND
ID: 2 - Business (BUS) - 3,850,000 VND
ID: 3 - First Class (FIRST) - 5,500,000 VND
```

---

## 🚀 Test Scenario 1: Basic Flight Search

### **Test 1.1: Search with All Parameters**

**Endpoint**: `GET /api/v1/bookings/search`

**Request URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=ECO
```

**cURL Command**:
```bash
curl -X GET "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=ECO" \
  -H "Accept: application/json"
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": [
    {
      "flightId": 1,
      "flightNumber": "VN001",
      "departureAirportId": 1,
      "departureAirportName": "SGN",
      "arrivalAirportId": 2,
      "arrivalAirportName": "HAN",
      "departureTime": "2024-01-16T08:00:00Z",
      "arrivalTime": "2024-01-16T10:25:00Z",
      "duration": 145,
      "aircraft": "Boeing 737",
      "availableSeats": {
        "ECO": 135,
        "BUS": 28,
        "FIRST": 10
      },
      "prices": {
        "ECO": 1650000,
        "BUS": 3850000,
        "FIRST": 5500000
      }
    }
  ]
}
```

**✅ Success Criteria**:
- Status: 200
- `success: true`
- Returns array of flights
- Contains all required fields
- Prices match sample data
- Available seats > 0

---

### **Test 1.2: Search with Multiple Passengers**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=3
```

**Expected Response**: ✅ 200 OK (same structure, different passenger count)

**✅ Verify**:
- Returns flights with enough seats for 3 passengers
- Prices calculated for multiple passengers (if applicable)
- All flights have `availableSeats` >= 3

---

### **Test 1.3: Search Different Route**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=2&arrivalAirportId=1&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ✅ 200 OK (returns VN002: HAN → SGN)

**✅ Verify**:
- Returns reverse direction flights
- Flight number is VN002
- Departure airport is HAN (ID: 2)
- Arrival airport is SGN (ID: 1)

---

## 🧪 Test Scenario 2: Seat Class Filter

### **Test 2.1: Search for Business Class**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=BUS
```

**Expected Response**: ✅ 200 OK

**✅ Verify**:
- Returns flights with business class available
- Business class price: 3,850,000 VND
- `availableSeats.BUS > 0`

---

### **Test 2.2: Search for First Class**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=FIRST
```

**Expected Response**: ✅ 200 OK

**✅ Verify**:
- Returns flights with first class available
- First class price: 5,500,000 VND
- `availableSeats.FIRST > 0`

---

## 📅 Test Scenario 3: Date Variations

### **Test 3.1: Search Tomorrow**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ✅ 200 OK

**✅ Verify**:
- Returns tomorrow's flights
- Contains VN001 (departs 08:00)

---

### **Test 3.2: Search Next Week**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=3&departureDate=2024-01-23&passengerCount=1
```

**Expected Response**: ✅ 200 OK or 200 OK (empty list)

**✅ Verify**:
- If flights exist, return them
- If no flights, return empty array with `success: true`

---

## 🚫 Test Scenario 4: Error Cases

### **Test 4.1: Invalid Departure Airport**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=999&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request or 404 Not Found
```json
{
  "error": "Airport not found" or "Invalid departure airport"
}
```

**✅ Verify**:
- Status: 400 or 404
- Error message is clear
- No flights returned

---

### **Test 4.2: Invalid Arrival Airport**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=999&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request
```json
{
  "error": "Airport not found"
}
```

**✅ Verify**:
- Status: 400
- Proper error message

---

### **Test 4.3: Same Departure and Arrival**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=1&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request
```json
{
  "error": "Departure and arrival airports must be different"
}
```

**✅ Verify**:
- Status: 400
- Error message
- No flights returned

---

### **Test 4.4: Invalid Date Format**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=invalid&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request
```json
{
  "error": "Invalid date format"
}
```

**✅ Verify**:
- Status: 400
- Clear error message

---

### **Test 4.5: Past Date**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2023-01-01&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request or 200 OK (empty)
```json
{
  "error": "Departure date cannot be in the past"
}
```

**✅ Verify**:
- Status: 400
- Error about past date OR empty results

---

### **Test 4.6: Negative Passenger Count**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=-1
```

**Expected Response**: ❌ 400 Bad Request
```json
{
  "error": "Passenger count must be greater than 0"
}
```

**✅ Verify**:
- Status: 400
- Error about invalid passenger count

---

### **Test 4.7: Missing Required Parameters**

**URL** (missing departureAirportId):
```
http://localhost:5000/api/v1/bookings/search?arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ❌ 400 Bad Request
```json
{
  "error": "Departure airport ID is required"
}
```

**✅ Verify**:
- Status: 400
- Clear error about missing parameter

---

## 🔄 Test Scenario 5: Pagination & Filtering

### **Test 5.1: Search with Sorting**

**URL** (if supported):
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&sortBy=price&sortOrder=asc
```

**Expected Response**: ✅ 200 OK (sorted by price, ascending)

**✅ Verify**:
- Flights sorted by price (lowest first)
- All required fields present

---

### **Test 5.2: Search with Limit**

**URL** (if supported):
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&limit=5
```

**Expected Response**: ✅ 200 OK (max 5 flights)

**✅ Verify**:
- Returns max 5 flights
- All flights match criteria

---

## 📊 Test Scenario 6: Advanced Search

### **Test 6.1: Search with Price Range**

**URL** (if supported):
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&minPrice=1000000&maxPrice=2000000
```

**Expected Response**: ✅ 200 OK (flights in price range)

**✅ Verify**:
- Only returns flights within price range
- Economy class (1,650,000) included
- Business class (3,850,000) excluded

---

### **Test 6.2: Search with Time Window**

**URL** (if supported):
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&departureTimeStart=06:00&departureTimeEnd=12:00
```

**Expected Response**: ✅ 200 OK (flights in time window)

**✅ Verify**:
- VN001 (08:00) included
- Only flights between 06:00 and 12:00

---

## 🔐 Test Scenario 7: Authentication

### **Test 7.1: Search Without Token (Public Endpoint)**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1
```

**Expected Response**: ✅ 200 OK (public endpoint, no auth required)

**✅ Verify**:
- Works without Authorization header
- Returns flights normally

---

### **Test 7.2: Search With Valid Token**

**URL**:
```
http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1
```

**Headers**:
```
Authorization: Bearer <VALID_TOKEN>
Accept: application/json
```

**Expected Response**: ✅ 200 OK

**✅ Verify**:
- Works with token
- Same results as without token

---

### **Test 7.3: Search With Invalid Token**

**Headers**:
```
Authorization: Bearer invalid.token.here
```

**Expected Response**: ❌ 401 Unauthorized (if endpoint requires auth)
OR ✅ 200 OK (if endpoint is public)

**✅ Verify**:
- Proper handling of invalid token
- Clear error message (if applicable)

---

## 📈 Test Scenario 8: Performance

### **Test 8.1: Response Time**

**Measure**:
```bash
time curl -X GET "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1"
```

**Expected**: < 1 second

**✅ Verify**:
- Response time acceptable
- No timeout errors
- Console log shows duration

---

### **Test 8.2: Multiple Concurrent Requests**

**Command**:
```bash
for i in {1..10}; do
  curl -X GET "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1" &
done
```

**✅ Verify**:
- All requests succeed
- No rate limiting errors
- Consistent response times

---

## 🧪 Using Postman

### **Setup**

1. **Create new Request**:
   - Method: `GET`
   - URL: `{{base_url}}/bookings/search`

2. **Add Query Parameters**:
   - `departureAirportId` = `1`
   - `arrivalAirportId` = `2`
   - `departureDate` = `2024-01-16`
   - `passengerCount` = `1`
   - `seatPreference` = `ECO` (optional)

3. **Add Headers**:
   - `Accept` = `application/json`
   - `Authorization` = `Bearer {{token}}` (optional for public endpoint)

4. **Click Send**

5. **Verify Response**:
   - Status: 200
   - Body contains flight data
   - Response time shown

---

## 📝 Test Results Template

```
Test Scenario 1.1: Basic Flight Search
=====================================
Endpoint: GET /api/v1/bookings/search
URL: http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=ECO
Status Code: 200 ✅
Response Time: 234ms ✅
Success: true ✅
Flights Returned: 1 ✅
Has VN001: Yes ✅
Economy Price: 1,650,000 ✅
Available Seats (ECO): 135 ✅
Notes: All criteria met
```

---

## 🎯 Summary Checklist

### Basic Searches
- [ ] Search with all parameters
- [ ] Search different routes
- [ ] Search multiple passengers
- [ ] Search by seat class

### Date Testing
- [ ] Search tomorrow
- [ ] Search next week
- [ ] Verify date format

### Error Handling
- [ ] Invalid airport ID
- [ ] Same departure/arrival
- [ ] Invalid date format
- [ ] Past date
- [ ] Negative passenger count
- [ ] Missing parameters

### Advanced Features
- [ ] Sorting (if available)
- [ ] Filtering (if available)
- [ ] Price range (if available)

### Performance
- [ ] Response time < 1s
- [ ] Multiple concurrent requests
- [ ] No errors under load

### Security
- [ ] Works without token
- [ ] Works with valid token
- [ ] Rejects invalid token (if applicable)

**Total Tests**: 25+ scenarios

---

## 🚀 Quick Test Commands

### Test 1: Basic Search
```bash
curl "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1"
```

### Test 2: Business Class
```bash
curl "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1&seatPreference=BUS"
```

### Test 3: Different Route
```bash
curl "http://localhost:5000/api/v1/bookings/search?departureAirportId=2&arrivalAirportId=1&departureDate=2024-01-16&passengerCount=1"
```

### Test 4: Invalid Airport
```bash
curl "http://localhost:5000/api/v1/bookings/search?departureAirportId=999&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=1"
```

---

## ✅ Success Criteria

**API Search is working when**:
- ✅ Basic search returns flights
- ✅ Filters work correctly
- ✅ Error handling is proper
- ✅ Response time < 1 second
- ✅ Pagination works (if available)
- ✅ Data matches sample data

---

**Ready to test?** Start with Test Scenario 1! 🚀

For Postman setup: See `POSTMAN_TESTING_GUIDE.md`  
For complete API plan: See `API_TESTING_PLAN.md`  
For quick reference: See `API_TESTING_QUICK_REFERENCE.md`
