const API_BASE_URL = 'http://localhost:5042/api/v1'

let authToken = null

export const setAuthToken = (token) => {
  authToken = token
  console.log('💾 Lưu token:', token ? token.substring(0, 20) + '...' : 'null')
  try {
    if (token) localStorage.setItem('authToken', token)
    else localStorage.removeItem('authToken')
  } catch (e) {
    console.error('Lỗi khi lưu token vào localStorage:', e)
  }
}

export const clearAuthToken = () => {
  authToken = null
  try {
    localStorage.removeItem('authToken')
  } catch (e) {}
}

const toLocalDateTimeString = (dateValue) => {
  if (!dateValue) return null
  // Tạo datetime với timezone local
  const date = new Date(dateValue + 'T00:00:00')
  return date.toISOString()
}

const makeRequest = async (endpoint, options = {}) => {
  const url = `${API_BASE_URL}${endpoint}`
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  if (authToken) {
    headers.Authorization = `Bearer ${authToken}`
    console.log('🔑 Token được gửi:', authToken.substring(0, 20) + '...')
  } else {
    console.warn('⚠️ Không có token! Cần đăng nhập.')
  }

  console.log('📤 Request:', { url, method: options.method || 'GET', hasToken: !!authToken })

  const response = await fetch(url, {
    ...options,
    headers,
    // Bỏ credentials: 'include' vì backend đang dùng wildcard CORS
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    try {
      console.error('❌ API request failed', { url, status: response.status, body: error })
    } catch (e) {}
    // auto-clear token on unauthorized to force re-login
    if (response.status === 401) {
      try {
        clearAuthToken()
      } catch (e) {}
    }
    const apiError = new Error(error.detail || `API Error: ${response.statusText}`)
    apiError.status = response.status
    apiError.responseBody = error
    throw apiError
  }

  return response.json()
}

export const searchFlights = (searchParams) => {
  const requestBody = {
    departureAirportId: searchParams.departureAirportId,
    arrivalAirportId: searchParams.arrivalAirportId,
    departureDate: toLocalDateTimeString(searchParams.departureDate),
    returnDate: searchParams.returnDate
      ? toLocalDateTimeString(searchParams.returnDate)
      : null,
    passengerCount: parseInt(searchParams.passengerCount, 10),
    seatPreference: searchParams.seatPreference || 1,
    flightNumber: null,
  }
  
  console.log('🔍 Search Flights Request:', requestBody)
  
  return makeRequest('/Flights/search', {
    method: 'POST',
    body: JSON.stringify(requestBody),
  })
}

export const createBooking = (bookingData) => {
  return makeRequest('/Bookings', {
    method: 'POST',
    body: JSON.stringify({
      outboundFlightId: bookingData.outboundFlightId,
      outboundFlightNumber: bookingData.outboundFlightNumber || null,
      outboundDepartureDate: bookingData.outboundDepartureDate || null,
      returnFlightId: bookingData.returnFlightId || null,
      returnFlightNumber: bookingData.returnFlightNumber || null,
      returnDepartureDate: bookingData.returnDepartureDate || null,
      passengerCount: bookingData.passengerCount,
      seatClassId: bookingData.seatClassId,
      passengers: bookingData.passengers,
      promotionId: bookingData.promotionId || null,
      contactEmail: bookingData.contactEmail,
    }),
  })
}

export const initiatePayment = (bookingId, paymentMethod = 'VNPAY') => {
  return makeRequest('/Payments', {
    method: 'POST',
    body: JSON.stringify({
      bookingId,
      paymentMethod,
      promoCode: null,
    }),
  })
}

export const getBookings = (page = 1, pageSize = 100) => {
  return makeRequest(`/Bookings?page=${page}&pageSize=${pageSize}`)
}

export const cancelBooking = (bookingId, reason = '') => {
  const body = reason ? { reason } : {}
  return makeRequest(`/Bookings/${bookingId}`, {
    method: 'DELETE',
    body: JSON.stringify(body),
  })
}

export const getPaymentStatus = (paymentId) => {
  return makeRequest(`/Payments/${paymentId}`)
}

export const login = (email, password) => {
  return makeRequest('/Users/login', {
    method: 'POST',
    body: JSON.stringify({
      email,
      password,
    }),
  })
}

export const registerAccount = ({ email, password, fullName, phone }) => {
  return makeRequest('/Users/register', {
    method: 'POST',
    body: JSON.stringify({
      email,
      password,
      fullName,
      phone,
    }),
  })
}

export const getFlightDefinitions = (activeOnly = true) => {
  return makeRequest(`/admin/flight-definitions?activeOnly=${activeOnly}`)
}

export const getFlightTemplates = () => {
  return makeRequest('/admin/flight-templates')
}

export const getFlightScheduleTemplate = (templateId) => {
  return makeRequest(`/admin/flight-templates/${templateId}`)
}

export const createFlightTemplate = (templateData) => {
  console.log('📤 Sending template data to API:', JSON.stringify(templateData, null, 2))
  return makeRequest('/admin/flight-templates', {
    method: 'POST',
    body: JSON.stringify(templateData),
  })
}

export const deleteFlightTemplate = (id) => {
  return makeRequest(`/admin/flight-templates/${id}`, {
    method: 'DELETE',
  })
}

export const generateFlightsFromTemplate = (generateData) => {
  console.log('🚀 Generating flights from template:', generateData)
  console.log('📊 Data types:', {
    templateId: typeof generateData.templateId,
    weekStartDate: typeof generateData.weekStartDate,
    numberOfWeeks: typeof generateData.numberOfWeeks,
  })
  console.log('📤 JSON to send:', JSON.stringify(generateData, null, 2))
  return makeRequest('/admin/flight-templates/generate', {
    method: 'POST',
    body: JSON.stringify(generateData),
  })
}

export const getActivePromotions = () => {
  return makeRequest('/admin/PromotionsAdmin/active')
}

export const getBestPromotion = async (bookingAmount) => {
  try {
    const promotions = await getActivePromotions()
    if (!promotions || promotions.length === 0) return null

    // Lọc các promotion còn hiệu lực và có thể áp dụng
    const validPromotions = promotions.filter(promo => {
      const now = new Date()
      const startDate = promo.startDate ? new Date(promo.startDate) : null
      const endDate = promo.endDate ? new Date(promo.endDate) : null
      
      // Kiểm tra thời gian hiệu lực
      const isActive = (!startDate || startDate <= now) && (!endDate || endDate >= now)
      
      // Kiểm tra điều kiện tối thiểu
      const meetsMinimum = !promo.minPurchaseAmount || bookingAmount >= promo.minPurchaseAmount
      
      return isActive && meetsMinimum && promo.isActive
    })

    if (validPromotions.length === 0) return null

    // Tính toán số tiền giảm cho mỗi promotion và chọn cái tốt nhất
    const promotionsWithDiscount = validPromotions.map(promo => {
      let discountAmount = 0
      
      if (promo.discountType === 'Percentage' || promo.discountType === 0) {
        discountAmount = (bookingAmount * promo.discountValue) / 100
        if (promo.maxDiscountAmount && discountAmount > promo.maxDiscountAmount) {
          discountAmount = promo.maxDiscountAmount
        }
      } else if (promo.discountType === 'FixedAmount' || promo.discountType === 1) {
        discountAmount = promo.discountValue
      }
      
      return {
        ...promo,
        calculatedDiscount: discountAmount
      }
    })

    // Sắp xếp theo số tiền giảm giá từ cao đến thấp
    promotionsWithDiscount.sort((a, b) => b.calculatedDiscount - a.calculatedDiscount)
    
    return promotionsWithDiscount[0]
  } catch (error) {
    console.error('Lỗi khi lấy promotion tốt nhất:', error)
    return null
  }
}
