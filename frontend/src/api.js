const API_ROOT_URL = 'http://localhost:5042'
const API_BASE_URL = `${API_ROOT_URL}/api/v1`

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
  } catch {
    // ignore storage cleanup errors
  }
}

const toLocalDateTimeString = (dateValue) => {
  if (!dateValue) return null
  // Tạo datetime với timezone local
  const date = new Date(dateValue + 'T00:00:00')
  return date.toISOString()
}

const makeRequestWithBase = async (baseUrl, endpoint, options = {}) => {
  const url = `${baseUrl}${endpoint}`
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
    } catch {
      // ignore console serialization errors
    }
    // auto-clear token on unauthorized to force re-login
    if (response.status === 401) {
      try {
        clearAuthToken()
      } catch {
        // ignore token cleanup errors
      }
    }
    const apiError = new Error(error.detail || error.message || `API Error: ${response.statusText}`)
    apiError.status = response.status
    apiError.responseBody = error
    throw apiError
  }

  const data = await response.json()
  
  // Kiểm tra nếu response có error (backend trả 200 nhưng có ValidationException)
  if (data.error || data.message) {
    console.warn('⚠️ API returned error in response body:', data)
    const apiError = new Error(data.error || data.message)
    apiError.status = response.status
    apiError.responseBody = data
    throw apiError
  }
  
  return data
}

const makeRequest = async (endpoint, options = {}) =>
  makeRequestWithBase(API_BASE_URL, endpoint, options)

export const getServices = () => {
  console.log('🔍 Fetching services...')
  return makeRequest('/additional-services').then((data) => {
    if (Array.isArray(data)) return data
    if (Array.isArray(data?.items)) return data.items
    if (Array.isArray(data?.data)) return data.data
    if (Array.isArray(data?.$values)) return data.$values
    return []
  })
}

export const getSeatClassServices = (seatClassId) =>
  makeRequest(`/additional-services/by-seat-class/${seatClassId}`).then((data) => {
    const mergeServiceLists = (optionalList, includedList) => {
      const optionalArr = Array.isArray(optionalList) ? optionalList : []
      const includedArr = Array.isArray(includedList) ? includedList : []
      const merged = [...optionalArr, ...includedArr]
      const seen = new Set()
      return merged.filter((item) => {
        const id = item?.id ?? item?.serviceId
        const key = id ?? JSON.stringify(item)
        if (seen.has(key)) return false
        seen.add(key)
        return true
      })
    }

    if (Array.isArray(data)) return data
    if (Array.isArray(data?.items)) return data.items
    if (Array.isArray(data?.data)) return data.data
    if (Array.isArray(data?.$values)) return data.$values
    if (Array.isArray(data?.optionalServices) || Array.isArray(data?.includedServices)) {
      return mergeServiceLists(data?.optionalServices, data?.includedServices)
    }
    if (data?.data && (Array.isArray(data.data?.optionalServices) || Array.isArray(data.data?.includedServices))) {
      return mergeServiceLists(data.data?.optionalServices, data.data?.includedServices)
    }
    return []
  })

export const addServiceToBooking = (bookingId, passengerId, additionalServiceId, quantity = 1) => {
  console.log('➕ Adding service to booking passenger:', {
    bookingId,
    passengerId,
    additionalServiceId,
    quantity,
  })
  return makeRequest(`/bookings/${bookingId}/passengers/${passengerId}/services`, {
    method: 'POST',
    body: JSON.stringify({
      additionalServiceId,
      quantity,
    }),
  })
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

export const getTicketsByBooking = (bookingId) =>
  makeRequest(`/Tickets/booking/${bookingId}`).then((data) => normalizeArrayResponse(data))

export const cancelTicket = (bookingId, ticketId, reason = '') =>
  makeRequest(`/Bookings/${bookingId}/tickets/${ticketId}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason: reason || null }),
  })

export const getDisruptionOptions = (bookingId, departureDate) => {
  const query = departureDate ? `?departureDate=${encodeURIComponent(departureDate)}` : ''
  return makeRequest(`/Bookings/${bookingId}/disruption-options${query}`).then((data) =>
    normalizeArrayResponse(data),
  )
}

export const cancelDisruptionDecision = (bookingId, decisionId) =>
  makeRequest(`/Bookings/${bookingId}/disruption-decisions/cancel?decisionId=${decisionId}`, {
    method: 'POST',
  })

export const rebookDisruptionDecision = (bookingId, decisionId, newFlightId) =>
  makeRequest(`/Bookings/${bookingId}/disruption-decisions/rebook`, {
    method: 'POST',
    body: JSON.stringify({ decisionId, newFlightId }),
  })

const normalizeArrayResponse = (data) => {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.data)) return data.data
  if (Array.isArray(data?.$values)) return data.$values
  return []
}

export const getSavedPassengers = () =>
  makeRequest('/saved-passengers').then((data) => normalizeArrayResponse(data))

export const createSavedPassenger = (payload) =>
  makeRequest('/saved-passengers', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const updateSavedPassenger = (id, payload) =>
  makeRequest(`/saved-passengers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

export const deleteSavedPassenger = (id) =>
  makeRequest(`/saved-passengers/${id}`, {
    method: 'DELETE',
  })

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

export const getAircrafts = () => {
  console.log('🔍 Fetching aircrafts from /admin/aircraft...')
  return makeRequest('/admin/aircraft?page=1&pageSize=100&includeDeleted=false')
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
  return makeRequest('/Promotions/available')
}

export const getAdminFlights = (page = 1, pageSize = 50) =>
  makeRequest(`/admin/FlightsAdmin?page=${page}&pageSize=${pageSize}`)

export const createAdminFlight = (payload) =>
  makeRequest('/admin/FlightsAdmin', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const updateAdminFlight = (flightId, payload) =>
  makeRequest(`/admin/FlightsAdmin/${flightId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

export const deleteAdminFlight = (flightId) =>
  makeRequest(`/admin/FlightsAdmin/${flightId}`, {
    method: 'DELETE',
  })

export const cancelAdminFlight = (flightId, reason) =>
  makeRequest(`/admin/FlightsAdmin/${flightId}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })

export const getAdminRoutes = (page = 1, pageSize = 100) =>
  makeRequest(`/admin/FlightsAdmin/routes?page=${page}&pageSize=${pageSize}`)

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

export const getPromotions = (page = 1, pageSize = 50) =>
  makeRequest(`/admin/PromotionsAdmin?page=${page}&pageSize=${pageSize}`)

export const getAdminPromotions = getPromotions

export const getActiveAdminPromotions = () =>
  makeRequest('/admin/PromotionsAdmin/active')

export const createPromotion = (payload) =>
  makeRequest('/admin/PromotionsAdmin', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const getNotifications = ({ unreadOnly = false, page = 1, pageSize = 20 } = {}) =>
  makeRequest(`/Notifications?unreadOnly=${unreadOnly}&page=${page}&pageSize=${pageSize}`)

export const getNotificationsUnreadCount = () =>
  makeRequest('/Notifications/unread-count')

export const markNotificationRead = (notificationId) =>
  makeRequest(`/Notifications/${notificationId}/read`, {
    method: 'PUT',
  })

export const markAllNotificationsRead = () =>
  makeRequest('/Notifications/read-all', {
    method: 'PUT',
  })

export const updatePromotion = (promotionId, payload) =>
  makeRequest(`/admin/PromotionsAdmin/${promotionId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

export const deactivatePromotion = (promotionId) =>
  makeRequest(`/admin/PromotionsAdmin/${promotionId}`, {
    method: 'DELETE',
  })

export const deletePromotion = deactivatePromotion

// ===== Ticket Upgrade APIs =====

/**
 * Lấy báo giá nâng hạng ghế
 * POST /api/v1/bookings/{bookingId}/tickets/{ticketId}/upgrade/quote
 */
export const getUpgradeQuote = (bookingId, ticketId, toSeatClassId) =>
  makeRequest(`/bookings/${bookingId}/tickets/${ticketId}/upgrade/quote`, {
    method: 'POST',
    body: JSON.stringify({ toSeatClassId }),
  })

/**
 * Tạo yêu cầu nâng hạng ghế
 * POST /api/v1/bookings/{bookingId}/tickets/{ticketId}/upgrade-requests
 */
export const createUpgradeRequest = (bookingId, ticketId, toSeatClassId) =>
  makeRequest(`/bookings/${bookingId}/tickets/${ticketId}/upgrade-requests`, {
    method: 'POST',
    body: JSON.stringify({ toSeatClassId }),
  })

/**
 * Khởi tạo thanh toán cho yêu cầu nâng hạng
 * POST /api/v1/ticket-upgrades/{requestId}/payments
 */
export const initiateUpgradePayment = (requestId, paymentMethod = 'VNPAY') => {
  makeRequest(`/ticket-upgrades/${requestId}/payments`, {
    method: 'POST',
    body: JSON.stringify({ paymentMethod }),
  })
}

// ===== Flight Change APIs =====

/**
 * Lấy danh sách chuyến bay có thể đổi
 * GET /api/v1/bookings/{bookingId}/change-options?legType={0|1}&departureDate={yyyy-MM-dd}
 */
export const getChangeFlightOptions = (bookingId, legType, departureDate) =>
  makeRequest(`/bookings/${bookingId}/change-options?legType=${legType}&departureDate=${departureDate}`).then((data) => {
    const candidatesData = data?.candidates !== undefined ? data.candidates : data
    return normalizeArrayResponse(candidatesData)
  })

/**
 * Lấy báo giá đổi chuyến bay
 * POST /api/v1/bookings/{bookingId}/change-quote
 */
export const getChangeFlightQuote = (bookingId, payload) =>
  makeRequest(`/bookings/${bookingId}/change-quote`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

/**
 * Xác nhận đổi chuyến bay
 * POST /api/v1/bookings/{bookingId}/change-confirm
 */
export const confirmChangeFlight = (bookingId, payload) =>
  makeRequest(`/bookings/${bookingId}/change-confirm`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
