# 🚀 API Testing Quick Reference

## Step-by-Step Quick Start (15 minutes)

### 1. Start the App
```bash
cd API
dotnet run
```
Wait for: `Listening on: http://localhost:5000`

### 2. Open Swagger UI
```
http://localhost:5000
```

### 3. Test 3 Key Endpoints

#### A. Login (Get Token)
```
POST /users/login
{
  "email": "user1@gmail.com",
  "password": "Test@1234"
}
```
✅ **Save the token** (you'll need it)

#### B. Search Flights (Public)
```
GET /bookings/search?departureAirportId=1&arrivalAirportId=2&passengerCount=1
```
✅ **Should see VN001 flight**

#### C. Get My Bookings (Protected)
```
GET /bookings/my-bookings
Headers: Authorization: Bearer <YOUR_TOKEN>
```
✅ **Should return 200 (empty or with data)**

---

## 📋 Key Test Scenarios

### Authentication Tests
```
1. Login → Get token
2. Use token in header
3. Invalid token → 401
4. Missing token → 401
```

### Booking Tests
```
1. Search flights
2. Create booking with promotion
3. View my bookings
4. Get booking details
```

### Admin Tests
```
1. Admin login
2. View all bookings
3. View refunds
4. Cancel booking
```

### Error Tests
```
1. Invalid email → 400
2. Wrong password → 401
3. Missing fields → 400
4. Not found → 404
5. Unauthorized → 403
```

---

## 🧪 Postman Setup (2 minutes)

### 1. Create Environment
```
base_url = http://localhost:5000/api/v1
token = (empty, fill after login)
admin_token = (empty, fill after admin login)
booking_id = (empty, fill after create booking)
```

### 2. Create Requests
- **Login**: POST {{base_url}}/users/login
- **Search**: GET {{base_url}}/bookings/search
- **Create Booking**: POST {{base_url}}/bookings
- **Admin Bookings**: GET {{base_url}}/admin/bookings

### 3. Add Auth Header (for protected endpoints)
```
Authorization: Bearer {{token}}
```

### 4. Use Pre-request Script (auto-save token)
```javascript
var jsonData = pm.response.json();
if (jsonData.data && jsonData.data.token) {
    pm.environment.set("token", jsonData.data.token);
}
```

---

## ✅ Test Results Checklist

| Test | Endpoint | Method | Status | Notes |
|------|----------|--------|--------|-------|
| Login | /users/login | POST | ✅ | Save token |
| Search Flights | /bookings/search | GET | ✅ | Public endpoint |
| Create Booking | /bookings | POST | ✅ | Needs auth + token |
| Get Bookings | /bookings/my-bookings | GET | ✅ | Needs auth |
| Admin Bookings | /admin/bookings | GET | ✅ | Admin only |
| Get Tickets | /tickets/{id} | GET | ✅ | Needs auth |
| Invalid Token | /bookings | GET | ❌ | Should be 401 |
| Missing Token | /bookings | GET | ❌ | Should be 401 |

---

## 🔑 Important Tokens to Save

```
User1 Token: ____________________
Admin Token: ____________________
Booking ID: ____________________
Booking Code: ____________________
```

---

## 📊 Response Status Codes

```
200 OK              → Success
201 Created         → Resource created
400 Bad Request     → Invalid input
401 Unauthorized    → Need valid token
403 Forbidden       → Not allowed
404 Not Found       → Resource doesn't exist
500 Server Error    → Something broke
```

---

## 🎯 Most Important Tests

### Must Pass
1. ✅ Login works
2. ✅ Token works on protected endpoint
3. ✅ Search flights returns data
4. ✅ Create booking works
5. ✅ Admin can view bookings
6. ✅ Invalid token rejected (401)
7. ✅ Missing token rejected (401)

### Optional (Advanced)
- Admin refund approval
- Payment processing
- Ticket generation
- Dynamic pricing

---

## 🚨 Common Issues & Fixes

### 401 Unauthorized on protected endpoint
**Cause**: Missing or invalid token
**Fix**: 
1. Login first
2. Copy token exactly (no extra spaces)
3. Use: `Authorization: Bearer <TOKEN>`

### 404 Not Found
**Cause**: Wrong endpoint or wrong ID
**Fix**: Check endpoint spelling and IDs match

### 400 Bad Request
**Cause**: Missing required fields or invalid format
**Fix**: Check request body format in guide

### No sample data
**Cause**: Database didn't seed
**Fix**: Check logs, restart app

---

## 💡 Pro Tips

1. **Use Postman Collection**: Organize all requests in folders
2. **Use Environment Variables**: Less typing, easier to change
3. **Save Tokens**: Copy token to environment for reuse
4. **Use Pre-scripts**: Auto-save responses to variables
5. **Test Error Cases**: Don't just test happy path
6. **Check Status Codes**: Not just response content

---

## 📱 cURL Quick Commands

### Login
```bash
curl -X POST http://localhost:5000/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user1@gmail.com","password":"Test@1234"}'
```

### Search Flights
```bash
curl -X GET "http://localhost:5000/api/v1/bookings/search?departureAirportId=1&arrivalAirportId=2&passengerCount=1"
```

### Get Bookings (with token)
```bash
curl -X GET http://localhost:5000/api/v1/bookings \
  -H "Authorization: Bearer <YOUR_TOKEN>"
```

---

## 🎊 Expected Sample Data

```
Users:
- user1@gmail.com / Test@1234
- user2@gmail.com / Test@1234
- admin@flightbooking.vn / Test@1234

Flights:
- VN001: SGN → HAN (08:00)
- VN002: HAN → SGN (14:00)
- VN003: SGN → DAD (09:00)

Promotions:
- SUMMER2024: 10% off
- EARLYBIRD100K: 100,000 VND off
- NEWUSER20: 20% off
```

---

## 📚 Full Documentation

- **Complete Plan**: `API_TESTING_PLAN.md`
- **Postman Guide**: `POSTMAN_TESTING_GUIDE.md`
- **Sample Data**: `SAMPLE_DATA_TESTING_GUIDE.md`
- **Quick Start**: `TESTING_QUICKSTART.md`

---

**Ready to test?** Start with Step 1 above! 🚀
