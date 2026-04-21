#!/bin/bash
# Flight Booking System - Sample cURL Requests for Testing
# Copy-paste các commands dưới đây vào Terminal để test API

BASE_URL="http://localhost:5042"
ADMIN_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwiZW1haWwiOiJhZG1pbkBmbGlnaHRib29raW5nLnZuIiwidW5pcXVlX25hbWUiOiJRdeG6o24gdHLhu4sgdmnDqm4iLCJyb2xlIjoiQWRtaW4iLCJuYmYiOjE3NzY3Nzg5MDIsImV4cCI6MTc3Njg2NTMwMSwiaWF0IjoxNzc2Nzc4OTAyLCJpc3MiOiJmbGlnaHQtYm9va2luZy1hcGkiLCJhdWQiOiJmbGlnaHQtYm9va2luZy1hcHAifQ.LQweCjrcMTouo0h4rfSKu2mqgOl_UeNxvERu0w14QFI"

echo "==========================================="
echo "  FLIGHT BOOKING SYSTEM - cURL TESTS"
echo "==========================================="
echo ""

# ========================================
# 1. AUTHENTICATION
# ========================================
echo "📝 [1] LOGIN - Get Token"
echo "POST /api/v1/Users/login"
curl -X POST "$BASE_URL/api/v1/Users/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@flightbooking.vn",
    "password": "Admin@123456"
  }' \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 2. FLIGHTS SEARCH
# ========================================
echo "✈️  [2] SEARCH FLIGHTS - HCM to Hanoi"
echo "GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22"
curl -X GET "$BASE_URL/api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22" \
  -H "Accept: application/json" \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 3. GET ALL FLIGHTS (Admin)
# ========================================
echo "📊 [3] GET FLIGHTS (Admin) - Page 1"
echo "GET /api/v1/admin/FlightsAdmin?page=1&pageSize=10"
curl -X GET "$BASE_URL/api/v1/admin/FlightsAdmin?page=1&pageSize=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Accept: application/json" \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 4. GET ALL PROMOTIONS (Admin)
# ========================================
echo "🎁 [4] GET PROMOTIONS (Admin)"
echo "GET /api/v1/admin/PromotionsAdmin?page=1&pageSize=10"
curl -X GET "$BASE_URL/api/v1/admin/PromotionsAdmin?page=1&pageSize=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Accept: application/json" \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 5. VALIDATE PROMOTION CODE
# ========================================
echo "✔️  [5] VALIDATE PROMOTION CODE - SUMMER20"
echo "POST /api/v1/promotions/validate"
curl -X POST "$BASE_URL/api/v1/promotions/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SUMMER20"
  }' \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 6. TEST PAGINATION
# ========================================
echo "📄 [6] GET FLIGHTS - Page 2"
echo "GET /api/v1/admin/FlightsAdmin?page=2&pageSize=10"
curl -X GET "$BASE_URL/api/v1/admin/FlightsAdmin?page=2&pageSize=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Accept: application/json" \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 7. TEST DIFFERENT ROUTE
# ========================================
echo "✈️  [7] SEARCH FLIGHTS - HCM to Da Nang"
echo "GET /api/v1/flights/search?departureId=1&arrivalId=3&date=2025-01-22"
curl -X GET "$BASE_URL/api/v1/flights/search?departureId=1&arrivalId=3&date=2025-01-22" \
  -H "Accept: application/json" \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 8. TEST INVALID PROMO CODE
# ========================================
echo "❌ [8] VALIDATE INVALID CODE"
echo "POST /api/v1/promotions/validate"
curl -X POST "$BASE_URL/api/v1/promotions/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "INVALIDCODE999"
  }' \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 9. TEST EXPIRED PROMO
# ========================================
echo "⏰ [9] VALIDATE EXPIRED CODE"
echo "POST /api/v1/promotions/validate"
curl -X POST "$BASE_URL/api/v1/promotions/validate" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "EXPIRED2024"
  }' \
  -s | jq '.'

echo ""
echo ""

# ========================================
# 10. GET FLIGHT WITH SEATS
# ========================================
echo "🪑 [10] GET FLIGHTS WITH SEAT INVENTORY"
echo "GET /api/v1/admin/FlightsAdmin?page=1&pageSize=5"
curl -X GET "$BASE_URL/api/v1/admin/FlightsAdmin?page=1&pageSize=5" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Accept: application/json" \
  -s | jq '.data[0] | {flightNumber, totalSeats, availableSeats, bookedSeats}'

echo ""
echo ""
echo "==========================================="
echo "✅ Tests completed!"
echo "==========================================="
