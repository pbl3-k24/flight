const API_ROOT_URL = 'http://localhost:5042'
const API_BASE_URL = `${API_ROOT_URL}/api/v1`

let authToken = null

export const setAuthToken = (token) => {
  authToken = token
  console.log(' Luu token:', token ? token.substring(0, 20) + '...' : 'null')
  try {
    if (token) localStorage.setItem('authToken', token)
    else localStorage.removeItem('authToken')
  } catch (e) {
    console.error('Lỗi khi luu token vï¿½o localStorage:', e)
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
  
  let date
  if (dateValue instanceof Date) {
    date = dateValue
  } else {
    const dateStr = String(dateValue).trim()
    const yyyymmddRegex = /^\d{4}-\d{2}-\d{2}$/
    if (yyyymmddRegex.test(dateStr)) {
      return `${dateStr}T00:00:00.000Z`
    }
    date = new Date(dateStr)
  }

  if (date && !Number.isNaN(date.getTime())) {
    const year = date.getUTCFullYear()
    const month = String(date.getUTCMonth() + 1).padStart(2, '0')
    const day = String(date.getUTCDate()).padStart(2, '0')
    const hours = String(date.getUTCHours()).padStart(2, '0')
    const minutes = String(date.getUTCMinutes()).padStart(2, '0')
    const seconds = String(date.getUTCSeconds()).padStart(2, '0')
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.000Z`
  }

  return null
}

const makeRequestWithBase = async (baseUrl, endpoint, options = {}) => {
  const url = `${baseUrl}${endpoint}`
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  const isAnonymousEndpoint =
    endpoint.includes('/Users/login') ||
    endpoint.includes('/Users/register') ||
    endpoint.includes('/Users/forgot-password') ||
    endpoint.includes('/Users/reset-password')

  if (authToken && !isAnonymousEndpoint) {
    headers.Authorization = `Bearer ${authToken}`
    console.log(' Token được g?i:', authToken.substring(0, 20) + '...')
  } else if (!authToken) {
    console.warn(' Khï¿½ng cï¿½ token! C?n đăng nhập.')
  }

  console.log(' Request:', { url, method: options.method || 'GET', hasToken: !!authToken })

  const response = await fetch(url, {
    ...options,
    headers,
    // B? credentials: 'include' vï¿½ backend dang dï¿½ng wildcard CORS
  })

  const parseResponseJsonSafely = async () => {
    const contentLength = response.headers.get('content-length')
    const contentType = response.headers.get('content-type') || ''
    if (response.status === 204 || contentLength === '0') return null
    if (!contentType.toLowerCase().includes('application/json')) return null
    return response.json().catch(() => null)
  }

  if (!response.ok) {
    const error = (await parseResponseJsonSafely()) || {}
    try {
      console.error('? API request failed', { url, status: response.status, body: error })
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

  const data = await parseResponseJsonSafely()
  if (data == null) return null
  
  // Ki?m tra n?u response cï¿½ error (backend tr? 200 nhung cï¿½ ValidationException)
  if (data.error || data.message) {
    console.warn(' API returned error in response body:', data)
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
  console.log(' Fetching services...')
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
  console.log('? Adding service to booking passenger:', {
    bookingId,
    passengerId,
    additionalServiceId,
    quantity,
  })
  return makeRequest(`/bookings/${bookingId}/passengers/${passengerId}/services`, {
    method: 'POST',
    body: JSON.stringify({
      additionalServiceId: parseInt(additionalServiceId, 10),
      quantity: parseInt(quantity, 10),
    }),
  })
}

export const searchFlights = (searchParams) => {
  const requestBody = {
    departureAirportId: parseInt(searchParams.departureAirportId, 10),
    arrivalAirportId: parseInt(searchParams.arrivalAirportId, 10),
    departureDate: toLocalDateTimeString(searchParams.departureDate),
    returnDate: searchParams.returnDate
      ? toLocalDateTimeString(searchParams.returnDate)
      : null,
    passengerCount: parseInt(searchParams.passengerCount, 10),
    seatPreference: searchParams.seatPreference ? parseInt(searchParams.seatPreference, 10) : null,
    flightNumber: null,
  }
  
  console.log(' Search Flights Request:', requestBody)
  
  return makeRequest('/Flights/search', {
    method: 'POST',
    body: JSON.stringify(requestBody),
  })
}

export const createBooking = (bookingData) => {
  return makeRequest('/Bookings', {
    method: 'POST',
    body: JSON.stringify({
      outboundFlightId: parseInt(bookingData.outboundFlightId, 10),
      outboundFlightNumber: bookingData.outboundFlightNumber || null,
      outboundDepartureDate: bookingData.outboundDepartureDate || null,
      returnFlightId: bookingData.returnFlightId ? parseInt(bookingData.returnFlightId, 10) : null,
      returnFlightNumber: bookingData.returnFlightNumber || null,
      returnDepartureDate: bookingData.returnDepartureDate || null,
      passengerCount: parseInt(bookingData.passengerCount, 10),
      seatClassId: parseInt(bookingData.seatClassId, 10),
      passengers: bookingData.passengers,
      promotionId: bookingData.promotionId ? parseInt(bookingData.promotionId, 10) : null,
      contactEmail: bookingData.contactEmail,
    }),
  })
}

export const initiatePayment = (bookingId, paymentMethod = 'VNPAY') => {
  return makeRequest('/Payments', {
    method: 'POST',
    body: JSON.stringify({
      bookingId: parseInt(bookingId, 10),
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
  if (Array.isArray(data?.data?.items)) return data.data.items
  if (Array.isArray(data?.data?.$values)) return data.data.$values
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

export const getPaymentsByBooking = (bookingId) =>
  makeRequest(`/Payments/booking/${bookingId}`).then((data) => normalizeArrayResponse(data))

export const login = (email, password) =>
  makeRequest('/Users/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })

export const registerAccount = ({ email, password, fullName, phone }) =>
  makeRequest('/Users/register', {
    method: 'POST',
    body: JSON.stringify({ email, password, fullName, phone }),
  })

/**
 * Yï¿½u c?u d?t l?i mật khẩu - g?i email cï¿½ ch?a OTP/link reset
 * POST /api/v1/Users/forgot-password
 */
export const forgotPassword = (email) =>
  makeRequest('/Users/forgot-password', {
    method: 'POST',
    body: JSON.stringify({ email }),
  })

/**
 * D?t l?i mật khẩu b?ng code dï¿½ nh?n qua email
 * POST /api/v1/Users/reset-password
 */
export const resetPassword = (code, newPassword) =>
  makeRequest('/Users/reset-password', {
    method: 'POST',
    body: JSON.stringify({ code, newPassword }),
  })

export const getAircrafts = () => {
  console.log(' Fetching aircrafts from /admin/aircraft...')
  return makeRequest('/admin/aircraft?page=1&pageSize=100&includeDeleted=false')
}

export const getFlightDefinitions = (activeOnly = true) =>
  makeRequest('/admin/FlightsAdmin?page=1&pageSize=20').then((data) => {
    const flights = normalizeArrayResponse(data)
    return flights
      .filter((flight) => (activeOnly ? flight?.isActive !== false : true))
      .map((flight) => ({
        id: flight?.id ?? flight?.flightId,
        routeId: flight?.routeId ?? flight?.route?.id ?? null,
        flightNumber: flight?.flightNumber ?? flight?.code ?? `FL-${flight?.id ?? flight?.flightId ?? 'N/A'}`,
        departureAirportCode:
          flight?.departureAirportCode
          ?? flight?.departureAirport?.code
          ?? flight?.route?.departureAirportCode
          ?? flight?.route?.departureAirport?.code
          ?? '--',
        arrivalAirportCode:
          flight?.arrivalAirportCode
          ?? flight?.arrivalAirport?.code
          ?? flight?.route?.arrivalAirportCode
          ?? flight?.route?.arrivalAirport?.code
          ?? '--',
        departureTime:
          flight?.departureTime
          ?? flight?.scheduledDepartureTime
          ?? flight?.departureDateTime
          ?? '--:--',
        arrivalTime:
          flight?.arrivalTime
          ?? flight?.scheduledArrivalTime
          ?? flight?.arrivalDateTime
          ?? '--:--',
      }))
      .filter((flight) => flight.id !== undefined && flight.id !== null)
  })

export const getFlightTemplates = () =>
  makeRequest('/admin/flight-templates')

export const getFlightScheduleTemplate = (templateId) =>
  makeRequest(`/admin/flight-templates/${templateId}`)

const normalizeTemplateTime = (value) => {
  if (!value) return null
  if (typeof value !== 'string') return value
  if (value.length >= 8) return value.slice(0, 8)
  if (value.length === 5) return `${value}:00`
  return value
}

const toNullableNumber = (value) => {
  if (value === '' || value === null || value === undefined) return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

const generateCodeFromName = (nameValue) => {
  const raw = String(nameValue ?? '').trim().toUpperCase()
  if (!raw) return `TPL${Date.now()}`
  const compact = raw.replace(/[^A-Z0-9]/g, '')
  if (compact) return compact.slice(0, 20)
  return `TPL${Date.now()}`
}

const normalizeTemplateCreatePayload = (templateData = {}) => ({
  Code: (() => {
    const code = String(templateData.Code ?? templateData.code ?? '').trim().toUpperCase()
    return code || generateCodeFromName(templateData.Name ?? templateData.name)
  })(),
  Name: String(templateData.Name ?? templateData.name ?? '').trim().toUpperCase(),
  Description: templateData.Description ?? templateData.description ?? null,
  EffectiveFrom: templateData.EffectiveFrom ?? templateData.effectiveFrom ?? null,
  EffectiveTo: templateData.EffectiveTo ?? templateData.effectiveTo ?? null,
  IsActive: Boolean(templateData.IsActive ?? templateData.isActive ?? true),
  Details: Array.isArray(templateData.Details ?? templateData.details)
    ? (templateData.Details ?? templateData.details).map((detail) => ({
        FlightDefinitionId: Number(detail.FlightDefinitionId ?? detail.flightDefinitionId ?? 0),
        DayOfWeek: Number(detail.DayOfWeek ?? detail.dayOfWeek ?? 0),
        AircraftOverrideId: toNullableNumber(detail.AircraftOverrideId ?? detail.aircraftOverrideId),
        DepartureTimeOverride: normalizeTemplateTime(detail.DepartureTimeOverride ?? detail.departureTimeOverride ?? detail.departureTime),
        ArrivalTimeOverride: normalizeTemplateTime(detail.ArrivalTimeOverride ?? detail.arrivalTimeOverride ?? detail.arrivalTime),
        ArrivalOffsetDaysOverride: toNullableNumber(detail.ArrivalOffsetDaysOverride ?? detail.arrivalOffsetDaysOverride),
        IsActive: Boolean(detail.IsActive ?? detail.isActive ?? true),
      }))
    : [],
})

export const createFlightTemplate = (templateData) => {
  const payload = normalizeTemplateCreatePayload(templateData)
  console.log(' Sending template data to API:', JSON.stringify(payload, null, 2))
  return makeRequest('/admin/flight-templates', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export const deleteFlightTemplate = (id) => {
  return makeRequest(`/admin/flight-templates/${id}`, {
    method: 'DELETE',
  })
}

export const generateFlightsFromTemplate = (generateData) => {
  console.log(' Generating flights from template:', generateData)
  console.log(' Data types:', {
    templateId: typeof generateData.templateId,
    weekStartDate: typeof generateData.weekStartDate,
    numberOfWeeks: typeof generateData.numberOfWeeks,
  })
  console.log(' JSON to send:', JSON.stringify(generateData, null, 2))
  return makeRequest('/admin/flight-templates/generate', {
    method: 'POST',
    body: JSON.stringify(generateData),
  })
}

export const getActivePromotions = () => {
  return makeRequest('/Promotions/available')
}

export const getAdminFlights = (date = '', page = 1, pageSize = 50) => {
  const normalizedDate = String(date || '').trim()
  if (normalizedDate) {
    return makeRequest(`/admin/FlightsAdmin/by-date?date=${encodeURIComponent(normalizedDate)}`)
  }
  return makeRequest(`/admin/FlightsAdmin?page=${page}&pageSize=${pageSize}`)
}

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

    // L?c cï¿½c promotion cï¿½n hi?u l?c vï¿½ cï¿½ th? ï¿½p d?ng
    const validPromotions = promotions.filter(promo => {
      const now = new Date()
      const startDate = promo.startDate ? new Date(promo.startDate) : null
      const endDate = promo.endDate ? new Date(promo.endDate) : null
      
      // Ki?m tra th?i gian hi?u l?c
      const isActive = (!startDate || startDate <= now) && (!endDate || endDate >= now)
      
      // Ki?m tra di?u ki?n t?i thi?u
      const meetsMinimum = !promo.minPurchaseAmount || bookingAmount >= promo.minPurchaseAmount
      
      return isActive && meetsMinimum && promo.isActive
    })

    if (validPromotions.length === 0) return null

    // Tï¿½nh toï¿½n s? ti?n gi?m cho m?i promotion vï¿½ ch?n cï¿½i t?t nh?t
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

    // S?p x?p theo s? ti?n gi?m giï¿½ t? cao d?n th?p
    promotionsWithDiscount.sort((a, b) => b.calculatedDiscount - a.calculatedDiscount)
    
    return promotionsWithDiscount[0]
  } catch (error) {
    console.error('Lỗi khi l?y promotion t?t nh?t:', error)
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
 * L?y bï¿½o giï¿½ nï¿½ng hạng ghế
 * POST /api/v1/bookings/{bookingId}/tickets/{ticketId}/upgrade/quote
 */
export const getUpgradeQuote = (bookingId, ticketId, toSeatClassId) =>
  makeRequest(`/bookings/${bookingId}/tickets/${ticketId}/upgrade/quote`, {
    method: 'POST',
    body: JSON.stringify({ toSeatClassId }),
  })

/**
 * T?o yï¿½u c?u nï¿½ng hạng ghế
 * POST /api/v1/bookings/{bookingId}/tickets/{ticketId}/upgrade-requests
 */
export const createUpgradeRequest = (bookingId, ticketId, toSeatClassId) =>
  makeRequest(`/bookings/${bookingId}/tickets/${ticketId}/upgrade-requests`, {
    method: 'POST',
    body: JSON.stringify({ toSeatClassId }),
  })

/**
 * Kh?i t?o thanh toï¿½n cho yï¿½u c?u nï¿½ng h?ng
 * POST /api/v1/ticket-upgrades/{requestId}/payments
 */
export const initiateUpgradePayment = (requestId, paymentMethod = 'VNPAY') => {
  return makeRequest(`/ticket-upgrades/${requestId}/payments`, {
    method: 'POST',
    body: JSON.stringify({ paymentMethod }),
  })
}

// ===== Flight Change APIs =====

/**
 * L?y danh sï¿½ch chuyến bay cï¿½ th? d?i
 * GET /api/v1/bookings/{bookingId}/change-options?legType={0|1}&departureDate={yyyy-MM-dd}
 */
export const getChangeFlightOptions = (bookingId, legType, departureDate) =>
  makeRequest(`/bookings/${bookingId}/change-options?legType=${legType}&departureDate=${departureDate}`).then((data) => {
    const candidatesData = data?.candidates !== undefined ? data.candidates : data
    return normalizeArrayResponse(candidatesData)
  })

/**
 * L?y bï¿½o giï¿½ d?i chuyến bay
 * POST /api/v1/bookings/{bookingId}/change-quote
 */
export const getChangeFlightQuote = (bookingId, payload) =>
  makeRequest(`/bookings/${bookingId}/change-quote`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

/**
 * Xï¿½c nh?n d?i chuyến bay
 * POST /api/v1/bookings/{bookingId}/change-confirm
 */
export const confirmChangeFlight = (bookingId, payload) =>
  makeRequest(`/bookings/${bookingId}/change-confirm`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
