import { useEffect, useMemo, useState } from 'react'
import {
  searchFlights,
  createBooking,
  initiatePayment,
  login,
  registerAccount,
  clearAuthToken,
  setAuthToken,
  getBookings,
  cancelBooking,
  getServices,
  getSeatClassServices,
  addServiceToBooking,
  getFlightDefinitions,
  getAircrafts,
  getFlightTemplates,
  getFlightScheduleTemplate,
  createFlightTemplate,
  deleteFlightTemplate,
  generateFlightsFromTemplate,
  getActivePromotions,
  getSavedPassengers,
  createSavedPassenger,
  updateSavedPassenger,
  deleteSavedPassenger,
  getAdminFlights,
  createAdminFlight,
  updateAdminFlight,
  deleteAdminFlight,
  cancelAdminFlight,
  getAdminRoutes,
  getAdminPromotions,
  createPromotion,
  updatePromotion,
  deletePromotion,
  getTicketsByBooking,
  cancelTicket,
  getDisruptionOptions,
  cancelDisruptionDecision,
  rebookDisruptionDecision,
  getNotifications,
  getNotificationsUnreadCount,
  markNotificationRead,
  markAllNotificationsRead,
  getUpgradeQuote,
  createUpgradeRequest,
  initiateUpgradePayment,
  getChangeFlightOptions,
  getChangeFlightQuote,
  confirmChangeFlight,
  forgotPassword,
  resetPassword,
} from './api'

const airports = [
  { Id: 1, Code: 'SGN', Name: 'Sân bay Tân Sơn Nhất', City: 'Thành phố Hồ Chí Minh', Province: 'Hồ Chí Minh', IsActive: true, IsDeleted: false },
  { Id: 2, Code: 'HAN', Name: 'Sân bay Nội Bài', City: 'Hà Nội', Province: 'Hà Nội', IsActive: true, IsDeleted: false },
  { Id: 3, Code: 'DAD', Name: 'Sân bay Quốc tế Đà Nẵng', City: 'Đà Nẵng', Province: 'Đà Nẵng', IsActive: true, IsDeleted: false },
  { Id: 4, Code: 'CTS', Name: 'Sân bay Cần Thơ', City: 'Cần Thơ', Province: 'Cần Thơ', IsActive: true, IsDeleted: false },
  { Id: 5, Code: 'VCA', Name: 'Sân bay Buôn Mê Thuột', City: 'Buôn Mê Thuột', Province: 'Đắk Lắk', IsActive: true, IsDeleted: false },
  { Id: 6, Code: 'HUI', Name: 'Sân bay Phú Bài', City: 'Huế', Province: 'Thừa Thiên Huế', IsActive: true, IsDeleted: false },
]

const seatClassMap = {
  Economy: 1,
  Business: 2,
}

const formatCurrency = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatTime = (value) => {
  if (!value) return '--:--'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '--:--'
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}

const formatDuration = (durationMinutes = 0) => {
  const durationHours = Math.floor(durationMinutes / 60)
  const durationMins = durationMinutes % 60
  return `${durationHours}h${durationMins}m`
}

const formatDateTime = (value) => {
  if (!value) return ''
  const date = new Date(value)
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

const toDateMs = (value) => {
  if (!value) return 0
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 0
  return date.getTime()
}

const sortNotificationsByTime = (items = []) =>
  [...items].sort((a, b) => toDateMs(b?.createdAt || b?.sentAt) - toDateMs(a?.createdAt || a?.sentAt))

const formatRouteLabel = (item) => {
  const from = item?.fromAirport || '---'
  const to = item?.toAirport || '---'
  const fromCode = item?.fromCode ? ` (${item.fromCode})` : ''
  const toCode = item?.toCode ? ` (${item.toCode})` : ''
  return `${from}${fromCode} → ${to}${toCode}`
}

const formatFlightMeta = (item) => {
  const parts = [item?.airlineCode, item?.flightNumber, item?.seatClass].filter(Boolean)
  return parts.join(' · ')
}

const getPromotionId = (promotion) => promotion?.promotionId ?? promotion?.id
const getPromotionCode = (promotion) => promotion?.promoCode ?? promotion?.code

const getFlightPrice = (flight, seatClass) =>
  (flight?.pricesByClass?.[seatClass] ?? flight?.price ?? 0)

const calculatePromotionDiscount = (promotion, bookingAmount) => {
  if (!promotion || !Number.isFinite(Number(bookingAmount))) return 0
  const amount = Number(bookingAmount)
  let discountAmount = 0

  if (promotion.discountType === 'Percentage' || promotion.discountType === 0) {
    discountAmount = (amount * Number(promotion.discountValue || 0)) / 100
    if (promotion.maxDiscountAmount && discountAmount > promotion.maxDiscountAmount) {
      discountAmount = promotion.maxDiscountAmount
    }
  } else if (promotion.discountType === 'FixedAmount' || promotion.discountType === 1) {
    discountAmount = Number(promotion.discountValue || 0)
  }

  return Math.max(0, discountAmount)
}

const getPromotionDisplayText = (promotion) => {
  if (!promotion) return ''
  const code = getPromotionCode(promotion)
  if (promotion.discountType === 'Percentage' || promotion.discountType === 0) {
    const percentValue = Number(promotion.discountValue || 0)
    const maxAmount = Number(promotion.maxDiscountAmount || 0)
    const maxLabel = maxAmount > 0 ? ` (tối đa ${formatCurrency(maxAmount)})` : ''
    return `${code} - Giảm ${percentValue}%${maxLabel}`
  }
  return `${code} - Giảm ${formatCurrency(Number(promotion.discountValue || 0))}`
}

const getPromotionTypeLabel = (discountType) =>
  Number(discountType) === 0 ? 'Giảm %' : 'Giảm tiền'

const getSeatInventorySummary = (seatInventory) => {
  if (!seatInventory || typeof seatInventory !== 'object') return []
  return Object.values(seatInventory)
    .filter(Boolean)
    .map((seat) => ({
      label: seat.className || (seat.seatClassId ? `Hạng ${seat.seatClassId}` : 'Hạng vé'),
      price: seat.currentPrice ?? seat.basePrice,
    }))
}

const toDateInputValueFromApi = (value) => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const toApiDateTimeValue = (value) => {
  if (!value) return null
  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) return null
  return date.toISOString()
}

const toDateTimeLocalInputValue = (value) => {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offsetMs = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

const toApiDateTimeFromLocal = (value) => {
  if (!value) return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return null
  return date.toISOString()
}

const TICKET_STATUS_LABELS = {
  0: 'Đã xuất vé',
  1: 'Đã sử dụng',
  2: 'Đã hoàn tiền',
  3: 'Đã hủy',
  4: 'Đã hủy bởi bạn',
  5: 'Đã hủy bởi hãng',
}

const LEGACY_TICKET_STATUS_MAP = {
  issued: 0,
  daxuatve: 0,
  xacnhan: 0,
  confirmed: 0,
  active: 0,
  0: 0,

  used: 1,
  dasudung: 1,
  1: 1,

  refunded: 2,
  dahoantien: 2,
  hoantien: 2,
  2: 2,

  cancelled: 3,
  canceled: 3,
  dahuy: 3,
  huy: 3,
  3: 3,

  cancelledbyuser: 4,
  canceledbyuser: 4,
  dahuyboinguoidung: 4,
  huyboinguoidung: 4,
  4: 4,

  cancelledbyadmin: 5,
  canceledbyadmin: 5,
  dahuyboiquantrivien: 5,
  huyboiquantrivien: 5,
  5: 5,
}

const resolveTicketStatusCode = (status) => {
  if (status === null || status === undefined) return null
  if (typeof status === 'number' && Number.isFinite(status)) return status

  const trimmed = String(status).trim()
  if (!trimmed) return null

  if (/^-?\d+$/.test(trimmed)) {
    const numeric = Number(trimmed)
    return Number.isFinite(numeric) ? numeric : null
  }

  const normalized = normalizeAscii(trimmed)
  if (normalized in LEGACY_TICKET_STATUS_MAP) {
    return LEGACY_TICKET_STATUS_MAP[normalized]
  }

  return null
}

const getTicketStatusLabel = (status) => {
  const code = resolveTicketStatusCode(status)
  if (code !== null && code !== undefined) {
    return TICKET_STATUS_LABELS[code] || 'Không xác định'
  }
  if (status === null || status === undefined || String(status).trim() === '') {
    return TICKET_STATUS_LABELS[0]
  }
  return String(status)
}

const BOOKING_STATUS_LABELS = {
  0: 'Pending',
  1: 'Completed',
  2: 'Failed',
  3: 'Refunded',
  4: 'RefundFailed',
  5: 'PendingRefund',
}

const LEGACY_BOOKING_STATUS_MAP = {
  pending: 0,
  cho: 0,
  'cho thanh toan': 0,
  'chua thanh toan': 0,
  unpaid: 0,
  'da dat': 0,
  completed: 1,
  paid: 1,
  'da thanh toan': 1,
  success: 1,
  failed: 2,
  'that bai': 2,
  loi: 2,
  refunded: 3,
  'da hoan': 3,
  'hoan tien': 3,
  refundfailed: 4,
  'hoan that bai': 4,
  pendingrefund: 5,
  'cho hoan': 5,
  'cho hoan tien': 5,
  'da huy': 5,
  huy: 5,
  cancel: 5,
  cancelled: 5,
  canceled: 5,
}

const normalizeAscii = (value) =>
  String(value || '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')

const resolveBookingStatusCode = (status) => {
  if (status === null || status === undefined) return null
  if (typeof status === 'number' && Number.isFinite(status)) return status

  const trimmed = String(status).trim()
  if (!trimmed) return null

  if (/^-?\d+$/.test(trimmed)) {
    const numeric = Number(trimmed)
    return Number.isFinite(numeric) ? numeric : null
  }

  const normalized = normalizeAscii(trimmed)
  if (normalized in LEGACY_BOOKING_STATUS_MAP) {
    return LEGACY_BOOKING_STATUS_MAP[normalized]
  }

  return null
}

const getBookingStatusLabel = (status) => {
  const code = resolveBookingStatusCode(status)
  if (code !== null && code !== undefined) {
    return BOOKING_STATUS_LABELS[code] || 'Unknown'
  }
  if (status === null || status === undefined || String(status).trim() === '') {
    return BOOKING_STATUS_LABELS[0]
  }
  return String(status)
}

const getBookingStatusClassName = (status) => {
  const code = resolveBookingStatusCode(status)
  if (code === 1) return 'text-emerald-600'
  if (code === 3) return 'text-blue-600'
  if (code === 2 || code === 4) return 'text-red-600'
  if (code === 0 || code === 5) return 'text-yellow-600'
  return 'text-slate-600'
}

const isPendingDisruptionDecision = (status) => {
  const normalized = normalizeAscii(status)
  return normalized.includes('pendingdisruptiondecision') || normalized.includes('disruption')
}

const getDisruptionLegLabel = (legType) => {
  if (legType === 0) return 'Chuyến đi'
  if (legType === 1) return 'Chuyến về'
  if (legType === 2) return 'Chặng nối'
  return 'Chuyến bay'
}

const toLocalDateInputValue = (date = new Date()) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const addDays = (date, days) => new Date(date.getFullYear(), date.getMonth(), date.getDate() + days)

const normalizeId = (value) => (value === null || value === undefined ? '' : String(value))

const getFlightId = (flight) => normalizeId(flight?.flightId ?? flight?.id ?? flight?.flightNumber)

const getFlightLabel = (flight) => flight?.flightNumber || getFlightId(flight) || 'Chuyến bay'

const decodeBase64Url = (value) => {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=')
  return atob(padded)
}

const getRoleFromToken = (token) => {
  try {
    const parts = token.split('.')
    if (parts.length < 2) return null
    const payload = JSON.parse(decodeBase64Url(parts[1]))
    const roleClaims = [
      payload.role,
      payload.roles,
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'],
    ]

    for (const claim of roleClaims) {
      if (Array.isArray(claim) && claim.length > 0) return String(claim[0]).toLowerCase()
      if (typeof claim === 'string' && claim.trim()) return claim.toLowerCase()
    }

    return null
  } catch {
    return null
  }
}

const Label = ({ children }) => (
  <p className="mb-2 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  />
)

const Select = ({ children, ...props }) => (
  <select
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  >
    {children}
  </select>
)

function App() {
  const today = toLocalDateInputValue()
  const tomorrow = toLocalDateInputValue(addDays(new Date(), 1))

  const [screen, setScreen] = useState('login')
  const [tripType, setTripType] = useState('oneway')
  const [searchData, setSearchData] = useState({
    fromAirportId: airports[0].Id,
    toAirportId: airports[1].Id,
    departDate: tomorrow,
    returnDate: toLocalDateInputValue(addDays(new Date(), 2)),
    passengers: '1',
    seatClass: 'Economy',
  })
  const [passengerCounts, setPassengerCounts] = useState({
    adult: 1,
    child: 0,
    infant: 0,
  })
  const [filters, setFilters] = useState({
    maxPrice: 3000000,
    timeSlot: 'all',
    seatClass: 'all',
  })
  const [selectedFlight, setSelectedFlight] = useState(null)
  const [returnFlight, setReturnFlight] = useState(null)
  const [outboundFlights, setOutboundFlights] = useState([])
  const [returnFlights, setReturnFlights] = useState([])
  const [roundtripStep, setRoundtripStep] = useState('outbound')
  const [passengerForms, setPassengerForms] = useState([])
  
  const createEmptyPassenger = (type = 'adult') => ({
    type,
    fullName: '',
    dob: '',
    gender: 'Nam',
    document: '',
    email: '',
    phone: '',
    savedPassengerId: '',
    age: '',
  })

  const getAgeFromDob = (dobValue) => {
    if (!dobValue) return null
    const dobDate = new Date(dobValue)
    if (Number.isNaN(dobDate.getTime())) return null
    const now = new Date()
    let age = now.getFullYear() - dobDate.getFullYear()
    const monthDiff = now.getMonth() - dobDate.getMonth()
    if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < dobDate.getDate())) {
      age -= 1
    }
    return age
  }
  const [bookingReference] = useState(() => `FB${Date.now().toString().slice(-8)}`)
  const [apiFlights, setApiFlights] = useState([])
  const [isLoadingFlights, setIsLoadingFlights] = useState(false)
  const [apiError, setApiError] = useState('')
  const [bookingId, setBookingId] = useState(null)
  const [bookingAmount, setBookingAmount] = useState(null)
  const [paymentData, setPaymentData] = useState(null)
  const [availablePromotions, setAvailablePromotions] = useState([])
  const [selectedPromotionId, setSelectedPromotionId] = useState('')
  const [isLoadingPromotion, setIsLoadingPromotion] = useState(false)
  const [bookingHistory, setBookingHistory] = useState([])
  const [historyNotice, setHistoryNotice] = useState('')
  const [historyError, setHistoryError] = useState('')
  const [isLoadingHistory, setIsLoadingHistory] = useState(false)
  const [isCancellingBookingId, setIsCancellingBookingId] = useState(null)
  const [bookingTicketsMap, setBookingTicketsMap] = useState({})
  const [loadingTicketsMap, setLoadingTicketsMap] = useState({})
  const [ticketErrorMap, setTicketErrorMap] = useState({})
  const [isCancellingTicketId, setIsCancellingTicketId] = useState(null)
  const [disruptionOptionsMap, setDisruptionOptionsMap] = useState({})
  const [disruptionLoadingMap, setDisruptionLoadingMap] = useState({})
  const [disruptionErrorMap, setDisruptionErrorMap] = useState({})
  const [disruptionNoticeMap, setDisruptionNoticeMap] = useState({})
  const [selectedDisruptionDecisionMap, setSelectedDisruptionDecisionMap] = useState({})
  const [selectedDisruptionActionMap, setSelectedDisruptionActionMap] = useState({})
  const [submittingDisruptionMap, setSubmittingDisruptionMap] = useState({})
  const [rebookModalState, setRebookModalState] = useState({
    isOpen: false,
    bookingId: null,
    decisionId: null,
    date: '',
    selectedFlightId: '',
  })
  const emptySavedPassengerForm = {
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    gender: '',
    nationality: '',
    documentNumber: '',
    email: '',
    phone: '',
  }
  const [savedPassengers, setSavedPassengers] = useState([])
  const [savedPassengerForm, setSavedPassengerForm] = useState(emptySavedPassengerForm)
  const [isLoadingSavedPassengers, setIsLoadingSavedPassengers] = useState(false)
  const [savedPassengerError, setSavedPassengerError] = useState('')
  const [savedPassengerNotice, setSavedPassengerNotice] = useState('')
  const [editingSavedPassengerId, setEditingSavedPassengerId] = useState(null)
  const [services, setServices] = useState([])
  const [isLoadingServices, setIsLoadingServices] = useState(false)
  const [serviceError, setServiceError] = useState('')
  const [showServicesModal, setShowServicesModal] = useState(false)
  const [currentBookingForServices, setCurrentBookingForServices] = useState(null)
  const [selectedServicesByPassenger, setSelectedServicesByPassenger] = useState({})
  const [selectedServicesByPassengerDraft, setSelectedServicesByPassengerDraft] = useState({
    outbound: {},
    return: {},
  })
  const [loginData, setLoginData] = useState({
    email: '',
    password: '',
  })
  const [isLoggingIn, setIsLoggingIn] = useState(false)
  const [registerData, setRegisterData] = useState({
    fullName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
  })
  const [isRegistering, setIsRegistering] = useState(false)
  const [registerError, setRegisterError] = useState('')
  const [authUser, setAuthUser] = useState(null)
  const [notifications, setNotifications] = useState([])
  const [isLoadingNotifications, setIsLoadingNotifications] = useState(false)
  const [notificationError, setNotificationError] = useState('')
  const [unreadNotificationCount, setUnreadNotificationCount] = useState(0)
  const [isNotificationPanelOpen, setIsNotificationPanelOpen] = useState(false)
  const [forgotEmail, setForgotEmail] = useState('')
  const [forgotStep, setForgotStep] = useState('email')
  const [resetData, setResetData] = useState({ token: '', password: '' })
  const [forgotError, setForgotError] = useState('')
  const [forgotLoading, setForgotLoading] = useState(false)
  const [forgotSuccess, setForgotSuccess] = useState(false)
  const [resetCode, setResetCode] = useState('')
  const [resetNewPassword, setResetNewPassword] = useState('')
  const [resetConfirmPassword, setResetConfirmPassword] = useState('')
  const [resetSuccess, setResetSuccess] = useState(false)
  
  // Template management states
  const [flightDefinitions, setFlightDefinitions] = useState([])
  const [isLoadingFlightDefinitions, setIsLoadingFlightDefinitions] = useState(false)
  const [aircrafts, setAircrafts] = useState([])
  const [isLoadingAircrafts, setIsLoadingAircrafts] = useState(false)
  const [flightTemplates, setFlightTemplates] = useState([])
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false)
  const [selectedTemplate, setSelectedTemplate] = useState(null)
  const [templateFormData, setTemplateFormData] = useState({
    name: '',
    description: '',
    isActive: true,
  })
  const [templateSlots, setTemplateSlots] = useState([])
  const [generateFormData, setGenerateFormData] = useState({
    templateId: null,
    weekStartDate: toLocalDateInputValue(addDays(new Date(), 1)),
    numberOfWeeks: 1,
  })
  const [adminNotice, setAdminNotice] = useState('')
  const [viewingTemplateDetail, setViewingTemplateDetail] = useState(null)
  const emptyFlightForm = {
    flightNumber: '',
    routeId: '',
    aircraftId: '',
    departureTime: '',
    arrivalTime: '',
    isActive: true,
  }
  const [adminFlights, setAdminFlights] = useState([])
  const [adminRoutes, setAdminRoutes] = useState([])
  const [isLoadingFlightsAdmin, setIsLoadingFlightsAdmin] = useState(false)
  const [flightAdminError, setFlightAdminError] = useState('')
  const [flightAdminNotice, setFlightAdminNotice] = useState('')
  const [flightFormData, setFlightFormData] = useState(emptyFlightForm)
  const [editingFlightId, setEditingFlightId] = useState(null)
  const [isCancellingAdminFlightId, setIsCancellingAdminFlightId] = useState(null)
  const [adminFlightFilters, setAdminFlightFilters] = useState({
    date: '',
    from: '',
    to: '',
    code: '',
  })
  const emptyPromotionForm = {
    code: '',
    description: '',
    discountType: 0,
    discountValue: '',
    maxDiscountAmount: '',
    minimumAmount: '',
    usageLimit: '',
    validFrom: '',
    validTo: '',
    isActive: true,
  }
  const [adminPromotions, setAdminPromotions] = useState([])
  const [isLoadingPromotionsAdmin, setIsLoadingPromotionsAdmin] = useState(false)
  const [promotionAdminError, setPromotionAdminError] = useState('')
  const [promotionAdminNotice, setPromotionAdminNotice] = useState('')
  const [promotionFormData, setPromotionFormData] = useState(emptyPromotionForm)
  const [editingPromotionId, setEditingPromotionId] = useState(null)
  const totalPassengers = passengerCounts.adult + passengerCounts.child + passengerCounts.infant
  const passengerDivisor = Math.max(1, totalPassengers)
  const sortedNotifications = useMemo(
    () => sortNotificationsByTime(notifications),
    [notifications],
  )
  const resolveAdminRouteInfo = (flight) => {
    if (!flight?.routeId || adminRoutes.length === 0) {
      return {
        from: '',
        to: '',
        code: flight?.routeCode || '',
      }
    }

    const matched = adminRoutes.find(
      (route) => String(route?.routeId) === String(flight?.routeId),
    )

    return {
      from: matched?.departureAirport || '',
      to: matched?.arrivalAirport || '',
      code: matched?.routeCode || matched?.code || flight?.routeCode || '',
    }
  }
  const filteredAdminFlights = useMemo(() => {
    const dateFilter = adminFlightFilters.date
    const fromFilter = normalizeAscii(adminFlightFilters.from)
    const toFilter = normalizeAscii(adminFlightFilters.to)
    const codeFilter = normalizeAscii(adminFlightFilters.code)

    if (!dateFilter && !fromFilter && !toFilter && !codeFilter) return adminFlights

    return adminFlights.filter((flight) => {
      const departDate = toDateInputValueFromApi(flight?.departureTime)
      const routeInfo = resolveAdminRouteInfo(flight)
      const resolvedFrom = routeInfo.from
      const resolvedTo = routeInfo.to
      const resolvedCode = routeInfo.code
      const matchesDate = dateFilter ? departDate === dateFilter : true

      const fromHaystack = normalizeAscii([
        flight?.departureAirport,
        flight?.departureAirportName,
        flight?.fromAirport,
        flight?.origin,
        flight?.originName,
        resolvedFrom,
        flight?.routeCode,
      ]
        .filter(Boolean)
        .join(' '))
      const toHaystack = normalizeAscii([
        flight?.arrivalAirport,
        flight?.arrivalAirportName,
        flight?.toAirport,
        flight?.destination,
        flight?.destinationName,
        resolvedTo,
        flight?.routeCode,
      ]
        .filter(Boolean)
        .join(' '))
      const codeHaystack = normalizeAscii([
        flight?.flightNumber,
        flight?.routeCode,
        resolvedCode,
      ]
        .filter(Boolean)
        .join(' '))

      const matchesFrom = fromFilter ? fromHaystack.includes(fromFilter) : true
      const matchesTo = toFilter ? toHaystack.includes(toFilter) : true
      const matchesCode = codeFilter ? codeHaystack.includes(codeFilter) : true

      return matchesDate && matchesFrom && matchesTo && matchesCode
    })
  }, [adminFlightFilters, adminFlights, adminRoutes])

  useEffect(() => {
    setSearchData((prev) => ({ ...prev, passengers: String(totalPassengers) }))
    setPassengerForms((prev) => {
      const existing = Array.isArray(prev) ? prev : []
      const existingAdults = existing.filter((item) => item.type === 'adult')
      const existingChildren = existing.filter((item) => item.type === 'child')
      const existingInfants = existing.filter((item) => item.type === 'infant')
      const next = []

      for (let i = 0; i < passengerCounts.adult; i += 1) {
        next.push(existingAdults[i] || createEmptyPassenger('adult'))
      }
      for (let i = 0; i < passengerCounts.child; i += 1) {
        next.push(existingChildren[i] || createEmptyPassenger('child'))
      }
      for (let i = 0; i < passengerCounts.infant; i += 1) {
        next.push(existingInfants[i] || createEmptyPassenger('infant'))
      }

      return next
    })
  }, [passengerCounts, totalPassengers])

  useEffect(() => {
    if (screen !== 'passenger' || !selectedFlight) return
    const seatClassId = seatClassMap[searchData.seatClass]
    if (!seatClassId) {
      setServiceError('Khong xac dinh duoc hang ghe de tai dich vu')
      setServices([])
      return
    }
    loadServices(seatClassId)
  }, [screen, selectedFlight, searchData.seatClass])

  useEffect(() => {
    if (tripType !== 'roundtrip') {
      setReturnFlight(null)
      setReturnFlights([])
      setRoundtripStep('outbound')
    }
  }, [tripType])

  useEffect(() => {
    setSelectedServicesByPassengerDraft((prev) => {
      const next = { outbound: {}, return: {} }
      passengerForms.forEach((_, idx) => {
        if (prev?.outbound?.[idx]) next.outbound[idx] = prev.outbound[idx]
        if (prev?.return?.[idx]) next.return[idx] = prev.return[idx]
      })
      return next
    })
  }, [passengerForms])

  // Load booking history once
  useEffect(() => {
    try {
      const raw = localStorage.getItem('bookingHistory')
      if (raw) {
        const parsed = JSON.parse(raw)
        if (Array.isArray(parsed)) setBookingHistory(parsed)
      }
    } catch (e) {
      // ignore parse errors
    }
  }, [])

  // restore auth token from localStorage on app start
  useEffect(() => {
    try {
      const token = localStorage.getItem('authToken')
      console.log('🔄 Khôi phục token từ localStorage:', token ? token.substring(0, 20) + '...' : 'không có')
      if (token) {
        setAuthToken(token)
        const roleFromToken = getRoleFromToken(token)
        const normalizedRole = roleFromToken === 'admin' ? 'admin' : 'user'
        let email = ''
        let fullName = ''
        try {
          const payload = JSON.parse(decodeBase64Url(token.split('.')[1] || ''))
          email = payload.email || payload.sub || ''
          fullName = payload.fullName || payload.name || ''
        } catch (e) {}
        setAuthUser({ email, fullName, role: normalizedRole })
        setScreen('search')
        console.log('✅ Đã khôi phục phiên đăng nhập:', { email, role: normalizedRole })
      }
    } catch (e) {
      console.error('❌ Lỗi khi khôi phục token:', e)
    }
  }, [])

  const extractUnreadCount = (payload) => {
    if (typeof payload === 'number') return payload
    if (typeof payload?.count === 'number') return payload.count
    if (typeof payload?.data === 'number') return payload.data
    if (typeof payload?.unreadCount === 'number') return payload.unreadCount
    return 0
  }

  const loadNotifications = async () => {
    if (!authUser) return
    setIsLoadingNotifications(true)
    setNotificationError('')
    try {
      const [list, unreadCount] = await Promise.all([
        getNotifications({ page: 1, pageSize: 20 }),
        getNotificationsUnreadCount(),
      ])
      setNotifications(Array.isArray(list) ? list : [])
      setUnreadNotificationCount(extractUnreadCount(unreadCount))
    } catch (error) {
      setNotificationError(error.message || 'Không thể tải thông báo')
    } finally {
      setIsLoadingNotifications(false)
    }
  }

  const handleMarkNotificationRead = async (notificationId) => {
    if (!notificationId) return
    try {
      await markNotificationRead(notificationId)
      setNotifications((prev) =>
        prev.map((item) =>
          item?.notificationId === notificationId
            ? { ...item, isRead: true, readAt: item?.readAt || new Date().toISOString() }
            : item,
        ),
      )
      setUnreadNotificationCount((prev) => Math.max(0, prev - 1))
    } catch (error) {
      setNotificationError(error.message || 'Không thể cập nhật trạng thái thông báo')
    }
  }

  const handleMarkAllNotificationsRead = async () => {
    try {
      await markAllNotificationsRead()
      setNotifications((prev) =>
        prev.map((item) => ({
          ...item,
          isRead: true,
          readAt: item?.readAt || new Date().toISOString(),
        })),
      )
      setUnreadNotificationCount(0)
    } catch (error) {
      setNotificationError(error.message || 'Không thể cập nhật trạng thái thông báo')
    }
  }

  useEffect(() => {
    if (!authUser) {
      setNotifications([])
      setUnreadNotificationCount(0)
      return
    }
    loadNotifications()
  }, [authUser])

  useEffect(() => {
    if (isNotificationPanelOpen && authUser) {
      loadNotifications()
    }
  }, [isNotificationPanelOpen, authUser])

  const logout = () => {
    try {
      clearAuthToken()
    } catch (e) {}
    setAuthUser(null)
    setNotifications([])
    setUnreadNotificationCount(0)
    setIsNotificationPanelOpen(false)
    setBookingHistory([])
    setHistoryError('')
    setScreen('login')
  }

  const mapBookingResponse = (booking) => {
    const outbound = booking?.outboundFlight || {}
    const passengers = booking?.passengers || []
    const primaryPassenger = passengers[0] || {}
    const passengerName = [primaryPassenger.firstName, primaryPassenger.lastName]
      .filter(Boolean)
      .join(' ')
    const passengerCount =
      Number(booking?.passengerCount) || (Array.isArray(passengers) ? passengers.length : 0) || 1
    const totalPrice =
      booking?.finalAmount ??
      booking?.totalAmount ??
      booking?.totalPrice ??
      booking?.amount ??
      booking?.paymentAmount ??
      (outbound?.price ?? 0) * passengerCount

    const rawStatus =
      booking?.status ??
      booking?.bookingStatus ??
      booking?.bookingStatusName ??
      booking?.paymentStatus ??
      booking?.paymentStatusName ??
      null

    return {
      bookingId: booking?.bookingId ?? booking?.bookingCode ?? '---',
      transactionRef: booking?.bookingCode || '',
      status: getBookingStatusLabel(rawStatus),
      createdAt: booking?.createdAt,
      fromAirport: outbound?.departureAirport || '',
      toAirport: outbound?.arrivalAirport || '',
      fromCode: '',
      toCode: '',
      flightId: outbound?.flightId ?? '',
      flightNumber: outbound?.flightNumber || '',
      airlineCode: '',
      departTime: outbound?.departureTime || '',
      arriveTime: outbound?.arrivalTime || '',
      seatClass: outbound?.seatClass || '',
      passengerName,
      passengerCount,
      passengers,
      totalPrice,
    }
  }

  const fetchAllBookings = async () => {
    const pageSize = 50
    let page = 1
    let all = []

    while (page <= 20) {
      const data = await getBookings(page, pageSize)
      const chunk = Array.isArray(data) ? data : []
      all = all.concat(chunk)
      if (chunk.length < pageSize) break
      page += 1
    }

    return all
  }

  const loadBookingHistory = async () => {
    setIsLoadingHistory(true)
    setHistoryError('')
    try {
      const bookings = await fetchAllBookings()
      const mapped = bookings.map(mapBookingResponse)
      mapped.sort((a, b) => new Date(b.createdAt || 0) - new Date(a.createdAt || 0))
      setBookingHistory(mapped)
    } catch (error) {
      setHistoryError(error.message || 'KhÃ´ng thá»ƒ táº£i lá»‹ch sá»­ Ä‘áº·t vÃ©')
    } finally {
      setIsLoadingHistory(false)
    }
  }

  const loadDisruptionOptionsForBooking = async (bookingId, departureDate) => {
    const bookingIdValue = Number(bookingId)
    if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: 'Mã booking không hợp lệ.',
      }))
      return null
    }

    const normalizedDate = departureDate
      ? toDateInputValueFromApi(departureDate) || String(departureDate)
      : ''

    setDisruptionLoadingMap((prev) => ({ ...prev, [bookingId]: true }))
    setDisruptionErrorMap((prev) => ({ ...prev, [bookingId]: '' }))
    setDisruptionNoticeMap((prev) => ({ ...prev, [bookingId]: '' }))

    try {
      const options = await getDisruptionOptions(bookingIdValue, normalizedDate)
      setDisruptionOptionsMap((prev) => ({ ...prev, [bookingId]: options }))

      if (!Array.isArray(options) || options.length === 0) {
        setDisruptionNoticeMap((prev) => ({
          ...prev,
          [bookingId]: 'Không có lựa chọn xử lý hủy chuyến cho booking này.',
        }))
        return options
      }

      const firstDecisionId = Number(options[0]?.decisionId)
      if (Number.isFinite(firstDecisionId)) {
        setSelectedDisruptionDecisionMap((prev) => ({
          ...prev,
          [bookingId]: String(firstDecisionId),
        }))
      }

      setSelectedDisruptionActionMap((prev) => ({
        ...prev,
        [bookingId]: prev[bookingId] || 'refund',
      }))

      return options
    } catch (error) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: error.message || 'Không thể tải lựa chọn xử lý hủy chuyến.',
      }))
      return null
    } finally {
      setDisruptionLoadingMap((prev) => ({ ...prev, [bookingId]: false }))
    }
  }

  const handleDisruptionRefund = async (bookingId, decisionId) => {
    const bookingIdValue = Number(bookingId)
    const decisionIdValue = Number(decisionId)
    if (!Number.isFinite(bookingIdValue) || !Number.isFinite(decisionIdValue)) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: 'Thiếu thông tin quyết định để hoàn tiền.',
      }))
      return
    }

    setSubmittingDisruptionMap((prev) => ({ ...prev, [decisionId]: true }))
    setDisruptionErrorMap((prev) => ({ ...prev, [bookingId]: '' }))
    setDisruptionNoticeMap((prev) => ({ ...prev, [bookingId]: '' }))

    try {
      await cancelDisruptionDecision(bookingIdValue, decisionIdValue)
      setDisruptionNoticeMap((prev) => ({
        ...prev,
        [bookingId]: 'Đã gửi yêu cầu hoàn tiền. Vui lòng chờ hệ thống xử lý.',
      }))
      await loadBookingHistory()
      await loadDisruptionOptionsForBooking(bookingIdValue)
    } catch (error) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: error.message || 'Không thể gửi yêu cầu hoàn tiền.',
      }))
    } finally {
      setSubmittingDisruptionMap((prev) => ({ ...prev, [decisionId]: false }))
    }
  }

  const handleDisruptionRebook = async (bookingId, decisionId, newFlightId) => {
    const bookingIdValue = Number(bookingId)
    const decisionIdValue = Number(decisionId)
    const selectedFlightId = Number(newFlightId)

    if (!Number.isFinite(bookingIdValue) || !Number.isFinite(decisionIdValue)) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: 'Thiếu thông tin quyết định để đổi chuyến.',
      }))
      return
    }

    if (!Number.isFinite(selectedFlightId) || selectedFlightId <= 0) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: 'Vui lòng chọn chuyến bay mới trước khi đổi chuyến.',
      }))
      return
    }

    setSubmittingDisruptionMap((prev) => ({ ...prev, [decisionId]: true }))
    setDisruptionErrorMap((prev) => ({ ...prev, [bookingId]: '' }))
    setDisruptionNoticeMap((prev) => ({ ...prev, [bookingId]: '' }))

    try {
      await rebookDisruptionDecision(bookingIdValue, decisionIdValue, selectedFlightId)
      setDisruptionNoticeMap((prev) => ({
        ...prev,
        [bookingId]: 'Đã gửi yêu cầu đổi chuyến. Vui lòng kiểm tra lại trạng thái booking.',
      }))
      await loadBookingHistory()
      await loadDisruptionOptionsForBooking(bookingIdValue)
    } catch (error) {
      setDisruptionErrorMap((prev) => ({
        ...prev,
        [bookingId]: error.message || 'Không thể đổi chuyến.',
      }))
    } finally {
      setSubmittingDisruptionMap((prev) => ({ ...prev, [decisionId]: false }))
    }
  }

  const getDisruptionDecisionOptions = (bookingIdValue, decisionIdValue) => {
    const list = Array.isArray(disruptionOptionsMap[bookingIdValue])
      ? disruptionOptionsMap[bookingIdValue]
      : []
    return list.find((option) => Number(option?.decisionId) === Number(decisionIdValue))
  }

  const filterDisruptionFlightsByDate = (flightOptions, dateValue) => {
    if (!dateValue) return flightOptions
    return flightOptions.filter((flight) => {
      const flightDate = toDateInputValueFromApi(flight?.departureTime)
      return flightDate === dateValue
    })
  }

  const openRebookModal = async (bookingIdValue, decisionIdValue, cancelledFlightDate) => {
    const decision = getDisruptionDecisionOptions(bookingIdValue, decisionIdValue)
    const flightOptions = Array.isArray(decision?.flightOptions) ? decision.flightOptions : []
    const defaultDate = toDateInputValueFromApi(cancelledFlightDate)
    const filteredOptions = filterDisruptionFlightsByDate(flightOptions, defaultDate)
    const firstOption = filteredOptions[0] || flightOptions[0]

    setRebookModalState({
      isOpen: true,
      bookingId: bookingIdValue,
      decisionId: decisionIdValue,
      date: defaultDate,
      selectedFlightId: firstOption?.flightId ? String(firstOption.flightId) : '',
    })

    if (!defaultDate) {
      return
    }

    const options = await loadDisruptionOptionsForBooking(bookingIdValue, defaultDate)
    const selectedDecision = Array.isArray(options)
      ? options.find((option) => Number(option?.decisionId) === Number(decisionIdValue))
      : null
    const selectedOptions = Array.isArray(selectedDecision?.flightOptions)
      ? selectedDecision.flightOptions
      : []

    setRebookModalState((prev) => ({
      ...prev,
      selectedFlightId: selectedOptions[0]?.flightId ? String(selectedOptions[0].flightId) : '',
    }))
  }

  const handleRebookDateChange = async (nextDate) => {
    setRebookModalState((prev) => ({
      ...prev,
      date: nextDate,
    }))

    if (!rebookModalState.bookingId || !rebookModalState.decisionId) return

    const options = await loadDisruptionOptionsForBooking(
      rebookModalState.bookingId,
      nextDate,
    )
    const selectedDecision = Array.isArray(options)
      ? options.find((option) => Number(option?.decisionId) === Number(rebookModalState.decisionId))
      : null
    const selectedOptions = Array.isArray(selectedDecision?.flightOptions)
      ? selectedDecision.flightOptions
      : []

    setRebookModalState((prev) => ({
      ...prev,
      selectedFlightId: selectedOptions[0]?.flightId ? String(selectedOptions[0].flightId) : '',
    }))
  }

  const closeRebookModal = () => {
    setRebookModalState({
      isOpen: false,
      bookingId: null,
      decisionId: null,
      date: '',
      selectedFlightId: '',
    })
  }

  const mapSavedPassengerForm = (passenger) => ({
    firstName: passenger?.firstName || '',
    lastName: passenger?.lastName || '',
    dateOfBirth: toDateInputValueFromApi(passenger?.dateOfBirth),
    gender: passenger?.gender || '',
    nationality: passenger?.nationality || '',
    documentNumber: passenger?.documentNumber || '',
    email: passenger?.email || '',
    phone: passenger?.phone || '',
  })

  const loadSavedPassengers = async () => {
    setIsLoadingSavedPassengers(true)
    setSavedPassengerError('')
    try {
      const data = await getSavedPassengers()
      setSavedPassengers(Array.isArray(data) ? data : [])
    } catch (error) {
      setSavedPassengerError(error.message || 'Không thể tải danh sách hành khách đã lưu.')
    } finally {
      setIsLoadingSavedPassengers(false)
    }
  }

  const mapFlightToForm = (flight) => ({
    flightNumber: flight?.flightNumber || '',
    routeId: '',
    aircraftId: '',
    departureTime: toDateTimeLocalInputValue(flight?.departureTime),
    arrivalTime: toDateTimeLocalInputValue(flight?.arrivalTime),
    isActive: flight?.isActive !== false,
  })

  const buildCreateFlightPayload = (form) => ({
    flightNumber: form.flightNumber?.trim() || null,
    routeId: Number(form.routeId || 0),
    aircraftId: Number(form.aircraftId || 0),
    departureTime: toApiDateTimeFromLocal(form.departureTime),
    arrivalTime: toApiDateTimeFromLocal(form.arrivalTime),
    isActive: Boolean(form.isActive),
  })

  const buildUpdateFlightPayload = (form) => ({
    flightNumber: form.flightNumber?.trim() || null,
    aircraftId: form.aircraftId ? Number(form.aircraftId) : null,
    departureTime: form.departureTime ? toApiDateTimeFromLocal(form.departureTime) : null,
    arrivalTime: form.arrivalTime ? toApiDateTimeFromLocal(form.arrivalTime) : null,
    isActive: Boolean(form.isActive),
  })

  const loadAdminFlights = async () => {
    setIsLoadingFlightsAdmin(true)
    setFlightAdminError('')
    try {
      const data = await getAdminFlights(1, 100)
      setAdminFlights(Array.isArray(data) ? data : [])
    } catch (error) {
      setFlightAdminError(error.message || 'Không thể tải danh sách chuyến bay.')
    } finally {
      setIsLoadingFlightsAdmin(false)
    }
  }

  const loadAdminRoutes = async () => {
    try {
      const data = await getAdminRoutes(1, 200)
      setAdminRoutes(Array.isArray(data) ? data : [])
    } catch (error) {
      setFlightAdminError(error.message || 'Không thể tải danh sách tuyến bay.')
    }
  }

  const startCreateFlight = () => {
    setEditingFlightId(null)
    setFlightFormData(emptyFlightForm)
    setFlightAdminError('')
    setFlightAdminNotice('')
  }

  const startEditFlight = (flight) => {
    if (!flight) return
    setEditingFlightId(flight.flightId)
    setFlightFormData(mapFlightToForm(flight))
    setFlightAdminError('')
    setFlightAdminNotice('')
  }

  const handleFlightSubmit = async (event) => {
    event.preventDefault()
    setFlightAdminError('')
    setFlightAdminNotice('')

    try {
      if (editingFlightId) {
        const payload = buildUpdateFlightPayload(flightFormData)
        await updateAdminFlight(editingFlightId, payload)
        setFlightAdminNotice('Đã cập nhật chuyến bay.')
      } else {
        if (!flightFormData.routeId || !flightFormData.aircraftId) {
          setFlightAdminError('Vui lòng chọn tuyến bay và máy bay.')
          return
        }
        if (!flightFormData.departureTime || !flightFormData.arrivalTime) {
          setFlightAdminError('Vui lòng nhập giờ bay và giờ đến.')
          return
        }
        const payload = buildCreateFlightPayload(flightFormData)
        await createAdminFlight(payload)
        setFlightAdminNotice('Đã tạo chuyến bay mới.')
      }
      setFlightFormData(emptyFlightForm)
      setEditingFlightId(null)
      await loadAdminFlights()
    } catch (error) {
      setFlightAdminError(error.message || 'Không thể lưu chuyến bay.')
    }
  }

  const handleDeleteFlight = async (flight) => {
    if (!flight?.flightId) return
    const confirmed = window.confirm('Bạn chắc chắn muốn xóa chuyến bay này?')
    if (!confirmed) return

    setFlightAdminError('')
    setFlightAdminNotice('')
    try {
      await deleteAdminFlight(flight.flightId)
      setFlightAdminNotice('Đã xóa chuyến bay.')
      if (editingFlightId === flight.flightId) {
        setEditingFlightId(null)
        setFlightFormData(emptyFlightForm)
      }
      await loadAdminFlights()
    } catch (error) {
      setFlightAdminError(error.message || 'Không thể xóa chuyến bay.')
    }
  }

  const handleCancelAdminFlight = async (flight) => {
    if (!flight?.flightId) return
    const confirmed = window.confirm('Bạn chắc chắn muốn hủy chuyến bay này?')
    if (!confirmed) return

    const reason = window.prompt('Lý do hủy chuyến bay:', 'Operational issue')
    if (reason === null) return
    if (!reason.trim()) {
      setFlightAdminError('Vui lòng nhập lý do hủy chuyến bay.')
      return
    }

    setIsCancellingAdminFlightId(flight.flightId)
    setFlightAdminError('')
    setFlightAdminNotice('')
    try {
      await cancelAdminFlight(flight.flightId, reason.trim())
      setFlightAdminNotice('Đã hủy chuyến bay.')
      await loadAdminFlights()
    } catch (error) {
      setFlightAdminError(error.message || 'Không thể hủy chuyến bay.')
    } finally {
      setIsCancellingAdminFlightId(null)
    }
  }

  const mapPromotionToForm = (promotion) => ({
    code: promotion?.code || '',
    description: promotion?.description || '',
    discountType: Number.isFinite(Number(promotion?.discountType)) ? Number(promotion.discountType) : 0,
    discountValue: Number.isFinite(Number(promotion?.discountValue)) ? String(promotion.discountValue) : '',
    maxDiscountAmount:
      promotion?.maxDiscountAmount === null || promotion?.maxDiscountAmount === undefined
        ? ''
        : String(promotion.maxDiscountAmount),
    minimumAmount: Number.isFinite(Number(promotion?.minimumAmount)) ? String(promotion.minimumAmount) : '',
    usageLimit:
      promotion?.usageLimit === null || promotion?.usageLimit === undefined
        ? ''
        : String(promotion.usageLimit),
    validFrom: toDateInputValueFromApi(promotion?.validFrom),
    validTo: toDateInputValueFromApi(promotion?.validTo),
    isActive: promotion?.isActive !== false,
  })

  const buildCreatePromotionPayload = (form) => ({
    code: form.code?.trim() || null,
    description: form.description?.trim() || null,
    discountType: Number(form.discountType || 0),
    discountValue: Number(form.discountValue || 0),
    maxDiscountAmount:
      form.maxDiscountAmount === '' || form.maxDiscountAmount === null
        ? null
        : Number(form.maxDiscountAmount),
    minimumAmount: Number(form.minimumAmount || 0),
    usageLimit: form.usageLimit === '' || form.usageLimit === null ? null : Number(form.usageLimit),
    validFrom: toApiDateTimeValue(form.validFrom),
    validTo: toApiDateTimeValue(form.validTo),
    isActive: Boolean(form.isActive),
  })

  const buildUpdatePromotionPayload = (form) => ({
    description: form.description?.trim() || null,
    discountValue: form.discountValue === '' || form.discountValue === null ? null : Number(form.discountValue),
    maxDiscountAmount:
      form.maxDiscountAmount === '' || form.maxDiscountAmount === null
        ? null
        : Number(form.maxDiscountAmount),
    usageLimit: form.usageLimit === '' || form.usageLimit === null ? null : Number(form.usageLimit),
    validTo: toApiDateTimeValue(form.validTo),
    isActive: Boolean(form.isActive),
  })

  const loadAdminPromotions = async () => {
    setIsLoadingPromotionsAdmin(true)
    setPromotionAdminError('')
    try {
      const data = await getAdminPromotions(1, 100)
      setAdminPromotions(Array.isArray(data) ? data : [])
    } catch (error) {
      setPromotionAdminError(error.message || 'Không thể tải danh sách khuyến mãi.')
    } finally {
      setIsLoadingPromotionsAdmin(false)
    }
  }

  const startCreatePromotion = () => {
    setEditingPromotionId(null)
    setPromotionFormData(emptyPromotionForm)
    setPromotionAdminError('')
    setPromotionAdminNotice('')
  }

  const startEditPromotion = (promotion) => {
    if (!promotion) return
    setEditingPromotionId(promotion.promotionId)
    setPromotionFormData(mapPromotionToForm(promotion))
    setPromotionAdminError('')
    setPromotionAdminNotice('')
  }

  const handlePromotionSubmit = async (event) => {
    event.preventDefault()
    setPromotionAdminError('')
    setPromotionAdminNotice('')

    try {
      if (editingPromotionId) {
        const payload = buildUpdatePromotionPayload(promotionFormData)
        await updatePromotion(editingPromotionId, payload)
        setPromotionAdminNotice('Đã cập nhật mã khuyến mãi.')
      } else {
        const payload = buildCreatePromotionPayload(promotionFormData)
        await createPromotion(payload)
        setPromotionAdminNotice('Đã tạo mã khuyến mãi mới.')
      }
      setPromotionFormData(emptyPromotionForm)
      setEditingPromotionId(null)
      await loadAdminPromotions()
    } catch (error) {
      setPromotionAdminError(error.message || 'Không thể lưu mã khuyến mãi.')
    }
  }

  const handleDeletePromotion = async (promotion) => {
    if (!promotion?.promotionId) return
    const confirmed = window.confirm('Bạn chắc chắn muốn xóa mã khuyến mãi này?')
    if (!confirmed) return

    setPromotionAdminError('')
    setPromotionAdminNotice('')
    try {
      await deletePromotion(promotion.promotionId)
      setPromotionAdminNotice('Đã xóa mã khuyến mãi.')
      if (editingPromotionId === promotion.promotionId) {
        setEditingPromotionId(null)
        setPromotionFormData(emptyPromotionForm)
      }
      await loadAdminPromotions()
    } catch (error) {
      setPromotionAdminError(error.message || 'Không thể xóa mã khuyến mãi.')
    }
  }

  const startCreateSavedPassenger = () => {
    setEditingSavedPassengerId(null)
    setSavedPassengerForm(emptySavedPassengerForm)
    setSavedPassengerError('')
    setSavedPassengerNotice('')
  }

  const startEditSavedPassenger = (passenger) => {
    if (!passenger) return
    setEditingSavedPassengerId(passenger.id)
    setSavedPassengerForm(mapSavedPassengerForm(passenger))
    setSavedPassengerError('')
    setSavedPassengerNotice('')
  }

  const buildSavedPassengerPayload = (form) => ({
    firstName: form.firstName?.trim() || null,
    lastName: form.lastName?.trim() || null,
    dateOfBirth: toApiDateTimeValue(form.dateOfBirth),
    gender: form.gender?.trim() || null,
    nationality: form.nationality?.trim() || null,
    documentNumber: form.documentNumber?.trim() || null,
    email: form.email?.trim() || null,
    phone: form.phone?.trim() || null,
  })

  const handleSavedPassengerSubmit = async (event) => {
    event.preventDefault()
    setSavedPassengerError('')
    setSavedPassengerNotice('')

    try {
      const payload = buildSavedPassengerPayload(savedPassengerForm)
      if (editingSavedPassengerId) {
        await updateSavedPassenger(editingSavedPassengerId, payload)
        setSavedPassengerNotice('Đã cập nhật hành khách đã lưu.')
      } else {
        await createSavedPassenger(payload)
        setSavedPassengerNotice('Đã thêm hành khách đã lưu.')
      }
      setSavedPassengerForm(emptySavedPassengerForm)
      setEditingSavedPassengerId(null)
      await loadSavedPassengers()
    } catch (error) {
      setSavedPassengerError(error.message || 'Không thể lưu hành khách.')
    }
  }

  const handleDeleteSavedPassenger = async (passenger) => {
    if (!passenger?.id) return
    const confirmed = window.confirm('Bạn chắc chắn muốn xóa hành khách này?')
    if (!confirmed) return

    setSavedPassengerError('')
    setSavedPassengerNotice('')
    try {
      await deleteSavedPassenger(passenger.id)
      setSavedPassengerNotice('Đã xóa hành khách đã lưu.')
      await loadSavedPassengers()
    } catch (error) {
      setSavedPassengerError(error.message || 'Không thể xóa hành khách.')
    }
  }

  const loadTicketsForBooking = async (bookingId) => {
    if (!bookingId) return
    setLoadingTicketsMap((prev) => ({ ...prev, [bookingId]: true }))
    setTicketErrorMap((prev) => ({ ...prev, [bookingId]: '' }))
    try {
      const tickets = await getTicketsByBooking(bookingId)
      setBookingTicketsMap((prev) => ({ ...prev, [bookingId]: tickets }))
    } catch (error) {
      setTicketErrorMap((prev) => ({
        ...prev,
        [bookingId]: error.message || 'Không thể tải danh sách vé.',
      }))
    } finally {
      setLoadingTicketsMap((prev) => ({ ...prev, [bookingId]: false }))
    }
  }

  const isTicketCancelled = (status) => {
    const code = resolveTicketStatusCode(status)
    if (code === 3 || code === 4 || code === 5) return true
    const normalized = String(status || '').trim().toLowerCase()
    return normalized.includes('cancel') || normalized.includes('huy')
  }

  const isTicketActionable = (status) => {
    const code = resolveTicketStatusCode(status)
    return code === 0
  }

  // ===== Ticket Upgrade State =====
  const [upgradeModal, setUpgradeModal] = useState(null)
  // upgradeModal = { booking, ticket, step: 'select'|'quote'|'confirm'|'payment', selectedClassId, quote, request }
  const [upgradeLoading, setUpgradeLoading] = useState(false)
  const [upgradeError, setUpgradeError] = useState('')

  const seatClasses = [
    { id: 1, name: 'Economy', label: 'Phổ thông', icon: '🪑' },
    { id: 2, name: 'Business', label: 'Thương gia', icon: '💼' },
  ]

  const openUpgradeModal = (booking, ticket) => {
    setUpgradeModal({ booking, ticket, step: 'select', selectedClassId: null, quote: null, request: null })
    setUpgradeError('')
  }

  const closeUpgradeModal = () => {
    setUpgradeModal(null)
    setUpgradeError('')
  }

  const handleGetUpgradeQuote = async () => {
    if (!upgradeModal?.selectedClassId) return
    setUpgradeLoading(true)
    setUpgradeError('')
    try {
      const quote = await getUpgradeQuote(
        upgradeModal.booking.bookingId,
        upgradeModal.ticket.ticketId,
        upgradeModal.selectedClassId
      )
      setUpgradeModal((prev) => ({ ...prev, step: 'quote', quote }))
    } catch (err) {
      setUpgradeError(err.message || 'Không thể lấy báo giá nâng hạng.')
    } finally {
      setUpgradeLoading(false)
    }
  }

  const handleCreateUpgradeRequest = async () => {
    setUpgradeLoading(true)
    setUpgradeError('')
    try {
      const request = await createUpgradeRequest(
        upgradeModal.booking.bookingId,
        upgradeModal.ticket.ticketId,
        upgradeModal.selectedClassId
      )
      setUpgradeModal((prev) => ({ ...prev, step: 'payment', request }))
    } catch (err) {
      setUpgradeError(err.message || 'Không thể tạo yêu cầu nâng hạng.')
    } finally {
      setUpgradeLoading(false)
    }
  }

  const handleInitiateUpgradePayment = async () => {
    if (!upgradeModal?.request?.requestId) return
    setUpgradeLoading(true)
    setUpgradeError('')
    try {
      const result = await initiateUpgradePayment(upgradeModal.request.requestId, 'VNPAY')
      if (result?.payment?.paymentUrl) {
        window.location.href = result.payment.paymentUrl
      } else {
        setUpgradeError('Không nhận được URL thanh toán.')
      }
    } catch (err) {
      setUpgradeError(err.message || 'Không thể khởi tạo thanh toán.')
    } finally {
      setUpgradeLoading(false)
    }
  }

  // ===== Change Flight State =====
  const [changeFlightModal, setChangeFlightModal] = useState(null)
  // changeFlightModal = { booking, step: 'select-flight'|'quote'|'confirm', legType, departureDate, options, selectedFlightId, quote }
  const [changeFlightLoading, setChangeFlightLoading] = useState(false)
  const [changeFlightError, setChangeFlightError] = useState('')

  const openChangeFlightModal = (booking) => {
    const today = new Date().toISOString().split('T')[0]
    setChangeFlightModal({ booking, step: 'select-flight', legType: 0, departureDate: today, options: null, selectedFlightId: null, quote: null })
    setChangeFlightError('')
  }

  const closeChangeFlightModal = () => {
    setChangeFlightModal(null)
    setChangeFlightError('')
  }

  const handleGetChangeOptions = async () => {
    setChangeFlightLoading(true)
    setChangeFlightError('')
    try {
      const data = await getChangeFlightOptions(
        changeFlightModal.booking.bookingId,
        changeFlightModal.legType,
        changeFlightModal.departureDate
      )
      const candidates = Array.isArray(data) ? data : (data?.candidates || [])
      setChangeFlightModal((prev) => ({ ...prev, options: candidates, selectedFlightId: null }))
    } catch (err) {
      setChangeFlightError(err.message || 'Không thể lấy danh sách chuyến bay.')
    } finally {
      setChangeFlightLoading(false)
    }
  }

  const handleGetChangeQuote = async () => {
    if (!changeFlightModal?.selectedFlightId) return
    setChangeFlightLoading(true)
    setChangeFlightError('')
    try {
      const quote = await getChangeFlightQuote(changeFlightModal.booking.bookingId, {
        legType: changeFlightModal.legType,
        newFlightId: changeFlightModal.selectedFlightId,
      })
      setChangeFlightModal((prev) => ({ ...prev, step: 'quote', quote }))
    } catch (err) {
      setChangeFlightError(err.message || 'Không thể lấy báo giá đổi chuyến.')
    } finally {
      setChangeFlightLoading(false)
    }
  }

  const handleConfirmChangeFlight = async () => {
    setChangeFlightLoading(true)
    setChangeFlightError('')
    try {
      const result = await confirmChangeFlight(changeFlightModal.booking.bookingId, {
        legType: changeFlightModal.legType,
        newFlightId: changeFlightModal.selectedFlightId,
      })
      setChangeFlightModal((prev) => ({ ...prev, step: 'confirm', confirmResult: result }))
      // Reload bookings after change
      if (result?.paymentRequired && result?.paymentUrl) {
        window.location.href = result.paymentUrl
      }
    } catch (err) {
      setChangeFlightError(err.message || 'Không thể xác nhận đổi chuyến.')
    } finally {
      setChangeFlightLoading(false)
    }
  }

  const handleForgotPassword = async () => {
    if (!forgotEmail || !forgotEmail.includes('@')) {
      setForgotError('Vui lòng nhập email hợp lệ.')
      return
    }
    setForgotLoading(true)
    setForgotError('')
    try {
      await forgotPassword(forgotEmail)
      setForgotSuccess(true)
    } catch (err) {
      // Backend luon tra OK - van hien thanh success de bao mat
      setForgotSuccess(true)
    } finally {
      setForgotLoading(false)
    }
  }

  const handleResetPassword = async () => {
    if (!resetCode.trim()) {
      setForgotError('Vui lòng nhập mã xác nhận.')
      return
    }
    if (!resetNewPassword || resetNewPassword.length < 6) {
      setForgotError('Mật khẩu phải có ít nhất 6 ký tự.')
      return
    }
    if (resetNewPassword !== resetConfirmPassword) {
      setForgotError('Mật khẩu xác nhận không khớp.')
      return
    }
    setForgotLoading(true)
    setForgotError('')
    try {
      await resetPassword(resetCode.trim(), resetNewPassword)
      setResetSuccess(true)
    } catch (err) {
      setForgotError(err.message || 'Mã không hợp lệ hoặc đã hết hạn.')
    } finally {
      setForgotLoading(false)
    }
  }

  const cancelTicketFromHistory = async (bookingId, ticket) => {
    if (!bookingId || !ticket?.ticketId) return
    if (!isTicketActionable(ticket.status)) return

    const confirmed = window.confirm('Bạn chắc chắn muốn hủy vé này?')
    if (!confirmed) return

    const reason = window.prompt('Lý do hủy vé (không bắt buộc):', '')
    if (reason === null) return

    setIsCancellingTicketId(ticket.ticketId)
    setTicketErrorMap((prev) => ({ ...prev, [bookingId]: '' }))
    try {
      await cancelTicket(bookingId, ticket.ticketId, reason.trim())
      await loadTicketsForBooking(bookingId)
    } catch (error) {
      setTicketErrorMap((prev) => ({
        ...prev,
        [bookingId]: error.message || 'Hủy vé thất bại.',
      }))
    } finally {
      setIsCancellingTicketId(null)
    }
  }

  const applySavedPassengerToForm = (index, passenger) => {
    if (!passenger) return
    const fullName = [passenger.lastName, passenger.firstName].filter(Boolean).join(' ')
    setPassengerForms((prev) =>
      prev.map((item, idx) =>
        idx === index
          ? {
              ...item,
              fullName: fullName || item.fullName,
              dob: toDateInputValueFromApi(passenger.dateOfBirth),
              gender: passenger.gender || item.gender,
              document: passenger.documentNumber || item.document,
              email: passenger.email || item.email,
              phone: passenger.phone || item.phone,
              savedPassengerId: String(passenger.id || ''),
            }
          : item
      )
    )
  }

  const updateServiceDraft = (leg, index, serviceId, nextValue) => {
    setSelectedServicesByPassengerDraft((prev) => {
      const currentLeg = prev?.[leg] || {}
      const currentPassenger = currentLeg[index] || {}
      return {
        ...prev,
        [leg]: {
          ...currentLeg,
          [index]: {
            ...currentPassenger,
            [serviceId]: nextValue,
          },
        },
      }
    })
  }

  useEffect(() => {
    if (screen !== 'saved-passengers' && screen !== 'passenger') return
    loadSavedPassengers()
  }, [screen])

  const loadServices = async (seatClassId) => {
    setIsLoadingServices(true)
    setServiceError('')
    try {
      const resolvedSeatClassId = Number(seatClassId)
      if (!Number.isFinite(resolvedSeatClassId) || resolvedSeatClassId <= 0) {
        throw new Error('Seat class khong hop le')
      }
      const data = await getSeatClassServices(resolvedSeatClassId)
      setServices(Array.isArray(data) ? data : [])
    } catch (error) {
      setServiceError(error.message || 'Khong the tai danh sach dich vu')
      setServices([])
    } finally {
      setIsLoadingServices(false)
    }
  }

  const getServicePriceLabel = (price) => {
    const numeric = Number(price || 0)
    return numeric === 0 ? 'Miễn phí' : formatCurrency(numeric)
  }

  const getPaymentAmount = (payment, fallback) => {
    if (!payment) return fallback
    const candidates = [
      payment.amount,
      payment.totalAmount,
      payment.finalAmount,
      payment.totalPrice,
      payment.paymentAmount,
    ]
    const resolved = candidates
      .map((value) => Number(value))
      .find((value) => Number.isFinite(value))
    return resolved ?? fallback
  }

  const persistBookingHistory = (next) => {
    setBookingHistory(next)
    try {
      localStorage.setItem('bookingHistory', JSON.stringify(next))
    } catch (e) {
      // ignore storage errors
    }
  }

  const isBookingCancelled = (status) => {
    const code = resolveBookingStatusCode(status)
    return code === 2 || code === 3 || code === 4 || code === 5
  }

  const isBookingPendingPayment = (status) => {
    const code = resolveBookingStatusCode(status)
    return code === 0
  }

  const cancelBookingFromHistory = async (item) => {
    if (!item) return

    const bookingIdValue = Number(item.bookingId)
    if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
      setHistoryError('Không thể hủy vé này vì thiếu mã booking hợp lệ.')
      return
    }

    if (isBookingCancelled(item.status)) {
      setHistoryNotice('Vé này đã được hủy trước đó.')
      return
    }

    const confirmed = window.confirm('Bạn chắc chắn muốn hủy vé này?')
    if (!confirmed) return

    const reason = window.prompt('Lý do hủy vé (không bắt buộc):', '')
    if (reason === null) return

    setIsCancellingBookingId(item.bookingId)
    setHistoryError('')
    setHistoryNotice('')
    try {
      await cancelBooking(bookingIdValue, reason.trim())
      const next = bookingHistory.map((entry) =>
        entry.bookingId === item.bookingId
          ? { ...entry, status: getBookingStatusLabel(5) }
          : entry
      )
      persistBookingHistory(next)
      setHistoryNotice('Đã hủy vé thành công.')
    } catch (error) {
      setHistoryError(error.message || 'Hủy vé thất bại. Vui lòng thử lại.')
    } finally {
      setIsCancellingBookingId(null)
    }
  }

  const saveBookingToHistory = () => {
    if (!selectedFlight) {
      setHistoryNotice('Chưa có chuyến bay để lưu.')
      return
    }

    const entry = {
      bookingId: bookingId || bookingReference,
      transactionRef: paymentData?.transactionRef || bookingReference,
      status: getBookingStatusLabel(1),
      createdAt: new Date().toISOString(),
      fromAirport: selectedFromAirport?.City || '',
      toAirport: selectedToAirport?.City || '',
      fromCode: selectedFromAirport?.Code || '',
      toCode: selectedToAirport?.Code || '',
      flightId: getFlightId(selectedFlight),
      flightNumber: getFlightLabel(selectedFlight),
      airlineCode: selectedFlight?.airlineCode || '',
      departTime: selectedFlight?.departureTime || '',
      arriveTime: selectedFlight?.arrivalTime || '',
      seatClass: searchData.seatClass,
      passengerName: passengerForms[0]?.fullName || '',
      passengerCount: Number(searchData.passengers || 1),
      totalPrice,
    }

    const exists = bookingHistory.some(
      (item) =>
        item.bookingId === entry.bookingId ||
        (entry.transactionRef && item.transactionRef === entry.transactionRef)
    )

    if (exists) {
      setHistoryNotice('Vé này đã được lưu trong lịch sử.')
      return
    }

    const next = [entry, ...bookingHistory]
    persistBookingHistory(next)
    setHistoryNotice('Đã lưu vé vào lịch sử đặt chỗ.')
  }

  const totalPrice =
    getFlightPrice(selectedFlight, searchData.seatClass) +
    (tripType === 'roundtrip' && returnFlight
      ? getFlightPrice(returnFlight, searchData.seatClass)
      : 0)
  
  const selectedPromotion = useMemo(() => {
    if (!selectedPromotionId) return null
    return availablePromotions.find(
      (promotion) => String(getPromotionId(promotion)) === String(selectedPromotionId)
    ) || null
  }, [availablePromotions, selectedPromotionId])

  const appliedPromotion = selectedPromotionId ? selectedPromotion : null
  const discountAmount = appliedPromotion?.calculatedDiscount || 0
  const finalPrice = totalPrice - discountAmount

  useEffect(() => {
    if (!selectedFlight || totalPrice === 0) {
      setAvailablePromotions([])
      setSelectedPromotionId('')
      return
    }

    const fetchPromotions = async () => {
      setIsLoadingPromotion(true)
      try {
        const promotions = await getActivePromotions()
        const now = new Date()
        const validPromotions = (Array.isArray(promotions) ? promotions : [])
          .filter((promo) => {
            const startDate = promo.startDate ? new Date(promo.startDate) : null
            const endDate = promo.endDate ? new Date(promo.endDate) : null
            const validFrom = promo.validFrom ? new Date(promo.validFrom) : null
            const validTo = promo.validTo ? new Date(promo.validTo) : null
            const isActive = (!startDate || startDate <= now) && (!endDate || endDate >= now)
            const isWithinValidRange = (!validFrom || validFrom <= now) && (!validTo || validTo >= now)
            const meetsMinimum = !promo.minPurchaseAmount || totalPrice >= promo.minPurchaseAmount
            return (isActive || isWithinValidRange) && meetsMinimum
          })
          .map((promo) => ({
            ...promo,
            calculatedDiscount: calculatePromotionDiscount(promo, totalPrice),
          }))
          .sort((a, b) => b.calculatedDiscount - a.calculatedDiscount)

        const best = validPromotions[0] || null
        setAvailablePromotions(validPromotions)

        if (!selectedPromotionId || !validPromotions.some(
          (promo) => String(getPromotionId(promo)) === String(selectedPromotionId)
        )) {
          setSelectedPromotionId(best ? String(getPromotionId(best)) : '')
        }
      } catch (error) {
        console.error('Lỗi khi tìm promotion:', error)
        setAvailablePromotions([])
        setSelectedPromotionId('')
      } finally {
        setIsLoadingPromotion(false)
      }
    }

    fetchPromotions()
  }, [selectedFlight, totalPrice])

  const selectedFromAirport = airports.find((airport) => airport.Id === searchData.fromAirportId)
  const selectedToAirport = airports.find((airport) => airport.Id === searchData.toAirportId)

  const isAdmin = authUser?.role === 'admin'

  const getWeekdayName = (dayIndex) => {
    const names = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']
    return names[dayIndex] || 'Thứ'
  }

  const getWeekdayLabel = (value) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return ''
    const dayIndex = (date.getDay() + 6) % 7
    return getWeekdayName(dayIndex)
  }

  const formatShortDate = (value) => {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return ''
    return `${date.getDate()}/${date.getMonth() + 1}`
  }

  const currentListDate =
    tripType === 'roundtrip' && roundtripStep === 'return'
      ? searchData.returnDate
      : searchData.departDate

  const dateOptions = useMemo(() => {
    if (!currentListDate) return []
    const base = new Date(`${currentListDate}T00:00:00`)
    if (Number.isNaN(base.getTime())) return []
    const todayDate = new Date(`${today}T00:00:00`)
    const range = 3
    const items = []

    for (let offset = -range; offset <= range; offset += 1) {
      const date = addDays(base, offset)
      if (date < todayDate) continue
      const value = toLocalDateInputValue(date)
      items.push({
        value,
        weekday: getWeekdayLabel(date),
        label: formatShortDate(date),
      })
    }

    return items
  }, [currentListDate, today])

  const currentFlights =
    tripType === 'roundtrip' && roundtripStep === 'return'
      ? returnFlights
      : outboundFlights

  const lowestPriceForCurrentDate = useMemo(() => {
    if (!Array.isArray(currentFlights) || currentFlights.length === 0) return null
    const prices = currentFlights
      .map((flight) => (flight?.pricesByClass?.[searchData.seatClass] ?? flight?.price) / passengerDivisor)
      .filter((value) => Number.isFinite(value))
    if (prices.length === 0) return null
    return Math.min(...prices)
  }, [currentFlights, searchData.seatClass, passengerDivisor])

  const filteredFlights = useMemo(() => {
    const now = new Date()
    return currentFlights.filter((flight) => {
      const departureDate = new Date(flight.departureTime)
      if (Number.isNaN(departureDate.getTime()) || departureDate < now) return false
      const pricesByClass = flight.pricesByClass || {}
      const flightPrice = (pricesByClass[searchData.seatClass] || 0) / passengerDivisor
      const byPrice = flightPrice <= filters.maxPrice
      const byClass = true

      let byTime = true
      const departHour = new Date(flight.departureTime).getHours()
      if (filters.timeSlot === 'morning') byTime = departHour < 12
      if (filters.timeSlot === 'afternoon') byTime = departHour >= 12 && departHour < 18
      if (filters.timeSlot === 'evening') byTime = departHour >= 18

      return byPrice && byClass && byTime
    })
  }, [filters, currentFlights, searchData.seatClass, totalPassengers, passengerDivisor])

  const performSearch = async (departDateOverride) => {
    const nextDepartDate = departDateOverride || searchData.departDate
    const nextReturnDate =
      tripType === 'roundtrip' && nextDepartDate > searchData.returnDate
        ? nextDepartDate
        : searchData.returnDate

    const nextSearchData = {
      ...searchData,
      departDate: nextDepartDate,
      returnDate: nextReturnDate,
    }

    if (nextSearchData.fromAirportId === nextSearchData.toAirportId) {
      setApiError('Điểm đi và điểm đến phải khác nhau')
      return
    }

    if (!nextSearchData.departDate || nextSearchData.departDate < today) {
      setApiError('Ngày đi phải từ hôm nay trở đi')
      return
    }

    if (tripType === 'roundtrip' && !nextSearchData.returnDate) {
      setApiError('Vui lòng chọn ngày về')
      return
    }

    if (tripType === 'roundtrip' && nextSearchData.returnDate < nextSearchData.departDate) {
      setApiError('Ngày về phải sau hoặc bằng ngày đi')
      return
    }

    setSearchData(nextSearchData)
    setIsLoadingFlights(true)
    setApiError('')
    try {
      const results = await searchFlights({
        departureAirportId: nextSearchData.fromAirportId,
        arrivalAirportId: nextSearchData.toAirportId,
        departureDate: nextSearchData.departDate,
        returnDate: null,
        passengerCount: totalPassengers,
        seatPreference: seatClassMap[nextSearchData.seatClass] || null,
      })

      setSelectedFlight(null)
      setReturnFlight(null)
      setBookingId(null)
      setPaymentData(null)
      const nextFlights = Array.isArray(results) ? results : []
      setOutboundFlights(nextFlights)
      setReturnFlights([])
      setRoundtripStep('outbound')
      const resultPrices = nextFlights
        .map((flight) => flight?.pricesByClass?.[nextSearchData.seatClass] ?? flight?.price)
        .filter((value) => Number.isFinite(value))
      const highestResultPrice = resultPrices.length > 0 ? Math.max(...resultPrices) : null
      setFilters((prev) => ({
        ...prev,
        seatClass: nextSearchData.seatClass,
        maxPrice: Number.isFinite(highestResultPrice) ? Math.max(prev.maxPrice, highestResultPrice) : prev.maxPrice,
      }))
      setScreen('list')
    } catch (error) {
      setApiError(error.message || 'Lỗi tìm kiếm chuyến bay. Vui lòng thử lại.')
    } finally {
      setIsLoadingFlights(false)
    }
  }

  const loadReturnFlights = async (originId, destinationId, returnDate) => {
    setIsLoadingFlights(true)
    setApiError('')
    try {
      const results = await searchFlights({
        departureAirportId: originId,
        arrivalAirportId: destinationId,
        departureDate: returnDate,
        returnDate: null,
        passengerCount: totalPassengers,
        seatPreference: seatClassMap[searchData.seatClass] || null,
      })
      const nextFlights = Array.isArray(results) ? results : []
      setReturnFlights(nextFlights)
      const resultPrices = nextFlights
        .map((flight) => flight?.pricesByClass?.[searchData.seatClass] ?? flight?.price)
        .filter((value) => Number.isFinite(value))
      const highestResultPrice = resultPrices.length > 0 ? Math.max(...resultPrices) : null
      setFilters((prev) => ({
        ...prev,
        seatClass: searchData.seatClass,
        maxPrice: Number.isFinite(highestResultPrice) ? Math.max(prev.maxPrice, highestResultPrice) : prev.maxPrice,
      }))
    } catch (error) {
      setApiError(error.message || 'Lỗi tìm kiếm chuyến bay. Vui lòng thử lại.')
    } finally {
      setIsLoadingFlights(false)
    }
  }

  useEffect(() => {
    let cancelled = false

    if (!bookingId) return undefined

    const initiatePaymentFlow = async () => {
      try {
        const paymentResponse = await initiatePayment(bookingId, 'VNPAY')
        if (!cancelled) {
          setPaymentData(paymentResponse)
        }
      } catch (error) {
        if (!cancelled) setApiError(error.message || 'Lỗi khởi tạo thanh toán')
      }
    }

    initiatePaymentFlow()
    return () => {
      cancelled = true
    }
  }, [bookingId])

  useEffect(() => {
    if (screen !== 'history') return
    if (!authUser) {
      setScreen('login')
      return
    }

    loadBookingHistory()
  }, [screen, authUser])

  // Load flight definitions khi vào màn hình templates
  useEffect(() => {
    let cancelled = false

    if (screen !== 'templates' || !isAdmin) return undefined

    const fetchFlightDefinitions = async () => {
      setIsLoadingFlightDefinitions(true)
      setApiError('')
      try {
        const definitions = await getFlightDefinitions(true)
        if (!cancelled) {
          setFlightDefinitions(Array.isArray(definitions) ? definitions : [])
        }
      } catch (error) {
        if (!cancelled) {
          setApiError(error.message || 'Không thể tải danh sách flight definitions')
        }
      } finally {
        if (!cancelled) {
          setIsLoadingFlightDefinitions(false)
        }
      }
    }

    fetchFlightDefinitions()

    return () => {
      cancelled = true
    }
  }, [screen, isAdmin])

  useEffect(() => {
    if (screen !== 'promotions' || !isAdmin) return
    loadAdminPromotions()
  }, [screen, isAdmin])

  useEffect(() => {
    if (screen !== 'flights' || !isAdmin) return
    loadAdminFlights()
    loadAdminRoutes()
    getAircrafts()
      .then((data) => setAircrafts(Array.isArray(data) ? data : []))
      .catch(() => setFlightAdminError('Không thể tải danh sách máy bay.'))
  }, [screen, isAdmin])

  // Load aircrafts khi vào màn hình templates
  useEffect(() => {
    let cancelled = false

    if (screen !== 'templates' || !isAdmin) return undefined

    const fetchAircrafts = async () => {
      console.log('🔄 Loading aircrafts...')
      setIsLoadingAircrafts(true)
      try {
        const aircraftList = await getAircrafts()
        console.log('✅ Aircrafts loaded:', aircraftList)
        if (!cancelled) {
          setAircrafts(Array.isArray(aircraftList) ? aircraftList : [])
        }
      } catch (error) {
        if (!cancelled) {
          console.error('❌ Không thể tải danh sách máy bay:', error)
        }
      } finally {
        if (!cancelled) {
          setIsLoadingAircrafts(false)
        }
      }
    }

    fetchAircrafts()

    return () => {
      cancelled = true
    }
  }, [screen, isAdmin])

  // Load templates khi vào màn hình templates
  useEffect(() => {
    let cancelled = false

    if (screen !== 'templates' || !isAdmin) return undefined

    const fetchTemplates = async () => {
      setIsLoadingTemplates(true)
      setApiError('')
      try {
        const templates = await getFlightTemplates()
        if (!cancelled) {
          setFlightTemplates(Array.isArray(templates) ? templates : [])
        }
      } catch (error) {
        if (!cancelled) {
          setApiError(error.message || 'Không thể tải danh sách templates')
        }
      } finally {
        if (!cancelled) {
          setIsLoadingTemplates(false)
        }
      }
    }

    fetchTemplates()

    return () => {
      cancelled = true
    }
  }, [screen, isAdmin])

  const renderHeader = () => (
    <header className="mb-8 rounded-2xl bg-[#1E40AF] px-5 py-5 text-white shadow-lg shadow-blue-200 md:px-8">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-xs uppercase tracking-[0.18em] text-blue-100">Flight Booking Demo</p>
          <h1 className="title-font mt-1 text-2xl font-bold md:text-3xl">Hệ thống đặt vé máy bay</h1>
          <p className="mt-1 text-sm text-blue-100">Demo luồng đặt vé cơ bản với dữ liệu giả lập</p>
        </div>
        <div>
          {authUser ? (
            <div className="flex items-center gap-3">
              <div
                className="relative"
                onMouseEnter={() => setIsNotificationPanelOpen(true)}
                onMouseLeave={() => setIsNotificationPanelOpen(false)}
              >
                <button
                  type="button"
                  onClick={() => setIsNotificationPanelOpen((prev) => !prev)}
                  className="relative flex h-10 w-10 items-center justify-center rounded-xl bg-white/15 text-white transition hover:bg-white/25"
                  aria-label="Thông báo"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    className="h-5 w-5"
                  >
                    <path d="M18 8a6 6 0 10-12 0c0 7-3 7-3 7h18s-3 0-3-7" />
                    <path d="M13.73 21a2 2 0 01-3.46 0" />
                  </svg>
                  {unreadNotificationCount > 0 && (
                    <span className="absolute -right-1 -top-1 min-w-[20px] rounded-full bg-rose-500 px-1.5 py-0.5 text-[11px] font-semibold text-white">
                      {unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}
                    </span>
                  )}
                </button>
                <div
                  className={`absolute right-0 top-full mt-3 w-80 rounded-2xl bg-white text-slate-900 shadow-xl ring-1 ring-black/5 transition ${
                    isNotificationPanelOpen
                      ? 'pointer-events-auto translate-y-0 opacity-100'
                      : 'pointer-events-none -translate-y-1 opacity-0'
                  }`}
                >
                  <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
                    <div className="text-sm font-semibold text-slate-900">Thông báo</div>
                    {unreadNotificationCount > 0 && (
                      <button
                        type="button"
                        onClick={handleMarkAllNotificationsRead}
                        className="text-xs font-semibold text-blue-600 hover:text-blue-700"
                      >
                        Đánh dấu tất cả đã đọc
                      </button>
                    )}
                  </div>
                  <div className="max-h-80 overflow-auto">
                    {isLoadingNotifications && (
                      <div className="px-4 py-5 text-sm text-slate-500">Đang tải thông báo...</div>
                    )}
                    {!isLoadingNotifications && notificationError && (
                      <div className="px-4 py-4 text-sm text-rose-600">{notificationError}</div>
                    )}
                    {!isLoadingNotifications && !notificationError && sortedNotifications.length === 0 && (
                      <div className="px-4 py-5 text-sm text-slate-500">Chưa có thông báo nào.</div>
                    )}
                    {!isLoadingNotifications && !notificationError && sortedNotifications.length > 0 && (
                      <div className="divide-y divide-slate-100">
                        {sortedNotifications.map((item) => {
                          const title = item?.subject || item?.category || 'Thông báo'
                          const message = item?.message || item?.errorMessage || item?.type || ''
                          const timestamp = formatDateTime(item?.createdAt || item?.sentAt)
                          const isRead = Boolean(item?.isRead)

                          return (
                            <button
                              type="button"
                              key={item?.notificationId || `${title}-${timestamp}`}
                              onClick={() => {
                                if (!isRead) {
                                  handleMarkNotificationRead(item?.notificationId)
                                }
                              }}
                              className={`flex w-full flex-col gap-1 px-4 py-3 text-left transition hover:bg-slate-50 ${
                                isRead ? 'text-slate-500' : 'text-slate-900'
                              }`}
                            >
                              <div className="flex items-center justify-between gap-2">
                                <span className="text-sm font-semibold">{title}</span>
                                {!isRead && (
                                  <span className="h-2 w-2 flex-none rounded-full bg-blue-600" />
                                )}
                              </div>
                              {message && <div className="text-xs text-slate-500">{message}</div>}
                              {timestamp && <div className="text-[11px] text-slate-400">{timestamp}</div>}
                            </button>
                          )
                        })}
                      </div>
                    )}
                  </div>
                </div>
              </div>
              <div className="text-sm text-blue-100">Xin chào, {authUser.fullName || authUser.email}</div>
              <button
                type="button"
                onClick={logout}
                className="rounded-xl bg-red-500 px-3 py-2 text-sm font-semibold text-white hover:bg-red-600"
              >
                Đăng xuất
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  )

  const renderLogin = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-6 shadow-lg shadow-slate-200">
      <h2 className="title-font mb-6 text-center text-2xl font-bold text-slate-900">Đăng nhập</h2>
      <div className="space-y-4">
        <div>
          <Label>Email</Label>
          <Input
            type="email"
            placeholder="you@email.com"
            value={loginData.email}
            onChange={(e) => setLoginData((prev) => ({ ...prev, email: e.target.value }))}
          />
        </div>
        <div>
          <Label>Password</Label>
          <Input
            type="password"
            placeholder="••••••••"
            value={loginData.password}
            onChange={(e) => setLoginData((prev) => ({ ...prev, password: e.target.value }))}
          />
        </div>
      </div>
      <button
        type="button"
        onClick={async () => {
          if (!loginData.email || !loginData.password) {
            setApiError('Vui lòng nhập email và mật khẩu')
            return
          }

          setIsLoggingIn(true)
          setApiError('')
          try {
            const auth = await login(loginData.email, loginData.password)
            const token = auth?.token || ''
            const roleFromToken = getRoleFromToken(token)
            const normalizedRole = roleFromToken === 'admin' ? 'admin' : 'user'

            if (token) {
              setAuthToken(token)
            }

            setAuthUser({
              email: auth?.email || loginData.email,
              fullName: auth?.fullName || '',
              role: normalizedRole,
            })
            setScreen('search')
          } catch (error) {
            setApiError(error.message || 'Đăng nhập thất bại')
          } finally {
            setIsLoggingIn(false)
          }
        }}
        disabled={isLoggingIn}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
      >
        {isLoggingIn ? 'Đang đăng nhập...' : 'Đăng nhập'}
      </button>
      {apiError && (
        <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">{apiError}</div>
      )}
      <p className="mt-4 text-center text-sm text-slate-500">
        Chưa có tài khoản?{' '}
        <button
          type="button"
          onClick={() => {
            setApiError('')
            setScreen('register')
          }}
          className="font-semibold text-[#1E40AF] hover:underline"
        >
          Đăng ký
        </button>
      </p>
      <p className="mt-2 text-center text-sm">
        <button
          type="button"
          onClick={() => {
            setApiError('')
            setForgotEmail(loginData.email || '')
            setForgotStep('email')
            setForgotError('')
            setForgotLoading(false)
            setScreen('forgot-password')
          }}
          className="text-slate-400 hover:text-[#1E40AF] hover:underline text-xs"
        >
          Quên mật khẩu?
        </button>
      </p>
    </div>
  )

  const renderRegister = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-6 shadow-lg shadow-slate-200">
      <h2 className="title-font mb-6 text-center text-2xl font-bold text-slate-900">Tạo tài khoản</h2>
      <div className="space-y-4">
        <div>
          <Label>Họ và tên</Label>
          <Input
            type="text"
            placeholder="Nguyễn Văn A"
            value={registerData.fullName}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, fullName: e.target.value }))}
          />
        </div>
        <div>
          <Label>Email</Label>
          <Input
            type="email"
            placeholder="you@email.com"
            value={registerData.email}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, email: e.target.value }))}
          />
        </div>
        <div>
          <Label>Số điện thoại</Label>
          <Input
            type="tel"
            placeholder="0900000000"
            value={registerData.phone}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, phone: e.target.value }))}
          />
        </div>
        <div>
          <Label>Mật khẩu</Label>
          <Input
            type="password"
            placeholder="••••••••"
            value={registerData.password}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, password: e.target.value }))}
          />
        </div>
        <div>
          <Label>Xác nhận mật khẩu</Label>
          <Input
            type="password"
            placeholder="••••••••"
            value={registerData.confirmPassword}
            onChange={(e) =>
              setRegisterData((prev) => ({ ...prev, confirmPassword: e.target.value }))
            }
          />
        </div>
      </div>
      <button
        type="button"
        onClick={async () => {
          if (!registerData.fullName || !registerData.email || !registerData.phone || !registerData.password) {
            setRegisterError('Vui lòng nhập đầy đủ thông tin')
            return
          }

          if (registerData.password !== registerData.confirmPassword) {
            setRegisterError('Mật khẩu xác nhận không khớp')
            return
          }

          setIsRegistering(true)
          setRegisterError('')
          try {
            const auth = await registerAccount({
              email: registerData.email,
              password: registerData.password,
              fullName: registerData.fullName,
              phone: registerData.phone,
            })
            const token = auth?.token || ''
            const roleFromToken = getRoleFromToken(token)
            const normalizedRole = roleFromToken === 'admin' ? 'admin' : 'user'

            if (token) {
              setAuthToken(token)
            }

            setAuthUser({
              email: auth?.email || registerData.email,
              fullName: auth?.fullName || registerData.fullName,
              role: normalizedRole,
            })
            setScreen('search')
          } catch (error) {
            setRegisterError(error.message || 'Đăng ký thất bại')
          } finally {
            setIsRegistering(false)
          }
        }}
        disabled={isRegistering}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
      >
        {isRegistering ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}
      </button>
      {registerError && (
        <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">{registerError}</div>
      )}
      <p className="mt-4 text-center text-sm text-slate-500">
        Đã có tài khoản?{' '}
        <button
          type="button"
          onClick={() => {
            setRegisterError('')
            setScreen('login')
          }}
          className="font-semibold text-[#1E40AF] hover:underline"
        >
          Đăng nhập
        </button>
      </p>
    </div>
  )

  // ===== RENDER: QUEN MAT KHAU =====
  const renderForgotPassword = () => (
    <div className="mx-auto w-full max-w-md">
      <div className="rounded-2xl bg-white p-8 shadow-lg shadow-slate-200">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
            <svg className="h-8 w-8 text-[#1E40AF]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-bold text-slate-900">Quên mật khẩu?</h2>
          <p className="mt-1 text-sm text-slate-500">Nhập email để nhận mã đặt lại mật khẩu</p>
        </div>
        {!forgotSuccess ? (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1">Email</label>
              <input
                type="email"
                placeholder="you@email.com"
                value={forgotEmail}
                onChange={(e) => setForgotEmail(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleForgotPassword() }}
                className="w-full rounded-xl border border-slate-300 px-4 py-3 text-sm focus:border-[#1E40AF] focus:outline-none focus:ring-2 focus:ring-blue-100"
              />
            </div>
            {forgotError && (
              <div className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{forgotError}</div>
            )}
            <button
              type="button"
              disabled={forgotLoading}
              onClick={handleForgotPassword}
              className="w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
            >
              {forgotLoading ? 'Đang gửi...' : 'Gửi mã đặt lại'}
            </button>
            <p className="text-xs text-center text-slate-400">
              Email sẽ được gửi dù địa chỉ có tồn tại hay không (để bảo mật).
            </p>
          </div>
        ) : (
          <div className="space-y-4 text-center">
            <div className="rounded-xl bg-green-50 p-5">
              <div className="text-3xl mb-2">✉️</div>
              <p className="font-semibold text-green-800">Email đã được gửi!</p>
              <p className="mt-1 text-sm text-green-600">
                Kiểm tra hộp thư của <strong>{forgotEmail}</strong> để lấy mã đặt lại mật khẩu.
              </p>
              <p className="mt-1 text-xs text-green-500">Mã có hiệu lực trong 1 giờ.</p>
            </div>
            <button
              type="button"
              onClick={() => {
                setResetCode('')
                setResetNewPassword('')
                setResetConfirmPassword('')
                setResetSuccess(false)
                setForgotError('')
                setScreen('reset-password')
              }}
              className="w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800"
            >
              Nhập mã đặt lại mật khẩu →
            </button>
          </div>
        )}
        <p className="mt-5 text-center text-sm text-slate-500">
          <button
            type="button"
            onClick={() => { setForgotSuccess(false); setForgotError(''); setScreen('login') }}
            className="font-semibold text-[#1E40AF] hover:underline"
          >
            ← Quay lại đăng nhập
          </button>
        </p>
      </div>
    </div>
  )

  // ===== RENDER: DAT LAI MAT KHAU =====
  const renderResetPassword = () => (
    <div className="mx-auto w-full max-w-md">
      <div className="rounded-2xl bg-white p-8 shadow-lg shadow-slate-200">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-indigo-100">
            <svg className="h-8 w-8 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-bold text-slate-900">Đặt lại mật khẩu</h2>
          <p className="mt-1 text-sm text-slate-500">Nhập mã đã gửi và mật khẩu mới của bạn</p>
        </div>
        {!resetSuccess ? (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1">Mã xác nhận</label>
              <input
                type="text"
                placeholder="Nhập mã từ email..."
                value={resetCode}
                onChange={(e) => setResetCode(e.target.value)}
                className="w-full rounded-xl border border-slate-300 px-4 py-3 text-sm tracking-widest font-mono focus:border-[#1E40AF] focus:outline-none focus:ring-2 focus:ring-blue-100"
              />
            </div>
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1">Mật khẩu mới</label>
              <input
                type="password"
                placeholder="••••••••"
                value={resetNewPassword}
                onChange={(e) => setResetNewPassword(e.target.value)}
                className="w-full rounded-xl border border-slate-300 px-4 py-3 text-sm focus:border-[#1E40AF] focus:outline-none focus:ring-2 focus:ring-blue-100"
              />
            </div>
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1">Xác nhận mật khẩu</label>
              <input
                type="password"
                placeholder="••••••••"
                value={resetConfirmPassword}
                onChange={(e) => setResetConfirmPassword(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleResetPassword() }}
                className="w-full rounded-xl border border-slate-300 px-4 py-3 text-sm focus:border-[#1E40AF] focus:outline-none focus:ring-2 focus:ring-blue-100"
              />
            </div>
            {forgotError && (
              <div className="rounded-xl bg-red-50 p-3 text-sm text-red-700">{forgotError}</div>
            )}
            <button
              type="button"
              disabled={forgotLoading}
              onClick={handleResetPassword}
              className="w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
            >
              {forgotLoading ? 'Đang xử lý...' : 'Đặt lại mật khẩu'}
            </button>
          </div>
        ) : (
          <div className="space-y-4 text-center">
            <div className="rounded-xl bg-green-50 p-5">
              <div className="text-3xl mb-2">✅</div>
              <p className="font-semibold text-green-800">Mật khẩu đã được đặt lại!</p>
              <p className="mt-1 text-sm text-green-600">Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.</p>
            </div>
            <button
              type="button"
              onClick={() => {
                setForgotSuccess(false)
                setResetSuccess(false)
                setForgotError('')
                setResetCode('')
                setResetNewPassword('')
                setResetConfirmPassword('')
                setScreen('login')
              }}
              className="w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800"
            >
              Đăng nhập ngay
            </button>
          </div>
        )}
        <p className="mt-5 text-center text-sm text-slate-500">
          Chưa có mã?{' '}
          <button
            type="button"
            onClick={() => { setForgotError(''); setForgotSuccess(false); setScreen('forgot-password') }}
            className="font-semibold text-[#1E40AF] hover:underline"
          >
            Gửi lại email
          </button>
        </p>
      </div>
    </div>
  )

  const renderSearch = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <h2 className="title-font mb-6 text-xl font-bold text-slate-900 md:text-2xl">
        Tìm kiếm chuyến bay
      </h2>
      <div className="mb-5 inline-flex rounded-xl bg-slate-100 p-1">
        {[
          { key: 'oneway', label: 'Một chiều' },
          { key: 'roundtrip', label: 'Khứ hồi' },
        ].map((option) => (
          <button
            type="button"
            key={option.key}
            onClick={() => setTripType(option.key)}
            className={`rounded-lg px-4 py-2 text-sm font-semibold transition ${
              tripType === option.key
                ? 'bg-[#1E40AF] text-white shadow'
                : 'text-slate-600 hover:text-[#1E40AF]'
            }`}
          >
            {option.label}
          </button>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <Label>Điểm đi</Label>
          <Select
            value={searchData.fromAirportId}
            onChange={(e) =>
              setSearchData((prev) => ({ ...prev, fromAirportId: Number(e.target.value) }))
            }
          >
            {airports.map((airport) => (
              <option key={airport.Id} value={airport.Id}>
                {airport.City} - {airport.Name} ({airport.Code})
              </option>
            ))}
          </Select>
        </div>
        <div>
          <Label>Điểm đến</Label>
          <Select
            value={searchData.toAirportId}
            onChange={(e) =>
              setSearchData((prev) => ({ ...prev, toAirportId: Number(e.target.value) }))
            }
          >
            {airports.map((airport) => (
              <option key={airport.Id} value={airport.Id}>
                {airport.City} - {airport.Name} ({airport.Code})
              </option>
            ))}
          </Select>
        </div>
        <div>
          <Label>Ngày đi</Label>
          <Input
            type="date"
            min={today}
            value={searchData.departDate}
            onChange={(e) =>
              setSearchData((prev) => ({
                ...prev,
                departDate: e.target.value,
                returnDate:
                  tripType === 'roundtrip' && e.target.value > prev.returnDate
                    ? e.target.value
                    : prev.returnDate,
              }))
            }
          />
        </div>

        {tripType === 'roundtrip' && (
          <div>
            <Label>Ngày về</Label>
            <Input
              type="date"
              min={searchData.departDate || today}
              value={searchData.returnDate}
              onChange={(e) => setSearchData((prev) => ({ ...prev, returnDate: e.target.value }))}
            />
          </div>
        )}

        <div>
          <Label>Hành khách</Label>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold text-slate-900">
                  {totalPassengers} hành khách
                </p>
                <p className="text-xs text-slate-500">
                  Người lớn, trẻ em, trẻ sơ sinh
                </p>
              </div>
            </div>
            <div className="mt-4 space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-700">Người lớn</p>
                  <p className="text-xs text-slate-500">Từ 12 tuổi trở lên</p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        adult: Math.max(1, prev.adult - 1),
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    −
                  </button>
                  <span className="w-6 text-center font-semibold">{passengerCounts.adult}</span>
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        adult: prev.adult + 1,
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    +
                  </button>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-700">Trẻ em</p>
                  <p className="text-xs text-slate-500">2 - 12 tuổi</p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        child: Math.max(0, prev.child - 1),
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    −
                  </button>
                  <span className="w-6 text-center font-semibold">{passengerCounts.child}</span>
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        child: prev.child + 1,
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    +
                  </button>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-slate-700">Em bé</p>
                  <p className="text-xs text-slate-500">0 - 2 tuổi</p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        infant: Math.max(0, prev.infant - 1),
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    −
                  </button>
                  <span className="w-6 text-center font-semibold">{passengerCounts.infant}</span>
                  <button
                    type="button"
                    onClick={() =>
                      setPassengerCounts((prev) => ({
                        ...prev,
                        infant: prev.infant + 1,
                      }))
                    }
                    className="rounded-full border border-slate-200 px-3 py-1 text-sm font-semibold"
                  >
                    +
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div>
          <Label>Hạng ghế</Label>
          <Select
            value={searchData.seatClass}
            onChange={(e) => setSearchData((prev) => ({ ...prev, seatClass: e.target.value }))}
          >
            <option value="Economy">Economy</option>
            <option value="Business">Business</option>
          </Select>
        </div>
      </div>

      <button
        type="button"
        onClick={() => performSearch()}
        disabled={isLoadingFlights}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50 md:w-auto"
      >
        {isLoadingFlights ? 'Đang tìm kiếm...' : 'Tìm kiếm'}
      </button>
      {apiError && (
        <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">
          {apiError}
        </div>
      )}
    </div>
  )

  const renderFlightList = () => (
    <div className="grid gap-5 lg:grid-cols-[260px_1fr]">
      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">Bộ lọc</h3>
        <div className="space-y-4">
          <div>
            <Label>Giá tối đa: {formatCurrency(filters.maxPrice)}</Label>
            <Input
              type="range"
              min="1000000"
              max="3000000"
              step="100000"
              value={filters.maxPrice}
              onChange={(e) =>
                setFilters((prev) => ({ ...prev, maxPrice: Number(e.target.value) }))
              }
            />
          </div>
          <div>
            <Label>Khung giờ bay</Label>
            <Select
              value={filters.timeSlot}
              onChange={(e) => setFilters((prev) => ({ ...prev, timeSlot: e.target.value }))}
            >
              <option value="all">Tất cả</option>
              <option value="morning">Sáng (00:00 - 11:59)</option>
              <option value="afternoon">Chiều (12:00 - 17:59)</option>
              <option value="evening">Tối (18:00 - 23:59)</option>
            </Select>
          </div>
          <div>
            <Label>Hạng ghế</Label>
            <Select
              value={filters.seatClass}
              onChange={(e) => setFilters((prev) => ({ ...prev, seatClass: e.target.value }))}
            >
              <option value="all">Tất cả</option>
              <option value="Economy">Economy</option>
              <option value="Business">Business</option>
            </Select>
          </div>
        </div>
      </aside>

      <section className="space-y-4">
        <div className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-600">
          {tripType === 'roundtrip'
            ? roundtripStep === 'outbound'
              ? 'Bước 1: Chọn chuyến đi (A → B)'
              : 'Bước 2: Chọn chuyến về (B → A)'
            : 'Chọn chuyến bay'}
        </div>
        {dateOptions.length > 0 && (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {dateOptions.map((option) => {
              const isActive = option.value === currentListDate
              const priceLabel = isActive && Number.isFinite(lowestPriceForCurrentDate)
                ? formatCurrency(lowestPriceForCurrentDate)
                : '—'

              return (
                <button
                  key={option.value}
                  type="button"
                  disabled={isLoadingFlights || isActive}
                  onClick={() => {
                    if (isActive) return
                    if (tripType === 'roundtrip' && roundtripStep === 'return') {
                      setSearchData((prev) => ({ ...prev, returnDate: option.value }))
                      loadReturnFlights(searchData.toAirportId, searchData.fromAirportId, option.value)
                    } else {
                      performSearch(option.value)
                    }
                  }}
                  className={`min-w-[130px] rounded-2xl border px-4 py-3 text-left text-sm transition ${
                    isActive
                      ? 'border-[#1E40AF] bg-blue-50 text-[#1E40AF]'
                      : 'border-slate-200 bg-white text-slate-700 hover:border-[#1E40AF]'
                  } ${isLoadingFlights ? 'opacity-60' : ''}`}
                >
                  <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {option.weekday}
                  </p>
                  <p className="mt-1 text-sm font-semibold">
                    {option.label}
                  </p>
                  <p className="mt-1 text-sm font-bold text-[#1E40AF]">
                    {priceLabel}
                  </p>
                </button>
              )
            })}
          </div>
        )}
        {filteredFlights.map((flight) => {
          const flightPrice = (flight.pricesByClass?.[searchData.seatClass] || 0) / passengerDivisor

          return (
            <article
              key={getFlightId(flight) || flight.flightNumber || flight.departureTime}
              className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 transition hover:-translate-y-0.5"
            >
              <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                <div>
                  <p className="text-sm font-semibold text-[#1E40AF]">{flight.airlineCode} {flight.aircraftModel}</p>
                  <p className="mt-1 text-lg font-bold text-slate-900">
                    {formatTime(flight.departureTime)} - {formatTime(flight.arrivalTime)}
                  </p>
                  <p className="text-sm text-slate-500">
                    {formatDuration(flight.durationMinutes)} | {searchData.seatClass}
                  </p>
                  <p className="text-sm text-slate-500">
                    ID chuyến bay: {getFlightId(flight)} · Mã hiển thị: {getFlightLabel(flight)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-xs text-slate-500">Ngày đi: {formatDateTime(flight.departureTime)}</p>
                  <p className="text-xs text-slate-500">Giá 1 vé</p>
                  <p className="text-xl font-bold text-slate-900">{formatCurrency(flightPrice)}</p>
                  <button
                    type="button"
                    onClick={() => {
                      const seatClassId = seatClassMap[searchData.seatClass]
                      if (seatClassId) {
                        loadServices(seatClassId)
                      } else {
                        setServiceError('Khong xac dinh duoc hang ghe de tai dich vu')
                        setServices([])
                      }
                      if (tripType === 'roundtrip' && roundtripStep === 'outbound') {
                        setSelectedFlight(flight)
                        setReturnFlight(null)
                        setRoundtripStep('return')
                        loadReturnFlights(searchData.toAirportId, searchData.fromAirportId, searchData.returnDate)
                      } else {
                        if (tripType === 'roundtrip') {
                          setReturnFlight(flight)
                        } else {
                          setSelectedFlight(flight)
                        }
                        setScreen('passenger')
                      }
                    }}
                    className="mt-2 rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
                  >
                    Chọn
                  </button>
                </div>
              </div>
            </article>
          )
        })}

        {!isLoadingFlights && filteredFlights.length === 0 && outboundFlights.length > 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
            Không tìm thấy chuyến bay phù hợp bộ lọc.
          </div>
        )}

        {!isLoadingFlights && outboundFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
            Vui lòng tìm kiếm chuyến bay trước.
          </div>
        )}
      </section>
    </div>
  )

  const renderPassenger = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h2 className="title-font mb-6 text-xl font-bold text-slate-900">Thông tin hành khách</h2>
        <div className="space-y-6">
          {passengerForms.map((passenger, index) => {
            const isAdult = passenger.type === 'adult'
            const isChild = passenger.type === 'child'
            return (
              <div key={`passenger-${index}`} className="rounded-2xl border border-slate-200 p-4">
                <div className="mb-4 flex items-center justify-between">
                  <p className="text-sm font-semibold text-slate-700">Hành khách {index + 1}</p>
                  <span className="rounded-full bg-blue-50 px-3 py-1 text-xs font-medium text-blue-700">
                    {isAdult ? 'Người lớn (>= 12 tuổi)' : isChild ? 'Trẻ em (2 - 12 tuổi)' : 'Trẻ sơ sinh (0 - 2 tuổi)'}
                  </span>
                </div>

                {savedPassengers.length > 0 && (
                  <div className="mb-4">
                    <Label>Chọn hành khách đã lưu</Label>
                    <Select
                      value={passenger.savedPassengerId}
                      onChange={(e) => {
                        const selectedId = Number(e.target.value)
                        const selected = savedPassengers.find((item) => item.id === selectedId)
                        if (selected) {
                          applySavedPassengerToForm(index, selected)
                        } else {
                          setPassengerForms((prev) =>
                            prev.map((item, idx) =>
                              idx === index ? { ...item, savedPassengerId: '' } : item
                            )
                          )
                        }
                      }}
                    >
                      <option value="">-- Chọn từ danh bạ --</option>
                      {savedPassengers.map((item) => {
                        const label = [item.lastName, item.firstName].filter(Boolean).join(' ') || '---'
                        return (
                          <option key={item.id} value={item.id}>
                            {label}{item.email ? ` · ${item.email}` : ''}
                          </option>
                        )
                      })}
                    </Select>
                  </div>
                )}

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <Label>Họ tên</Label>
                    <Input
                      placeholder="Nguyễn Văn A"
                      value={passenger.fullName}
                      onChange={(e) =>
                        setPassengerForms((prev) =>
                          prev.map((item, idx) =>
                            idx === index ? { ...item, fullName: e.target.value } : item
                          )
                        )
                      }
                    />
                  </div>

                  <div>
                    <Label>Ngày sinh</Label>
                    <Input
                      type="date"
                      value={passenger.dob}
                      onChange={(e) =>
                        setPassengerForms((prev) =>
                          prev.map((item, idx) =>
                            idx === index ? { ...item, dob: e.target.value } : item
                          )
                        )
                      }
                    />
                    {isChild && (
                      <p className="mt-1 text-xs text-slate-500">Trẻ em: từ 2 đến 12 tuổi</p>
                    )}
                    {!isAdult && !isChild && (
                      <p className="mt-1 text-xs text-slate-500">Trẻ sơ sinh: dưới 2 tuổi</p>
                    )}
                  </div>
                  <div>
                    <Label>Giới tính</Label>
                    <div className="flex flex-wrap gap-3 pt-2">
                      {['Nam', 'Nữ', 'Khác'].map((genderOption) => (
                        <label key={genderOption} className="inline-flex items-center gap-2 text-sm text-slate-700">
                          <input
                            type="radio"
                            name={`gender-${index}`}
                            value={genderOption}
                            checked={passenger.gender === genderOption}
                            onChange={(e) =>
                              setPassengerForms((prev) =>
                                prev.map((item, idx) =>
                                  idx === index ? { ...item, gender: e.target.value } : item
                                )
                              )
                            }
                          />
                          {genderOption}
                        </label>
                      ))}
                    </div>
                  </div>
                  {isAdult && (
                    <div className="md:col-span-2">
                      <Label>CCCD / Passport</Label>
                      <Input
                        placeholder="012345678901"
                        value={passenger.document}
                        onChange={(e) =>
                          setPassengerForms((prev) =>
                            prev.map((item, idx) =>
                              idx === index ? { ...item, document: e.target.value } : item
                            )
                          )
                        }
                      />
                    </div>
                  )}
                  {isAdult && (
                    <>
                      <div>
                        <Label>Email</Label>
                        <Input
                          type="email"
                          placeholder="email@example.com"
                          value={passenger.email}
                          onChange={(e) =>
                            setPassengerForms((prev) =>
                              prev.map((item, idx) =>
                                idx === index ? { ...item, email: e.target.value } : item
                              )
                            )
                          }
                        />
                      </div>
                      <div>
                        <Label>Số điện thoại</Label>
                        <Input
                          placeholder="0901234567"
                          value={passenger.phone}
                          onChange={(e) =>
                            setPassengerForms((prev) =>
                              prev.map((item, idx) =>
                                idx === index ? { ...item, phone: e.target.value } : item
                              )
                            )
                          }
                        />
                      </div>
                    </>
                  )}
                  {passenger.type !== 'infant' && (
                    <div className="md:col-span-2 space-y-3">
                      {['outbound', tripType === 'roundtrip' ? 'return' : null]
                        .filter(Boolean)
                        .map((leg) => (
                          <div
                            key={`${index}-${leg}`}
                            className="rounded-xl border border-slate-200 bg-slate-50 p-3"
                          >
                            <p className="mb-2 text-sm font-semibold text-slate-700">
                              Dịch vụ thêm ({leg === 'outbound' ? 'chuyến đi' : 'chuyến về'})
                            </p>
                            {serviceError && (
                              <p className="mb-2 text-xs text-red-600">{serviceError}</p>
                            )}
                            {isLoadingServices && (
                              <p className="text-xs text-slate-500">Đang tải dịch vụ...</p>
                            )}
                            {!isLoadingServices && services.length === 0 && (
                              <p className="text-xs text-slate-500">Không có dịch vụ khả dụng cho hạng ghế đã chọn.</p>
                            )}
                            {!isLoadingServices && services.length > 0 && (
                              <div className="space-y-2">
                                {services.map((service) => {
                                  const serviceId = service.serviceId || service.id
                                  const current =
                                    selectedServicesByPassengerDraft?.[leg]?.[index]?.[serviceId] || 0
                                  return (
                                    <div
                                      key={`${index}-${leg}-${serviceId}`}
                                      className="flex items-center justify-between rounded-lg bg-white p-2"
                                    >
                                      <div>
                                        <p className="text-sm font-medium text-slate-800">{service.serviceName}</p>
                                        <p className="text-xs text-slate-500">{getServicePriceLabel(service.price)}</p>
                                      </div>
                                      <div className="flex items-center gap-2">
                                        <button
                                          type="button"
                                          onClick={() => {
                                            if (current > 0) {
                                              updateServiceDraft(leg, index, serviceId, current - 1)
                                            }
                                          }}
                                          className="rounded-lg bg-slate-200 px-3 py-1 text-sm font-semibold text-slate-700 hover:bg-slate-300"
                                        >
                                          -
                                        </button>
                                        <span className="w-6 text-center text-sm font-semibold">{current}</span>
                                        <button
                                          type="button"
                                          onClick={() => {
                                            updateServiceDraft(leg, index, serviceId, current + 1)
                                          }}
                                          className="rounded-lg bg-[#1E40AF] px-3 py-1 text-sm font-semibold text-white hover:bg-blue-800"
                                        >
                                          +
                                        </button>
                                      </div>
                                    </div>
                                  )
                                })}
                              </div>
                            )}
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
          <Label>Chọn mã giảm giá</Label>
          <Select
            value={selectedPromotionId}
            onChange={(e) => setSelectedPromotionId(e.target.value)}
          >
            <option value="">Không dùng mã</option>
            {availablePromotions.map((promo) => (
              <option key={String(getPromotionId(promo))} value={String(getPromotionId(promo))}>
                {getPromotionDisplayText(promo)}
              </option>
            ))}
          </Select>
          {isLoadingPromotion && (
            <p className="mt-2 text-xs text-slate-500">Đang tải mã giảm giá...</p>
          )}
          {!isLoadingPromotion && availablePromotions.length === 0 && (
            <p className="mt-2 text-xs text-slate-500">Không có mã giảm giá khả dụng.</p>
          )}
          {appliedPromotion && (
            <p className="mt-2 text-xs text-emerald-700">
              Đang áp dụng: {getPromotionDisplayText(appliedPromotion)}
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={async () => {
            if (tripType === 'roundtrip' && !returnFlight) {
              setApiError('Vui lòng chọn chuyến về trước khi tiếp tục')
              return
            }
            if (!passengerForms.length) {
              setApiError('Vui lòng điền đầy đủ thông tin hành khách')
              return
            }

            const validationError = passengerForms.find((passenger) => {
              if (!passenger.fullName) return true
              if (!passenger.dob || !passenger.gender) return true
              if (passenger.type === 'adult') {
                return !passenger.dob || !passenger.document || !passenger.email || !passenger.phone
              }
              const ageValue = getAgeFromDob(passenger.dob)
              if (!Number.isFinite(ageValue)) return true
              if (passenger.type === 'child') return ageValue < 2 || ageValue > 12
              return ageValue < 0 || ageValue >= 2
            })

            if (validationError) {
              setApiError('Vui lòng điền đúng thông tin hành khách theo loại')
              return
            }

            if (!authUser?.email) {
              setApiError('Vui lòng đăng nhập để dùng email tài khoản khi đặt vé')
              return
            }

            setIsLoadingFlights(true)
            setApiError('')
            try {
              const seatClassId = seatClassMap[searchData.seatClass] || 1
              const passengersPayload = passengerForms.map((passenger, idx) => {
                const [firstName, ...rest] = passenger.fullName.trim().split(' ')
                const lastName = rest.join(' ')
                const dateOfBirth = new Date(passenger.dob).toISOString()
                const outboundSelections = selectedServicesByPassengerDraft?.outbound?.[idx] || {}
                const returnSelections =
                  tripType === 'roundtrip' ? selectedServicesByPassengerDraft?.return?.[idx] || {} : {}
                const optionalServices = passenger.type === 'infant'
                  ? []
                  : Object.entries(outboundSelections)
                    .map(([serviceId, quantity]) => ({
                      additionalServiceId: Number(serviceId),
                      quantity: Number(quantity || 0),
                    }))
                    .filter((item) => Number.isFinite(item.additionalServiceId) && item.quantity > 0)
                const returnOptionalServices = passenger.type === 'infant'
                  ? []
                  : Object.entries(returnSelections)
                    .map(([serviceId, quantity]) => ({
                      additionalServiceId: Number(serviceId),
                      quantity: Number(quantity || 0),
                    }))
                    .filter((item) => Number.isFinite(item.additionalServiceId) && item.quantity > 0)
                return {
                  firstName: firstName || passenger.fullName,
                  lastName,
                  email: passenger.type === 'adult' ? passenger.email : '',
                  phone: passenger.type === 'adult' ? passenger.phone : '',
                  dateOfBirth,
                  nationality: 'VN',
                  passportNumber: passenger.type === 'adult' ? passenger.document : '',
                  optionalServices,
                  returnOptionalServices: tripType === 'roundtrip' ? returnOptionalServices : [],
                }
              })

              const contactEmail =
                passengerForms.find((passenger) => passenger.type === 'adult' && passenger.email)
                  ?.email || authUser.email

              const booking = await createBooking({
                outboundFlightId: getFlightId(selectedFlight),
                outboundFlightNumber: selectedFlight.flightNumber,
                outboundDepartureDate: selectedFlight.departureTime,
                returnFlightId: tripType === 'roundtrip' ? getFlightId(returnFlight) : null,
                returnFlightNumber: tripType === 'roundtrip' ? returnFlight?.flightNumber || null : null,
                returnDepartureDate: tripType === 'roundtrip' ? returnFlight?.departureTime || null : null,
                passengerCount: parseInt(searchData.passengers, 10),
                seatClassId,
                passengers: passengersPayload,
                promotionId: appliedPromotion ? getPromotionId(appliedPromotion) : null,
                contactEmail,
              })
              setBookingId(booking.bookingId)
              setBookingAmount(booking.finalAmount ?? booking.totalAmount ?? null)
              setScreen('payment')
            } catch (error) {
              setApiError(error.message || 'Lỗi tạo booking. Vui lòng thử lại.')
            } finally {
              setIsLoadingFlights(false)
            }
          }}
          disabled={isLoadingFlights}
          className="mt-6 rounded-xl bg-[#1E40AF] px-5 py-3 text-sm font-semibold text-white hover:bg-blue-800 disabled:opacity-50"
        >
          {isLoadingFlights ? 'Đang xử lý...' : 'Tiếp tục'}
        </button>
        {apiError && (
          <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">
            {apiError}
          </div>
        )}
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">Tóm tắt chuyến bay</h3>
        <p className="text-sm text-slate-500">{selectedFromAirport?.City} - {selectedFromAirport?.Name} ({selectedFromAirport?.Code})</p>
        <p className="text-sm text-slate-500">{selectedToAirport?.City} - {selectedToAirport?.Name} ({selectedToAirport?.Code})</p>
        {selectedFlight && (
          <>
            <p className="mt-2 text-sm font-semibold text-slate-900">{selectedFlight.airlineCode}</p>
            <p className="text-sm text-slate-600">
              {formatTime(selectedFlight.departureTime)} - {formatTime(selectedFlight.arrivalTime)} (
              {formatDuration(selectedFlight.durationMinutes)})
            </p>
            <p className="mt-1 text-xs text-slate-500">
              ID chuyến bay: {getFlightId(selectedFlight)} · Số hiệu: {getFlightLabel(selectedFlight)}
            </p>
          </>
        )}
        {tripType === 'roundtrip' && returnFlight && (
          <>
            <p className="mt-3 text-sm font-semibold text-slate-900">Chuyến về</p>
            <p className="text-sm text-slate-600">
              {formatTime(returnFlight.departureTime)} - {formatTime(returnFlight.arrivalTime)} (
              {formatDuration(returnFlight.durationMinutes)})
            </p>
            <p className="mt-1 text-xs text-slate-500">
              ID chuyến bay: {getFlightId(returnFlight)} · Số hiệu: {getFlightLabel(returnFlight)}
            </p>
          </>
        )}
        
        <div className="mt-3 border-t border-slate-100 pt-3">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Giá 1 vé:</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {appliedPromotion && (
            <div className="mt-2 rounded-lg bg-green-50 p-2">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-semibold text-green-700">🎁 {getPromotionCode(appliedPromotion)}</p>
                  {appliedPromotion.description && (
                    <p className="text-xs text-green-600">{appliedPromotion.description}</p>
                  )}
                </div>
                <p className="text-sm font-bold text-green-700">-{formatCurrency(discountAmount)}</p>
              </div>
            </div>
          )}
          
          <div className="mt-2 flex justify-between border-t border-slate-100 pt-2 text-base font-bold text-[#1E40AF]">
            <span>Tổng cộng:</span>
            <span>{formatCurrency(finalPrice)}</span>
          </div>
        </div>
      </aside>
    </div>
  )

  const renderPayment = () => {
    const paymentSummaryAmount = Number.isFinite(Number(bookingAmount))
      ? Number(bookingAmount)
      : getPaymentAmount(paymentData, null)
    return (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h2 className="title-font mb-4 text-xl font-bold text-slate-900">Thanh toán</h2>
        <p className="mb-5 text-sm text-slate-500">Phương thức thanh toán: VNPAY</p>
        {paymentData && (
          <div className="mb-4 rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">
            <p>Trạng thái: {getBookingStatusLabel(paymentData.status)}</p>
            <p>Số tiền: {paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}</p>
          </div>
        )}

        <div className="mb-5 rounded-2xl border border-dashed border-blue-300 bg-blue-50 p-6">
          <p className="text-sm text-slate-600">
            Mở trang thanh toán VNPAY trong trình duyệt để thực hiện giao dịch.
          </p>
          <p className="mt-2 break-all text-xs text-slate-500">
            Mã thanh toán: {paymentData?.transactionRef || bookingReference}
          </p>
          <div className="mt-4">
            {paymentData?.paymentUrl || paymentData?.paymentLink ? (
              <a
                className="inline-flex items-center justify-center rounded-xl bg-[#1E40AF] px-5 py-3 text-sm font-semibold text-white hover:bg-blue-800"
                href={paymentData.paymentUrl || paymentData.paymentLink}
                target="_blank"
                rel="noreferrer"
              >
                Mở trang thanh toán
              </a>
            ) : (
              <p className="text-sm text-red-600">
                Chưa có đường dẫn thanh toán. Vui lòng thử lại.
              </p>
            )}
          </div>
          <div className="mt-4 flex flex-wrap gap-3">
            <button
              type="button"
              onClick={saveBookingToHistory}
              className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              Xác nhận đã thanh toán
            </button>
            <button
              type="button"
              onClick={() => setScreen('history')}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Xem vé đã đặt
            </button>
          </div>
          {historyNotice && (
            <div className="mt-3 rounded-xl bg-emerald-50 px-4 py-3 text-xs text-emerald-700">
              {historyNotice}
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-5 text-sm text-slate-700">
          <p className="font-semibold text-slate-900">Quy trình thanh toán VNPAY (Sandbox)</p>
          <ol className="mt-3 list-decimal space-y-2 pl-5">
            <li>Chọn phương thức: Thẻ nội địa và tài khoản ngân hàng (Local ATM Card).</li>
            <li>Chọn logo ngân hàng NCB.</li>
            <li>
              Nhập thẻ test: Số thẻ 9704198526191432198, Tên chủ thẻ NGUYEN VAN A,
              Ngày phát hành 07/15, OTP 123456.
            </li>
            <li>Bấm "Thanh toán".</li>
          </ol>
        </div>

        <button
          type="button"
          onClick={() => {
            setSelectedFlight(null)
            setPassengerForms([])
            setHistoryNotice('')
            setScreen('search')
          }}
          className="mt-4 rounded-xl border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
        >
          Huỷ
        </button>
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">Tóm tắt đơn hàng</h3>
        <div className="space-y-2 text-sm text-slate-600">
          <p>ID chuyến bay: {getFlightId(selectedFlight)}</p>
          <p>Số hiệu chuyến bay: {getFlightLabel(selectedFlight)}</p>
          <p>Ngày cất cánh: {formatDateTime(selectedFlight?.departureTime)}</p>
          <p>Hãng bay: {selectedFlight?.airlineCode}</p>
          {tripType === 'roundtrip' && returnFlight && (
            <>
              <p>Chuyến về: {getFlightLabel(returnFlight)}</p>
              <p>Ngày về: {formatDateTime(returnFlight?.departureTime)}</p>
            </>
          )}
          <p>Hành khách: {passengerForms[0]?.fullName || 'Chưa nhập'}</p>
          <p>Số lượng: {searchData.passengers}</p>
          {paymentData && (
            <p className="break-all text-xs text-slate-500">Mã giao dịch: {paymentData.transactionRef}</p>
          )}
        </div>
        
        <div className="mt-4 border-t border-slate-100 pt-4 space-y-2">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Giá 1 vé:</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {appliedPromotion && (
            <div className="flex justify-between text-sm text-green-600">
              <span>🎁 Giảm giá ({getPromotionDisplayText(appliedPromotion)}):</span>
              <span>-{formatCurrency(discountAmount)}</span>
            </div>
          )}
          
          <div className="flex justify-between border-t border-slate-100 pt-2 text-base font-bold text-[#1E40AF]">
            <span>Giá sau giảm:</span>
            <span>{paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}</span>
          </div>
        </div>
      </aside>
    </div>
  )
  }

  const renderSavedPassengers = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_360px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Danh bạ</p>
            <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Hành khách đã lưu</h2>
            <p className="mt-1 text-sm text-slate-500">
              Lưu thông tin hành khách để đặt vé nhanh hơn.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={loadSavedPassengers}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Làm mới
            </button>
            <button
              type="button"
              onClick={startCreateSavedPassenger}
              className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
            >
              + Thêm mới
            </button>
          </div>
        </div>

        {isLoadingSavedPassengers && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Đang tải hành khách đã lưu...
          </div>
        )}

        {savedPassengerError && (
          <div className="mb-4 rounded-2xl bg-red-50 p-4 text-sm text-red-700">
            {savedPassengerError}
          </div>
        )}

        {savedPassengerNotice && (
          <div className="mb-4 rounded-2xl bg-emerald-50 p-4 text-sm text-emerald-700">
            {savedPassengerNotice}
          </div>
        )}

        {!isLoadingSavedPassengers && savedPassengers.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-500">
            Chưa có hành khách nào được lưu.
          </div>
        )}

        {!isLoadingSavedPassengers && savedPassengers.length > 0 && (
          <div className="space-y-4">
            {savedPassengers.map((passenger) => {
              const fullName = [passenger?.lastName, passenger?.firstName]
                .filter(Boolean)
                .join(' ')
              const dobLabel = toDateInputValueFromApi(passenger?.dateOfBirth) || '---'
              return (
                <article
                  key={passenger.id}
                  className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                >
                  <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                    <div>
                      <p className="text-sm font-semibold text-[#1E40AF]">
                        {fullName || '---'}
                      </p>
                      <p className="text-sm text-slate-500">
                        Ngày sinh: {dobLabel} · Giới tính: {passenger?.gender || '---'}
                      </p>
                      <p className="text-sm text-slate-500">
                        Quốc tịch: {passenger?.nationality || '---'} · Giấy tờ: {passenger?.documentNumber || '---'}
                      </p>
                      <p className="text-xs text-slate-500">
                        Email: {passenger?.email || '---'} · SĐT: {passenger?.phone || '---'}
                      </p>
                    </div>
                    <div className="flex flex-wrap items-center justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => startEditSavedPassenger(passenger)}
                        className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                      >
                        Sửa
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeleteSavedPassenger(passenger)}
                        className="rounded-xl bg-red-500 px-3 py-2 text-xs font-semibold text-white hover:bg-red-600"
                      >
                        Xóa
                      </button>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">
          {editingSavedPassengerId ? 'Cập nhật hành khách' : 'Thêm hành khách mới'}
        </h3>
        <form onSubmit={handleSavedPassengerSubmit} className="space-y-3">
          <div>
            <Label>Họ</Label>
            <Input
              value={savedPassengerForm.lastName}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, lastName: event.target.value }))
              }
              placeholder="Nguyễn"
            />
          </div>
          <div>
            <Label>Tên</Label>
            <Input
              value={savedPassengerForm.firstName}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, firstName: event.target.value }))
              }
              placeholder="An"
            />
          </div>
          <div>
            <Label>Ngày sinh</Label>
            <Input
              type="date"
              value={savedPassengerForm.dateOfBirth}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, dateOfBirth: event.target.value }))
              }
            />
          </div>
          <div>
            <Label>Giới tính</Label>
            <Select
              value={savedPassengerForm.gender}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, gender: event.target.value }))
              }
            >
              <option value="">-- Chọn --</option>
              <option value="Nam">Nam</option>
              <option value="Nữ">Nữ</option>
              <option value="Khác">Khác</option>
            </Select>
          </div>
          <div>
            <Label>Quốc tịch</Label>
            <Input
              value={savedPassengerForm.nationality}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, nationality: event.target.value }))
              }
              placeholder="Việt Nam"
            />
          </div>
          <div>
            <Label>Số giấy tờ</Label>
            <Input
              value={savedPassengerForm.documentNumber}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, documentNumber: event.target.value }))
              }
              placeholder="0123456789"
            />
          </div>
          <div>
            <Label>Email</Label>
            <Input
              type="email"
              value={savedPassengerForm.email}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, email: event.target.value }))
              }
              placeholder="email@example.com"
            />
          </div>
          <div>
            <Label>Số điện thoại</Label>
            <Input
              value={savedPassengerForm.phone}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, phone: event.target.value }))
              }
              placeholder="0901234567"
            />
          </div>

          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              className="flex-1 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              {editingSavedPassengerId ? 'Lưu cập nhật' : 'Thêm hành khách'}
            </button>
            <button
              type="button"
              onClick={startCreateSavedPassenger}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Reset
            </button>
          </div>
        </form>
      </aside>
    </div>
  )

  const renderHistory = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Lịch sử</p>
          <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Vé đã đặt</h2>
          <p className="mt-1 text-sm text-slate-500">Danh sách các booking đã xác nhận thanh toán.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={loadBookingHistory}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Làm mới
          </button>
          <button
            type="button"
            onClick={() => setScreen('search')}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            Đặt vé mới
          </button>
        </div>
      </div>

      {isLoadingHistory && (
        <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
          Đang tải lịch sử đặt vé...
        </div>
      )}

      {historyError && (
        <div className="mb-4 rounded-2xl bg-red-50 p-4 text-sm text-red-700">
          {historyError}
        </div>
      )}

      {historyNotice && (
        <div className="mb-4 rounded-2xl bg-emerald-50 p-4 text-sm text-emerald-700">
          {historyNotice}
        </div>
      )}

      {!isLoadingHistory && bookingHistory.length === 0 && (
        <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-500">
          Chưa có vé nào được lưu. Hãy hoàn tất thanh toán và lưu vé.
        </div>
      )}

      {!isLoadingHistory && bookingHistory.length > 0 && (
        <div className="space-y-4">
          {bookingHistory.map((item) => {
            const disruptionOptions = Array.isArray(disruptionOptionsMap[item.bookingId])
              ? disruptionOptionsMap[item.bookingId]
              : []
            const isDisruptionLoading = Boolean(disruptionLoadingMap[item.bookingId])
            const disruptionError = disruptionErrorMap[item.bookingId]
            const disruptionNotice = disruptionNoticeMap[item.bookingId]
            const selectedDecisionId = selectedDisruptionDecisionMap[item.bookingId] || ''
            const selectedAction = selectedDisruptionActionMap[item.bookingId] || 'refund'

            return (
              <article
                key={`${item.bookingId}-${item.transactionRef}`}
                className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
              >
              <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="text-sm font-semibold text-[#1E40AF]">
                    {formatRouteLabel(item)}
                  </p>
                  <p className="mt-1 text-lg font-bold text-slate-900">
                    {formatTime(item.departTime)} - {formatTime(item.arriveTime)}
                  </p>
                  <p className="text-sm text-slate-500">
                    Ngày cất cánh: {formatDateTime(item.departTime)}
                  </p>
                  {formatFlightMeta(item) && (
                    <p className="text-sm text-slate-500">{formatFlightMeta(item)}</p>
                  )}
                  <p className="mt-1 text-xs text-slate-500">
                    Mã booking: {item.bookingId}
                    {item.transactionRef ? ` · Mã giao dịch: ${item.transactionRef}` : ''}
                  </p>
                </div>
                <div className="text-right">
                  <p
                    className={`text-sm font-semibold ${getBookingStatusClassName(item.status)}`}
                  >
                    {getBookingStatusLabel(item.status)}
                  </p>
                  <p className="text-sm text-slate-500">{formatDateTime(item.createdAt)}</p>
                  <p className="mt-2 text-lg font-bold text-slate-900">
                    {formatCurrency(item.totalPrice)}
                  </p>
                  <p className="text-xs text-slate-500">
                    Hành khách: {item.passengerName || '---'} · {item.passengerCount} vé
                  </p>
                  {Array.isArray(item.passengers) &&
                    item.passengers.some(
                      (passenger) => Array.isArray(passenger?.services) && passenger.services.length > 0
                    ) && (
                      <div className="mt-3 rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs">
                        <p className="font-semibold text-slate-700">Dịch vụ đã chọn</p>
                        <div className="mt-2 space-y-2">
                          {item.passengers.map((passenger, passengerIndex) => {
                            const services = Array.isArray(passenger?.services) ? passenger.services : []
                            if (services.length === 0) return null
                            const passengerLabel =
                              [passenger?.lastName, passenger?.firstName].filter(Boolean).join(' ') ||
                              `Hành khách ${passengerIndex + 1}`

                            return (
                              <div key={passenger?.passengerId || passengerIndex}>
                                <p className="font-semibold text-slate-600">{passengerLabel}</p>
                                <ul className="mt-1 space-y-1">
                                  {services.map((service, serviceIndex) => (
                                    <li
                                      key={`${service?.additionalServiceId || serviceIndex}-${serviceIndex}`}
                                      className="flex items-center justify-between text-slate-600"
                                    >
                                      <span>
                                        {service?.serviceName || `Dịch vụ #${service?.additionalServiceId || '-'}`}
                                        {service?.quantity > 1 ? ` x${service.quantity}` : ''}
                                      </span>
                                      <span>
                                        {Number.isFinite(Number(service?.price))
                                          ? formatCurrency(service.price)
                                          : ''}
                                      </span>
                                    </li>
                                  ))}
                                </ul>
                              </div>
                            )
                          })}
                        </div>
                      </div>
                    )}
                  <div className="mt-3 flex flex-wrap gap-2 justify-end">
                    <button
                      type="button"
                      onClick={() => loadTicketsForBooking(item.bookingId)}
                      className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                    >
                      Xem vé
                    </button>
                    {/* Nút thanh toán cho booking chưa thanh toán */}
                    {!isBookingCancelled(item.status) && isBookingPendingPayment(item.status) && (
                      <button
                        type="button"
                        onClick={async () => {
                          try {
                            setHistoryError('')
                            setHistoryNotice('')
                            const bookingIdValue = Number(item.bookingId)
                            if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
                              setHistoryError('Mã booking không hợp lệ')
                              return
                            }
                            
                            setHistoryNotice('Đang chuyển đến trang thanh toán...')
                            const paymentResponse = await initiatePayment(bookingIdValue, 'VNPAY')
                            
                            if (paymentResponse?.paymentUrl) {
                              // Chuyển hướng trực tiếp sang VNPay
                              window.location.href = paymentResponse.paymentUrl
                            } else {
                              setHistoryError('Không nhận được link thanh toán từ server')
                            }
                          } catch (error) {
                            setHistoryError(error.message || 'Lỗi khi khởi tạo thanh toán')
                          }
                        }}
                        className="rounded-xl bg-green-600 px-4 py-2 text-xs font-semibold text-white hover:bg-green-700"
                      >
                        💳 Thanh toán ngay
                      </button>
                    )}
                    {/* Nút thêm dịch vụ */}
                    {!isBookingCancelled(item.status) && (
                      <button
                        type="button"
                        onClick={() => {
                          const passengers = item?.passengers || []
                          const seatClassId = seatClassMap[item?.seatClass] || null
                          setCurrentBookingForServices({
                            ...item,
                          })
                          const initialSelection = passengers.reduce((acc, passenger) => {
                            if (passenger?.passengerId) {
                              acc[passenger.passengerId] = {}
                            }
                            return acc
                          }, {})
                          setSelectedServicesByPassenger(initialSelection)
                          setShowServicesModal(true)
                          loadServices(seatClassId)
                        }}
                        className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-700"
                      >
                        ➕ Thêm dịch vụ
                      </button>
                    )}
                    {/* Nút đổi chuyến */}
                    {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                      <button
                        type="button"
                        onClick={() => openChangeFlightModal(item)}
                        className="rounded-xl border border-emerald-400 bg-emerald-50 px-4 py-2 text-xs font-semibold text-emerald-700 hover:bg-emerald-100"
                      >
                        🔄 Đổi chuyến
                      </button>
                    )}
                    {/* Nút hủy vé */}
                    {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                      <button
                        type="button"
                        onClick={() => cancelBookingFromHistory(item)}
                        disabled={isCancellingBookingId === item.bookingId}
                        className="rounded-xl bg-red-500 px-4 py-2 text-xs font-semibold text-white hover:bg-red-600 disabled:opacity-60"
                      >
                        {isCancellingBookingId === item.bookingId ? 'Đang hủy...' : 'Hủy vé'}
                      </button>
                    )}
                    {(isBookingCancelled(item.status) || isPendingDisruptionDecision(item.status)) && (
                      <button
                        type="button"
                        onClick={() => loadDisruptionOptionsForBooking(item.bookingId)}
                        className="rounded-xl bg-amber-500 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-600"
                      >
                        Tải lựa chọn xử lý
                      </button>
                    )}
                  </div>
                  {isDisruptionLoading && (
                    <div className="mt-3 rounded-xl border border-dashed border-slate-300 bg-slate-50 p-3 text-xs text-slate-500">
                      Đang tải lựa chọn xử lý hủy chuyến...
                    </div>
                  )}
                  {disruptionError && (
                    <div className="mt-3 rounded-xl bg-red-50 p-3 text-xs text-red-700">
                      {disruptionError}
                    </div>
                  )}
                  {disruptionNotice && (
                    <div className="mt-3 rounded-xl bg-emerald-50 p-3 text-xs text-emerald-700">
                      {disruptionNotice}
                    </div>
                  )}
                  {disruptionOptions.length > 0 && (
                    <div className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-3 text-xs text-slate-700">
                      <p className="font-semibold text-amber-700">Phương án xử lý khi chuyến bị hủy</p>
                      <div className="mt-2 grid gap-3 md:grid-cols-[1.2fr_1fr]">
                        <div>
                          <label className="mb-1 block text-xs font-semibold text-slate-600">
                            Chuyến bị ảnh hưởng
                          </label>
                          <select
                            value={selectedDecisionId}
                            onChange={(event) =>
                              setSelectedDisruptionDecisionMap((prev) => ({
                                ...prev,
                                [item.bookingId]: event.target.value,
                              }))
                            }
                            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs text-slate-700"
                          >
                            {disruptionOptions.map((option, optionIndex) => (
                              <option
                                key={`${option?.decisionId || optionIndex}-${optionIndex}`}
                                value={option?.decisionId ?? ''}
                              >
                                {getDisruptionLegLabel(option?.legType)} · Quyết định #{option?.decisionId}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div>
                          <label className="mb-1 block text-xs font-semibold text-slate-600">
                            Chọn phương án
                          </label>
                          <div className="flex flex-wrap gap-3">
                            <label className="flex items-center gap-2 text-xs text-slate-700">
                              <input
                                type="radio"
                                name={`disruption-action-${item.bookingId}`}
                                value="refund"
                                checked={selectedAction === 'refund'}
                                onChange={() =>
                                  setSelectedDisruptionActionMap((prev) => ({
                                    ...prev,
                                    [item.bookingId]: 'refund',
                                  }))
                                }
                              />
                              Hoàn tiền
                            </label>
                            <label className="flex items-center gap-2 text-xs text-slate-700">
                              <input
                                type="radio"
                                name={`disruption-action-${item.bookingId}`}
                                value="rebook"
                                checked={selectedAction === 'rebook'}
                                onChange={() =>
                                  setSelectedDisruptionActionMap((prev) => ({
                                    ...prev,
                                    [item.bookingId]: 'rebook',
                                  }))
                                }
                              />
                              Đổi chuyến
                            </label>
                          </div>
                        </div>
                      </div>
                      <div className="mt-3 flex flex-wrap justify-end gap-2">
                        <button
                          type="button"
                          disabled={!selectedDecisionId}
                          onClick={() => {
                            if (!selectedDecisionId) return
                            if (selectedAction === 'refund') {
                              handleDisruptionRefund(item.bookingId, selectedDecisionId)
                              return
                            }
                            openRebookModal(item.bookingId, selectedDecisionId, item.departTime)
                          }}
                          className="rounded-xl bg-amber-600 px-4 py-2 text-xs font-semibold text-white hover:bg-amber-700 disabled:opacity-60"
                        >
                          Xác nhận
                        </button>
                      </div>
                    </div>
                  )}
                  {loadingTicketsMap[item.bookingId] && (
                    <div className="mt-3 rounded-xl border border-dashed border-slate-300 bg-slate-50 p-3 text-xs text-slate-500">
                      Đang tải danh sách vé...
                    </div>
                  )}
                  {ticketErrorMap[item.bookingId] && (
                    <div className="mt-3 rounded-xl bg-red-50 p-3 text-xs text-red-700">
                      {ticketErrorMap[item.bookingId]}
                    </div>
                  )}
                  {Array.isArray(bookingTicketsMap[item.bookingId]) &&
                    bookingTicketsMap[item.bookingId].length > 0 && (
                      <div className="mt-3 rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs">
                        <p className="font-semibold text-slate-700">Danh sách vé</p>
                        <div className="mt-2 space-y-2">
                          {bookingTicketsMap[item.bookingId].map((ticket) => (
                            <div
                              key={ticket.ticketId}
                              className="rounded-lg border border-slate-200 bg-white p-3"
                            >
                              <div className="flex flex-wrap items-center justify-between gap-2">
                                <div>
                                  <div className="flex flex-wrap items-center gap-2">
                                    <p className="text-sm font-semibold text-slate-900">
                                      {ticket.ticketNumber || `Vé #${ticket.ticketId}`}
                                    </p>
                                    {ticket.status !== undefined && ticket.status !== null && (
                                      (() => {
                                        const code = resolveTicketStatusCode(ticket.status)
                                        let bg = '#FEF9C3' // Default yellow
                                        let textCol = '#92400E' // Default brown/orange
                                        let icon = '•'
                                        
                                        if (code === 0) {
                                          bg = '#E8F5E9' // light green
                                          textCol = '#2E7D32' // green
                                          icon = '✓'
                                        } else if (code === 1) {
                                          bg = '#F5F5F5' // light gray
                                          textCol = '#616161' // dark gray
                                          icon = '🛄'
                                        } else if (code === 2) {
                                          bg = '#E3F2FD' // light blue
                                          textCol = '#1565C0' // blue
                                          icon = '↺'
                                        } else if (code === 3 || code === 4 || code === 5) {
                                          bg = '#FEE2E2' // light red
                                          textCol = '#DC2626' // red
                                          icon = '✕'
                                        }
                                        
                                        const label = getTicketStatusLabel(ticket.status)
                                        return (
                                          <span
                                            className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-bold"
                                            style={{ background: bg, color: textCol }}
                                          >
                                            <span>{icon}</span>
                                            <span>{label}</span>
                                          </span>
                                        )
                                      })()
                                    )}
                                  </div>
                                  <p className="text-xs text-slate-500">
                                    {ticket.passengerName || '---'} · {ticket.flightNumber || '---'}
                                  </p>
                                  <p className="text-xs text-slate-500">
                                    {ticket.departureAirport || '---'} → {ticket.arrivalAirport || '---'}
                                  </p>
                                  <p className="text-xs text-slate-500">
                                    Giờ bay: {formatDateTime(ticket.departureTime)}
                                  </p>
                                </div>
                                {isTicketActionable(ticket.status) && (
                                  <div className="flex flex-col gap-1.5">
                                    <button
                                      type="button"
                                      onClick={() => openUpgradeModal(item, ticket)}
                                      className="rounded-xl border border-indigo-300 bg-indigo-50 px-3 py-2 text-xs font-semibold text-indigo-700 hover:bg-indigo-100"
                                    >
                                      ⬆️ Nâng hạng
                                    </button>
                                    <button
                                      type="button"
                                      disabled={isCancellingTicketId === ticket.ticketId}
                                      onClick={() => cancelTicketFromHistory(item.bookingId, ticket)}
                                      className="rounded-xl bg-red-500 px-3 py-2 text-xs font-semibold text-white hover:bg-red-600 disabled:opacity-60"
                                    >
                                      {isCancellingTicketId === ticket.ticketId ? 'Đang hủy...' : 'Hủy vé'}
                                    </button>
                                  </div>
                                )}
                              </div>
                              {Array.isArray(ticket.services) && ticket.services.length > 0 && (
                                <div className="mt-2 rounded-lg border border-slate-100 bg-slate-50 p-2">
                                  <p className="text-xs font-semibold text-slate-600">Dịch vụ</p>
                                  <ul className="mt-1 space-y-1">
                                    {ticket.services.map((service, serviceIndex) => (
                                      <li
                                        key={`${service?.additionalServiceId || serviceIndex}-${serviceIndex}`}
                                        className="flex items-center justify-between text-xs text-slate-600"
                                      >
                                        <span>
                                          {service?.serviceName || `Dịch vụ #${service?.additionalServiceId || '-'}`}
                                          {service?.quantity > 1 ? ` x${service.quantity}` : ''}
                                        </span>
                                        <span>
                                          {Number.isFinite(Number(service?.totalPrice))
                                            ? formatCurrency(service.totalPrice)
                                            : Number.isFinite(Number(service?.unitPrice))
                                            ? formatCurrency(service.unitPrice)
                                            : ''}
                                        </span>
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                              )}
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                </div>
              </div>
              </article>
            )
          })}
        </div>
      )}

      {/* Modal chọn dịch vụ */}
      {showServicesModal && currentBookingForServices && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl">
            <div className="mb-4 flex items-start justify-between">
              <div>
                <h3 className="text-2xl font-bold text-slate-900">
                  ➕ Thêm Dịch Vụ
                </h3>
                <p className="mt-1 text-sm text-slate-600">
                  Booking: {currentBookingForServices.bookingId} - {currentBookingForServices.flightNumber}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowServicesModal(false)
                  setCurrentBookingForServices(null)
                  setSelectedServicesByPassenger({})
                }}
                className="rounded-lg bg-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-300"
              >
                ✕ Đóng
              </button>
            </div>

            {currentBookingForServices?.passengers?.length > 0 ? (
              <div className="mb-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
                <p className="text-sm font-semibold text-slate-700">
                  Chọn dịch vụ theo từng hành khách
                </p>
                <p className="mt-1 text-xs text-slate-500">
                  Các dịch vụ có sẵn theo hạng ghế sẽ hiển thị giá 0 (miễn phí).
                </p>
              </div>
            ) : (
              <div className="mb-4 rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-500">
                Booking chưa có thông tin hành khách.
              </div>
            )}

            {isLoadingServices && (
              <div className="py-8 text-center text-sm text-slate-500">
                Đang tải danh sách dịch vụ...
              </div>
            )}

            {!isLoadingServices && services.length === 0 && (
              <div className="py-8 text-center text-sm text-slate-500">
                Không có dịch vụ nào
              </div>
            )}

            {!isLoadingServices && services.length > 0 && (
              <div className="space-y-4">
                {currentBookingForServices.passengers.map((passenger) => {
                  const passengerName = [passenger.lastName, passenger.firstName]
                    .filter(Boolean)
                    .join(' ')
                  const label =
                    passengerName ||
                    passenger.email ||
                    passenger.phone ||
                    `Hành khách #${passenger.passengerId}`
                  const passengerId = passenger.passengerId
                  const passengerSelection = selectedServicesByPassenger[passengerId] || {}

                  return (
                    <div
                      key={passengerId}
                      className="rounded-xl border border-slate-200 bg-slate-50 p-4"
                    >
                      <div className="mb-3 flex items-center justify-between">
                        <h4 className="text-sm font-semibold text-slate-700">{label}</h4>
                        <span className="text-xs text-slate-500">Chọn dịch vụ</span>
                      </div>
                      <div className="space-y-3">
                        {services.map((service) => {
                          const serviceId = service.serviceId || service.id
                          const current = passengerSelection[serviceId] || 0
                          return (
                            <div
                              key={serviceId}
                              className="flex items-start justify-between gap-4 rounded-lg border border-slate-200 bg-white p-3"
                            >
                              <div className="flex-1">
                                <h5 className="text-sm font-semibold text-slate-900">
                                  {service.serviceName}
                                </h5>
                                <p className="mt-1 text-xs text-slate-600">{service.description}</p>
                                <p className="mt-2 text-sm font-semibold text-[#1E40AF]">
                                  {getServicePriceLabel(service.price)}
                                </p>
                              </div>
                              <div className="flex items-center gap-2">
                                <button
                                  type="button"
                                  onClick={() => {
                                    if (current > 0) {
                                      setSelectedServicesByPassenger((prev) => ({
                                        ...prev,
                                        [passengerId]: {
                                          ...passengerSelection,
                                          [serviceId]: current - 1,
                                        },
                                      }))
                                    }
                                  }}
                                  className="rounded-lg bg-slate-200 px-3 py-1 text-sm font-semibold text-slate-700 hover:bg-slate-300"
                                >
                                  −
                                </button>
                                <span className="w-8 text-center font-semibold">
                                  {current}
                                </span>
                                <button
                                  type="button"
                                  onClick={() => {
                                    setSelectedServicesByPassenger((prev) => ({
                                      ...prev,
                                      [passengerId]: {
                                        ...passengerSelection,
                                        [serviceId]: current + 1,
                                      },
                                    }))
                                  }}
                                  className="rounded-lg bg-[#1E40AF] px-3 py-1 text-sm font-semibold text-white hover:bg-blue-800"
                                >
                                  +
                                </button>
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}

            <div className="mt-6 rounded-xl border-2 border-green-500 bg-green-50 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-slate-600">Tổng tiền dịch vụ:</p>
                  <p className="text-2xl font-bold text-slate-900">
                    {formatCurrency(
                      Object.entries(selectedServicesByPassenger).reduce((total, [passengerId, selections]) => {
                        return (
                          total +
                          Object.entries(selections || {}).reduce((sum, [serviceId, quantity]) => {
                            const service = services.find(
                              (s) => (s.serviceId || s.id) === Number(serviceId)
                            )
                            return sum + (service?.price || 0) * Number(quantity || 0)
                          }, 0)
                        )
                      }, 0)
                    )}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      setHistoryError('')
                      setHistoryNotice('')

                      const bookingIdValue = Number(currentBookingForServices.bookingId)
                      if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
                        setHistoryError('Mã booking không hợp lệ')
                        return
                      }

                      const passengers = currentBookingForServices?.passengers || []
                      if (passengers.length === 0) {
                        setHistoryError('Không tìm thấy hành khách để thêm dịch vụ')
                        return
                      }

                      const selectedCount = Object.values(selectedServicesByPassenger).reduce(
                        (sum, selections) =>
                          sum +
                          Object.values(selections || {}).reduce((inner, qty) => inner + qty, 0),
                        0
                      )
                      if (selectedCount === 0) {
                        setHistoryError('Vui lòng chọn ít nhất 1 dịch vụ')
                        return
                      }

                      setHistoryNotice('Đang thêm dịch vụ...')

                      for (const passenger of passengers) {
                        const passengerIdValue = Number(passenger?.passengerId)
                        if (!Number.isFinite(passengerIdValue) || passengerIdValue <= 0) {
                          continue
                        }

                        const selections = selectedServicesByPassenger[passengerIdValue] || {}
                        for (const [serviceId, quantity] of Object.entries(selections)) {
                          if (quantity > 0) {
                            await addServiceToBooking(
                              bookingIdValue,
                              passengerIdValue,
                              Number(serviceId),
                              quantity
                            )
                          }
                        }
                      }

                      setHistoryNotice(`✅ Đã thêm ${selectedCount} dịch vụ vào booking!`)
                      setShowServicesModal(false)
                      setCurrentBookingForServices(null)
                      setSelectedServicesByPassenger({})
                    } catch (error) {
                      setHistoryError(error.message || 'Lỗi khi thêm dịch vụ')
                    }
                  }}
                  className="rounded-xl bg-green-600 px-6 py-3 text-sm font-semibold text-white hover:bg-green-700"
                >
                  ✓ Xác nhận thêm dịch vụ
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {rebookModalState.isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl">
            <div className="mb-4 flex items-start justify-between">
              <div>
                <h3 className="text-xl font-bold text-slate-900">Đổi chuyến bay</h3>
                <p className="mt-1 text-sm text-slate-600">
                  Chọn ngày và chuyến bay phù hợp để đổi.
                </p>
              </div>
              <button
                type="button"
                onClick={closeRebookModal}
                className="rounded-full p-2 text-slate-400 hover:bg-slate-100"
              >
                ✕
              </button>
            </div>

            {(() => {
              const decision = getDisruptionDecisionOptions(
                rebookModalState.bookingId,
                rebookModalState.decisionId,
              )
              const allOptions = Array.isArray(decision?.flightOptions)
                ? decision.flightOptions
                : []
              const filteredOptions = filterDisruptionFlightsByDate(
                allOptions,
                rebookModalState.date,
              )

              return (
                <div className="space-y-4">
                  <div className="rounded-2xl border border-slate-200 bg-white p-4">
                    <label className="mb-2 block text-xs font-semibold text-slate-600">
                      Chọn ngày bay
                    </label>
                    <input
                      type="date"
                      value={rebookModalState.date}
                      onChange={async (event) => {
                        const nextDate = event.target.value
                        const bookingId = Number(rebookModalState.bookingId)
                        const decisionId = Number(rebookModalState.decisionId)

                        setRebookModalState((prev) => ({
                          ...prev,
                          date: nextDate,
                          selectedFlightId: '',
                        }))

                        if (!nextDate || !Number.isFinite(bookingId)) {
                          return
                        }

                        setDisruptionLoadingMap((prev) => ({ ...prev, [bookingId]: true }))
                        setDisruptionErrorMap((prev) => ({ ...prev, [bookingId]: '' }))

                        try {
                          const options = await getDisruptionOptions(bookingId, nextDate)
                          setDisruptionOptionsMap((prev) => ({ ...prev, [bookingId]: options }))

                          const selectedDecision = Array.isArray(options)
                            ? options.find((option) => Number(option?.decisionId) === decisionId)
                            : null
                          const selectedOptions = Array.isArray(selectedDecision?.flightOptions)
                            ? selectedDecision.flightOptions
                            : []
                          const firstMatch = selectedOptions[0]

                          setRebookModalState((prev) => ({
                            ...prev,
                            selectedFlightId: firstMatch?.flightId ? String(firstMatch.flightId) : '',
                          }))
                        } catch (error) {
                          setDisruptionErrorMap((prev) => ({
                            ...prev,
                            [bookingId]: error.message || 'Không thể tải chuyến bay theo ngày đã chọn.',
                          }))
                        } finally {
                          setDisruptionLoadingMap((prev) => ({ ...prev, [bookingId]: false }))
                        }
                      }}
                      className="w-full max-w-xs rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700"
                    />
                  </div>

                  <div className="space-y-3">
                    {filteredOptions.map((flight) => {
                      const isSelected = String(flight.flightId) === rebookModalState.selectedFlightId

                      return (
                        <article
                          key={flight.flightId}
                          className={`rounded-2xl border bg-white p-4 shadow-sm transition ${
                            isSelected
                              ? 'border-[#1E40AF] shadow-blue-100'
                              : 'border-slate-200 hover:border-[#1E40AF]'
                          }`}
                        >
                          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                            <div>
                              <p className="text-sm font-semibold text-[#1E40AF]">
                                {flight.flightNumber || `Chuyến #${flight.flightId}`}
                              </p>
                              <p className="mt-1 text-lg font-bold text-slate-900">
                                {formatTime(flight.departureTime)} - {formatTime(flight.arrivalTime)}
                              </p>
                              <p className="text-sm text-slate-500">
                                Ngày bay: {formatDateTime(flight.departureTime)}
                              </p>
                              <p className="text-xs text-slate-500">
                                Ghế trống: {Number.isFinite(Number(flight.availableSeats))
                                  ? flight.availableSeats
                                  : '---'}
                              </p>
                            </div>
                            <div className="text-right">
                              <button
                                type="button"
                                onClick={() =>
                                  setRebookModalState((prev) => ({
                                    ...prev,
                                    selectedFlightId: String(flight.flightId),
                                  }))
                                }
                                className={`rounded-xl px-4 py-2 text-xs font-semibold transition ${
                                  isSelected
                                    ? 'bg-emerald-600 text-white'
                                    : 'bg-[#1E40AF] text-white hover:bg-blue-800'
                                }`}
                              >
                                {isSelected ? 'Đã chọn' : 'Chọn chuyến'}
                              </button>
                            </div>
                          </div>
                        </article>
                      )
                    })}
                    {filteredOptions.length === 0 && (
                      <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500">
                        Không có chuyến bay phù hợp với ngày đã chọn.
                      </div>
                    )}
                  </div>

                  <div className="flex flex-wrap justify-end gap-2">
                    <button
                      type="button"
                      onClick={closeRebookModal}
                      className="rounded-xl border border-slate-300 px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                    >
                      Hủy
                    </button>
                    <button
                      type="button"
                      disabled={!rebookModalState.selectedFlightId}
                      onClick={async () => {
                        await handleDisruptionRebook(
                          rebookModalState.bookingId,
                          rebookModalState.decisionId,
                          rebookModalState.selectedFlightId,
                        )
                        closeRebookModal()
                      }}
                      className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-700 disabled:opacity-60"
                    >
                      Xác nhận đổi chuyến
                    </button>
                  </div>
                </div>
              )
            })()}
          </div>
        </div>
      )}
    </div>
  )

  const renderPromotionManagement = () => (
    <div className="grid gap-5 lg:grid-cols-[1.1fr_360px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Admin</p>
            <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Quản lý khuyến mãi</h2>
            <p className="mt-1 text-sm text-slate-500">Tạo, cập nhật, vô hiệu và xóa mã khuyến mãi.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={loadAdminPromotions}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Làm mới
            </button>
            <button
              type="button"
              onClick={startCreatePromotion}
              className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
            >
              + Tạo mã
            </button>
          </div>
        </div>

        {promotionAdminError && (
          <div className="mb-4 rounded-2xl bg-red-50 p-4 text-sm text-red-700">
            {promotionAdminError}
          </div>
        )}

        {promotionAdminNotice && (
          <div className="mb-4 rounded-2xl bg-emerald-50 p-4 text-sm text-emerald-700">
            {promotionAdminNotice}
          </div>
        )}

        {isLoadingPromotionsAdmin && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Đang tải danh sách khuyến mãi...
          </div>
        )}

        {!isLoadingPromotionsAdmin && adminPromotions.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Chưa có mã khuyến mãi nào.
          </div>
        )}

        {!isLoadingPromotionsAdmin && adminPromotions.length > 0 && (
          <div className="space-y-4">
            {adminPromotions.map((promo) => (
              <article
                key={promo.promotionId}
                className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
              >
                <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="text-lg font-bold text-slate-900">{promo.code || '---'}</p>
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-semibold ${
                          promo.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {promo.isActive ? 'Hoạt động' : 'Tạm dừng'}
                      </span>
                    </div>
                    <p className="mt-1 text-sm text-slate-500">{promo.description || 'Không có mô tả'}</p>
                    <p className="mt-2 text-sm text-slate-600">
                      {getPromotionTypeLabel(promo.discountType)} · Giá trị: {formatCurrency(promo.discountValue)}
                    </p>
                    <p className="text-xs text-slate-500">
                      Tối đa: {promo.maxDiscountAmount ? formatCurrency(promo.maxDiscountAmount) : 'Không giới hạn'} ·
                      Tối thiểu: {formatCurrency(promo.minimumAmount || 0)}
                    </p>
                    <p className="text-xs text-slate-500">
                      Hiệu lực: {formatDateTime(promo.validFrom)} → {formatDateTime(promo.validTo)}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-semibold text-slate-900">{promo.usageCount} lượt dùng</p>
                    <p className="text-xs text-slate-500">
                      Giới hạn: {promo.usageLimit ?? 'Không giới hạn'}
                    </p>
                    <div className="mt-3 flex flex-wrap justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => startEditPromotion(promo)}
                        className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                      >
                        Sửa
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeletePromotion(promo)}
                        className="rounded-xl bg-red-500 px-3 py-2 text-xs font-semibold text-white hover:bg-red-600"
                      >
                        Xóa
                      </button>
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">
          {editingPromotionId ? 'Cập nhật khuyến mãi' : 'Tạo khuyến mãi mới'}
        </h3>
        <form onSubmit={handlePromotionSubmit} className="space-y-4">
          <div>
            <Label>Mã khuyến mãi</Label>
            <Input
              value={promotionFormData.code}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, code: e.target.value }))}
              placeholder="VD: SUMMER2026"
              disabled={!!editingPromotionId}
            />
          </div>
          <div>
            <Label>Mô tả</Label>
            <Input
              value={promotionFormData.description}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, description: e.target.value }))}
              placeholder="VD: Giảm giá mùa hè"
            />
          </div>
          <div>
            <Label>Loại giảm</Label>
            <Select
              value={promotionFormData.discountType}
              onChange={(e) =>
                setPromotionFormData((prev) => ({ ...prev, discountType: Number(e.target.value) }))
              }
              disabled={!!editingPromotionId}
            >
              <option value={0}>Giảm theo %</option>
              <option value={1}>Giảm tiền</option>
            </Select>
          </div>
          <div>
            <Label>Giá trị giảm</Label>
            <Input
              type="number"
              value={promotionFormData.discountValue}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, discountValue: e.target.value }))}
              placeholder="VD: 10 hoặc 100000"
            />
          </div>
          <div>
            <Label>Giảm tối đa</Label>
            <Input
              type="number"
              value={promotionFormData.maxDiscountAmount}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, maxDiscountAmount: e.target.value }))}
              placeholder="VD: 500000"
            />
          </div>
          <div>
            <Label>Giá trị tối thiểu</Label>
            <Input
              type="number"
              value={promotionFormData.minimumAmount}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, minimumAmount: e.target.value }))}
              placeholder="VD: 1000000"
              disabled={!!editingPromotionId}
            />
          </div>
          <div>
            <Label>Giới hạn lượt dùng</Label>
            <Input
              type="number"
              value={promotionFormData.usageLimit}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, usageLimit: e.target.value }))}
              placeholder="VD: 100"
            />
          </div>
          <div>
            <Label>Hiệu lực từ</Label>
            <Input
              type="date"
              value={promotionFormData.validFrom}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validFrom: e.target.value }))}
              disabled={!!editingPromotionId}
            />
          </div>
          <div>
            <Label>Hiệu lực đến</Label>
            <Input
              type="date"
              value={promotionFormData.validTo}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validTo: e.target.value }))}
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={promotionFormData.isActive}
              onChange={(e) => setPromotionFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
              className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
            />
            Kích hoạt khuyến mãi
          </label>
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              className="flex-1 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              {editingPromotionId ? 'Lưu cập nhật' : 'Tạo khuyến mãi'}
            </button>
            {editingPromotionId && (
              <button
                type="button"
                onClick={startCreatePromotion}
                className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Hủy sửa
              </button>
            )}
          </div>
        </form>
      </aside>
    </div>
  )

  const renderFlightManagement = () => (
    <div className="grid gap-5 lg:grid-cols-[1.1fr_360px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Admin</p>
            <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Quản lý chuyến bay</h2>
            <p className="mt-1 text-sm text-slate-500">CRUD chuyến bay cụ thể (giờ bay, máy bay).</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="date"
              value={adminFlightFilters.date}
              onChange={(e) =>
                setAdminFlightFilters((prev) => ({ ...prev, date: e.target.value }))
              }
              className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none md:w-40"
            />
            <input
              type="text"
              value={adminFlightFilters.from}
              onChange={(e) =>
                setAdminFlightFilters((prev) => ({ ...prev, from: e.target.value }))
              }
              placeholder="Sân bay đi"
              className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none md:w-40"
            />
            <input
              type="text"
              value={adminFlightFilters.to}
              onChange={(e) =>
                setAdminFlightFilters((prev) => ({ ...prev, to: e.target.value }))
              }
              placeholder="Sân bay đến"
              className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none md:w-40"
            />
            <input
              type="text"
              value={adminFlightFilters.code}
              onChange={(e) =>
                setAdminFlightFilters((prev) => ({ ...prev, code: e.target.value }))
              }
              placeholder="Mã chuyến bay"
              className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none md:w-40"
            />
            <button
              type="button"
              onClick={() =>
                setAdminFlightFilters({
                  date: '',
                  from: '',
                  to: '',
                  code: '',
                })
              }
              className="rounded-xl border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Xóa lọc
            </button>
            <button
              type="button"
              onClick={loadAdminFlights}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Làm mới
            </button>
            <button
              type="button"
              onClick={startCreateFlight}
              className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
            >
              + Tạo chuyến bay
            </button>
          </div>
        </div>

        {flightAdminError && (
          <div className="mb-4 rounded-2xl bg-red-50 p-4 text-sm text-red-700">
            {flightAdminError}
          </div>
        )}

        {flightAdminNotice && (
          <div className="mb-4 rounded-2xl bg-emerald-50 p-4 text-sm text-emerald-700">
            {flightAdminNotice}
          </div>
        )}

        {isLoadingFlightsAdmin && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Đang tải danh sách chuyến bay...
          </div>
        )}

        {!isLoadingFlightsAdmin && adminFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Chưa có chuyến bay nào.
          </div>
        )}

        {!isLoadingFlightsAdmin && adminFlights.length > 0 && filteredAdminFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
            Không tìm thấy chuyến bay phù hợp.
          </div>
        )}

        {!isLoadingFlightsAdmin && filteredAdminFlights.length > 0 && (
          <div className="space-y-4">
            {filteredAdminFlights.map((flight) => {
              const seatSummary = getSeatInventorySummary(flight.seatInventory)
              return (
                <article
                  key={flight.flightId}
                  className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                >
                  <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-lg font-bold text-slate-900">{flight.flightNumber || '---'}</p>
                        <span
                          className={`rounded-full px-2 py-1 text-xs font-semibold ${
                            flight.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'
                          }`}
                        >
                          {flight.isActive ? 'Hoạt động' : 'Tạm dừng'}
                        </span>
                      </div>
                      <p className="mt-1 text-sm text-slate-600">
                        {flight.routeCode || '---'} · {flight.aircraftModel || 'Chưa rõ máy bay'}
                      </p>
                      <p className="text-xs text-slate-500">
                        Cất cánh: {formatDateTime(flight.departureTime)}
                      </p>
                      <p className="text-xs text-slate-500">
                        Hạ cánh: {formatDateTime(flight.arrivalTime)}
                      </p>
                      {seatSummary.length > 0 && (
                        <div className="mt-2 flex flex-wrap gap-2">
                          {seatSummary.map((seat, idx) => (
                            <span
                              key={`${flight.flightId}-${idx}`}
                              className="rounded-full bg-blue-50 px-2 py-1 text-xs text-blue-700"
                            >
                              {seat.label}: {Number.isFinite(Number(seat.price)) ? formatCurrency(seat.price) : '--'}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                    <div className="text-right">
                      <div className="mt-3 flex flex-wrap justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => startEditFlight(flight)}
                          className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => handleCancelAdminFlight(flight)}
                          disabled={isCancellingAdminFlightId === flight.flightId}
                          className="rounded-xl border border-amber-300 px-3 py-2 text-xs font-semibold text-amber-700 hover:bg-amber-50 disabled:opacity-60"
                        >
                          {isCancellingAdminFlightId === flight.flightId ? 'Đang hủy...' : 'Hủy chuyến'}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDeleteFlight(flight)}
                          className="rounded-xl bg-red-500 px-3 py-2 text-xs font-semibold text-white hover:bg-red-600"
                        >
                          Xóa
                        </button>
                      </div>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">
          {editingFlightId ? 'Cập nhật chuyến bay' : 'Tạo chuyến bay mới'}
        </h3>
        <form onSubmit={handleFlightSubmit} className="space-y-4">
          <div>
            <Label>Số hiệu chuyến bay</Label>
            <Input
              value={flightFormData.flightNumber}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, flightNumber: e.target.value }))}
              placeholder="VD: VN211"
            />
          </div>
          <div>
            <Label>Tuyến bay</Label>
            <Select
              value={flightFormData.routeId}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, routeId: e.target.value }))}
              disabled={!!editingFlightId}
            >
              <option value="">Chọn tuyến bay</option>
              {adminRoutes.map((route) => (
                <option key={route.routeId} value={route.routeId}>
                  {route.departureAirport} → {route.arrivalAirport}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label>Máy bay</Label>
            <Select
              value={flightFormData.aircraftId}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, aircraftId: e.target.value }))}
            >
              <option value="">Chọn máy bay</option>
              {aircrafts.map((aircraft) => (
                <option key={aircraft.aircraftId} value={aircraft.aircraftId}>
                  {aircraft.model} · {aircraft.registrationNumber}
                </option>
              ))}
            </Select>
          </div>
          <div>
            <Label>Giờ cất cánh</Label>
            <Input
              type="datetime-local"
              value={flightFormData.departureTime}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, departureTime: e.target.value }))}
            />
          </div>
          <div>
            <Label>Giờ hạ cánh</Label>
            <Input
              type="datetime-local"
              value={flightFormData.arrivalTime}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, arrivalTime: e.target.value }))}
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={flightFormData.isActive}
              onChange={(e) => setFlightFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
              className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
            />
            Kích hoạt chuyến bay
          </label>
          <p className="text-xs text-slate-500">
            Giá vé hiển thị theo hạng ghế từ hệ thống; backend hiện chưa có endpoint chỉnh giá theo chuyến bay.
          </p>
          <div className="flex flex-wrap gap-2">
            <button
              type="submit"
              className="flex-1 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              {editingFlightId ? 'Lưu cập nhật' : 'Tạo chuyến bay'}
            </button>
            {editingFlightId && (
              <button
                type="button"
                onClick={startCreateFlight}
                className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Hủy sửa
              </button>
            )}
          </div>
        </form>
      </aside>
    </div>
  )

  const renderTemplateManagement = () => (
    <div className="space-y-5">
      {/* Header */}
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="mb-6">
          <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Admin</p>
          <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Quản lý Flight Templates</h2>
          <p className="mt-1 text-sm text-slate-500">
            Tạo template chuyến bay theo thứ trong tuần và tự động sinh chuyến bay từ template
          </p>
        </div>

        {adminNotice && (
          <div className="mb-4 rounded-xl bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-[#1E40AF]">
            <div className="flex items-start gap-2">
              <span className="text-lg">ℹ️</span>
              <div className="flex-1">{adminNotice}</div>
            </div>
          </div>
        )}

        {apiError && (
          <div className="mb-4 rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            <div className="flex items-start gap-2">
              <span className="text-lg">⚠️</span>
              <div className="flex-1 whitespace-pre-wrap">{apiError}</div>
            </div>
          </div>
        )}

        {/* Form tạo template */}
        <div className="mb-6 rounded-xl border border-slate-200 bg-slate-50 p-5">
          <h3 className="mb-4 text-lg font-bold text-slate-900">📋 Tạo Template Mới</h3>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <Label>Tên Template</Label>
              <Input
                placeholder="VD: Template Tuần Thường"
                value={templateFormData.name}
                onChange={(e) =>
                  setTemplateFormData((prev) => ({ ...prev, name: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>Mô tả</Label>
              <Input
                placeholder="VD: Lịch bay cho các ngày thường"
                value={templateFormData.description}
                onChange={(e) =>
                  setTemplateFormData((prev) => ({ ...prev, description: e.target.value }))
                }
              />
            </div>
          </div>

          <div className="mt-4">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={templateFormData.isActive}
                onChange={(e) =>
                  setTemplateFormData((prev) => ({ ...prev, isActive: e.target.checked }))
                }
                className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
              />
              <span className="text-sm text-slate-700">Kích hoạt template</span>
            </label>
          </div>

          <button
            type="button"
            onClick={async () => {
              if (!templateFormData.name) {
                setApiError('Vui lòng nhập tên template')
                return
              }

              if (templateSlots.length === 0) {
                setApiError('Vui lòng thêm ít nhất 1 chuyến bay vào template')
                return
              }

              try {
                const templateData = {
                  name: templateFormData.name,
                  description: templateFormData.description,
                  isActive: templateFormData.isActive,
                  details: templateSlots.map((slot) => {
                    let prefix = 'FL'
                    if (slot.flightDefinition.flightNumber) {
                      const match = slot.flightDefinition.flightNumber.match(/^([A-Z]+)/)
                      if (match) {
                        prefix = match[1]
                      }
                    }

                    // Đảm bảo time format là HH:mm:ss
                    const formatTime = (time) => {
                      if (!time) return '08:00:00'
                      // Nếu có format HH:mm:ss.sssssss, chỉ lấy HH:mm:ss
                      return time.substring(0, 8)
                    }

                    return {
                      routeId: slot.flightDefinition.routeId,
                      aircraftId: slot.flightDefinition.selectedAircraftId || slot.flightDefinition.defaultAircraftId || 1,
                      dayOfWeek: slot.dayOfWeek,
                      departureTime: formatTime(slot.flightDefinition.departureTime),
                      arrivalTime: formatTime(slot.flightDefinition.arrivalTime),
                      flightNumberPrefix: prefix,
                      flightNumberSuffix: '',
                    }
                  }),
                }

                console.log('📤 Creating template:', templateData)

                await createFlightTemplate(templateData)
                setAdminNotice(`✅ Đã tạo template "${templateFormData.name}" với ${templateSlots.length} chuyến bay/tuần!`)
                setTemplateFormData({ name: '', description: '', isActive: true })
                setTemplateSlots([])

                // Reload templates
                const templates = await getFlightTemplates()
                setFlightTemplates(Array.isArray(templates) ? templates : [])
              } catch (error) {
                setApiError(error.message || 'Lỗi khi tạo template')
              }
            }}
            className="mt-4 rounded-xl bg-[#1E40AF] px-6 py-3 text-sm font-semibold text-white hover:bg-blue-800"
          >
            💾 Lưu Template ({templateSlots.length} chuyến bay/tuần)
          </button>
        </div>

        {/* Danh sách templates */}
        <div className="mb-6">
          <h3 className="mb-4 text-lg font-bold text-slate-900">📚 Danh Sách Templates</h3>
          {isLoadingTemplates && (
            <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
              Đang tải danh sách templates...
            </div>
          )}
          {!isLoadingTemplates && flightTemplates.length === 0 && (
            <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
              Chưa có template nào. Hãy tạo template đầu tiên!
            </div>
          )}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {flightTemplates.map((template) => (
              <div
                key={template.templateId || template.id || template.Id}
                className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm hover:shadow-md transition"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h4 className="font-semibold text-slate-900">{template.name}</h4>
                    <p className="mt-1 text-xs text-slate-500">{template.description}</p>
                    <div className="mt-2 flex items-center gap-2">
                      <span className="text-xs font-medium text-slate-600">
                        📅 {template.details?.length || 0} chuyến/tuần
                      </span>
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                          template.isActive
                            ? 'bg-green-100 text-green-700'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {template.isActive ? '✓ Hoạt động' : '⏸ Tạm dừng'}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    onClick={async () => {
                      try {
                        setApiError('')
                        // Thử nhiều field có thể có
                        const templateId = template.templateId || template.id || template.Id
                        console.log('🔍 Template object:', template)
                        console.log('🔍 Template ID:', templateId)
                        
                        if (!templateId) {
                          setApiError('Không tìm thấy ID của template')
                          return
                        }
                        
                        const templateDetail = await getFlightScheduleTemplate(templateId)
                        console.log('📋 Template Detail:', templateDetail)
                        setViewingTemplateDetail(templateDetail)
                      } catch (error) {
                        setApiError(error.message || 'Lỗi khi tải chi tiết template')
                      }
                    }}
                    className="flex-1 rounded-lg bg-slate-600 px-3 py-2 text-sm font-semibold text-white hover:bg-slate-700"
                  >
                    👁️ Xem chi tiết
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedTemplate(template)
                      setGenerateFormData((prev) => ({
                        ...prev,
                        templateId: template.templateId || template.id || template.Id,
                      }))
                    }}
                    className="flex-1 rounded-lg bg-[#1E40AF] px-3 py-2 text-sm font-semibold text-white hover:bg-blue-800"
                  >
                    🚀 Sinh chuyến bay
                  </button>
                  <button
                    type="button"
                    onClick={async () => {
                      if (window.confirm(`Xóa template "${template.name}"?`)) {
                        try {
                          const templateId = template.templateId || template.id || template.Id
                          await deleteFlightTemplate(templateId)
                          setAdminNotice(`✅ Đã xóa template "${template.name}"`)
                          const templates = await getFlightTemplates()
                          setFlightTemplates(Array.isArray(templates) ? templates : [])
                        } catch (error) {
                          setApiError(error.message || 'Lỗi khi xóa template')
                        }
                      }
                    }}
                    className="rounded-lg bg-red-500 px-3 py-2 text-sm font-semibold text-white hover:bg-red-600"
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Form sinh chuyến bay từ template */}
        {selectedTemplate && (
          <div className="rounded-xl border-2 border-green-500 bg-green-50 p-5">
            <h3 className="mb-2 text-lg font-bold text-slate-900">
              🚀 Sinh Chuyến Bay từ Template
            </h3>
            <p className="mb-4 text-sm text-green-700">
              Template: <strong>{selectedTemplate.name}</strong> ({selectedTemplate.details?.length || 0} chuyến/tuần)
            </p>

            <div className="rounded-lg bg-white p-4 mb-4">
              <p className="text-sm text-slate-700 mb-2">
                <strong>Template này sẽ tạo chuyến bay theo thứ:</strong>
              </p>
              <div className="flex flex-wrap gap-2">
                {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                  const count = selectedTemplate.details?.filter(d => d.dayOfWeek === dayIndex).length || 0
                  return count > 0 ? (
                    <span key={dayIndex} className="rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-700">
                      {getWeekdayName(dayIndex)}: {count} chuyến
                    </span>
                  ) : null
                })}
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <div>
                <Label>Ngày bắt đầu tuần (Thứ 2)</Label>
                <Input
                  type="date"
                  value={generateFormData.weekStartDate}
                  onChange={(e) =>
                    setGenerateFormData((prev) => ({ ...prev, weekStartDate: e.target.value }))
                  }
                />
                <p className="mt-1 text-xs text-slate-500">Chọn ngày Thứ 2 để bắt đầu</p>
              </div>
              <div>
                <Label>Số tuần muốn sinh</Label>
                <Input
                  type="number"
                  min="1"
                  max="52"
                  value={generateFormData.numberOfWeeks}
                  onChange={(e) =>
                    setGenerateFormData((prev) => ({
                      ...prev,
                      numberOfWeeks: parseInt(e.target.value, 10),
                    }))
                  }
                />
                <p className="mt-1 text-xs text-slate-500">
                  Tổng: {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} chuyến bay
                </p>
              </div>
              <div className="flex items-end">
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      setApiError('')
                      setAdminNotice('')
                      
                      // Validate dữ liệu trước khi gửi
                      const templateId = Number(generateFormData.templateId)
                      const numberOfWeeks = Number(generateFormData.numberOfWeeks)
                      
                      if (!templateId || templateId <= 0) {
                        setApiError('Template ID không hợp lệ')
                        return
                      }
                      
                      if (!numberOfWeeks || numberOfWeeks <= 0) {
                        setApiError('Số tuần phải lớn hơn 0')
                        return
                      }
                      
                      if (!generateFormData.weekStartDate) {
                        setApiError('Vui lòng chọn ngày bắt đầu')
                        return
                      }

                      // Chuyển date sang ISO datetime với timezone UTC
                      const weekStartDateTime = new Date(generateFormData.weekStartDate + 'T00:00:00Z').toISOString()

                      setAdminNotice('⏳ Đang sinh chuyến bay từ template...')
                      
                      const result = await generateFlightsFromTemplate({
                        templateId: templateId,
                        weekStartDate: weekStartDateTime, // ISO datetime với timezone
                        numberOfWeeks: numberOfWeeks,
                      })
                      
                      console.log('📊 Result from API:', result)
                      
                      // Kiểm tra nếu có lỗi trong response (backend trả 200 nhưng có error)
                      if (result.error || result.message?.includes('trùng') || result.message?.includes('đã tồn tại')) {
                        const errorMsg = result.error || result.message || 'Có lỗi xảy ra khi sinh chuyến bay'
                        setApiError(`❌ ${errorMsg}`)
                        setAdminNotice('')
                        return
                      }
                      
                      // Thành công
                      setAdminNotice(
                        `✅ Thành công! Đã sinh ${result.totalFlightsGenerated || 0} chuyến bay! ` +
                        (result.totalFlightsSkipped > 0 ? `(Bỏ qua ${result.totalFlightsSkipped} chuyến trùng)` : '')
                      )
                      setSelectedTemplate(null)
                    } catch (error) {
                      console.error('❌ Lỗi khi sinh chuyến bay:', error)
                      
                      // Xử lý các loại lỗi khác nhau
                      let errorMessage = 'Lỗi khi sinh chuyến bay'
                      
                      if (error.message) {
                        // Kiểm tra lỗi trùng chuyến bay
                        if (error.message.includes('trùng') || error.message.includes('đã tồn tại')) {
                          errorMessage = `❌ ${error.message}`
                        } 
                        // Kiểm tra lỗi validation
                        else if (error.message.includes('ValidationException') || error.message.includes('validation')) {
                          errorMessage = `⚠️ Lỗi dữ liệu: ${error.message}`
                        }
                        // Lỗi khác
                        else {
                          errorMessage = `❌ ${error.message}`
                        }
                      }
                      
                      // Hiển thị chi tiết lỗi từ response body nếu có
                      if (error.responseBody) {
                        console.log('📋 Chi tiết lỗi:', error.responseBody)
                        if (error.responseBody.detail) {
                          errorMessage = `❌ ${error.responseBody.detail}`
                        } else if (error.responseBody.title) {
                          errorMessage = `❌ ${error.responseBody.title}`
                        }
                      }
                      
                      setApiError(errorMessage)
                      setAdminNotice('')
                    }
                  }}
                  className="w-full rounded-xl bg-green-600 px-4 py-3 text-sm font-semibold text-white hover:bg-green-700"
                >
                  🚀 Sinh {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} Chuyến Bay
                </button>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setSelectedTemplate(null)}
              className="mt-3 text-sm text-slate-600 hover:text-slate-900 underline"
            >
              ← Hủy và chọn template khác
            </button>
          </div>
        )}
      </section>

      {/* Khung template theo thứ */}
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <div className="mb-4">
          <h3 className="text-lg font-bold text-slate-900">📅 Khung Template Theo Thứ</h3>
          <p className="text-sm text-slate-500">
            Chọn flight definition và thêm vào thứ tương ứng (Thứ 2 - Chủ nhật)
          </p>
        </div>

        <div className="grid gap-5 lg:grid-cols-[300px_1fr]">
          {/* Danh sách flight definitions */}
          <aside className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <h4 className="mb-3 text-sm font-bold text-slate-900">Flight Definitions</h4>
            <div className="max-h-[600px] space-y-2 overflow-y-auto">
              {isLoadingFlightDefinitions && (
                <div className="text-xs text-slate-500">Đang tải...</div>
              )}
              {!isLoadingFlightDefinitions && flightDefinitions.length === 0 && (
                <div className="text-xs text-slate-500">Chưa có flight definitions</div>
              )}
              {flightDefinitions.map((def) => (
                <div
                  key={def.id}
                  className="rounded-lg border border-slate-200 bg-white p-3 text-xs"
                >
                  <p className="font-semibold text-[#1E40AF]">{def.flightNumber}</p>
                  <p className="mt-1 text-slate-700">
                    {def.departureAirportCode} → {def.arrivalAirportCode}
                  </p>
                  <p className="text-slate-500">
                    {def.departureTime} - {def.arrivalTime}
                  </p>
                  
                  {/* Chọn máy bay */}
                  <select
                    id={`aircraft-${def.id}`}
                    className="mt-2 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                    defaultValue=""
                  >
                    <option value="">
                      {isLoadingAircrafts ? 'Đang tải...' : `Chọn máy bay... (${aircrafts.length})`}
                    </option>
                    {aircrafts.map((aircraft) => (
                      <option key={aircraft.id || aircraft.aircraftId} value={aircraft.id || aircraft.aircraftId}>
                        {aircraft.registrationNumber || aircraft.model || `Aircraft ${aircraft.id || aircraft.aircraftId}`}
                      </option>
                    ))}
                  </select>
                  
                  {/* Chọn thứ */}
                  <select
                    className="mt-2 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                    onChange={(e) => {
                      const dayOfWeek = parseInt(e.target.value, 10)
                      if (dayOfWeek >= 0) {
                        // Lấy aircraft được chọn
                        const aircraftSelect = document.getElementById(`aircraft-${def.id}`)
                        const selectedAircraftId = aircraftSelect ? parseInt(aircraftSelect.value, 10) : null
                        
                        if (!selectedAircraftId) {
                          setApiError('Vui lòng chọn máy bay trước!')
                          setTimeout(() => setApiError(''), 3000)
                          e.target.value = ''
                          return
                        }
                        
                        // Kiểm tra trùng
                        const isDuplicate = templateSlots.some(
                          (slot) => slot.flightDefinition.id === def.id && slot.dayOfWeek === dayOfWeek
                        )
                        if (isDuplicate) {
                          setApiError(`Flight ${def.flightNumber} đã tồn tại trong ${getWeekdayName(dayOfWeek)}!`)
                          setTimeout(() => setApiError(''), 3000)
                          e.target.value = ''
                          return
                        }

                        setTemplateSlots((prev) => [
                          ...prev,
                          {
                            id: `${def.id}-${dayOfWeek}-${Date.now()}`,
                            flightDefinition: { ...def, selectedAircraftId },
                            dayOfWeek,
                          },
                        ])
                        setAdminNotice(`✅ Đã thêm ${def.flightNumber} vào ${getWeekdayName(dayOfWeek)}`)
                        setTimeout(() => setAdminNotice(''), 2000)
                        e.target.value = ''
                        aircraftSelect.value = ''
                      }
                    }}
                  >
                    <option value="">Chọn thứ...</option>
                    <option value="0">Thứ 2</option>
                    <option value="1">Thứ 3</option>
                    <option value="2">Thứ 4</option>
                    <option value="3">Thứ 5</option>
                    <option value="4">Thứ 6</option>
                    <option value="5">Thứ 7</option>
                    <option value="6">Chủ nhật</option>
                  </select>
                </div>
              ))}
            </div>
          </aside>

          {/* Khung template theo thứ */}
          <div className="grid grid-cols-7 gap-2">
            {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
              const slotsForDay = templateSlots.filter((slot) => slot.dayOfWeek === dayIndex)
              return (
                <div
                  key={dayIndex}
                  className="rounded-xl border border-slate-200 bg-slate-50 p-3"
                >
                  <div className="mb-2 text-center">
                    <p className="text-xs font-bold text-slate-900">{getWeekdayName(dayIndex)}</p>
                    <p className="text-xs text-slate-500">({slotsForDay.length} chuyến)</p>
                  </div>
                  <div className="space-y-2">
                    {slotsForDay.map((slot) => (
                      <div
                        key={slot.id}
                        className="rounded-lg border border-blue-200 bg-blue-50 p-2"
                      >
                        <p className="text-xs font-semibold text-[#1E40AF]">
                          {slot.flightDefinition.flightNumber}
                        </p>
                        <p className="text-xs text-slate-600">
                          {slot.flightDefinition.departureAirportCode} → {slot.flightDefinition.arrivalAirportCode}
                        </p>
                        <p className="text-xs text-slate-500">
                          {slot.flightDefinition.departureTime?.substring(0, 5)}
                        </p>
                        {slot.flightDefinition.selectedAircraftId && (
                          <p className="text-xs text-green-700 font-medium">
                            ✈️ Aircraft ID: {slot.flightDefinition.selectedAircraftId}
                          </p>
                        )}
                        <button
                          type="button"
                          onClick={() => {
                            setTemplateSlots((prev) => prev.filter((s) => s.id !== slot.id))
                            setAdminNotice('Đã xóa chuyến bay khỏi template')
                            setTimeout(() => setAdminNotice(''), 2000)
                          }}
                          className="mt-1 w-full rounded bg-red-100 px-2 py-1 text-xs text-red-700 hover:bg-red-200"
                        >
                          Xóa
                        </button>
                      </div>
                    ))}
                    {slotsForDay.length === 0 && (
                      <div className="rounded-lg border border-dashed border-slate-300 p-3 text-center text-xs text-slate-400">
                        Chưa có chuyến bay
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </section>

      {/* Modal xem chi tiết template */}
      {viewingTemplateDetail && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl">
            <div className="mb-4 flex items-start justify-between">
              <div>
                <h3 className="text-2xl font-bold text-slate-900">
                  📋 {viewingTemplateDetail.name}
                </h3>
                <p className="mt-1 text-sm text-slate-600">{viewingTemplateDetail.description}</p>
                <div className="mt-2 flex items-center gap-2">
                  <span
                    className={`rounded-full px-3 py-1 text-xs font-medium ${
                      viewingTemplateDetail.isActive
                        ? 'bg-green-100 text-green-700'
                        : 'bg-slate-100 text-slate-600'
                    }`}
                  >
                    {viewingTemplateDetail.isActive ? '✓ Hoạt động' : '⏸ Tạm dừng'}
                  </span>
                  <span className="rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-700">
                    {viewingTemplateDetail.details?.length || 0} chuyến bay/tuần
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setViewingTemplateDetail(null)}
                className="rounded-lg bg-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-300"
              >
                ✕ Đóng
              </button>
            </div>

            {/* Hiển thị theo thứ */}
            <div className="mt-6">
              <h4 className="mb-3 text-lg font-bold text-slate-900">Lịch bay theo thứ</h4>
              <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
                {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                  const flightsForDay = (viewingTemplateDetail.details || []).filter(
                    (d) => d.dayOfWeek === dayIndex
                  )
                  return (
                    <div
                      key={dayIndex}
                      className="rounded-xl border border-slate-200 bg-slate-50 p-3"
                    >
                      <div className="mb-2 text-center">
                        <p className="text-sm font-bold text-slate-900">
                          {getWeekdayName(dayIndex)}
                        </p>
                        <p className="text-xs text-slate-500">({flightsForDay.length} chuyến)</p>
                      </div>
                      <div className="space-y-2">
                        {flightsForDay.map((detail, idx) => (
                          <div
                            key={idx}
                            className="rounded-lg border border-blue-200 bg-blue-50 p-2"
                          >
                            <p className="text-xs font-semibold text-[#1E40AF]">
                              {detail.flightNumberPrefix || 'FL'}
                            </p>
                            <p className="text-xs text-slate-600">
                              Route ID: {detail.routeId}
                            </p>
                            <p className="text-xs text-slate-600">
                              Aircraft ID: {detail.aircraftId}
                            </p>
                            <p className="text-xs text-slate-500">
                              {detail.departureTime} → {detail.arrivalTime}
                            </p>
                          </div>
                        ))}
                        {flightsForDay.length === 0 && (
                          <div className="rounded-lg border border-dashed border-slate-300 p-3 text-center text-xs text-slate-400">
                            Không có chuyến bay
                          </div>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Hiển thị raw data */}
            <div className="mt-6">
              <h4 className="mb-2 text-sm font-bold text-slate-900">📊 Raw Data (JSON)</h4>
              <pre className="max-h-60 overflow-auto rounded-lg bg-slate-900 p-4 text-xs text-green-400">
                {JSON.stringify(viewingTemplateDetail, null, 2)}
              </pre>
            </div>
          </div>
        </div>
      )}
    </div>
  )

  return (
    <main className="mx-auto min-h-screen w-full max-w-6xl px-4 py-6 md:px-6 md:py-10">
      {renderHeader()}
      {screen !== 'login' && (
        <div className="mb-5 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setScreen('search')}
            className={`rounded-full px-3 py-1 text-xs font-semibold ${
              screen === 'search' || screen === 'list' || screen === 'passenger' || screen === 'payment'
                ? 'bg-[#1E40AF] text-white'
                : 'bg-slate-200 text-slate-500'
            }`}
          >
            Khách hàng
          </button>
          <button
            type="button"
            onClick={() => setScreen('history')}
            className={`rounded-full px-3 py-1 text-xs font-semibold ${
              screen === 'history' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
            }`}
          >
            Vé của tôi
          </button>
          <button
            type="button"
            onClick={() => setScreen('saved-passengers')}
            className={`rounded-full px-3 py-1 text-xs font-semibold ${
              screen === 'saved-passengers' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
            }`}
          >
            Hành khách đã lưu
          </button>
          {isAdmin && (
            <>
              <button
                type="button"
                onClick={() => setScreen('flights')}
                className={`rounded-full px-3 py-1 text-xs font-semibold ${
                  screen === 'flights' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
                }`}
              >
                ✈️ Quản lý Chuyến bay
              </button>
              <button
                type="button"
                onClick={() => setScreen('templates')}
                className={`rounded-full px-3 py-1 text-xs font-semibold ${
                  screen === 'templates' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
                }`}
              >
                📋 Quản lý Templates
              </button>
              <button
                type="button"
                onClick={() => setScreen('promotions')}
                className={`rounded-full px-3 py-1 text-xs font-semibold ${
                  screen === 'promotions' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
                }`}
              >
                🎁 Quản lý Khuyến mãi
              </button>
            </>
          )}
        </div>
      )}
      {screen !== 'login' &&
        (screen === 'search' || screen === 'list' || screen === 'passenger' || screen === 'payment') && (
        <div className="mb-5 flex flex-wrap gap-2">
          {[
            { key: 'search', label: 'Tìm kiếm' },
            { key: 'list', label: 'Danh sách' },
            { key: 'passenger', label: 'Hành khách' },
            { key: 'payment', label: 'Thanh toán' },
          ].map((step, idx) => {
            const activeOrder = ['search', 'list', 'passenger', 'payment'].indexOf(screen)
            const stepOrder = ['search', 'list', 'passenger', 'payment'].indexOf(step.key)
            return (
              <div
                key={step.key}
                className={`rounded-full px-3 py-1 text-xs font-semibold ${
                  stepOrder <= activeOrder
                    ? 'bg-[#1E40AF] text-white'
                    : 'bg-slate-200 text-slate-500'
                }`}
              >
                {idx + 1}. {step.label}
              </div>
            )
          })}
        </div>
      )}

      {screen === 'login' && renderLogin()}
      {screen === 'forgot-password' && renderForgotPassword()}
      {screen === 'reset-password' && renderResetPassword()}
      {screen === 'register' && renderRegister()}
      {screen === 'search' && renderSearch()}
      {screen === 'list' && renderFlightList()}
      {screen === 'passenger' && selectedFlight && renderPassenger()}
      {screen === 'payment' && selectedFlight && renderPayment()}
      {screen === 'history' && renderHistory()}
      {screen === 'saved-passengers' && renderSavedPassengers()}
      {screen === 'flights' && isAdmin && renderFlightManagement()}
      {screen === 'templates' && isAdmin && renderTemplateManagement()}
      {screen === 'promotions' && isAdmin && renderPromotionManagement()}

      {/* ===== MODAL: NANG HANG VE ===== */}
      {upgradeModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4"
          style={{ background: 'rgba(15,23,42,0.65)', backdropFilter: 'blur(6px)' }}
          onClick={(e) => { if (e.target === e.currentTarget) closeUpgradeModal() }}
        >
          <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-indigo-600 to-purple-600 px-6 py-4">
              <div>
                <h3 className="text-lg font-bold text-white">⬆️ Nâng hạng vé</h3>
                <p className="text-xs text-indigo-200">
                  {upgradeModal.ticket?.ticketNumber || `Vé #${upgradeModal.ticket?.ticketId}`} · {upgradeModal.ticket?.passengerName}
                </p>
              </div>
              <button onClick={closeUpgradeModal} className="text-indigo-200 hover:text-white text-xl font-bold">✕</button>
            </div>
            <div className="p-6">
              <div className="mb-5 flex items-center gap-2 text-xs">
                {['select', 'quote', 'payment'].map((s, i) => (
                  <div key={s} className="flex items-center gap-2">
                    {i > 0 && <div className="h-px w-6 bg-slate-200" />}
                    <div className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold ${
                      upgradeModal.step === s ? 'bg-indigo-600 text-white'
                      : ['select', 'quote', 'payment'].indexOf(upgradeModal.step) > i ? 'bg-indigo-200 text-indigo-700'
                      : 'bg-slate-100 text-slate-400'
                    }`}>{i + 1}</div>
                    <span className={upgradeModal.step === s ? 'font-semibold text-slate-800' : 'text-slate-400'}>
                      {i === 0 ? 'Chọn hạng' : i === 1 ? 'Báo giá' : 'Thanh toán'}
                    </span>
                  </div>
                ))}
              </div>

              {upgradeModal.step === 'select' && (
                <div className="space-y-3">
                  <p className="text-sm text-slate-600">Chọn hạng ghế bạn muốn nâng lên:</p>
                  <div className="grid grid-cols-2 gap-3">
                    {seatClasses.map((cls) => {
                      const currentClass = upgradeModal.ticket?.seatClass || ''
                      const isCurrent = currentClass.toLowerCase().includes(cls.name.toLowerCase())
                      const isSelected = upgradeModal.selectedClassId === cls.id
                      return (
                        <button
                          key={cls.id}
                          type="button"
                          disabled={isCurrent}
                          onClick={() => setUpgradeModal((prev) => ({ ...prev, selectedClassId: cls.id }))}
                          className={`rounded-xl border-2 p-4 text-left transition-all ${
                            isCurrent ? 'cursor-not-allowed border-slate-200 bg-slate-50 opacity-50'
                            : isSelected ? 'border-indigo-500 bg-indigo-50'
                            : 'border-slate-200 hover:border-indigo-300 hover:bg-indigo-50'
                          }`}
                        >
                          <div className="text-2xl">{cls.icon}</div>
                          <div className="mt-1 font-semibold text-slate-800">{cls.label}</div>
                          {isCurrent && <div className="text-xs text-slate-500">(Hạng hiện tại)</div>}
                        </button>
                      )
                    })}
                  </div>
                  {upgradeError && <p className="text-xs text-red-600">{upgradeError}</p>}
                  <button
                    type="button"
                    disabled={!upgradeModal.selectedClassId || upgradeLoading}
                    onClick={handleGetUpgradeQuote}
                    className="w-full rounded-xl bg-indigo-600 py-3 text-sm font-bold text-white hover:bg-indigo-700 disabled:opacity-50"
                  >
                    {upgradeLoading ? 'Đang tính...' : 'Xem báo giá →'}
                  </button>
                </div>
              )}

              {upgradeModal.step === 'quote' && upgradeModal.quote && (
                <div className="space-y-4">
                  <div className="rounded-xl bg-indigo-50 p-4 space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Giá vé hiện tại</span>
                      <span className="font-semibold">{formatCurrency(upgradeModal.quote.currentTicketPrice || upgradeModal.quote.paidAmountOfOldTicket || 0)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Giá hạng mới</span>
                      <span className="font-semibold">{formatCurrency(upgradeModal.quote.newClassPrice || upgradeModal.quote.newTicketAmount || 0)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Chênh lệch giá</span>
                      <span className="font-semibold text-orange-600">{formatCurrency(upgradeModal.quote.fareDifference || upgradeModal.quote.priceDifference || 0)}</span>
                    </div>
                    {(upgradeModal.quote.upgradeFee > 0) && (
                      <div className="flex justify-between text-sm">
                        <span className="text-slate-600">Phí nâng hạng</span>
                        <span className="font-semibold text-orange-600">{formatCurrency(upgradeModal.quote.upgradeFee)}</span>
                      </div>
                    )}
                    <div className="border-t border-indigo-200 pt-2 flex justify-between">
                      <span className="font-bold text-slate-800">Tổng thanh toán</span>
                      <span className="font-bold text-indigo-700 text-base">{formatCurrency(upgradeModal.quote.upgradeAmount || 0)}</span>
                    </div>
                    <p className="text-xs text-slate-500">Đơn vị: {upgradeModal.quote.currency || 'VND'}</p>
                  </div>
                  {upgradeError && <p className="text-xs text-red-600">{upgradeError}</p>}
                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => setUpgradeModal((prev) => ({ ...prev, step: 'select' }))}
                      className="flex-1 rounded-xl border border-slate-300 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
                    >
                      ← Quay lại
                    </button>
                    <button
                      type="button"
                      disabled={upgradeLoading}
                      onClick={handleCreateUpgradeRequest}
                      className="flex-1 rounded-xl bg-indigo-600 py-3 text-sm font-bold text-white hover:bg-indigo-700 disabled:opacity-50"
                    >
                      {upgradeLoading ? 'Đang xử lý...' : 'Xác nhận nâng hạng →'}
                    </button>
                  </div>
                </div>
              )}

              {upgradeModal.step === 'payment' && (
                <div className="space-y-4">
                  <div className="rounded-xl bg-green-50 p-4 text-center">
                    <div className="text-3xl mb-2">✅</div>
                    <p className="font-semibold text-green-800">Yêu cầu nâng hạng đã được tạo!</p>
                    {upgradeModal.request?.expiresAt && (
                      <p className="text-xs text-green-600 mt-1">
                        Hết hạn: {new Date(upgradeModal.request.expiresAt).toLocaleString('vi-VN')}
                      </p>
                    )}
                  </div>
                  <div className="rounded-xl bg-slate-50 p-4 text-sm space-y-1">
                    <div className="flex justify-between">
                      <span className="text-slate-500">Mã yêu cầu</span>
                      <span className="font-semibold">#{upgradeModal.request?.requestId}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">Số tiền thanh toán</span>
                      <span className="font-bold text-indigo-700">{formatCurrency(upgradeModal.request?.priceDifference || 0)}</span>
                    </div>
                  </div>
                  {upgradeError && <p className="text-xs text-red-600">{upgradeError}</p>}
                  <button
                    type="button"
                    disabled={upgradeLoading}
                    onClick={handleInitiateUpgradePayment}
                    className="w-full rounded-xl bg-indigo-600 py-3 text-sm font-bold text-white hover:bg-indigo-700 disabled:opacity-50"
                  >
                    {upgradeLoading ? 'Đang khởi tạo...' : '💳 Thanh toán VNPay'}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ===== MODAL: DOI CHUYEN BAY ===== */}
      {changeFlightModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4"
          style={{ background: 'rgba(15,23,42,0.65)', backdropFilter: 'blur(6px)' }}
          onClick={(e) => { if (e.target === e.currentTarget) closeChangeFlightModal() }}
        >
          <div className="w-full max-w-lg rounded-2xl bg-white shadow-2xl" style={{ maxHeight: '90vh', overflowY: 'auto' }} onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between rounded-t-2xl bg-gradient-to-r from-emerald-600 to-teal-600 px-6 py-4 sticky top-0 z-10">
              <div>
                <h3 className="text-lg font-bold text-white">🔄 Đổi chuyến bay</h3>
                <p className="text-xs text-emerald-200">Booking #{changeFlightModal.booking?.bookingId}</p>
              </div>
              <button onClick={closeChangeFlightModal} className="text-emerald-200 hover:text-white text-xl font-bold">✕</button>
            </div>
            <div className="p-6">
              <div className="mb-5 flex items-center gap-2 text-xs">
                {['select-flight', 'quote', 'confirm'].map((s, i) => (
                  <div key={s} className="flex items-center gap-2">
                    {i > 0 && <div className="h-px w-6 bg-slate-200" />}
                    <div className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold ${
                      changeFlightModal.step === s ? 'bg-emerald-600 text-white'
                      : ['select-flight', 'quote', 'confirm'].indexOf(changeFlightModal.step) > i ? 'bg-emerald-200 text-emerald-700'
                      : 'bg-slate-100 text-slate-400'
                    }`}>{i + 1}</div>
                    <span className={changeFlightModal.step === s ? 'font-semibold text-slate-800' : 'text-slate-400'}>
                      {i === 0 ? 'Tìm chuyến' : i === 1 ? 'Báo giá' : 'Xác nhận'}
                    </span>
                  </div>
                ))}
              </div>

              {changeFlightModal.step === 'select-flight' && (
                <div className="space-y-4">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-xs font-semibold text-slate-600">Chặng bay</label>
                      <select
                        value={changeFlightModal.legType}
                        onChange={(e) => setChangeFlightModal((prev) => ({ ...prev, legType: Number(e.target.value), options: null }))}
                        className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                      >
                        <option value={0}>Chặng đi (Outbound)</option>
                        <option value={1}>Chặng về (Return)</option>
                      </select>
                    </div>
                    <div>
                      <label className="text-xs font-semibold text-slate-600">Ngày bay mới</label>
                      <input
                        type="date"
                        value={changeFlightModal.departureDate}
                        min={new Date().toISOString().split('T')[0]}
                        onChange={(e) => setChangeFlightModal((prev) => ({ ...prev, departureDate: e.target.value, options: null }))}
                        className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none"
                      />
                    </div>
                  </div>
                  <button
                    type="button"
                    disabled={changeFlightLoading}
                    onClick={handleGetChangeOptions}
                    className="w-full rounded-xl bg-emerald-600 py-2.5 text-sm font-bold text-white hover:bg-emerald-700 disabled:opacity-50"
                  >
                    {changeFlightLoading ? 'Đang tìm...' : '🔍 Tìm chuyến bay khả dụng'}
                  </button>
                  {changeFlightError && <p className="text-xs text-red-600">{changeFlightError}</p>}
                  {changeFlightModal.options !== null && (
                    <div className="space-y-2">
                      <p className="text-xs font-semibold text-slate-600">
                        {changeFlightModal.options.length > 0
                          ? `Tìm thấy ${changeFlightModal.options.length} chuyến bay:`
                          : 'Không có chuyến bay nào phù hợp.'}
                      </p>
                      {changeFlightModal.options.map((flight) => (
                        <button
                          key={flight.flightId}
                          type="button"
                          onClick={() => setChangeFlightModal((prev) => ({ ...prev, selectedFlightId: flight.flightId }))}
                          className={`w-full rounded-xl border-2 p-3 text-left transition-all ${
                            changeFlightModal.selectedFlightId === flight.flightId
                              ? 'border-emerald-500 bg-emerald-50'
                              : 'border-slate-200 hover:border-emerald-300'
                          }`}
                        >
                          <div className="flex items-center justify-between">
                            <div>
                              <p className="font-bold text-slate-800">{flight.flightNumber}</p>
                              <p className="text-xs text-slate-500">
                                {new Date(flight.departureTime).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                                {' → '}
                                {new Date(flight.arrivalTime).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
                              </p>
                              <p className="text-xs text-slate-400">Chỗ trống: {flight.availableSeats}</p>
                            </div>
                            <div className="text-right">
                              <p className="font-bold text-emerald-700">{formatCurrency(flight.unitFare)}</p>
                              {changeFlightModal.selectedFlightId === flight.flightId && (
                                <span className="text-xs text-emerald-600">✓ Đã chọn</span>
                              )}
                            </div>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                  {changeFlightModal.selectedFlightId && (
                    <button
                      type="button"
                      disabled={changeFlightLoading}
                      onClick={handleGetChangeQuote}
                      className="w-full rounded-xl bg-emerald-600 py-3 text-sm font-bold text-white hover:bg-emerald-700 disabled:opacity-50"
                    >
                      {changeFlightLoading ? 'Đang tính...' : 'Xem báo giá →'}
                    </button>
                  )}
                </div>
              )}

              {changeFlightModal.step === 'quote' && changeFlightModal.quote && (
                <div className="space-y-4">
                  <div className="rounded-xl bg-emerald-50 p-4 space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Giá vé cũ</span>
                      <span className="font-semibold">{formatCurrency(changeFlightModal.quote.oldAmount || 0)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Giá vé mới</span>
                      <span className="font-semibold">{formatCurrency(changeFlightModal.quote.newAmount || 0)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">Chênh lệch</span>
                      <span className={`font-semibold ${(changeFlightModal.quote.fareDifference || 0) >= 0 ? 'text-orange-600' : 'text-green-600'}`}>
                        {(changeFlightModal.quote.fareDifference || 0) >= 0 ? '+' : ''}{formatCurrency(changeFlightModal.quote.fareDifference || 0)}
                      </span>
                    </div>
                    {(changeFlightModal.quote.changeFee > 0) && (
                      <div className="flex justify-between text-sm">
                        <span className="text-slate-600">Phí đổi chuyến</span>
                        <span className="font-semibold text-orange-600">{formatCurrency(changeFlightModal.quote.changeFee)}</span>
                      </div>
                    )}
                    <div className="border-t border-emerald-200 pt-2 flex justify-between">
                      <span className="font-bold text-slate-800">Tổng thanh toán thêm</span>
                      <span className={`font-bold text-base ${(changeFlightModal.quote.netAmount || 0) > 0 ? 'text-orange-600' : 'text-green-600'}`}>
                        {(changeFlightModal.quote.netAmount || 0) > 0 ? '+' : ''}{formatCurrency(changeFlightModal.quote.netAmount || 0)}
                      </span>
                    </div>
                    <p className="text-xs text-slate-500">Đơn vị: {changeFlightModal.quote.currency || 'VND'}</p>
                  </div>
                  {changeFlightError && <p className="text-xs text-red-600">{changeFlightError}</p>}
                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => setChangeFlightModal((prev) => ({ ...prev, step: 'select-flight' }))}
                      className="flex-1 rounded-xl border border-slate-300 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
                    >
                      ← Quay lại
                    </button>
                    <button
                      type="button"
                      disabled={changeFlightLoading}
                      onClick={handleConfirmChangeFlight}
                      className="flex-1 rounded-xl bg-emerald-600 py-3 text-sm font-bold text-white hover:bg-emerald-700 disabled:opacity-50"
                    >
                      {changeFlightLoading ? 'Đang xử lý...' : 'Xác nhận đổi →'}
                    </button>
                  </div>
                </div>
              )}

              {changeFlightModal.step === 'confirm' && (
                <div className="space-y-4 text-center">
                  <div className="rounded-xl bg-green-50 p-6">
                    <div className="text-4xl mb-3">🎉</div>
                    <p className="font-bold text-green-800 text-lg">Đổi chuyến thành công!</p>
                    {changeFlightModal.confirmResult?.paymentRequired ? (
                      <p className="text-sm text-orange-600 mt-2">Yêu cầu thanh toán bổ sung — đang chuyển hướng...</p>
                    ) : (
                      <p className="text-sm text-green-600 mt-2">Chuyến bay của bạn đã được cập nhật.</p>
                    )}
                  </div>
                  <button
                    type="button"
                    onClick={() => { closeChangeFlightModal(); window.location.reload() }}
                    className="w-full rounded-xl bg-emerald-600 py-3 text-sm font-bold text-white hover:bg-emerald-700"
                  >
                    Đóng &amp; Tải lại
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </main>
  )
}

export default App

