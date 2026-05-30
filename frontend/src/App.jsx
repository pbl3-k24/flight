import { useEffect, useMemo, useState } from 'react'
import FlightTemplateManagement from './components/flight-templates/FlightTemplateManagement'
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
      label: seat.seatClassName || seat.className || (seat.seatClassId ? (Number(seat.seatClassId) === 2 ? 'Business' : 'Economy') : 'Hạng vé'),
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
  const [paymentReturnBookingId, setPaymentReturnBookingId] = useState(null)
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
  const [selectedTemplateDays, setSelectedTemplateDays] = useState({})
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
    date: toLocalDateInputValue(),
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
  const [editingTemplateId, setEditingTemplateId] = useState(null)
  const [isPromotionModalOpen, setIsPromotionModalOpen] = useState(false)
  const [isFlightModalOpen, setIsFlightModalOpen] = useState(false)
  const [isTemplateModalOpen, setIsTemplateModalOpen] = useState(false)
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

  useEffect(() => {
    try {
      const pathname = (window.location?.pathname || '').toLowerCase()
      if (!pathname.includes('/payment-result')) return

      const params = new URLSearchParams(window.location.search || '')
      const bookingIdParam = Number(params.get('bookingId'))
      const resolvedBookingId = Number.isFinite(bookingIdParam) && bookingIdParam > 0 ? bookingIdParam : null

      setPaymentReturnBookingId(resolvedBookingId)
      setScreen('history')
      if (resolvedBookingId) {
        setHistoryNotice('Thanh toán hoàn tất. Đang tải lại lịch sử vé...')
      }
    } catch (error) {
      // ignore URL parsing errors
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

  const loadAdminFlights = async (dateOverride) => {
    setIsLoadingFlightsAdmin(true)
    setFlightAdminError('')
    try {
      const dateToLoad = dateOverride !== undefined ? dateOverride : adminFlightFilters.date
      const data = await getAdminFlights(dateToLoad, 1, 100)
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
    setIsFlightModalOpen(true)
  }

  const startEditFlight = (flight) => {
    if (!flight) return
    setEditingFlightId(flight.flightId)
    setFlightFormData(mapFlightToForm(flight))
    setFlightAdminError('')
    setFlightAdminNotice('')
    setIsFlightModalOpen(true)
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
      setIsFlightModalOpen(false)
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
    setIsPromotionModalOpen(true)
  }

  const startEditPromotion = (promotion) => {
    if (!promotion) return
    setEditingPromotionId(promotion.promotionId)
    setPromotionFormData(mapPromotionToForm(promotion))
    setPromotionAdminError('')
    setPromotionAdminNotice('')
    setIsPromotionModalOpen(true)
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
      setIsPromotionModalOpen(false)
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

  const loadTemplateForEditing = async (template) => {
    try {
      setApiError('')
      setAdminNotice('')
      const templateId = template.templateId || template.id || template.Id
      
      if (!templateId) {
        setApiError('Không tìm thấy ID của template')
        return
      }
      
      const templateDetail = await getFlightScheduleTemplate(templateId)
      console.log('📋 Template Detail loaded for editing:', templateDetail)
      
      setEditingTemplateId(templateId)
      setTemplateFormData({
        name: templateDetail.name || '',
        description: templateDetail.description || '',
        isActive: templateDetail.isActive !== false,
      })
      
      // Reconstruct templateSlots from details
      const slots = (templateDetail.details || []).map((detail, index) => {
        // Look for matching definition
        let def = flightDefinitions.find(
          (d) => String(d.routeId) === String(detail.routeId) &&
                 d.departureTime.substring(0, 5) === detail.departureTime.substring(0, 5)
        )
        
        if (!def) {
          // If no exact definition match, fallback to structured definition
          def = {
            id: `fallback-def-${index}-${Date.now()}`,
            flightNumber: `${detail.flightNumberPrefix || 'FL'}${detail.flightNumberSuffix || ''}`,
            routeId: detail.routeId,
            departureAirportCode: `Route #${detail.routeId}`,
            arrivalAirportCode: '',
            departureTime: detail.departureTime,
            arrivalTime: detail.arrivalTime,
          }
        }
        
        return {
          id: `slot-edit-${index}-${Date.now()}`,
          flightDefinition: {
            ...def,
            selectedAircraftId: detail.aircraftId,
          },
          dayOfWeek: detail.dayOfWeek,
        }
      })
      
      setTemplateSlots(slots)
      setSelectedTemplateDays({})
      setIsTemplateModalOpen(true)
    } catch (error) {
      setApiError(error.message || 'Lỗi khi tải chi tiết template để chỉnh sửa')
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
  const [expandedFlightId, setExpandedFlightId] = useState(null)
  const [flightDetailTab, setFlightDetailTab] = useState('flight')


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

  useEffect(() => {
    if (screen !== 'history' || !paymentReturnBookingId || !authUser) return
    loadTicketsForBooking(paymentReturnBookingId)
  }, [screen, paymentReturnBookingId, authUser])

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
    loadAdminRoutes()
    getAircrafts()
      .then((data) => setAircrafts(Array.isArray(data) ? data : []))
      .catch(() => setFlightAdminError('Không thể tải danh sách máy bay.'))
  }, [screen, isAdmin])


  useEffect(() => {
    if (screen !== 'flights' || !isAdmin) return
    loadAdminFlights(adminFlightFilters.date)
  }, [screen, isAdmin, adminFlightFilters.date])

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
    <header className="sticky top-0 z-50 mb-8 rounded-2xl glass-panel px-5 py-4 text-slate-800 shadow-md flex items-center justify-between transition-all duration-instant">
      <div className="flex items-center gap-3 cursor-pointer" onClick={() => setScreen('search')}>
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-blue-600 to-indigo-600 text-white shadow-md shadow-blue-200">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="h-5 w-5 rotate-45">
            <line x1="22" y1="2" x2="11" y2="13"></line>
            <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
          </svg>
        </div>
        <div>
          <span className="title-font text-lg font-extrabold tracking-tight bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">FlyNow.vn</span>
          <p className="text-[10px] uppercase font-bold tracking-widest text-slate-400">Vé máy bay cực nhanh</p>
        </div>
      </div>

      <nav className="hidden md:flex items-center gap-1 rounded-full bg-slate-100/80 p-1">
        <button
          type="button"
          onClick={() => setScreen('search')}
          className={`rounded-full px-4 py-1.5 text-xs font-semibold transition-all ${
            screen === 'search' || screen === 'list' || screen === 'passenger' || screen === 'payment'
              ? 'bg-blue-600 text-white shadow-sm'
              : 'text-slate-600 hover:text-slate-900'
          }`}
        >
          Tìm chuyến bay
        </button>
        <button
          type="button"
          onClick={() => setScreen('history')}
          className={`rounded-full px-4 py-1.5 text-xs font-semibold transition-all ${
            screen === 'history' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'
          }`}
        >
          Vé của tôi
        </button>
        <button
          type="button"
          onClick={() => setScreen('saved-passengers')}
          className={`rounded-full px-4 py-1.5 text-xs font-semibold transition-all ${
            screen === 'saved-passengers' ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'
          }`}
        >
          Danh bạ
        </button>
        {isAdmin && (
          <div className="flex items-center gap-1 border-l border-slate-200 pl-1 ml-1">
            <button
              type="button"
              onClick={() => setScreen('flights')}
              className={`rounded-full px-3 py-1.5 text-[11px] font-semibold transition-all ${
                screen === 'flights' ? 'bg-indigo-600 text-white shadow-sm' : 'text-indigo-600/80 hover:text-indigo-900'
              }`}
            >
              ✈️ Chuyến bay
            </button>
            <button
              type="button"
              onClick={() => setScreen('templates')}
              className={`rounded-full px-3 py-1.5 text-[11px] font-semibold transition-all ${
                screen === 'templates' ? 'bg-indigo-600 text-white shadow-sm' : 'text-indigo-600/80 hover:text-indigo-900'
              }`}
            >
              📋 Templates
            </button>
            <button
              type="button"
              onClick={() => setScreen('promotions')}
              className={`rounded-full px-3 py-1.5 text-[11px] font-semibold transition-all ${
                screen === 'promotions' ? 'bg-indigo-600 text-white shadow-sm' : 'text-indigo-600/80 hover:text-indigo-900'
              }`}
            >
              🎁 Khuyến mãi
            </button>
          </div>
        )}
      </nav>

      <div className="flex items-center gap-3">
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
                className="relative flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-slate-600 transition-all hover:bg-slate-200"
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
                  <span className="absolute -right-1 -top-1 min-w-[18px] rounded-full bg-rose-500 px-1 py-0.5 text-[10px] font-bold text-white shadow">
                    {unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}
                  </span>
                )}
              </button>
              <div
                className={`absolute right-0 top-full mt-2 w-80 rounded-2xl bg-white text-slate-900 shadow-xl border border-slate-100 transition z-50 ${
                  isNotificationPanelOpen
                    ? 'pointer-events-auto translate-y-0 opacity-100'
                    : 'pointer-events-none -translate-y-1 opacity-0'
                }`}
              >
                <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3">
                  <div className="text-sm font-bold text-slate-800">Thông báo</div>
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
                <div className="max-h-80 overflow-y-auto">
                  {isLoadingNotifications && (
                    <div className="px-4 py-5 text-xs text-slate-400 text-center">Đang tải thông báo...</div>
                  )}
                  {!isLoadingNotifications && notificationError && (
                    <div className="px-4 py-4 text-xs text-rose-600 text-center">{notificationError}</div>
                  )}
                  {!isLoadingNotifications && !notificationError && sortedNotifications.length === 0 && (
                    <div className="px-4 py-5 text-xs text-slate-400 text-center">Chưa có thông báo nào.</div>
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
                              isRead ? 'text-slate-400 font-normal' : 'text-slate-800 font-medium'
                            }`}
                          >
                            <div className="flex items-center justify-between gap-2">
                              <span className="text-xs font-bold">{title}</span>
                              {!isRead && (
                                <span className="h-2 w-2 flex-none rounded-full bg-blue-600" />
                              )}
                            </div>
                            {message && <div className="text-xs text-slate-500 leading-tight">{message}</div>}
                            {timestamp && <div className="text-[10px] text-slate-400 mt-0.5">{timestamp}</div>}
                          </button>
                        )
                      })}
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-2 rounded-xl bg-slate-50 border border-slate-200/60 p-1 pr-3 shadow-sm">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-100 text-blue-700 font-bold text-sm shadow-inner">
                {(authUser.fullName || authUser.email)[0].toUpperCase()}
              </div>
              <div className="hidden sm:block text-left">
                <p className="text-xs font-bold text-slate-700 leading-tight">
                  {authUser.fullName || authUser.email.split('@')[0]}
                </p>
                <p className="text-[10px] text-slate-400 capitalize">{authUser.role}</p>
              </div>
              <button
                type="button"
                onClick={logout}
                className="ml-2 rounded-lg bg-rose-50 p-1 text-rose-600 hover:bg-rose-100 transition-all hover:scale-105"
                title="Đăng xuất"
              >
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" className="h-4 w-4">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                  <polyline points="16 17 21 12 16 7" />
                  <line x1="21" y1="12" x2="9" y2="12" />
                </svg>
              </button>
            </div>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setScreen('login')}
            className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-bold text-white shadow-md shadow-blue-200 hover:bg-blue-700 active:scale-[0.98] transition-all"
          >
            Đăng nhập
          </button>
        )}
      </div>
    </header>
  )

  const renderLogin = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-7 shadow-lg border border-slate-100 transition duration-fast hover:shadow-xl">
      <div className="mb-6 text-center">
        <h2 className="title-font text-2xl font-extrabold text-slate-800">Chào mừng trở lại</h2>
        <p className="text-xs text-slate-400 mt-1">Đăng nhập tài khoản FlyNow của bạn</p>
      </div>
      <div className="space-y-4">
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Email liên hệ</label>
          <input
            type="email"
            placeholder="you@email.com"
            value={loginData.email}
            onChange={(e) => setLoginData((prev) => ({ ...prev, email: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div>
          <div className="flex justify-between items-center mb-1.5">
            <label className="block text-xs font-bold text-slate-700">Mật khẩu</label>
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
              className="text-slate-400 hover:text-blue-600 text-xs transition font-medium"
            >
              Quên mật khẩu?
            </button>
          </div>
          <input
            type="password"
            placeholder="••••••••"
            value={loginData.password}
            onChange={(e) => setLoginData((prev) => ({ ...prev, password: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
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
        className="mt-6 w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow-md shadow-blue-200 transition-all hover:bg-blue-750 active:scale-[0.98] disabled:opacity-50"
      >
        {isLoggingIn ? 'Đang đăng nhập...' : 'Đăng nhập'}
      </button>
      {apiError && (
        <div className="mt-4 rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">{apiError}</div>
      )}
      <p className="mt-5 text-center text-xs text-slate-500">
        Chưa có tài khoản?{' '}
        <button
          type="button"
          onClick={() => {
            setApiError('')
            setScreen('register')
          }}
          className="font-bold text-blue-600 hover:underline transition"
        >
          Đăng ký ngay
        </button>
      </p>
    </div>
  )

  const renderRegister = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-7 shadow-lg border border-slate-100 transition duration-fast hover:shadow-xl">
      <div className="mb-6 text-center">
        <h2 className="title-font text-2xl font-extrabold text-slate-800">Tạo tài khoản</h2>
        <p className="text-xs text-slate-400 mt-1">Trở thành thành viên để nhận nhiều ưu đãi</p>
      </div>
      <div className="space-y-4">
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Họ và tên</label>
          <input
            type="text"
            placeholder="Nguyễn Văn A"
            value={registerData.fullName}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, fullName: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Email</label>
          <input
            type="email"
            placeholder="you@email.com"
            value={registerData.email}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, email: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Số điện thoại</label>
          <input
            type="tel"
            placeholder="0900000000"
            value={registerData.phone}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, phone: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Mật khẩu</label>
          <input
            type="password"
            placeholder="••••••••"
            value={registerData.password}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, password: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-700">Xác nhận mật khẩu</label>
          <input
            type="password"
            placeholder="••••••••"
            value={registerData.confirmPassword}
            onChange={(e) =>
              setRegisterData((prev) => ({ ...prev, confirmPassword: e.target.value }))
            }
            className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition duration-instant focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
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
        className="mt-6 w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow-md shadow-blue-200 transition-all hover:bg-blue-750 active:scale-[0.98] disabled:opacity-50"
      >
        {isRegistering ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}
      </button>
      {registerError && (
        <div className="mt-4 rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">{registerError}</div>
      )}
      <p className="mt-5 text-center text-xs text-slate-500">
        Đã có tài khoản?{' '}
        <button
          type="button"
          onClick={() => {
            setRegisterError('')
            setScreen('login')
          }}
          className="font-bold text-blue-600 hover:underline transition"
        >
          Đăng nhập
        </button>
      </p>
    </div>
  )

  // ===== RENDER: QUEN MAT KHAU =====
  const renderForgotPassword = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-7 shadow-lg border border-slate-100 transition duration-fast">
      <div className="mb-6 text-center">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-blue-50 text-blue-600 shadow-sm shadow-blue-50">
          <svg className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
          </svg>
        </div>
        <h2 className="title-font text-2xl font-extrabold text-slate-800">Quên mật khẩu?</h2>
        <p className="mt-1 text-xs text-slate-400">Nhập email để nhận mã đặt lại mật khẩu</p>
      </div>
      {!forgotSuccess ? (
        <div className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1.5">Địa chỉ Email</label>
            <input
              type="email"
              placeholder="you@email.com"
              value={forgotEmail}
              onChange={(e) => setForgotEmail(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') handleForgotPassword() }}
              className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            />
          </div>
          {forgotError && (
            <div className="rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">{forgotError}</div>
          )}
          <button
            type="button"
            disabled={forgotLoading}
            onClick={handleForgotPassword}
            className="w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow-md shadow-blue-200 transition-all hover:bg-blue-750 disabled:opacity-50"
          >
            {forgotLoading ? 'Đang gửi...' : 'Gửi mã đặt lại'}
          </button>
          <p className="text-[10px] text-center text-slate-400">
            Email sẽ được gửi nếu tài khoản tồn tại trong hệ thống.
          </p>
        </div>
      ) : (
        <div className="space-y-4 text-center">
          <div className="rounded-xl bg-emerald-50 border border-emerald-100 p-5">
            <div className="text-3xl mb-2">✉️</div>
            <p className="font-bold text-emerald-800">Email đã được gửi!</p>
            <p className="mt-1 text-xs text-emerald-600 leading-relaxed">
              Kiểm tra hộp thư của <strong>{forgotEmail}</strong> để lấy mã đặt lại mật khẩu.
            </p>
            <p className="mt-1 text-[10px] text-emerald-500 font-medium">Mã có hiệu lực trong 1 giờ.</p>
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
            className="w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow hover:bg-blue-700 transition"
          >
            Nhập mã đặt lại mật khẩu →
          </button>
        </div>
      )}
      <p className="mt-5 text-center text-xs">
        <button
          type="button"
          onClick={() => { setForgotSuccess(false); setForgotError(''); setScreen('login') }}
          className="font-bold text-blue-600 hover:underline"
        >
          ← Quay lại đăng nhập
        </button>
      </p>
    </div>
  )

  // ===== RENDER: DAT LAI MAT KHAU =====
  const renderResetPassword = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-7 shadow-lg border border-slate-100 transition duration-fast">
      <div className="mb-6 text-center">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-indigo-50 text-indigo-600 shadow-sm shadow-indigo-50">
          <svg className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
        </div>
        <h2 className="title-font text-2xl font-extrabold text-slate-800">Đặt lại mật khẩu</h2>
        <p className="mt-1 text-xs text-slate-400">Nhập mã đã gửi và mật khẩu mới của bạn</p>
      </div>
      {!resetSuccess ? (
        <div className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1.5">Mã xác nhận</label>
            <input
              type="text"
              placeholder="Nhập mã xác nhận..."
              value={resetCode}
              onChange={(e) => setResetCode(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm tracking-widest font-mono text-center focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1.5">Mật khẩu mới</label>
            <input
              type="password"
              placeholder="••••••••"
              value={resetNewPassword}
              onChange={(e) => setResetNewPassword(e.target.value)}
              className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1.5">Xác nhận mật khẩu</label>
            <input
              type="password"
              placeholder="••••••••"
              value={resetConfirmPassword}
              onChange={(e) => setResetConfirmPassword(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') handleResetPassword() }}
              className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
            />
          </div>
          {forgotError && (
            <div className="rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">{forgotError}</div>
          )}
          <button
            type="button"
            disabled={forgotLoading}
            onClick={handleResetPassword}
            className="w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow-md shadow-blue-200 transition-all hover:bg-blue-750 disabled:opacity-50"
          >
            {forgotLoading ? 'Đang xử lý...' : 'Đặt lại mật khẩu'}
          </button>
        </div>
      ) : (
        <div className="space-y-4 text-center">
          <div className="rounded-xl bg-emerald-50 border border-emerald-100 p-5">
            <div className="text-3xl mb-2">✅</div>
            <p className="font-bold text-emerald-800">Mật khẩu đã được đặt lại!</p>
            <p className="mt-1 text-xs text-emerald-600 leading-relaxed">Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.</p>
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
            className="w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-bold text-white shadow hover:bg-blue-750 transition"
          >
            Đăng nhập ngay
          </button>
        </div>
      )}
      <p className="mt-5 text-center text-xs">
        Chưa có mã?{' '}
        <button
          type="button"
          onClick={() => { setForgotError(''); setForgotSuccess(false); setScreen('forgot-password') }}
          className="font-bold text-blue-600 hover:underline"
        >
          Gửi lại email
        </button>
      </p>
    </div>
  )


  const renderSearch = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg border border-slate-100 md:p-7 transition duration-fast hover:shadow-xl">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <span className="text-xs uppercase font-extrabold tracking-wider text-blue-600">Đăng ký chuyến đi</span>
          <h2 className="title-font text-2xl font-extrabold text-slate-800 mt-0.5">Tìm kiếm chuyến bay</h2>
        </div>
        <div className="inline-flex rounded-xl bg-slate-100 p-1">
          {[
            { key: 'oneway', label: 'Một chiều' },
            { key: 'roundtrip', label: 'Khứ hồi' },
          ].map((option) => (
            <button
              type="button"
              key={option.key}
              onClick={() => setTripType(option.key)}
              className={`rounded-lg px-4 py-2 text-xs font-bold transition-all ${
                tripType === option.key
                  ? 'bg-blue-600 text-white shadow-md shadow-blue-100'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
        {/* Origin Airport and Swap Button */}
        <div className="relative md:col-span-2 lg:col-span-2 grid grid-cols-[1fr_auto_1fr] items-center gap-2">
          <div>
            <label className="mb-1.5 block text-xs font-bold text-slate-600">Điểm khởi hành</label>
            <select
              value={searchData.fromAirportId}
              onChange={(e) =>
                setSearchData((prev) => ({ ...prev, fromAirportId: Number(e.target.value) }))
              }
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              {airports.map((airport) => (
                <option key={airport.Id} value={airport.Id}>
                  {airport.City} ({airport.Code})
                </option>
              ))}
            </select>
          </div>

          <button
            type="button"
            onClick={() =>
              setSearchData((prev) => ({
                ...prev,
                fromAirportId: prev.toAirportId,
                toAirportId: prev.fromAirportId,
              }))
            }
            className="mt-5 flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white text-blue-600 shadow-sm transition hover:bg-slate-50 hover:border-blue-300 active:scale-95"
            title="Đảo chiều sân bay"
          >
            ⇄
          </button>

          <div>
            <label className="mb-1.5 block text-xs font-bold text-slate-600">Điểm đến</label>
            <select
              value={searchData.toAirportId}
              onChange={(e) =>
                setSearchData((prev) => ({ ...prev, toAirportId: Number(e.target.value) }))
              }
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              {airports.map((airport) => (
                <option key={airport.Id} value={airport.Id}>
                  {airport.City} ({airport.Code})
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Departure Date */}
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-600">Ngày khởi hành</label>
          <input
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
            className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          />
        </div>

        {/* Return Date */}
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-600">
            Ngày trở về {tripType === 'oneway' && <span className="text-slate-300 font-normal">(Một chiều)</span>}
          </label>
          <input
            type="date"
            disabled={tripType === 'oneway'}
            min={searchData.departDate || today}
            value={searchData.returnDate}
            onChange={(e) => setSearchData((prev) => ({ ...prev, returnDate: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100 disabled:opacity-50 disabled:bg-slate-50"
          />
        </div>
      </div>

      <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3 mt-5 border-t border-slate-100 pt-5">
        {/* Passenger selector with dropdown counter panel */}
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-600">Số hành khách</label>
          <div className="relative group">
            <div className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 flex justify-between items-center cursor-default">
              <span>{totalPassengers} Hành khách</span>
              <span className="text-[10px] text-slate-400">▼</span>
            </div>
            <div className="absolute top-full left-0 mt-1 w-72 rounded-2xl bg-white border border-slate-150 p-4 shadow-xl z-30 opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto group-focus-within:opacity-100 group-focus-within:pointer-events-auto transition duration-fast">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-bold text-slate-800">Người lớn</p>
                    <p className="text-[10px] text-slate-400">Từ 12 tuổi</p>
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
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      −
                    </button>
                    <span className="w-6 text-center text-xs font-bold">{passengerCounts.adult}</span>
                    <button
                      type="button"
                      onClick={() =>
                        setPassengerCounts((prev) => ({
                          ...prev,
                          adult: prev.adult + 1,
                        }))
                      }
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      +
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-bold text-slate-800">Trẻ em</p>
                    <p className="text-[10px] text-slate-400">2 - 12 tuổi</p>
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
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      −
                    </button>
                    <span className="w-6 text-center text-xs font-bold">{passengerCounts.child}</span>
                    <button
                      type="button"
                      onClick={() =>
                        setPassengerCounts((prev) => ({
                          ...prev,
                          child: prev.child + 1,
                        }))
                      }
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      +
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-xs font-bold text-slate-800">Em bé</p>
                    <p className="text-[10px] text-slate-400">Dưới 2 tuổi</p>
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
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      −
                    </button>
                    <span className="w-6 text-center text-xs font-bold">{passengerCounts.infant}</span>
                    <button
                      type="button"
                      onClick={() =>
                        setPassengerCounts((prev) => ({
                          ...prev,
                          infant: prev.infant + 1,
                        }))
                      }
                      className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-50 font-bold"
                    >
                      +
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Seat Class Selection */}
        <div>
          <label className="mb-1.5 block text-xs font-bold text-slate-600">Hạng khoang</label>
          <select
            value={searchData.seatClass}
            onChange={(e) => setSearchData((prev) => ({ ...prev, seatClass: e.target.value }))}
            className="w-full rounded-xl border border-slate-200 bg-white px-3 py-3 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          >
            <option value="Economy">Phổ thông (Economy)</option>
            <option value="Business">Thương gia (Business)</option>
          </select>
        </div>

        {/* Search Submit Button */}
        <div className="flex items-end">
          <button
            type="button"
            onClick={() => performSearch()}
            disabled={isLoadingFlights}
            className="w-full rounded-xl bg-blue-600 py-3 text-xs font-bold text-white shadow-md shadow-blue-200 hover:bg-blue-755 transition active:scale-[0.98] disabled:opacity-50"
          >
            {isLoadingFlights ? 'Đang tìm kiếm...' : '🔍 Tìm chuyến bay'}
          </button>
        </div>
      </div>

      {apiError && (
        <div className="mt-4 rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">
          {apiError}
        </div>
      )}
    </div>
  )

  const renderFlightList = () => (
    <div className="grid gap-5 lg:grid-cols-[260px_1fr]">
      {/* Sidebar Filter */}
      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg border border-slate-100 transition duration-fast hover:shadow-xl">
        <div className="flex items-center gap-2 mb-5 pb-3 border-b border-slate-100">
          <span className="text-lg">⚙️</span>
          <h3 className="title-font text-base font-extrabold text-slate-800">Bộ lọc tìm kiếm</h3>
        </div>
        <div className="space-y-5">
          <div>
            <div className="flex justify-between items-center mb-1.5">
              <label className="text-xs font-bold text-slate-600">Giá tối đa</label>
              <span className="text-xs font-bold text-blue-600">{formatCurrency(filters.maxPrice)}</span>
            </div>
            <input
              type="range"
              min="1000000"
              max="5000000"
              step="100000"
              value={filters.maxPrice}
              onChange={(e) =>
                setFilters((prev) => ({ ...prev, maxPrice: Number(e.target.value) }))
              }
              className="w-full h-1.5 bg-slate-100 rounded-lg appearance-none cursor-pointer accent-blue-600"
            />
          </div>
          <div>
            <label className="mb-1.5 block text-xs font-bold text-slate-600">Khung giờ bay</label>
            <select
              value={filters.timeSlot}
              onChange={(e) => setFilters((prev) => ({ ...prev, timeSlot: e.target.value }))}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs text-slate-700 font-medium outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              <option value="all">Tất cả khung giờ</option>
              <option value="morning">Sáng sớm (00:00 - 11:59)</option>
              <option value="afternoon">Chiều mát (12:00 - 17:59)</option>
              <option value="evening">Tối muộn (18:00 - 23:59)</option>
            </select>
          </div>
          <div>
            <label className="mb-1.5 block text-xs font-bold text-slate-600">Hạng khoang</label>
            <select
              value={filters.seatClass}
              onChange={(e) => setFilters((prev) => ({ ...prev, seatClass: e.target.value }))}
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs text-slate-700 font-medium outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              <option value="all">Tất cả hạng ghế</option>
              <option value="Economy">Economy</option>
              <option value="Business">Business</option>
            </select>
          </div>
        </div>
      </aside>

      {/* Main Results panel */}
      <section className="space-y-4">
        {/* Step Guide label */}
        <div className="rounded-2xl border border-slate-150 bg-white px-4 py-3 text-xs font-bold text-slate-600 flex items-center gap-2 shadow-sm">
          <span className="flex h-5 w-5 items-center justify-center rounded-full bg-blue-100 text-blue-600 text-[10px]">ℹ️</span>
          <span>
            {tripType === 'roundtrip'
              ? roundtripStep === 'outbound'
                ? 'Bước 1: Chọn chuyến bay chiều đi (Khởi hành)'
                : 'Bước 2: Chọn chuyến bay chiều về (Khứ hồi)'
              : 'Chọn chuyến bay phù hợp với hành trình của bạn'}
          </span>
        </div>

        {/* Date options tabs slider */}
        {dateOptions.length > 0 && (
          <div className="flex gap-2 overflow-x-auto pb-2 scrollbar-thin">
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
                  className={`min-w-[120px] flex-none rounded-2xl border p-3 text-left transition-all ${
                    isActive
                      ? 'border-blue-600 bg-blue-50/50 shadow-sm shadow-blue-50/20'
                      : 'border-slate-200 bg-white hover:border-blue-300'
                  } ${isLoadingFlights ? 'opacity-50' : ''}`}
                >
                  <p className="text-[10px] font-extrabold uppercase tracking-wider text-slate-400">
                    {option.weekday}
                  </p>
                  <p className="mt-0.5 text-xs font-bold text-slate-800">
                    {option.label}
                  </p>
                  <p className="mt-1 text-[11px] font-extrabold text-blue-600">
                    {priceLabel}
                  </p>
                </button>
              )
            })}
          </div>
        )}

        {/* Flights mapping */}
        <div className="space-y-4">
          {filteredFlights.map((flight) => {
            const flightPrice = (flight.pricesByClass?.[searchData.seatClass] || 0) / passengerDivisor
            const flightId = getFlightId(flight)
            const isExpanded = expandedFlightId === flightId

            return (
              <article
                key={flightId || flight.flightNumber || flight.departureTime}
                className={`rounded-2xl bg-white border p-5 shadow-sm transition-all duration-fast hover:shadow-md ${
                  isExpanded ? 'border-blue-500 ring-2 ring-blue-50' : 'border-slate-100'
                }`}
              >
                <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                  <div className="grid grid-cols-[auto_1fr] items-center gap-4">
                    {/* Airline Code Label */}
                    <div className="flex h-12 w-12 flex-col items-center justify-center rounded-xl bg-slate-50 border border-slate-200/60 shadow-inner">
                      <span className="text-xs font-extrabold text-slate-800">{flight.airlineCode}</span>
                      <span className="text-[8px] font-bold text-slate-400 leading-none mt-0.5">{flight.flightNumber}</span>
                    </div>

                    {/* Flight Detail */}
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-base font-extrabold text-slate-800">{formatTime(flight.departureTime)}</span>
                        <span className="text-slate-300">→</span>
                        <span className="text-base font-extrabold text-slate-800">{formatTime(flight.arrivalTime)}</span>
                        <span className="ml-2 rounded-lg bg-blue-50 px-2 py-0.5 text-[10px] font-bold text-blue-600 border border-blue-100">
                          {formatDuration(flight.durationMinutes)}
                        </span>
                      </div>
                      <p className="text-xs text-slate-500 mt-1">
                        Máy bay: <span className="font-bold text-slate-600">{flight.aircraftModel || 'Boeing 787'}</span> · Hạng: <span className="font-bold text-slate-600">{searchData.seatClass}</span>
                      </p>
                      <button
                        type="button"
                        onClick={() => {
                          setExpandedFlightId(isExpanded ? null : flightId)
                        }}
                        className="mt-2 text-xs font-bold text-blue-600 hover:text-blue-700 flex items-center gap-1 transition"
                      >
                        {isExpanded ? 'Ẩn chi tiết ▲' : 'Xem chi tiết chuyến bay ▼'}
                      </button>
                    </div>
                  </div>

                  {/* Price & Selection */}
                  <div className="flex items-center justify-between border-t border-slate-100 pt-4 md:border-none md:pt-0 md:flex-col md:items-end md:justify-center">
                    <div className="text-left md:text-right">
                      <p className="text-[10px] uppercase font-bold tracking-wider text-slate-400">Giá vé (đã gồm thuế)</p>
                      <p className="text-lg font-extrabold text-slate-800">{formatCurrency(flightPrice)}</p>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        const seatClassId = seatClassMap[searchData.seatClass]
                        if (seatClassId) {
                          loadServices(seatClassId)
                        } else {
                          setServiceError('Không xác định được hạng ghế để tải dịch vụ')
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
                      className="rounded-xl bg-blue-600 px-5 py-2.5 text-xs font-bold text-white shadow-md shadow-blue-100 hover:bg-blue-755 transition active:scale-[0.97]"
                    >
                      Chọn vé
                    </button>
                  </div>
                </div>

                {/* Expandable Flight detail timeline */}
                {isExpanded && (
                  <div className="mt-5 border-t border-slate-100 pt-4 bg-slate-50/50 rounded-2xl p-4 border border-slate-200/50 transition-all duration-fast">
                    {/* Tabs indicator */}
                    <div className="flex gap-2 border-b border-slate-200 pb-2 mb-4">
                      {[
                        { key: 'flight', label: 'Hành trình' },
                        { key: 'price', label: 'Chi tiết giá' },
                        { key: 'terms', label: 'Điều kiện vé' },
                      ].map((t) => (
                        <button
                          key={t.key}
                          type="button"
                          onClick={() => setFlightDetailTab(t.key)}
                          className={`px-3 py-1 text-xs font-bold transition-all relative ${
                            flightDetailTab === t.key
                              ? 'text-blue-600 after:absolute after:bottom-0 after:left-0 after:right-0 after:h-0.5 after:bg-blue-600'
                              : 'text-slate-400 hover:text-slate-700'
                          }`}
                        >
                          {t.label}
                        </button>
                      ))}
                    </div>

                    {/* Tab contents */}
                    {flightDetailTab === 'flight' && (
                      <div className="relative pl-6 border-l border-blue-200 ml-3 space-y-5 my-2">
                        {/* Departure airport node */}
                        <div className="relative">
                          <span className="absolute -left-[30px] top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-blue-600 text-[8px] text-white font-extrabold ring-4 ring-blue-100">●</span>
                          <p className="text-xs font-bold text-slate-800">
                            {formatTime(flight.departureTime)} · {formatDateTime(flight.departureTime).split(',')[0]}
                          </p>
                          <p className="text-xs text-slate-500 font-semibold mt-0.5">
                            {airports.find(a => a.Code === flight.departureAirportCode)?.Name || flight.departureAirportCode || 'Sân bay xuất phát'}
                          </p>
                        </div>
                        {/* Flight code indicator */}
                        <div className="rounded-xl border border-slate-150 bg-white p-3 text-xs text-slate-500 max-w-sm shadow-sm flex items-center gap-3">
                          <span className="text-xl">✈️</span>
                          <div>
                            <p className="font-bold text-slate-700">{flight.airlineCode} {flight.flightNumber}</p>
                            <p className="text-[10px] text-slate-400">Vận hành bởi máy bay {flight.aircraftModel || 'Airbus A321'}</p>
                          </div>
                        </div>
                        {/* Arrival airport node */}
                        <div className="relative">
                          <span className="absolute -left-[30px] top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-indigo-600 text-[8px] text-white font-extrabold ring-4 ring-indigo-100">●</span>
                          <p className="text-xs font-bold text-slate-800">
                            {formatTime(flight.arrivalTime)} · {formatDateTime(flight.arrivalTime).split(',')[0]}
                          </p>
                          <p className="text-xs text-slate-500 font-semibold mt-0.5">
                            {airports.find(a => a.Code === flight.arrivalAirportCode)?.Name || flight.arrivalAirportCode || 'Sân bay hạ cánh'}
                          </p>
                        </div>
                      </div>
                    )}

                    {flightDetailTab === 'price' && (
                      <div className="space-y-2 text-xs text-slate-600">
                        <div className="flex justify-between font-medium">
                          <span>Giá cơ bản (Base fare):</span>
                          <span>{formatCurrency(flightPrice * 0.8)}</span>
                        </div>
                        <div className="flex justify-between font-medium">
                          <span>Thuế và phí sân bay (Taxes & Fees):</span>
                          <span>{formatCurrency(flightPrice * 0.2)}</span>
                        </div>
                        <div className="border-t border-slate-100 pt-2 flex justify-between font-extrabold text-slate-800">
                          <span>Tổng tiền vé:</span>
                          <span className="text-blue-600">{formatCurrency(flightPrice)}</span>
                        </div>
                      </div>
                    )}

                    {flightDetailTab === 'terms' && (
                      <div className="space-y-1.5 text-xs text-slate-600">
                        <p><strong>• Hành lý xách tay:</strong> 7kg tiêu chuẩn.</p>
                        <p><strong>• Hành lý ký gửi:</strong> Được chọn mua thêm tại bước khai báo hành khách.</p>
                        <p><strong>• Thay đổi vé:</strong> Được hỗ trợ trước giờ cất cánh 24 tiếng (áp dụng phí thay đổi và chênh lệch vé nếu có).</p>
                        <p><strong>• Hoàn vé:</strong> Tuân thủ điều kiện quy định tương ứng với từng hạng đặt chỗ.</p>
                      </div>
                    )}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      </section>
    </div>
  )


  const renderPassenger = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      {/* Passenger profile details */}
      <section className="rounded-2xl bg-white p-5 shadow-lg border border-slate-100 md:p-7 hover:shadow-xl transition">
        <div className="flex items-center gap-2 mb-6 pb-3 border-b border-slate-100">
          <span className="text-xl">👤</span>
          <h2 className="title-font text-xl font-extrabold text-slate-800">Thông tin hành khách</h2>
        </div>
        <div className="space-y-6">
          {passengerForms.map((passenger, index) => {
            const isAdult = passenger.type === 'adult'
            const isChild = passenger.type === 'child'
            return (
              <div key={`passenger-${index}`} className="rounded-2xl border border-slate-200 p-5 bg-slate-50/30">
                <div className="mb-4 flex items-center justify-between">
                  <p className="text-xs font-extrabold text-blue-600 uppercase tracking-wider">Hành khách {index + 1}</p>
                  <span className="rounded-full bg-blue-50 px-3 py-1 text-[10px] font-bold text-blue-600 border border-blue-100">
                    {isAdult ? 'Người lớn (Từ 12 tuổi)' : isChild ? 'Trẻ em (2 - 12 tuổi)' : 'Trẻ sơ sinh (Dưới 2 tuổi)'}
                  </span>
                </div>

                {savedPassengers.length > 0 && (
                  <div className="mb-4">
                    <label className="mb-1.5 block text-xs font-bold text-slate-600">Chọn hành khách đã lưu</label>
                    <select
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
                      className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                    >
                      <option value="">-- Chọn hành khách từ danh bạ --</option>
                      {savedPassengers.map((item) => {
                        const label = [item.lastName, item.firstName].filter(Boolean).join(' ') || '---'
                        return (
                          <option key={item.id} value={item.id}>
                            {label}{item.email ? ` · ${item.email}` : ''}
                          </option>
                        )
                      })}
                    </select>
                  </div>
                )}

                <div className="grid gap-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <label className="mb-1.5 block text-xs font-bold text-slate-600">Họ và tên hành khách</label>
                    <input
                      type="text"
                      placeholder="Nguyễn Văn A"
                      value={passenger.fullName}
                      onChange={(e) =>
                        setPassengerForms((prev) =>
                          prev.map((item, idx) =>
                            idx === index ? { ...item, fullName: e.target.value } : item
                          )
                        )
                      }
                      className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                    />
                  </div>

                  <div>
                    <label className="mb-1.5 block text-xs font-bold text-slate-600">Ngày sinh</label>
                    <input
                      type="date"
                      value={passenger.dob}
                      onChange={(e) =>
                        setPassengerForms((prev) =>
                          prev.map((item, idx) =>
                            idx === index ? { ...item, dob: e.target.value } : item
                          )
                        )
                      }
                      className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                    />
                    {isChild && (
                      <p className="mt-1 text-[10px] font-medium text-slate-400">Trẻ em: từ 2 đến dưới 12 tuổi</p>
                    )}
                    {!isAdult && !isChild && (
                      <p className="mt-1 text-[10px] font-medium text-slate-400">Trẻ sơ sinh: dưới 2 tuổi</p>
                    )}
                  </div>
                  <div>
                    <label className="mb-1.5 block text-xs font-bold text-slate-600">Giới tính</label>
                    <div className="flex flex-wrap gap-3 pt-1.5">
                      {['Nam', 'Nữ', 'Khác'].map((genderOption) => (
                        <label key={genderOption} className="inline-flex items-center gap-2 text-xs font-semibold text-slate-700 cursor-pointer">
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
                            className="h-4 w-4 rounded-full border-slate-300 text-blue-600 focus:ring-blue-100"
                          />
                          {genderOption}
                        </label>
                      ))}
                    </div>
                  </div>

                  {isAdult && (
                    <div className="md:col-span-2">
                      <label className="mb-1.5 block text-xs font-bold text-slate-600">CCCD / Số Passport</label>
                      <input
                        type="text"
                        placeholder="012345678901"
                        value={passenger.document}
                        onChange={(e) =>
                          setPassengerForms((prev) =>
                            prev.map((item, idx) =>
                              idx === index ? { ...item, document: e.target.value } : item
                            )
                          )
                        }
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                      />
                    </div>
                  )}

                  {isAdult && (
                    <>
                      <div>
                        <label className="mb-1.5 block text-xs font-bold text-slate-600">Địa chỉ Email</label>
                        <input
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
                          className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-xs font-bold text-slate-600">Số điện thoại liên hệ</label>
                        <input
                          type="tel"
                          placeholder="0901234567"
                          value={passenger.phone}
                          onChange={(e) =>
                            setPassengerForms((prev) =>
                              prev.map((item, idx) =>
                                idx === index ? { ...item, phone: e.target.value } : item
                              )
                            )
                          }
                          className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                        />
                      </div>
                    </>
                  )}

                  {passenger.type !== 'infant' && (
                    <div className="md:col-span-2 space-y-3 mt-2">
                      {['outbound', tripType === 'roundtrip' ? 'return' : null]
                        .filter(Boolean)
                        .map((leg) => (
                          <div
                            key={`${index}-${leg}`}
                            className="rounded-xl border border-slate-200 bg-slate-50 p-4"
                          >
                            <p className="mb-3 text-xs font-bold text-slate-700 flex items-center gap-1.5">
                              {leg === 'outbound' ? '🧳 Dịch vụ chiều đi' : '🧳 Dịch vụ chiều về'}
                            </p>
                            {serviceError && (
                              <p className="mb-2 text-xs text-rose-600 font-semibold">{serviceError}</p>
                            )}
                            {isLoadingServices && (
                              <p className="text-xs text-slate-400 text-center py-2">Đang tải dịch vụ...</p>
                            )}
                            {!isLoadingServices && services.length === 0 && (
                              <p className="text-xs text-slate-400 text-center py-2">Không có dịch vụ nào khả dụng.</p>
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
                                      className="flex items-center justify-between rounded-xl bg-white p-3 border border-slate-100 shadow-sm"
                                    >
                                      <div>
                                        <p className="text-xs font-bold text-slate-800">{service.serviceName}</p>
                                        <p className="text-[10px] text-slate-400 font-semibold mt-0.5">{getServicePriceLabel(service.price)}</p>
                                      </div>
                                      <div className="flex items-center gap-2">
                                        <button
                                          type="button"
                                          onClick={() => {
                                            if (current > 0) {
                                              updateServiceDraft(leg, index, serviceId, current - 1)
                                            }
                                          }}
                                          className="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-100 text-slate-600 hover:bg-slate-200 transition font-bold"
                                        >
                                          −
                                        </button>
                                        <span className="w-6 text-center text-xs font-bold">{current}</span>
                                        <button
                                          type="button"
                                          onClick={() => {
                                            updateServiceDraft(leg, index, serviceId, current + 1)
                                          }}
                                          className="flex h-7 w-7 items-center justify-center rounded-lg bg-blue-600 text-white hover:bg-blue-755 transition font-bold"
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

        {/* Promo Input */}
        <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4 shadow-inner">
          <label className="mb-1.5 block text-xs font-bold text-slate-600">🎁 Mã ưu đãi giảm giá</label>
          <select
            value={selectedPromotionId}
            onChange={(e) => setSelectedPromotionId(e.target.value)}
            className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs font-bold text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
          >
            <option value="">Không dùng mã giảm giá</option>
            {availablePromotions.map((promo) => (
              <option key={String(getPromotionId(promo))} value={String(getPromotionId(promo))}>
                {getPromotionDisplayText(promo)}
              </option>
            ))}
          </select>
          {isLoadingPromotion && (
            <p className="mt-1.5 text-[10px] text-slate-400">Đang tải mã giảm giá...</p>
          )}
          {!isLoadingPromotion && availablePromotions.length === 0 && (
            <p className="mt-1.5 text-[10px] text-slate-400">Không có ưu đãi khả dụng lúc này.</p>
          )}
          {appliedPromotion && (
            <p className="mt-1.5 text-[10px] font-extrabold text-emerald-600">
              ✓ Đang áp dụng: {getPromotionDisplayText(appliedPromotion)}
            </p>
          )}
        </div>

        {/* Submit */}
        <div className="flex justify-end gap-3 mt-6">
          <button
            type="button"
            onClick={() => setScreen('list')}
            className="rounded-xl border border-slate-200 px-5 py-3 text-xs font-bold text-slate-500 hover:bg-slate-50 transition active:scale-95"
          >
            Quay lại
          </button>
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
            className="rounded-xl bg-blue-600 px-6 py-3 text-xs font-bold text-white shadow hover:bg-blue-700 disabled:opacity-50 transition active:scale-95"
          >
            {isLoadingFlights ? 'Đang khởi tạo đặt vé...' : 'Tiếp tục ➔'}
          </button>
        </div>
        {apiError && (
          <div className="mt-4 rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-medium">
            {apiError}
          </div>
        )}
      </section>

      {/* Booking Summary Sidebar */}
      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg border border-slate-100 hover:shadow-xl transition">
        <h3 className="title-font mb-4 text-base font-extrabold text-slate-800 pb-2 border-b border-slate-100">Chi tiết hành trình</h3>
        <div className="space-y-4">
          <div>
            <span className="text-[10px] font-extrabold text-blue-600 uppercase">Chiều đi (Outbound)</span>
            <p className="text-xs font-bold text-slate-800 mt-0.5">{selectedFromAirport?.City} ({selectedFromAirport?.Code}) ➔ {selectedToAirport?.City} ({selectedToAirport?.Code})</p>
            {selectedFlight && (
              <div className="mt-1 bg-slate-50 border border-slate-150 rounded-xl p-2.5 text-xs text-slate-500">
                <p className="font-bold text-slate-700">{selectedFlight.airlineCode} {selectedFlight.flightNumber}</p>
                <p className="text-[10px] mt-0.5">{formatTime(selectedFlight.departureTime)} - {formatTime(selectedFlight.arrivalTime)} ({formatDuration(selectedFlight.durationMinutes)})</p>
              </div>
            )}
          </div>

          {tripType === 'roundtrip' && returnFlight && (
            <div>
              <span className="text-[10px] font-extrabold text-indigo-600 uppercase">Chiều về (Return)</span>
              <p className="text-xs font-bold text-slate-800 mt-0.5">{selectedToAirport?.City} ({selectedToAirport?.Code}) ➔ {selectedFromAirport?.City} ({selectedFromAirport?.Code})</p>
              <div className="mt-1 bg-slate-50 border border-slate-150 rounded-xl p-2.5 text-xs text-slate-500">
                <p className="font-bold text-slate-700">{returnFlight.airlineCode} {returnFlight.flightNumber}</p>
                <p className="text-[10px] mt-0.5">{formatTime(returnFlight.departureTime)} - {formatTime(returnFlight.arrivalTime)} ({formatDuration(returnFlight.durationMinutes)})</p>
              </div>
            </div>
          )}

          <div className="border-t border-slate-100 pt-4 space-y-2.5">
            <div className="flex justify-between text-xs text-slate-500 font-semibold">
              <span>Giá vé cơ bản:</span>
              <span>{formatCurrency(totalPrice)}</span>
            </div>

            {appliedPromotion && (
              <div className="rounded-xl bg-emerald-50 border border-emerald-100 p-2.5 text-xs text-emerald-800 flex justify-between items-center">
                <div>
                  <p className="font-bold">🎉 Khuyến mãi</p>
                  <p className="text-[10px] text-emerald-600 mt-0.5">{getPromotionCode(appliedPromotion)}</p>
                </div>
                <span className="font-extrabold">-{formatCurrency(discountAmount)}</span>
              </div>
            )}

            <div className="flex justify-between border-t border-slate-100 pt-3 text-sm font-extrabold text-blue-600">
              <span>Tổng thanh toán:</span>
              <span>{formatCurrency(finalPrice)}</span>
            </div>
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
        {/* Payment Main Form */}
        <section className="rounded-2xl bg-white p-5 shadow-lg border border-slate-100 md:p-7 hover:shadow-xl transition">
          <div className="flex items-center gap-2 mb-6 pb-3 border-b border-slate-100">
            <span className="text-xl">💳</span>
            <h2 className="title-font text-xl font-extrabold text-slate-800">Thanh toán hóa đơn</h2>
          </div>
          <p className="mb-4 text-xs font-bold text-slate-500">Cổng thanh toán hỗ trợ: VNPAY</p>

          {paymentData && (
            <div className="mb-4 rounded-2xl bg-blue-50 border border-blue-100 p-4 text-xs text-blue-800 flex justify-between items-center">
              <div>
                <p className="font-bold">Trạng thái booking</p>
                <p className="mt-0.5">{getBookingStatusLabel(paymentData.status)}</p>
              </div>
              <div className="text-right">
                <p className="font-bold">Tổng thanh toán</p>
                <p className="mt-0.5 font-extrabold text-sm">{paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}</p>
              </div>
            </div>
          )}

          <div className="mb-6 rounded-2xl border-2 border-dashed border-blue-200 bg-blue-50/30 p-6 text-center">
            <p className="text-xs font-semibold text-slate-600 leading-relaxed max-w-md mx-auto">
              Hệ thống sẽ chuyển hướng bạn đến trang thanh toán bảo mật của VNPAY Sandbox để hoàn tất giao dịch.
            </p>
            <p className="mt-2 text-[10px] font-mono text-slate-400">
              Mã tham chiếu giao dịch: {paymentData?.transactionRef || bookingReference}
            </p>
            <div className="mt-5 flex justify-center">
              {paymentData?.paymentUrl || paymentData?.paymentLink ? (
                <a
                  className="inline-flex items-center justify-center rounded-xl bg-blue-600 px-6 py-3 text-xs font-bold text-white shadow-md shadow-blue-200 hover:bg-blue-755 transition active:scale-95"
                  href={paymentData.paymentUrl || paymentData.paymentLink}
                  target="_blank"
                  rel="noreferrer"
                >
                  🚀 Mở trang thanh toán VNPAY
                </a>
              ) : (
                <p className="text-xs text-rose-600 font-bold">
                  Không nhận được liên kết thanh toán từ máy chủ. Vui lòng thử lại.
                </p>
              )}
            </div>

            <div className="mt-5 flex flex-wrap gap-3 justify-center">
              <button
                type="button"
                onClick={saveBookingToHistory}
                className="rounded-xl bg-emerald-600 px-4 py-2.5 text-xs font-bold text-white shadow hover:bg-emerald-700 active:scale-95 transition"
              >
                ✓ Xác nhận đã thanh toán xong
              </button>
              <button
                type="button"
                onClick={() => setScreen('history')}
                className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 active:scale-95 transition"
              >
                Xem lịch sử đặt vé
              </button>
            </div>
            {historyNotice && (
              <div className="mt-4 rounded-xl bg-emerald-50 border border-emerald-100 px-4 py-2.5 text-xs text-emerald-700 font-medium">
                {historyNotice}
              </div>
            )}
          </div>

          {/* NCB Simulated Credit Card */}
          <div className="rounded-2xl border border-slate-200 p-5 bg-slate-50/50">
            <h4 className="text-xs font-bold text-slate-800 mb-2">Thẻ ATM Test Sandbox (Ngân hàng NCB)</h4>
            <p className="text-[10px] text-slate-400 mb-3">Bạn có thể sử dụng thông tin thẻ dưới đây khi trang thanh toán VNPAY yêu cầu:</p>

            <div className="relative mx-auto my-4 w-72 h-44 rounded-2xl bg-gradient-to-br from-slate-800 via-slate-900 to-black p-5 text-white shadow-xl flex flex-col justify-between overflow-hidden">
              <div className="absolute top-0 right-0 w-24 h-24 bg-gradient-to-br from-white/10 to-transparent rounded-full -mr-8 -mt-8" />
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold uppercase tracking-wider text-slate-400">NCB Bank (ATM Card)</span>
                <span className="h-6 w-9 rounded bg-amber-500/20 border border-amber-500/30 flex items-center justify-center font-extrabold text-[10px] text-amber-400">CHIP</span>
              </div>
              <div className="my-2">
                <p className="text-xs font-bold text-slate-500 tracking-widest leading-none mb-1">CARD NUMBER</p>
                <p className="text-base font-extrabold tracking-widest text-slate-100 font-mono">9704 1985 2619 1432 198</p>
              </div>
              <div className="flex items-end justify-between">
                <div>
                  <p className="text-[8px] font-bold text-slate-500 uppercase leading-none">Card Holder</p>
                  <p className="text-xs font-extrabold tracking-wide text-slate-200 mt-1 uppercase font-mono">NGUYEN VAN A</p>
                </div>
                <div className="text-right">
                  <p className="text-[8px] font-bold text-slate-500 uppercase leading-none">Expires</p>
                  <p className="text-xs font-extrabold text-slate-200 mt-1 font-mono">07 / 15</p>
                </div>
              </div>
            </div>

            <ol className="list-decimal space-y-1.5 text-xs text-slate-500 pl-5 mt-4 leading-relaxed">
              <li>Tại cổng VNPAY, chọn phương thức <strong>"Thẻ nội địa và tài khoản ngân hàng"</strong>.</li>
              <li>Chọn biểu tượng logo ngân hàng <strong>NCB</strong>.</li>
              <li>Nhập thông tin số thẻ, tên chủ thẻ và ngày hết hạn như trên mockup.</li>
              <li>Bấm thanh toán và nhập mã OTP test: <strong>123456</strong> để hoàn tất.</li>
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
            className="mt-5 text-xs font-semibold text-slate-500 hover:text-slate-700 underline"
          >
            Hủy bỏ giao dịch
          </button>
        </section>

        {/* Order Summary Sidebar */}
        <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg border border-slate-100 hover:shadow-xl transition">
          <h3 className="title-font mb-4 text-base font-extrabold text-slate-800 pb-2 border-b border-slate-100">Chi tiết đơn đặt</h3>
          <div className="space-y-3.5 text-xs text-slate-500">
            <div>
              <span className="text-[10px] font-extrabold text-blue-600 uppercase">Mã đặt chỗ (ID)</span>
              <p className="font-bold text-slate-800 mt-0.5">#{getFlightId(selectedFlight)}</p>
            </div>
            <div>
              <span className="text-[10px] font-extrabold text-slate-400 uppercase">Số hiệu chuyến đi</span>
              <p className="font-bold text-slate-800 mt-0.5">{selectedFlight?.airlineCode} {selectedFlight?.flightNumber}</p>
            </div>
            <div>
              <span className="text-[10px] font-extrabold text-slate-400 uppercase">Thời gian cất cánh</span>
              <p className="font-bold text-slate-800 mt-0.5">{formatDateTime(selectedFlight?.departureTime)}</p>
            </div>

            {tripType === 'roundtrip' && returnFlight && (
              <>
                <div className="border-t border-slate-100 pt-3">
                  <span className="text-[10px] font-extrabold text-indigo-600 uppercase">Số hiệu chuyến về</span>
                  <p className="font-bold text-slate-800 mt-0.5">{returnFlight?.airlineCode} {returnFlight?.flightNumber}</p>
                </div>
                <div>
                  <span className="text-[10px] font-extrabold text-slate-400 uppercase">Thời gian cất cánh về</span>
                  <p className="font-bold text-slate-800 mt-0.5">{formatDateTime(returnFlight?.departureTime)}</p>
                </div>
              </>
            )}

            <div className="border-t border-slate-100 pt-3 space-y-2">
              <div className="flex justify-between font-semibold">
                <span>Số lượng hành khách:</span>
                <span className="text-slate-800 font-extrabold">{searchData.passengers} vé</span>
              </div>
              <div className="flex justify-between font-semibold">
                <span>Mức giá gốc:</span>
                <span>{formatCurrency(totalPrice)}</span>
              </div>

              {appliedPromotion && (
                <div className="flex justify-between font-semibold text-emerald-600">
                  <span>Mã giảm ưu đãi:</span>
                  <span>-{formatCurrency(discountAmount)}</span>
                </div>
              )}

              <div className="flex justify-between border-t border-slate-100 pt-2 text-sm font-extrabold text-blue-600">
                <span>Giá sau giảm:</span>
                <span>{paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}</span>
              </div>
            </div>
          </div>
        </aside>
      </div>
    )
  }


  const renderSavedPassengers = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_360px]">
      {/* Saved passengers listing */}
      <section className="rounded-2xl bg-white p-5 shadow-lg border border-slate-100 md:p-7 hover:shadow-xl transition">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <span className="text-xs uppercase font-extrabold tracking-wider text-blue-600">Danh bạ của bạn</span>
            <h2 className="title-font mt-1 text-2xl font-extrabold text-slate-800">Hành khách đã lưu</h2>
            <p className="mt-1 text-xs text-slate-400">
              Lưu trữ thông tin hành khách giúp việc đặt vé máy bay diễn ra nhanh chóng hơn.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={loadSavedPassengers}
              className="rounded-xl border border-slate-200 px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-50 transition active:scale-95"
            >
              Làm mới
            </button>
            <button
              type="button"
              onClick={startCreateSavedPassenger}
              className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-bold text-white shadow hover:bg-blue-700 transition active:scale-95"
            >
              + Thêm mới
            </button>
          </div>
        </div>

        {isLoadingSavedPassengers && (
          <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 p-6 text-center text-xs text-slate-400 font-medium">
            Đang tải dữ liệu danh bạ...
          </div>
        )}

        {savedPassengerError && (
          <div className="mb-4 rounded-xl bg-rose-50 border border-rose-100 p-3.5 text-xs text-rose-600 font-medium">
            {savedPassengerError}
          </div>
        )}

        {savedPassengerNotice && (
          <div className="mb-4 rounded-xl bg-emerald-50 border border-emerald-100 p-3.5 text-xs text-emerald-600 font-medium">
            {savedPassengerNotice}
          </div>
        )}

        {!isLoadingSavedPassengers && savedPassengers.length === 0 && (
          <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs text-slate-400 font-medium">
            Chưa có hành khách nào được lưu trong danh bạ của bạn.
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
                  className="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm hover:shadow-md transition duration-fast"
                >
                  <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                    <div className="grid grid-cols-[auto_1fr] items-center gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600 font-extrabold text-sm border border-blue-100">
                        {(fullName || 'A')[0].toUpperCase()}
                      </div>
                      <div>
                        <p className="text-sm font-bold text-slate-800">
                          {fullName || 'Chưa rõ tên'}
                        </p>
                        <p className="text-[11px] text-slate-400 mt-0.5">
                          Ngày sinh: <span className="font-bold text-slate-600">{dobLabel}</span> · Giới tính: <span className="font-bold text-slate-600">{passenger?.gender || '---'}</span>
                        </p>
                        <p className="text-[11px] text-slate-400">
                          Quốc tịch: <span className="font-bold text-slate-600">{passenger?.nationality || '---'}</span> · Giấy tờ: <span className="font-bold text-slate-600">{passenger?.documentNumber || '---'}</span>
                        </p>
                        <p className="text-[10px] text-slate-400 mt-1">
                          Email: {passenger?.email || '---'} · SĐT: {passenger?.phone || '---'}
                        </p>
                      </div>
                    </div>
                    <div className="flex flex-wrap items-center justify-end gap-2 border-t border-slate-50 pt-3 md:border-none md:pt-0">
                      <button
                        type="button"
                        onClick={() => startEditSavedPassenger(passenger)}
                        className="rounded-lg border border-slate-200 px-3 py-1.5 text-[11px] font-bold text-slate-600 hover:bg-slate-50 transition active:scale-95"
                      >
                        Sửa
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeleteSavedPassenger(passenger)}
                        className="rounded-lg bg-rose-50 border border-rose-100 px-3 py-1.5 text-[11px] font-bold text-rose-600 hover:bg-rose-100 transition active:scale-95"
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

      {/* Saved Passenger Input Form */}
      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg border border-slate-100 hover:shadow-xl transition">
        <h3 className="title-font mb-4 text-base font-extrabold text-slate-800 pb-2 border-b border-slate-100">
          {editingSavedPassengerId ? '📝 Cập nhật thông tin' : '👤 Lưu thành viên mới'}
        </h3>
        <form onSubmit={handleSavedPassengerSubmit} className="space-y-3.5">
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Họ đệm</label>
            <input
              value={savedPassengerForm.lastName}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, lastName: event.target.value }))
              }
              placeholder="VD: Nguyễn"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Tên gọi</label>
            <input
              value={savedPassengerForm.firstName}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, firstName: event.target.value }))
              }
              placeholder="VD: An"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Ngày sinh</label>
            <input
              type="date"
              value={savedPassengerForm.dateOfBirth}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, dateOfBirth: event.target.value }))
              }
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Giới tính</label>
            <select
              value={savedPassengerForm.gender}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, gender: event.target.value }))
              }
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            >
              <option value="">-- Chọn giới tính --</option>
              <option value="Nam">Nam</option>
              <option value="Nữ">Nữ</option>
              <option value="Khác">Khác</option>
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Quốc tịch</label>
            <input
              value={savedPassengerForm.nationality}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, nationality: event.target.value }))
              }
              placeholder="Việt Nam"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Số giấy tờ (CCCD/Passport)</label>
            <input
              value={savedPassengerForm.documentNumber}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, documentNumber: event.target.value }))
              }
              placeholder="VD: 0123456789"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Địa chỉ Email</label>
            <input
              type="email"
              value={savedPassengerForm.email}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, email: event.target.value }))
              }
              placeholder="email@example.com"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-bold text-slate-600">Số điện thoại</label>
            <input
              value={savedPassengerForm.phone}
              onChange={(event) =>
                setSavedPassengerForm((prev) => ({ ...prev, phone: event.target.value }))
              }
              placeholder="VD: 0901234567"
              className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-xs text-slate-700 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
            />
          </div>

          <div className="flex gap-2 pt-2">
            <button
              type="submit"
              className="flex-1 rounded-xl bg-blue-600 px-4 py-2.5 text-xs font-bold text-white shadow hover:bg-blue-700 active:scale-95 transition"
            >
              {editingSavedPassengerId ? 'Lưu thay đổi' : 'Thêm danh bạ'}
            </button>
            <button
              type="button"
              onClick={startCreateSavedPassenger}
              className="rounded-xl border border-slate-200 px-4 py-2.5 text-xs font-semibold text-slate-500 hover:bg-slate-50 active:scale-95 transition"
            >
              Đặt lại
            </button>
          </div>
        </form>
      </aside>
    </div>
  )

  const renderHistory = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg border border-slate-100 md:p-7 hover:shadow-xl transition">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-slate-100">
        <div>
          <span className="text-xs uppercase font-extrabold tracking-wider text-blue-600">Quản lý vé của bạn</span>
          <h2 className="title-font mt-0.5 text-2xl font-extrabold text-slate-800">Lịch sử đặt vé</h2>
          <p className="text-xs text-slate-400">Xem lại và cập nhật dịch vụ cho tất cả các vé bạn đã đặt.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={loadBookingHistory}
            className="rounded-xl border border-slate-200 px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-50 transition active:scale-95"
          >
            Làm mới
          </button>
          <button
            type="button"
            onClick={() => setScreen('search')}
            className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-bold text-white shadow hover:bg-blue-755 transition active:scale-95"
          >
            Đặt vé mới
          </button>
        </div>
      </div>

      {isLoadingHistory && (
        <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 p-6 text-center text-xs text-slate-400 font-medium">
          Đang tải lịch sử đặt vé...
        </div>
      )}

      {historyError && (
        <div className="mb-4 rounded-xl bg-rose-50 border border-rose-100 p-3.5 text-xs text-rose-600 font-medium">
          {historyError}
        </div>
      )}

      {historyNotice && (
        <div className="mb-4 rounded-xl bg-emerald-50 border border-emerald-100 p-3.5 text-xs text-emerald-600 font-medium">
          {historyNotice}
        </div>
      )}

      {!isLoadingHistory && bookingHistory.length === 0 && (
        <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs text-slate-400 font-medium">
          Chưa tìm thấy dữ liệu đặt vé nào trong tài khoản. Hãy thực hiện đặt vé và thanh toán.
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
                className="rounded-2xl border border-slate-100 bg-white p-5 shadow-sm hover:shadow-md transition duration-fast"
              >
                <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                  <div className="space-y-1.5 text-xs">
                    <span className="text-[10px] font-extrabold uppercase text-blue-600 bg-blue-50 px-2 py-0.5 rounded border border-blue-100">
                      {formatRouteLabel(item)}
                    </span>
                    <p className="text-base font-extrabold text-slate-800 mt-2">
                      {formatTime(item.departTime)} - {formatTime(item.arriveTime)}
                    </p>
                    <p className="text-slate-500 font-medium">
                      Khởi hành: <span className="font-bold text-slate-700">{formatDateTime(item.departTime)}</span>
                    </p>
                    {formatFlightMeta(item) && (
                      <p className="text-slate-500 font-semibold">{formatFlightMeta(item)}</p>
                    )}
                    <p className="text-[10px] text-slate-400 pt-1 font-mono">
                      Mã đơn đặt: {item.bookingId} {item.transactionRef ? `· Mã GD: ${item.transactionRef}` : ''}
                    </p>
                  </div>
                  <div className="text-left md:text-right space-y-1.5 md:flex md:flex-col md:items-end">
                    <span
                      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-[10px] font-bold border ${getBookingStatusClassName(item.status)}`}
                    >
                      ● {getBookingStatusLabel(item.status)}
                    </span>
                    <p className="text-[10px] text-slate-400 font-medium">Ngày đặt: {formatDateTime(item.createdAt)}</p>
                    <p className="text-lg font-extrabold text-slate-800">
                      {formatCurrency(item.totalPrice)}
                    </p>
                    <p className="text-[11px] font-bold text-slate-500">
                      Hành khách: <span className="text-slate-700">{item.passengerName || '---'}</span> · {item.passengerCount} Vé
                    </p>
                  </div>
                </div>

                {/* Additional services purchased section */}
                {Array.isArray(item.passengers) &&
                  item.passengers.some(
                    (passenger) => Array.isArray(passenger?.services) && passenger.services.length > 0
                  ) && (
                    <div className="mt-4 rounded-xl border border-slate-150 bg-slate-50/50 p-4 text-xs">
                      <p className="font-bold text-slate-700 flex items-center gap-1">🧳 Dịch vụ đã đăng ký</p>
                      <div className="mt-2.5 divide-y divide-slate-150">
                        {item.passengers.map((passenger, passengerIndex) => {
                          const services = Array.isArray(passenger?.services) ? passenger.services : []
                          if (services.length === 0) return null
                          const passengerLabel =
                            [passenger?.lastName, passenger?.firstName].filter(Boolean).join(' ') ||
                            `Hành khách ${passengerIndex + 1}`

                          return (
                            <div key={passenger?.passengerId || passengerIndex} className="py-2 first:pt-0 last:pb-0">
                              <p className="font-bold text-slate-600">{passengerLabel}</p>
                              <ul className="mt-1 space-y-1 pl-4 list-disc">
                                {services.map((service, serviceIndex) => (
                                  <li
                                    key={`${service?.additionalServiceId || serviceIndex}-${serviceIndex}`}
                                    className="text-slate-500 font-medium"
                                  >
                                    <span className="font-semibold text-slate-600">{service?.serviceName || `Dịch vụ #${service?.additionalServiceId || '-'}`}</span>
                                    {service?.quantity > 1 ? ` (Số lượng: ${service.quantity})` : ''} · Phí: {Number.isFinite(Number(service?.price)) ? formatCurrency(service.price) : ''}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )}

                {/* Booking controls actions */}
                <div className="mt-4 border-t border-slate-50 pt-3 flex flex-wrap gap-2 justify-end">
                  <button
                    type="button"
                    onClick={() => loadTicketsForBooking(item.bookingId)}
                    className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-bold text-slate-600 hover:bg-slate-50 transition active:scale-95"
                  >
                    Xem chi tiết vé
                  </button>

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
                          
                          setHistoryNotice('Đang chuyển hướng sang trang thanh toán VNPAY...')
                          const paymentResponse = await initiatePayment(bookingIdValue, 'VNPAY')
                          
                          if (paymentResponse?.paymentUrl) {
                            window.location.href = paymentResponse.paymentUrl
                          } else {
                            setHistoryError('Không nhận được liên kết thanh toán từ VNPAY')
                          }
                        } catch (error) {
                          setHistoryError(error.message || 'Lỗi khi khởi tạo thanh toán')
                        }
                      }}
                      className="rounded-xl bg-emerald-600 px-4 py-2 text-xs font-bold text-white shadow-md shadow-emerald-50 hover:bg-emerald-700 transition active:scale-95"
                    >
                      💳 Thanh toán trực tuyến
                    </button>
                  )}

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
                      className="rounded-xl bg-blue-600 px-4 py-2 text-xs font-bold text-white shadow hover:bg-blue-700 transition active:scale-95"
                    >
                      ➕ Mua thêm dịch vụ
                    </button>
                  )}

                  {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                    <button
                      type="button"
                      onClick={() => openChangeFlightModal(item)}
                      className="rounded-xl border border-emerald-300 bg-emerald-50 px-4 py-2 text-xs font-bold text-emerald-700 hover:bg-emerald-100 transition active:scale-95"
                    >
                      🔄 Thay đổi giờ bay
                    </button>
                  )}

                  {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                    <button
                      type="button"
                      onClick={() => cancelBookingFromHistory(item)}
                      disabled={isCancellingBookingId === item.bookingId}
                      className="rounded-xl bg-rose-500 px-4 py-2 text-xs font-bold text-white shadow hover:bg-rose-600 disabled:opacity-60 transition active:scale-95"
                    >
                      {isCancellingBookingId === item.bookingId ? 'Đang xử lý...' : 'Hủy bỏ vé'}
                    </button>
                  )}

                  {(isBookingCancelled(item.status) || isPendingDisruptionDecision(item.status)) && (
                    <button
                      type="button"
                      onClick={() => loadDisruptionOptionsForBooking(item.bookingId)}
                      className="rounded-xl bg-amber-500 px-4 py-2 text-xs font-bold text-white shadow hover:bg-amber-600 transition"
                    >
                      Phương án xử lý hủy chuyến
                    </button>
                  )}
                </div>

                {isDisruptionLoading && (
                  <div className="mt-3 rounded-xl border border-dashed border-slate-200 bg-slate-50 p-3 text-xs text-slate-400 text-center font-medium">
                    Đang tìm kiếm giải pháp chuyển đổi...
                  </div>
                )}
                {disruptionError && (
                  <div className="mt-3 rounded-xl bg-rose-50 border border-rose-100 p-3 text-xs text-rose-600 font-semibold">
                    {disruptionError}
                  </div>
                )}
                {disruptionNotice && (
                  <div className="mt-3 rounded-xl bg-emerald-50 border border-emerald-100 p-3 text-xs text-emerald-700 font-semibold">
                    {disruptionNotice}
                  </div>
                )}

                {/* Disruption refund options */}
                {disruptionOptions.length > 0 && (
                  <div className="mt-4 rounded-xl border border-amber-200 bg-amber-50/40 p-4 text-xs">
                    <p className="font-bold text-amber-800 flex items-center gap-1">⚠️ Phương án đổi/hoàn vé do lỗi khai thác</p>
                    <div className="mt-3 grid gap-3 md:grid-cols-[1.2fr_1fr]">
                      <div>
                        <label className="mb-1 block text-xs font-bold text-slate-600">Chọn chặng bị hủy</label>
                        <select
                          value={selectedDecisionId}
                          onChange={(event) =>
                            setSelectedDisruptionDecisionMap((prev) => ({
                              ...prev,
                              [item.bookingId]: event.target.value,
                            }))
                          }
                          className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700"
                        >
                          {disruptionOptions.map((option, optionIndex) => (
                            <option
                              key={`${option?.decisionId || optionIndex}-${optionIndex}`}
                              value={option?.decisionId ?? ''}
                            >
                              {getDisruptionLegLabel(option?.legType)} · Chuyến bay #{option?.decisionId}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="mb-1 block text-xs font-bold text-slate-600">Chọn hình thức xử lý</label>
                        <div className="flex gap-4 pt-1">
                          <label className="flex items-center gap-2 text-xs font-semibold text-slate-700 cursor-pointer">
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
                            Hoàn lại tiền vé
                          </label>
                          <label className="flex items-center gap-2 text-xs font-semibold text-slate-700 cursor-pointer">
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
                            Đổi sang chuyến khác
                          </label>
                        </div>
                      </div>
                    </div>
                    <div className="mt-3 flex justify-end">
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
                        className="rounded-xl bg-amber-600 px-5 py-2 text-xs font-bold text-white shadow hover:bg-amber-700 active:scale-95 disabled:opacity-50 transition"
                      >
                        Xác nhận phương án
                      </button>
                    </div>
                  </div>
                )}

                {/* Sub-tickets mapping */}
                {loadingTicketsMap[item.bookingId] && (
                  <div className="mt-3 text-xs text-slate-400 text-center font-medium">
                    Đang tải danh sách vé...
                  </div>
                )}
                {ticketErrorMap[item.bookingId] && (
                  <div className="mt-3 text-xs text-rose-600 font-semibold text-center">
                    {ticketErrorMap[item.bookingId]}
                  </div>
                )}
                {Array.isArray(bookingTicketsMap[item.bookingId]) &&
                  bookingTicketsMap[item.bookingId].length > 0 && (
                    <div className="mt-4 rounded-xl border border-slate-150 bg-slate-50/50 p-4 text-xs space-y-3">
                      <p className="font-bold text-slate-700">🎟️ Chi tiết vé máy bay</p>
                      <div className="grid gap-3 md:grid-cols-2">
                        {bookingTicketsMap[item.bookingId].map((ticket) => (
                          <div
                            key={ticket.ticketId}
                            className="rounded-xl border border-slate-150 bg-white p-3.5 shadow-sm space-y-2"
                          >
                            <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-50 pb-2">
                              <div>
                                <span className="text-[10px] font-extrabold uppercase text-slate-400">Vé số</span>
                                <p className="text-xs font-bold text-slate-800">{ticket.ticketNumber || `Vé #${ticket.ticketId}`}</p>
                              </div>
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
                                      className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[9px] font-bold border"
                                      style={{ background: bg, color: textCol, borderColor: textCol + '20' }}
                                    >
                                      <span>{icon}</span>
                                      <span>{label}</span>
                                    </span>
                                  )
                                })()
                              )}
                            </div>
                            <div className="text-[11px] text-slate-500 space-y-1 font-medium">
                              <p>Hành khách: <span className="font-bold text-slate-700">{ticket.passengerName || '---'}</span></p>
                              <p>Số hiệu bay: <span className="font-bold text-slate-700">{ticket.flightNumber || '---'}</span></p>
                              <p>Hành trình: <span className="font-semibold text-slate-700">{ticket.departureAirport || '---'} ➔ {ticket.arrivalAirport || '---'}</span></p>
                              <p>Giờ khởi hành: <span className="font-semibold text-slate-700">{formatDateTime(ticket.departureTime)}</span></p>
                              <p>Hạng vé: <span className="font-bold text-indigo-700 bg-indigo-50 border border-indigo-100 rounded px-2 py-0.5 text-[10px] inline-block mt-0.5">{ticket.seatClassName || ticket.seatClassCode || ticket.seatClass || (Number(ticket.seatClassId) === 2 ? 'Business' : 'Economy')}</span></p>
                            </div>

                            {/* Ticket additional services */}
                            {Array.isArray(ticket.services) && ticket.services.length > 0 && (
                              <div className="mt-2 rounded-lg bg-slate-50 p-2 border border-slate-100 text-[10px]">
                                <p className="font-bold text-slate-500">Dịch vụ mua kèm:</p>
                                <ul className="mt-1 space-y-1">
                                  {ticket.services.map((service, serviceIndex) => (
                                    <li
                                      key={`${service?.additionalServiceId || serviceIndex}-${serviceIndex}`}
                                      className="flex items-center justify-between text-slate-600 font-semibold"
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

                            {isTicketActionable(ticket.status) && (
                              <div className="flex gap-2 pt-2 border-t border-slate-50">
                                <button
                                  type="button"
                                  onClick={() => openUpgradeModal(item, ticket)}
                                  className="flex-1 rounded-lg border border-indigo-200 bg-indigo-50 px-2 py-1.5 text-[10px] font-bold text-indigo-700 hover:bg-indigo-100 transition active:scale-95"
                                >
                                  ⬆️ Nâng hạng vé
                                </button>
                                <button
                                  type="button"
                                  disabled={isCancellingTicketId === ticket.ticketId}
                                  onClick={() => cancelTicketFromHistory(item.bookingId, ticket)}
                                  className="flex-1 rounded-lg bg-rose-50 border border-rose-100 px-2 py-1.5 text-[10px] font-bold text-rose-600 hover:bg-rose-100 transition active:scale-95 disabled:opacity-50"
                                >
                                  {isCancellingTicketId === ticket.ticketId ? 'Đang hủy...' : 'Hủy vé này'}
                                </button>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
              </article>
            )
          })}
        </div>
      )}

      {/* Services Modal selection */}
      {showServicesModal && currentBookingForServices && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl border border-slate-100">
            <div className="mb-4 flex items-start justify-between pb-3 border-b border-slate-150">
              <div>
                <h3 className="text-lg font-extrabold text-slate-800">
                  ➕ Đăng Ký Mua Thêm Dịch Vụ
                </h3>
                <p className="mt-0.5 text-xs text-slate-400">
                  Booking ID: #{currentBookingForServices.bookingId} - Chuyến bay: {currentBookingForServices.flightNumber}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowServicesModal(false)
                  setCurrentBookingForServices(null)
                  setSelectedServicesByPassenger({})
                }}
                className="rounded-lg bg-slate-100 p-2 text-slate-500 hover:bg-slate-200 transition font-bold"
              >
                ✕ Đóng
              </button>
            </div>

            {currentBookingForServices?.passengers?.length > 0 ? (
              <div className="mb-4 rounded-xl border border-blue-100 bg-blue-50/30 p-4">
                <p className="text-xs font-bold text-blue-800">
                  Hãy đăng ký thêm hành lý, ký gửi suất ăn riêng theo từng hành khách
                </p>
                <p className="mt-0.5 text-[10px] text-slate-500">
                  Các dịch vụ đã bao gồm trong tiêu chuẩn hạng ghế sẽ hiển thị giá 0đ (Miễn phí).
                </p>
              </div>
            ) : (
              <div className="mb-4 rounded-xl border border-dashed border-slate-200 bg-slate-50 p-4 text-xs text-slate-400 text-center font-medium">
                Đơn đặt vé này hiện chưa có thông tin hành khách hợp lệ.
              </div>
            )}

            {isLoadingServices && (
              <div className="py-8 text-center text-xs text-slate-400 font-medium">
                Đang kết nối tải danh mục dịch vụ phụ trợ...
              </div>
            )}

            {!isLoadingServices && services.length === 0 && (
              <div className="py-8 text-center text-xs text-slate-400 font-medium">
                Không tìm thấy danh mục dịch vụ phù hợp cho chuyến bay này.
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
                      className="rounded-xl border border-slate-200 bg-slate-50/50 p-4"
                    >
                      <div className="mb-3 flex items-center justify-between">
                        <h4 className="text-xs font-bold text-slate-700">{label}</h4>
                        <span className="text-[10px] text-slate-400 font-semibold">Tùy chọn dịch vụ</span>
                      </div>
                      <div className="space-y-3">
                        {services.map((service) => {
                          const serviceId = service.serviceId || service.id
                          const current = passengerSelection[serviceId] || 0
                          return (
                            <div
                              key={serviceId}
                              className="flex items-start justify-between gap-4 rounded-xl border border-slate-150 bg-white p-3 shadow-sm"
                            >
                              <div className="flex-1">
                                <h5 className="text-xs font-bold text-slate-800">
                                  {service.serviceName}
                                </h5>
                                <p className="mt-0.5 text-[10px] text-slate-400 font-medium leading-relaxed">{service.description}</p>
                                <p className="mt-2 text-xs font-extrabold text-blue-600">
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
                                  className="flex h-7 w-7 items-center justify-center rounded-lg bg-slate-100 text-slate-600 hover:bg-slate-200 font-bold text-xs"
                                >
                                  −
                                </button>
                                <span className="w-8 text-center text-xs font-bold">
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
                                  className="flex h-7 w-7 items-center justify-center rounded-lg bg-blue-600 text-white hover:bg-blue-700 font-bold text-xs"
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

            <div className="mt-6 rounded-xl border border-slate-200 bg-slate-50 p-4 flex flex-wrap items-center justify-between gap-3 shadow-inner">
              <div>
                <p className="text-[10px] font-bold text-slate-400">TỔNG CỘNG PHÍ DỊCH VỤ:</p>
                <p className="text-xl font-extrabold text-slate-800">
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

                    setHistoryNotice('Đang cập nhật thêm dịch vụ phụ trợ...')

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

                    setHistoryNotice(`✅ Đăng ký mua thêm ${selectedCount} dịch vụ thành công!`)
                    setShowServicesModal(false)
                    setCurrentBookingForServices(null)
                    setSelectedServicesByPassenger({})
                  } catch (error) {
                    setHistoryError(error.message || 'Lỗi khi thêm dịch vụ')
                  }
                }}
                className="rounded-xl bg-blue-600 px-5 py-3 text-xs font-bold text-white shadow hover:bg-blue-700 active:scale-95 transition"
              >
                ✓ Xác nhận đăng ký mua dịch vụ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Disruption Rebook Modal Selection */}
      {rebookModalState.isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl border border-slate-100">
            <div className="mb-4 flex items-start justify-between pb-3 border-b border-slate-150">
              <div>
                <h3 className="text-lg font-extrabold text-slate-800">Đổi Chuyến Bay</h3>
                <p className="mt-0.5 text-xs text-slate-400">
                  Lựa chọn ngày bay và giờ bay mới phù hợp hơn cho hành trình của bạn.
                </p>
              </div>
              <button
                type="button"
                onClick={closeRebookModal}
                className="rounded-lg bg-slate-100 p-2 text-slate-500 hover:bg-slate-200 transition font-bold"
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
                  <div className="rounded-xl border border-slate-200 bg-slate-50 p-4">
                    <label className="mb-2 block text-xs font-bold text-slate-600">
                      Chọn ngày khởi hành mới
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
                      className="w-full max-w-xs rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 outline-none focus:border-blue-500"
                    />
                  </div>

                  <div className="space-y-3">
                    {filteredOptions.map((flight) => {
                      const isSelected = String(flight.flightId) === rebookModalState.selectedFlightId

                      return (
                        <article
                          key={flight.flightId}
                          className={`rounded-2xl border bg-white p-4 shadow-sm transition-all duration-fast ${
                            isSelected
                              ? 'border-blue-600 shadow-md shadow-blue-50'
                              : 'border-slate-150 hover:border-blue-300'
                          }`}
                        >
                          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
                            <div className="grid grid-cols-[auto_1fr] items-center gap-3">
                              <span className="text-xl">✈️</span>
                              <div>
                                <p className="text-xs font-bold text-blue-600">
                                  {flight.flightNumber || `Chuyến #${flight.flightId}`}
                                </p>
                                <p className="mt-1 text-sm font-bold text-slate-800">
                                  {formatTime(flight.departureTime)} - {formatTime(flight.arrivalTime)}
                                </p>
                                <p className="text-[10px] font-bold text-emerald-650">
                                  Còn trống: {flight.availableSeats} ghế
                                </p>
                              </div>
                            </div>
                            <button
                              type="button"
                              onClick={() =>
                                setRebookModalState((prev) => ({
                                  ...prev,
                                  selectedFlightId: String(flight.flightId),
                                }))
                              }
                              className={`rounded-xl px-4 py-2 text-xs font-bold transition ${
                                isSelected
                                  ? 'bg-emerald-600 text-white hover:bg-emerald-700 shadow-md shadow-emerald-150'
                                  : 'bg-blue-600 text-white hover:bg-blue-700 shadow-md shadow-blue-150'
                              }`}
                            >
                              {isSelected ? 'Đang chọn' : 'Đổi chuyến này'}
                            </button>
                          </div>
                        </article>
                      )
                    })}

                    {filteredOptions.length === 0 && (
                      <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs font-bold text-slate-400">
                        Không có chuyến bay thay thế nào trong ngày đã chọn.
                      </div>
                    )}
                  </div>

                  <div className="flex gap-3 border-t border-slate-100 pt-4">
                    <button
                      type="button"
                      onClick={closeRebookModal}
                      className="flex-1 rounded-xl border border-slate-200 bg-white py-2.5 text-xs font-bold text-slate-700 hover:bg-slate-50 transition"
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
                          rebookModalState.selectedFlightId
                        )
                        closeRebookModal()
                      }}
                      className="flex-1 rounded-xl bg-emerald-600 py-2.5 text-xs font-bold text-white hover:bg-emerald-700 transition shadow-md shadow-emerald-250 disabled:opacity-50"
                    >
                      Xác nhận đổi chuyến bay miễn phí
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
    <div className="space-y-6">
      {/* List section of promotions */}
      <section className="rounded-3xl bg-white p-6 shadow-xl border border-slate-100 shadow-slate-100/50 md:p-8">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-5">
          <div>
            <span className="inline-flex items-center gap-1.5 rounded-full bg-indigo-50 px-2.5 py-1 text-xs font-bold uppercase tracking-wider text-indigo-600">
              <span className="h-1.5 w-1.5 rounded-full bg-indigo-600 animate-pulse"></span>
              Bảng quản trị
            </span>
            <h2 className="title-font mt-2 text-2xl font-black text-slate-800">Quản lý khuyến mãi</h2>
            <p className="mt-1 text-sm text-slate-500 font-medium">Cấu hình, theo dõi và quản lý các chương trình ưu đãi.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={loadAdminPromotions}
              className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-50 hover:text-slate-800 transition-all duration-instant shadow-sm"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-3.5 w-3.5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
              </svg>
              Làm mới
            </button>
            <button
              type="button"
              onClick={startCreatePromotion}
              className="inline-flex items-center gap-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-4 py-2.5 text-xs font-bold text-white hover:from-blue-700 hover:to-indigo-700 transition-all duration-instant shadow-md shadow-blue-200"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="3" stroke="currentColor" className="h-3.5 w-3.5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              Tạo mã mới
            </button>
          </div>
        </div>

        {promotionAdminError && (
          <div className="mb-5 rounded-2xl border border-red-100 bg-red-50/50 p-4 text-xs font-medium text-red-700 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5">
              <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.753-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clipRule="evenodd" />
            </svg>
            <span>{promotionAdminError}</span>
          </div>
        )}

        {promotionAdminNotice && (
          <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-medium text-emerald-700 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5">
              <path fillRule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12Zm13.36-1.814a.75.75 0 1 0-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 0 0-1.06 1.06l2.25 2.25a.75.75 0 0 0 1.14-.094l3.74-5.24Z" clipRule="evenodd" />
            </svg>
            <span>{promotionAdminNotice}</span>
          </div>
        )}

        {isLoadingPromotionsAdmin && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs font-semibold text-slate-400">
            <div className="mx-auto mb-3 h-7 w-7 animate-spin rounded-full border-2 border-slate-300 border-t-blue-600"></div>
            Đang tải danh sách khuyến mãi...
          </div>
        )}

        {!isLoadingPromotionsAdmin && adminPromotions.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-12 text-center text-xs font-bold text-slate-400">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.5" stroke="currentColor" className="mx-auto mb-3 h-8 w-8 text-slate-300">
              <path strokeLinecap="round" strokeLinejoin="round" d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 9.75M15 9.75M9 14.25h6" />
            </svg>
            Chưa có mã khuyến mãi nào được tạo.
          </div>
        )}

        {!isLoadingPromotionsAdmin && adminPromotions.length > 0 && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {adminPromotions.map((promo) => (
              <article
                key={promo.promotionId}
                onClick={() => startEditPromotion(promo)}
                className="group relative overflow-hidden rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm hover:shadow-md hover:border-indigo-400 transition-all duration-fast cursor-pointer select-none"
              >
                {/* Visual side-strip */}
                <div className={`absolute top-0 bottom-0 left-0 w-1.5 ${promo.isActive ? 'bg-gradient-to-b from-indigo-500 to-blue-600' : 'bg-slate-350'}`} />

                <div className="flex flex-col h-full justify-between gap-3 pl-2">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between gap-2 flex-wrap">
                      <span className="font-mono text-xs font-extrabold uppercase tracking-wide text-indigo-700 bg-indigo-50 border border-indigo-100 rounded-lg px-2.5 py-0.5">
                        🎫 {promo.code || '---'}
                      </span>
                      <span
                        className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-bold ${
                          promo.isActive
                            ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        <span className={`h-1.5 w-1.5 rounded-full ${promo.isActive ? 'bg-emerald-600' : 'bg-slate-400'}`}></span>
                        {promo.isActive ? 'Hoạt động' : 'Tạm dừng'}
                      </span>
                    </div>

                    <h4 className="text-sm font-bold text-slate-800 line-clamp-2 min-h-[40px] group-hover:text-indigo-700 transition-colors">
                      {promo.description || 'Không có mô tả'}
                    </h4>
                    
                    <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs font-semibold pt-1 border-t border-slate-100/60">
                      <span className="text-blue-700">
                        🛡️ Hạng: {getPromotionTypeLabel(promo.discountType)}
                      </span>
                      <span className="text-emerald-600">
                        💰 Trị giá: {formatCurrency(promo.discountValue)}
                      </span>
                    </div>

                    <div className="grid grid-cols-1 gap-y-0.5 text-[10px] font-medium text-slate-500 pt-1.5">
                      <div>Tối đa: <span className="font-bold text-slate-700">{promo.maxDiscountAmount ? formatCurrency(promo.maxDiscountAmount) : 'Không giới hạn'}</span></div>
                      <div>Tối thiểu: <span className="font-bold text-slate-700">{formatCurrency(promo.minimumAmount || 0)}</span></div>
                      <div className="mt-1 flex items-center gap-1 text-slate-400 border-t border-slate-50 pt-1">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-3 w-3 shrink-0">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
                        </svg>
                        <span className="truncate">{formatDateTime(promo.validFrom).split(',')[0]} → {formatDateTime(promo.validTo).split(',')[0]}</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center justify-between border-t border-slate-100 pt-2 shrink-0">
                    <div className="text-left">
                      <div className="text-[11px] font-bold text-slate-700">{promo.usageCount} lượt dùng</div>
                      <div className="text-[10px] font-medium text-slate-400">
                        Giới hạn: {promo.usageLimit ?? 'Không giới hạn'}
                      </div>
                    </div>

                    <div className="flex gap-1">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation()
                          startEditPromotion(promo)
                        }}
                        className="rounded-lg border border-indigo-200 bg-indigo-50/50 hover:bg-indigo-100 px-2.5 py-1 text-xs font-bold text-indigo-700 transition"
                      >
                        Xem/Sửa
                      </button>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleDeletePromotion(promo)
                        }}
                        className="rounded-lg bg-red-50 hover:bg-red-150 px-2.5 py-1 text-xs font-bold text-red-650 transition"
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

      {/* Pop-up glassmorphic form modal */}
      {isPromotionModalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm overflow-y-auto animate-fadeIn"
          onClick={() => setIsPromotionModalOpen(false)}
        >
          <div 
            className="w-full max-w-lg rounded-3xl bg-white p-6 md:p-8 shadow-2xl border border-slate-100 animate-slideUp max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-5 flex items-center justify-between pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-50 text-indigo-650">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4.5 w-4.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10" />
                  </svg>
                </div>
                <h3 className="title-font text-lg font-black text-slate-800">
                  {editingPromotionId ? 'Chi tiết & Cập nhật' : 'Tạo khuyến mãi mới'}
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setIsPromotionModalOpen(false)}
                className="rounded-lg bg-slate-100 hover:bg-slate-200 p-1.5 text-slate-500 font-bold text-xs transition"
              >
                ✕ Đóng
              </button>
            </div>

            <form onSubmit={handlePromotionSubmit} className="space-y-4">
              <div>
                <Label className="text-xs font-bold text-slate-600">Mã khuyến mãi</Label>
                <Input
                  value={promotionFormData.code}
                  onChange={(e) => setPromotionFormData((prev) => ({ ...prev, code: e.target.value }))}
                  placeholder="VD: SUMMER2026"
                  disabled={!!editingPromotionId}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-bold text-slate-600">Mô tả chương trình</Label>
                <Input
                  value={promotionFormData.description}
                  onChange={(e) => setPromotionFormData((prev) => ({ ...prev, description: e.target.value }))}
                  placeholder="VD: Giảm giá mùa hè rực rỡ"
                  className="mt-1"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label className="text-xs font-bold text-slate-600">Loại giảm</Label>
                  <Select
                    value={promotionFormData.discountType}
                    onChange={(e) =>
                      setPromotionFormData((prev) => ({ ...prev, discountType: Number(e.target.value) }))
                    }
                    disabled={!!editingPromotionId}
                    className="mt-1"
                  >
                    <option value={0}>Giảm theo %</option>
                    <option value={1}>Giảm tiền mặt</option>
                  </Select>
                </div>
                <div>
                  <Label className="text-xs font-bold text-slate-600">Giá trị giảm</Label>
                  <Input
                    type="number"
                    value={promotionFormData.discountValue}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, discountValue: e.target.value }))}
                    placeholder="VD: 10 hoặc 100000"
                    className="mt-1"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label className="text-xs font-bold text-slate-600">Giảm tối đa</Label>
                  <Input
                    type="number"
                    value={promotionFormData.maxDiscountAmount}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, maxDiscountAmount: e.target.value }))}
                    placeholder="VD: 500000"
                    className="mt-1"
                  />
                </div>
                <div>
                  <Label className="text-xs font-bold text-slate-600">Hóa đơn tối thiểu</Label>
                  <Input
                    type="number"
                    value={promotionFormData.minimumAmount}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, minimumAmount: e.target.value }))}
                    placeholder="VD: 1000000"
                    disabled={!!editingPromotionId}
                    className="mt-1"
                  />
                </div>
              </div>
              <div>
                <Label className="text-xs font-bold text-slate-600">Giới hạn lượt sử dụng</Label>
                <Input
                  type="number"
                  value={promotionFormData.usageLimit}
                  onChange={(e) => setPromotionFormData((prev) => ({ ...prev, usageLimit: e.target.value }))}
                  placeholder="VD: 100"
                  className="mt-1"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label className="text-xs font-bold text-slate-600">Có hiệu lực từ</Label>
                  <Input
                    type="date"
                    value={promotionFormData.validFrom}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validFrom: e.target.value }))}
                    disabled={!!editingPromotionId}
                    className="mt-1 text-xs"
                  />
                </div>
                <div>
                  <Label className="text-xs font-bold text-slate-600">Hiệu lực đến</Label>
                  <Input
                    type="date"
                    value={promotionFormData.validTo}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validTo: e.target.value }))}
                    className="mt-1 text-xs"
                  />
                </div>
              </div>
              
              <label className="flex items-center gap-2.5 rounded-xl border border-slate-200 bg-white p-3 text-xs font-semibold text-slate-700 hover:bg-slate-50 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={promotionFormData.isActive}
                  onChange={(e) => setPromotionFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-2 focus:ring-indigo-100 cursor-pointer"
                />
                <span>Kích hoạt mã khuyến mãi</span>
              </label>

              <div className="flex gap-2 pt-3 border-t border-slate-100">
                <button
                  type="submit"
                  className="flex-1 rounded-xl bg-indigo-600 py-2.5 text-xs font-bold text-white hover:bg-indigo-700 transition shadow-md shadow-indigo-200"
                >
                  {editingPromotionId ? 'Lưu cập nhật' : 'Tạo khuyến mãi'}
                </button>
                <button
                  type="button"
                  onClick={() => setIsPromotionModalOpen(false)}
                  className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-bold text-slate-700 hover:bg-slate-100 transition"
                >
                  Hủy
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )

  const renderFlightManagement = () => (
    <div className="space-y-6">
      {/* List panel of flights & filtration */}
      <section className="rounded-3xl bg-white p-6 shadow-xl border border-slate-100 shadow-slate-100/50 md:p-8">
        <div className="mb-6 border-b border-slate-100 pb-5">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-blue-50 px-2.5 py-1 text-xs font-bold uppercase tracking-wider text-blue-600">
                <span className="h-1.5 w-1.5 rounded-full bg-blue-600 animate-pulse"></span>
                Điều hành bay
              </span>
              <h2 className="title-font mt-2 text-2xl font-black text-slate-800">Quản lý chuyến bay</h2>
              <p className="mt-1 text-sm text-slate-500 font-medium font-sans">Chi tiết hành trình bay, cất cánh và quản lý hạng ghế.</p>
            </div>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={loadAdminFlights}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-50 hover:text-slate-800 transition-all shadow-sm"
              >
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-3.5 w-3.5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
                </svg>
                Làm mới
              </button>
            </div>
          </div>

          {/* Quick Filters Toolbar */}
          <div className="mt-5 grid grid-cols-2 gap-2 sm:grid-cols-4 bg-slate-50 p-3 rounded-2xl border border-slate-100">
            <div className="relative">
              <input
                type="date"
                value={adminFlightFilters.date}
                onChange={(e) =>
                  setAdminFlightFilters((prev) => ({ ...prev, date: e.target.value }))
                }
                className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none"
              />
            </div>
            <div>
              <input
                type="text"
                value={adminFlightFilters.from}
                onChange={(e) =>
                  setAdminFlightFilters((prev) => ({ ...prev, from: e.target.value }))
                }
                placeholder="Đi từ..."
                className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none placeholder:text-slate-400"
              />
            </div>
            <div>
              <input
                type="text"
                value={adminFlightFilters.to}
                onChange={(e) =>
                  setAdminFlightFilters((prev) => ({ ...prev, to: e.target.value }))
                }
                placeholder="Đến..."
                className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-xs font-semibold text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none placeholder:text-slate-400"
              />
            </div>
            <div className="flex gap-1.5">
              <input
                type="text"
                value={adminFlightFilters.code}
                onChange={(e) =>
                  setAdminFlightFilters((prev) => ({ ...prev, code: e.target.value }))
                }
                placeholder="Mã chuyến..."
                className="w-full rounded-xl border border-slate-200 bg-white px-2.5 py-2 text-xs font-semibold text-slate-700 shadow-sm focus:border-blue-400 focus:outline-none placeholder:text-slate-400"
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
                className="rounded-xl border border-slate-200 bg-white p-2 hover:bg-slate-100 transition-colors shadow-sm shrink-0"
                title="Xóa bộ lọc"
              >
                ✕
              </button>
            </div>
          </div>
        </div>

        {flightAdminError && (
          <div className="mb-5 rounded-2xl border border-red-100 bg-red-50/50 p-4 text-xs font-medium text-red-700 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5">
              <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.753-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clipRule="evenodd" />
            </svg>
            <span>{flightAdminError}</span>
          </div>
        )}

        {flightAdminNotice && (
          <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-medium text-emerald-700 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5">
              <path fillRule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12Zm13.36-1.814a.75.75 0 1 0-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 0 0-1.06 1.06l2.25 2.25a.75.75 0 0 0 1.14-.094l3.74-5.24Z" clipRule="evenodd" />
            </svg>
            <span>{flightAdminNotice}</span>
          </div>
        )}

        {isLoadingFlightsAdmin && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-10 text-center text-xs font-semibold text-slate-400">
            <div className="mx-auto mb-3 h-7 w-7 animate-spin rounded-full border-2 border-slate-300 border-t-blue-600"></div>
            Đang tải danh sách chuyến bay...
          </div>
        )}

        {!isLoadingFlightsAdmin && adminFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-12 text-center text-xs font-bold text-slate-400">
            Chưa có chuyến bay nào được tạo.
          </div>
        )}

        {!isLoadingFlightsAdmin && adminFlights.length > 0 && filteredAdminFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-10 text-center text-xs font-bold text-slate-400">
            Không tìm thấy chuyến bay nào phù hợp với bộ lọc.
          </div>
        )}

        {!isLoadingFlightsAdmin && filteredAdminFlights.length > 0 && (
          <div className="space-y-4">
            {filteredAdminFlights.map((flight) => {
              const seatSummary = getSeatInventorySummary(flight.seatInventory)
              const totalSeatsRemaining = flight.availableSeats !== undefined 
                ? flight.availableSeats 
                : Object.values(flight.seatInventory || {}).reduce((sum, s) => sum + (s?.availableSeats ?? s?.available ?? 0), 0)

              return (
                <article
                  key={flight.flightId}
                  className="relative overflow-hidden rounded-2xl border border-slate-200/80 bg-white p-5 shadow-sm hover:shadow-md hover:border-slate-300 transition-all duration-fast"
                >
                  <div className={`absolute top-0 bottom-0 left-0 w-1.5 ${flight.isActive ? 'bg-gradient-to-b from-blue-450 to-blue-650' : 'bg-slate-300'}`} />

                  <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between pl-2">
                    <div className="space-y-1.5 flex-1">
                      <div className="flex items-center flex-wrap gap-2">
                        <span className="font-mono text-base font-extrabold tracking-tight text-blue-700 bg-blue-50 border border-blue-100 rounded-lg px-2.5 py-0.5">
                          ✈️ {flight.flightNumber || '---'}
                        </span>
                        <span
                          className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-bold ${
                            flight.isActive
                              ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                              : 'bg-slate-100 text-slate-600'
                          }`}
                        >
                          <span className={`h-1.5 w-1.5 rounded-full ${flight.isActive ? 'bg-emerald-600' : 'bg-slate-400'}`}></span>
                          {flight.isActive ? 'Hoạt động' : 'Tạm dừng'}
                        </span>

                        {/* HIGH-CONTRAST EMPTY SEATS BADGE */}
                        <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-extrabold border ${
                          totalSeatsRemaining === 0 
                            ? 'bg-red-50 text-red-700 border-red-100' 
                            : totalSeatsRemaining < 10 
                            ? 'bg-amber-50 text-amber-700 border-amber-100' 
                            : 'bg-emerald-50 text-emerald-700 border-emerald-100'
                        }`}>
                          💺 Số ghế trống: {totalSeatsRemaining}
                        </span>
                      </div>

                      <div className="flex items-center gap-2 text-sm font-bold text-slate-800">
                        <span>{flight.routeCode || '---'}</span>
                        <span className="h-1.5 w-1.5 rounded-full bg-slate-300"></span>
                        <span className="text-xs font-semibold text-slate-500">{flight.aircraftModel || 'Chưa rõ máy bay'}</span>
                      </div>

                      <div className="space-y-1 text-xs font-semibold text-slate-600 pt-1">
                        <div className="flex items-center gap-2 text-slate-500">
                          <span className="inline-flex h-4 w-4 items-center justify-center rounded bg-slate-100 text-[10px]">🛫</span>
                          Cất cánh: <span className="text-slate-700">{formatDateTime(flight.departureTime)}</span>
                        </div>
                        <div className="flex items-center gap-2 text-slate-500">
                          <span className="inline-flex h-4 w-4 items-center justify-center rounded bg-slate-100 text-[10px]">🛬</span>
                          Hạ cánh: <span className="text-slate-700">{formatDateTime(flight.arrivalTime)}</span>
                        </div>
                      </div>

                      {seatSummary.length > 0 && (
                        <div className="mt-3 flex flex-wrap gap-1.5 pt-2 border-t border-slate-100/60">
                          {seatSummary.map((seat, idx) => (
                            <span
                              key={`${flight.flightId}-${idx}`}
                              className="inline-flex items-center gap-1 rounded-lg bg-slate-50 border border-slate-100 px-2 py-1 text-[11px] font-bold text-slate-600"
                            >
                              <span className="h-1.5 w-1.5 rounded-full bg-indigo-500"></span>
                              {seat.label}: <span className="text-indigo-650">{Number.isFinite(Number(seat.price)) ? formatCurrency(seat.price) : '--'}</span>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>

                    <div className="flex flex-row items-center justify-between border-t border-slate-100 pt-3 md:flex-col md:items-end md:justify-start md:border-t-0 md:pt-0 shrink-0 gap-3">
                      <div className="flex flex-wrap gap-1.5 md:justify-end">
                        <button
                          type="button"
                          onClick={() => startEditFlight(flight)}
                          className="inline-flex items-center rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-bold text-slate-700 hover:bg-slate-50 transition-all shadow-sm"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => handleCancelAdminFlight(flight)}
                          disabled={isCancellingAdminFlightId === flight.flightId}
                          className="inline-flex items-center rounded-lg border border-amber-200 bg-amber-50/50 px-2.5 py-1.5 text-xs font-bold text-amber-700 hover:bg-amber-100 transition-all disabled:opacity-60"
                        >
                          {isCancellingAdminFlightId === flight.flightId ? 'Đang hủy...' : 'Hủy chuyến'}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDeleteFlight(flight)}
                          className="inline-flex items-center rounded-lg bg-red-50 px-2.5 py-1.5 text-xs font-bold text-red-650 hover:bg-red-100 transition-all"
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

      {/* Pop-up glassmorphic flight modal */}
      {isFlightModalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm overflow-y-auto animate-fadeIn"
          onClick={() => setIsFlightModalOpen(false)}
        >
          <div 
            className="w-full max-w-lg rounded-3xl bg-white p-6 md:p-8 shadow-2xl border border-slate-100 animate-slideUp max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-5 flex items-center justify-between pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-50 text-blue-650">
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4.5 w-4.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5" />
                  </svg>
                </div>
                <h3 className="title-font text-lg font-black text-slate-800">
                  {editingFlightId ? 'Cập nhật chuyến bay' : 'Tạo chuyến bay mới'}
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setIsFlightModalOpen(false)}
                className="rounded-lg bg-slate-100 hover:bg-slate-200 p-1.5 text-slate-500 font-bold text-xs transition"
              >
                ✕ Đóng
              </button>
            </div>

            <form onSubmit={handleFlightSubmit} className="space-y-4">
              <div>
                <Label className="text-xs font-bold text-slate-600">Số hiệu chuyến bay</Label>
                <Input
                  value={flightFormData.flightNumber}
                  onChange={(e) => setFlightFormData((prev) => ({ ...prev, flightNumber: e.target.value }))}
                  placeholder="VD: VN211"
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-bold text-slate-600">Tuyến bay</Label>
                <Select
                  value={flightFormData.routeId}
                  onChange={(e) => setFlightFormData((prev) => ({ ...prev, routeId: e.target.value }))}
                  disabled={!!editingFlightId}
                  className="mt-1"
                >
                  <option value="">Chọn tuyến bay khả dụng</option>
                  {adminRoutes.map((route) => (
                    <option key={route.routeId} value={route.routeId}>
                      {route.departureAirportCode || route.departureAirport || route.departureAirport} → {route.arrivalAirportCode || route.arrivalAirport || route.arrivalAirport}
                    </option>
                  ))}
                </Select>
              </div>

              <div>
                <Label className="text-xs font-bold text-slate-600">Chọn máy bay</Label>
                <Select
                  value={flightFormData.aircraftId}
                  onChange={(e) => setFlightFormData((prev) => ({ ...prev, aircraftId: e.target.value }))}
                  className="mt-1"
                >
                  <option value="">Chọn máy bay khả dụng</option>
                  {aircrafts.map((aircraft) => (
                    <option key={aircraft.aircraftId || aircraft.id} value={aircraft.aircraftId || aircraft.id}>
                      {aircraft.model} ({aircraft.registrationNumber})
                    </option>
                  ))}
                </Select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label className="text-xs font-bold text-slate-600">Thời gian cất cánh</Label>
                  <Input
                    type="datetime-local"
                    value={flightFormData.departureTime}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, departureTime: e.target.value }))}
                    className="mt-1 text-xs"
                  />
                </div>
                <div>
                  <Label className="text-xs font-bold text-slate-600">Thời gian hạ cánh</Label>
                  <Input
                    type="datetime-local"
                    value={flightFormData.arrivalTime}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, arrivalTime: e.target.value }))}
                    className="mt-1 text-xs"
                  />
                </div>
              </div>

              <label className="flex items-center gap-2.5 rounded-xl border border-slate-200 bg-white p-3 text-xs font-semibold text-slate-700 hover:bg-slate-50 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={flightFormData.isActive}
                  onChange={(e) => setFlightFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-2 focus:ring-blue-100 cursor-pointer"
                />
                <span>Kích hoạt chuyến bay này</span>
              </label>

              <div className="flex gap-2 pt-3 border-t border-slate-100">
                <button
                  type="submit"
                  className="flex-1 rounded-xl bg-blue-600 py-2.5 text-xs font-bold text-white hover:bg-blue-700 transition shadow-md shadow-blue-200"
                >
                  {editingFlightId ? 'Lưu cập nhật' : 'Tạo chuyến bay'}
                </button>
                <button
                  type="button"
                  onClick={() => setIsFlightModalOpen(false)}
                  className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-xs font-bold text-slate-700 hover:bg-slate-100 transition"
                >
                  Hủy
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )

  const renderTemplateManagement = () => (
    <div className="space-y-6">
      {/* Bright White Header Panel matching Flight & Promotion Dashboards */}
      <section className="rounded-3xl bg-white p-6 shadow-xl border border-slate-100 shadow-slate-100/50 md:p-8">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-650">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-5 w-5">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5m-9-6h.008v.008H12v-.008ZM12 15h.008v.008H12V15Zm0 2.25h.008v.008H12v-.008ZM9.75 15h.008v.008H9.75V15Zm0 2.25h.008v.008H9.75v-.008ZM7.5 15h.008v.008H7.5V15Zm0 2.25h.008v.008H7.5v-.008Zm6.75-4.5h.008v.008h-.008v-.008Zm0 2.25h.008v.008h-.008V15Zm0 2.25h.008v.008h-.008v-.008Zm2.25-4.5h.008v.008H16.5v-.008Zm0 2.25h.008v.008H16.5V15Z" />
              </svg>
            </div>
            <div>
              <h2 className="title-font mt-1 text-2xl font-black text-slate-800">Quản lý Flight Templates</h2>
              <p className="mt-1 text-xs text-slate-500 font-medium">Lập kế hoạch bay định kỳ theo thứ trong tuần và tự động hóa việc sinh lịch bay hàng tuần.</p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => {
              setEditingTemplateId(null)
              setTemplateFormData({ name: '', description: '', isActive: true })
              setTemplateSlots([])
              setSelectedTemplateDays({})
              setApiError('')
              setAdminNotice('')
              setIsTemplateModalOpen(true)
            }}
            className="inline-flex items-center gap-2 rounded-xl bg-blue-600 hover:bg-blue-700 px-5 py-2.5 text-xs font-bold text-white transition shadow-md shadow-blue-200 shrink-0"
          >
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="3" stroke="currentColor" className="h-4 w-4">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            Tạo template mới
          </button>
        </div>

        {adminNotice && (
          <div className="mt-4 rounded-2xl border border-indigo-500/30 bg-indigo-500/10 px-4 py-3 text-xs font-semibold text-indigo-200 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5 text-indigo-400">
              <path fillRule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12Zm11.378-3.917c-.89-.777-2.366-.777-3.255 0a.75.75 0 0 1-.988-1.129c1.454-1.272 3.776-1.272 5.23 0a.75.75 0 0 1-.987 1.13c.001-.001.001-.001 0 0Zm-2.639 3.167a.75.75 0 0 0-.219.53v2.25a.75.75 0 0 0 1.5 0v-2.25a.75.75 0 0 0-.22-.53l-.53-.53-.53.53Zm-.72-.72-.53-.53a.75.75 0 1 0-1.06 1.06l.53.53a.75.75 0 0 0 1.06-1.06Zm4.24 0a.75.75 0 0 0 1.06 0l.53-.53a.75.75 0 1 0-1.06-1.06l-.53.53a.75.75 0 0 0 0 1.06Z" clipRule="evenodd" />
            </svg>
            <span>{adminNotice}</span>
          </div>
        )}

        {apiError && (
          <div className="mt-4 rounded-2xl border border-red-500/30 bg-red-50/10 px-4 py-3 text-xs font-semibold text-red-200 flex items-start gap-2.5 animate-fadeIn">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5 text-red-400">
              <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.753-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clipRule="evenodd" />
            </svg>
            <span className="whitespace-pre-wrap">{apiError}</span>
          </div>
        )}
      </section>

      {/* Section: List of saved templates (Normal View) */}
      <section className="rounded-3xl bg-white p-6 shadow-xl border border-slate-100 shadow-slate-100/50">
        <div className="mb-6">
          <h3 className="text-lg font-black text-slate-800">📚 Danh Sách Flight Templates Đã Lưu</h3>
          <p className="text-xs text-slate-500 font-medium">Danh sách các khung giờ bay chuẩn được cấu hình trong hệ thống.</p>
        </div>

        {isLoadingTemplates && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs font-semibold text-slate-400">
            Đang tải dữ liệu template...
          </div>
        )}
        {!isLoadingTemplates && flightTemplates.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-8 text-center text-xs font-bold text-slate-400">
            Chưa có template nào được lưu.
          </div>
        )}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {flightTemplates.map((template) => (
            <div
              key={template.templateId || template.id || template.Id}
              className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-indigo-400 transition-all duration-fast flex flex-col justify-between"
            >
              <div>
                <div className="flex items-start justify-between gap-2">
                  <h4 className="font-extrabold text-sm text-slate-800">{template.name}</h4>
                  <span
                    className={`rounded-full px-2 py-0.5 text-[9px] font-bold shrink-0 ${
                      template.isActive
                        ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                        : 'bg-slate-100 text-slate-600'
                    }`}
                  >
                    {template.isActive ? 'Hoạt động' : 'Tạm dừng'}
                  </span>
                </div>
                <p className="mt-1.5 text-xs text-slate-500 font-medium leading-relaxed line-clamp-2">{template.description}</p>
                <div className="mt-3 flex items-center gap-2 text-xs font-bold text-indigo-700 bg-indigo-50/80 border border-indigo-100 rounded-lg p-2">
                  📅 Quy mô: {template.details?.length || 0} chuyến bay/tuần
                </div>
              </div>

              <div className="mt-4 pt-3 border-t border-slate-100 grid grid-cols-4 gap-1">
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      setApiError('')
                      const templateId = template.templateId || template.id || template.Id
                      
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
                  className="rounded-xl border border-slate-200 hover:border-indigo-300 hover:bg-indigo-50/30 py-2 text-[10px] font-bold text-slate-700 transition text-center"
                >
                  Chi tiết
                </button>

                <button
                  type="button"
                  onClick={() => loadTemplateForEditing(template)}
                  className="rounded-xl border border-blue-200 hover:border-blue-300 bg-blue-50/30 py-2 text-[10px] font-bold text-blue-700 transition text-center"
                >
                  Sửa
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
                  className="rounded-xl bg-indigo-600 hover:bg-indigo-750 py-2 text-[10px] font-bold text-white transition text-center shadow shadow-indigo-100"
                >
                  Sinh lịch
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
                  className="rounded-xl bg-red-50 hover:bg-red-150 py-2 text-[10px] font-bold text-red-650 transition text-center"
                >
                  Xóa
                </button>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Editor Modal Overlay (Tạo & Sửa template) */}
      {isTemplateModalOpen && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm overflow-y-auto animate-fadeIn"
          onClick={() => setIsTemplateModalOpen(false)}
        >
          <div 
            className="w-full max-w-7xl rounded-3xl bg-white p-6 md:p-8 shadow-2xl border border-slate-100 animate-slideUp max-h-[92vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-5 flex items-center justify-between pb-3 border-b border-slate-150">
              <div className="flex items-center gap-2">
                <span className="text-xl">📋</span>
                <h3 className="title-font text-lg font-black text-slate-800">
                  {editingTemplateId ? `Cập nhật Template: ${templateFormData.name}` : 'Thiết lập Template Mới'}
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setIsTemplateModalOpen(false)}
                className="rounded-lg bg-slate-100 hover:bg-slate-200 p-1.5 text-slate-500 font-bold text-xs transition"
              >
                ✕ Đóng lại
              </button>
            </div>

            {/* Error & Notice inside Modal */}
            {apiError && (
              <div className="mb-5 rounded-2xl border border-red-100 bg-red-50/50 p-4 text-xs font-semibold text-red-700 flex items-start gap-2.5 animate-fadeIn">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5 text-red-500">
                  <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.753-2.5-2.598-4.5L9.4 3.003ZM12 8.25a.75.75 0 0 1 .75.75v3.75a.75.75 0 0 1-1.5 0V9a.75.75 0 0 1 .75-.75Zm0 8.25a.75.75 0 1 0 0-1.5.75.75 0 0 0 0 1.5Z" clipRule="evenodd" />
                </svg>
                <span className="whitespace-pre-wrap">{apiError}</span>
              </div>
            )}

            {adminNotice && (
              <div className="mb-5 rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-semibold text-emerald-700 flex items-start gap-2.5 animate-fadeIn">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 shrink-0 mt-0.5 text-emerald-500">
                  <path fillRule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12Zm13.36-1.814a.75.75 0 1 0-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 0 0-1.06 1.06l2.25 2.25a.75.75 0 0 0 1.14-.094l3.74-5.24Z" clipRule="evenodd" />
                </svg>
                <span>{adminNotice}</span>
              </div>
            )}

            {/* Template Header Fields */}
            <div className="grid gap-4 md:grid-cols-2 bg-slate-50 p-4 rounded-2xl border border-slate-100">
              <div>
                <Label className="text-xs font-bold text-slate-600">Tên Template</Label>
                <Input
                  placeholder="VD: Lịch bay mùa hè 2026 - Tần suất cao"
                  value={templateFormData.name}
                  onChange={(e) =>
                    setTemplateFormData((prev) => ({ ...prev, name: e.target.value }))
                  }
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-bold text-slate-600">Mô tả lịch trình</Label>
                <Input
                  placeholder="VD: Áp dụng lịch bay định kỳ tăng tần suất trục vàng Hà Nội - Sài Gòn"
                  value={templateFormData.description}
                  onChange={(e) =>
                    setTemplateFormData((prev) => ({ ...prev, description: e.target.value }))
                  }
                  className="mt-1"
                />
              </div>
              <div className="md:col-span-2 flex items-center justify-between mt-2 pt-2 border-t border-slate-200/50">
                <label className="flex items-center gap-2.5 rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-50 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={templateFormData.isActive}
                    onChange={(e) =>
                      setTemplateFormData((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                    className="h-4 w-4 rounded border-slate-300 text-indigo-650 focus:ring-2 focus:ring-indigo-100 cursor-pointer"
                  />
                  <span>Kích hoạt template này để sinh chuyến bay</span>
                </label>
              </div>
            </div>

            {/* Layout split scheduling system */}
            <div className="grid gap-6 lg:grid-cols-[290px_1fr] mt-5">
              {/* Predefined flight definitions */}
              <aside className="rounded-2xl border border-slate-200 bg-slate-50 p-4 shrink-0 max-h-[60vh] flex flex-col">
                <h4 className="mb-3 text-xs font-bold text-slate-700 flex items-center gap-1.5 uppercase tracking-wider pb-2 border-b border-slate-200">
                  <span>📋 Định nghĩa sẵn có</span>
                </h4>
                
                <div className="space-y-3 overflow-y-auto pr-1 flex-1">
                  {isLoadingFlightDefinitions && (
                    <div className="text-xs text-center font-bold text-slate-400 py-6">Đang tải định nghĩa...</div>
                  )}
                  {!isLoadingFlightDefinitions && flightDefinitions.length === 0 && (
                    <div className="text-xs text-center text-slate-400 py-6 font-bold">Chưa có dữ liệu định nghĩa.</div>
                  )}
                  {flightDefinitions.map((def) => (
                    <div
                      key={def.id}
                      className="rounded-xl border border-slate-200 bg-white p-3.5 shadow-sm space-y-2 hover:border-blue-400 hover:shadow transition-all"
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-mono text-xs font-extrabold text-blue-700 bg-blue-50 border border-blue-100 rounded-lg px-2 py-0.5">
                          {def.flightNumber}
                        </span>
                      </div>
                      <div className="text-[11px] font-bold text-slate-700 leading-tight">
                        📍 {def.departureAirportCode} → {def.arrivalAirportCode}
                      </div>
                      <div className="text-[10px] font-semibold text-slate-500">
                        🕒 Giờ bay: {def.departureTime} - {def.arrivalTime}
                      </div>
                      
                      {/* Select aircraft */}
                      <div className="space-y-1">
                        <label className="text-[9px] font-bold text-slate-400 uppercase tracking-wider">Chọn máy bay</label>
                        <select
                          id={`aircraft-${def.id}`}
                          className="w-full rounded-lg border border-slate-200 bg-slate-50 px-2 py-1.5 text-[11px] font-medium text-slate-700 focus:outline-none focus:ring-1 focus:ring-blue-400"
                          defaultValue=""
                        >
                          <option value="">
                            {isLoadingAircrafts ? 'Đang tải...' : `Chọn máy bay... (${aircrafts.length})`}
                          </option>
                          {aircrafts.map((aircraft) => (
                            <option key={aircraft.id || aircraft.aircraftId} value={aircraft.id || aircraft.aircraftId}>
                              ✈️ {aircraft.registrationNumber || aircraft.model}
                            </option>
                          ))}
                        </select>
                      </div>
                      
                      {/* Select day of week */}
                      <div className="space-y-1">
                        <label className="text-[9px] font-bold text-slate-400 uppercase tracking-wider">Thêm vào thứ</label>
                        <div className="grid grid-cols-4 gap-1">
                          {[
                            { value: 0, label: 'T2' },
                            { value: 1, label: 'T3' },
                            { value: 2, label: 'T4' },
                            { value: 3, label: 'T5' },
                            { value: 4, label: 'T6' },
                            { value: 5, label: 'T7' },
                            { value: 6, label: 'CN' },
                          ].map((day) => {
                            const checked = (selectedTemplateDays[def.id] || []).includes(day.value)
                            return (
                              <label
                                key={`${def.id}-day-${day.value}`}
                                className="inline-flex items-center gap-1 rounded-md border border-slate-200 bg-white px-1.5 py-1 text-[10px] font-semibold text-slate-700"
                              >
                                <input
                                  type="checkbox"
                                  checked={checked}
                                  onChange={(e) => {
                                    const nextChecked = e.target.checked
                                    setSelectedTemplateDays((prev) => {
                                      const current = Array.isArray(prev[def.id]) ? prev[def.id] : []
                                      const next = nextChecked
                                        ? [...current, day.value]
                                        : current.filter((d) => d !== day.value)
                                      return { ...prev, [def.id]: next }
                                    })
                                  }}
                                  className="h-3 w-3 rounded border-slate-300 text-indigo-600"
                                />
                                <span>{day.label}</span>
                              </label>
                            )
                          })}
                        </div>
                        <button
                          type="button"
                          className="w-full rounded-lg border border-slate-200 bg-blue-50 px-2 py-1.5 text-[11px] font-bold text-blue-700 hover:bg-blue-100 transition"
                          onClick={() => {
                            const aircraftSelect = document.getElementById(`aircraft-${def.id}`)
                            const selectedAircraftId = aircraftSelect ? parseInt(aircraftSelect.value, 10) : null
                            const selectedDays = (selectedTemplateDays[def.id] || []).slice().sort((a, b) => a - b)

                            if (!selectedAircraftId) {
                              setApiError('Vui lòng chọn máy bay trước khi thêm vào lịch trình!')
                              setTimeout(() => setApiError(''), 3500)
                              return
                            }

                            if (selectedDays.length === 0) {
                              setApiError('Vui lòng chọn ít nhất 1 thứ để thêm vào lịch trình!')
                              setTimeout(() => setApiError(''), 3500)
                              return
                            }

                            const duplicateDays = selectedDays.filter((dayOfWeek) =>
                              templateSlots.some(
                                (slot) => slot.flightDefinition.id === def.id && slot.dayOfWeek === dayOfWeek,
                              ),
                            )

                            if (duplicateDays.length > 0) {
                              setApiError(`Flight ${def.flightNumber} đã được gán vào ${getWeekdayName(duplicateDays[0])}!`)
                              setTimeout(() => setApiError(''), 3500)
                              return
                            }

                            setTemplateSlots((prev) => [
                              ...prev,
                              ...selectedDays.map((dayOfWeek) => ({
                                id: `${def.id}-${dayOfWeek}-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
                                flightDefinition: { ...def, selectedAircraftId },
                                dayOfWeek,
                              })),
                            ])
                            setAdminNotice(`✅ Đã thêm thành công ${def.flightNumber} vào ${selectedDays.length} thứ đã chọn`)
                            setTimeout(() => setAdminNotice(''), 2000)
                            setSelectedTemplateDays((prev) => ({ ...prev, [def.id]: [] }))
                            if (aircraftSelect) aircraftSelect.value = ''
                          }}
                        >
                          Thêm
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </aside>

              {/* 7-column Calendar Board */}
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-2 overflow-x-auto min-h-[40vh]">
                {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                  const slotsForDay = templateSlots.filter((slot) => slot.dayOfWeek === dayIndex)
                  return (
                    <div
                      key={dayIndex}
                      className="rounded-2xl border border-slate-200 bg-slate-50 p-2.5 flex flex-col min-w-[125px] h-full"
                    >
                      <div className="mb-2 text-center pb-2 border-b border-slate-200">
                        <p className="text-xs font-bold text-slate-800">{getWeekdayName(dayIndex)}</p>
                        <p className="text-[10px] font-semibold text-slate-400">({slotsForDay.length} chuyến)</p>
                      </div>
                      <div className="space-y-2 flex-1 overflow-y-auto max-h-[45vh] pr-0.5">
                        {slotsForDay.map((slot) => (
                          <div
                            key={slot.id}
                            className="relative rounded-xl border border-blue-200 bg-blue-50/50 p-2 hover:border-blue-400 transition"
                          >
                            <p className="font-mono text-[11px] font-extrabold text-blue-700">
                              ✈️ {slot.flightDefinition.flightNumber}
                            </p>
                            <p className="text-[10px] font-bold text-slate-700 mt-0.5">
                              {slot.flightDefinition.departureAirportCode} ➔ {slot.flightDefinition.arrivalAirportCode}
                            </p>
                            <p className="text-[9px] font-medium text-slate-500">
                              🕒 {slot.flightDefinition.departureTime?.substring(0, 5)}
                            </p>
                            {slot.flightDefinition.selectedAircraftId && (
                              <p className="text-[8px] text-emerald-700 font-extrabold bg-emerald-50 border border-emerald-100 rounded px-1 mt-1 inline-block">
                                Aircraft: #{slot.flightDefinition.selectedAircraftId}
                              </p>
                            )}
                            <button
                              type="button"
                              onClick={() => {
                                setTemplateSlots((prev) => prev.filter((s) => s.id !== slot.id))
                                setAdminNotice('Đã loại bỏ chuyến bay khỏi template nháp')
                                setTimeout(() => setAdminNotice(''), 2000)
                              }}
                              className="mt-2 w-full rounded-lg bg-red-50 hover:bg-red-100 py-0.5 text-[9px] font-bold text-red-600 transition"
                            >
                              Xóa bỏ
                            </button>
                          </div>
                        ))}
                        {slotsForDay.length === 0 && (
                          <div className="rounded-xl border border-dashed border-slate-350 p-4 text-center text-[10px] font-medium text-slate-400/80">
                            Trống
                          </div>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Modal Bottom Actions */}
            <div className="mt-6 pt-4 border-t border-slate-100 flex items-center justify-between flex-wrap gap-4">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setIsTemplateModalOpen(false)}
                  className="rounded-xl border border-slate-200 hover:bg-slate-100 px-5 py-2.5 text-xs font-bold text-slate-600 transition"
                >
                  Hủy bỏ
                </button>

                {templateSlots.length > 0 && (
                  <button
                    type="button"
                    onClick={() => {
                      if (window.confirm('Bạn có chắc chắn muốn xóa toàn bộ các chuyến bay đã lên lịch nháp này không?')) {
                        setTemplateSlots([])
                        setAdminNotice('Đã dọn sạch toàn bộ lịch nháp')
                        setTimeout(() => setAdminNotice(''), 2000)
                      }
                    }}
                    className="rounded-xl bg-red-50 hover:bg-red-100 px-5 py-2.5 text-xs font-bold text-red-600 transition border border-red-200"
                  >
                    Clear lịch nháp
                  </button>
                )}
              </div>

              <button
                type="button"
                className="hidden"
              />

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
                    const details = templateSlots.map((slot) => {
                      let prefix = 'FL'
                      if (slot.flightDefinition.flightNumber) {
                        const match = slot.flightDefinition.flightNumber.match(/^([A-Z]+)/)
                        if (match) {
                          prefix = match[1]
                        }
                      }

                      const formatTimeValue = (time) => {
                        if (!time) return '08:00:00'
                        const timeStr = String(time).trim()
                        if (timeStr.includes('T')) {
                          const parts = timeStr.split('T')
                          if (parts[1]) return parts[1].substring(0, 8)
                        }
                        if (timeStr.length === 5) return `${timeStr}:00`
                        return timeStr.substring(0, 8)
                      }

                      const flightDefinitionId = Number(slot.flightDefinition.id)
                      return {
                        flightDefinitionId,
                        aircraftOverrideId: Number(
                          slot?.flightDefinition?.selectedAircraftId ?? slot?.flightDefinition?.defaultAircraftId ?? 0
                        ) || null,
                        dayOfWeek: parseInt(slot.dayOfWeek, 10),
                        departureTimeOverride: formatTimeValue(slot.flightDefinition.departureTime),
                        arrivalTimeOverride: formatTimeValue(slot.flightDefinition.arrivalTime),
                        arrivalOffsetDaysOverride: 0,
                        isActive: true,
                        flightNumberPrefix: prefix,
                        flightNumberSuffix: '',
                      }
                    })

                    const templateData = {
                      code: (templateFormData.name || 'TPL').trim().toUpperCase(),
                      name: templateFormData.name,
                      description: templateFormData.description,
                      isActive: templateFormData.isActive,
                      details,
                    }

                    if (details.some((d) => !Number.isInteger(d.flightDefinitionId) || d.flightDefinitionId <= 0)) {
                      setApiError('Khong the luu template: co detail chua map duoc FlightDefinitionId hop le.')
                      return
                    }

                    console.log('📤 Sending template payload to save:', templateData)

                    if (editingTemplateId) {
                      // Simulating updating by deleting then creating
                      setAdminNotice(`⏳ Đang cập nhật template "${templateFormData.name}"...`)
                      await deleteFlightTemplate(editingTemplateId)
                    }

                    await createFlightTemplate(templateData)
                    setAdminNotice(`✅ Đã lưu thành công template "${templateFormData.name}" với quy mô ${templateSlots.length} chuyến bay/tuần!`)
                    
                    setTemplateFormData({ name: '', description: '', isActive: true })
                    setTemplateSlots([])
                    setSelectedTemplateDays({})
                    setEditingTemplateId(null)
                    setIsTemplateModalOpen(false)

                    // Reload templates
                    const templates = await getFlightTemplates()
                    setFlightTemplates(Array.isArray(templates) ? templates : [])
                  } catch (error) {
                    setApiError(error.message || 'Lỗi khi lưu template')
                  }
                }}
                className="rounded-xl bg-indigo-600 hover:bg-indigo-700 px-6 py-2.5 text-xs font-bold text-white transition-all shadow-md shadow-indigo-200 flex items-center gap-2"
              >
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4 w-4">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v6m3-3H9m12 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                {editingTemplateId ? 'Lưu thay đổi cập nhật' : `Lưu Lịch Trình Template (${templateSlots.length} chuyến bay/tuần)`}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Generator panel (glassmorphic popup modal overlay) */}
      {selectedTemplate && (
        <div 
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm overflow-y-auto animate-fadeIn"
          onClick={() => setSelectedTemplate(null)}
        >
          <div 
            className="w-full max-w-xl rounded-3xl bg-white p-6 md:p-8 shadow-2xl border border-slate-100 animate-slideUp"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-4 flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="text-base font-black text-slate-800 flex items-center gap-2">
                <span className="flex h-6 w-6 items-center justify-center rounded-full bg-emerald-100 text-emerald-700 text-xs">🚀</span>
                <span>Khởi tạo chuyến bay tự động từ Template</span>
              </h3>
              <button
                type="button"
                onClick={() => setSelectedTemplate(null)}
                className="text-slate-400 hover:text-slate-600 text-sm font-bold"
              >
                ✕
              </button>
            </div>
            
            <p className="mb-4 text-xs font-bold text-indigo-900 bg-indigo-50 border border-indigo-100 rounded-xl p-3 leading-relaxed">
              Template kích hoạt: <strong className="underline">{selectedTemplate.name}</strong> ({selectedTemplate.details?.length || 0} chuyến bay/tuần).
              Hệ thống sẽ tự động đối chiếu lịch trình, tạo mới các chuyến bay thực tế theo thời gian tương ứng.
            </p>

            <div className="space-y-4">
              <div>
                <Label className="text-xs font-bold text-slate-600">Ngày bắt đầu tuần mới (Thứ 2) <span className="text-rose-500">*</span></Label>
                <Input
                  type="date"
                  value={generateFormData.weekStartDate}
                  onChange={(e) =>
                    setGenerateFormData((prev) => ({ ...prev, weekStartDate: e.target.value }))
                  }
                  className="mt-1 text-xs"
                />
                <p className="mt-1.5 text-[10px] font-bold text-amber-700 bg-amber-50 rounded px-2.5 py-1 border border-amber-100">⚠️ Yêu cầu chọn đúng ngày Thứ 2 làm mốc đầu tuần (Chỉ được chọn Thứ 2).</p>
              </div>
              
              <div>
                <Label className="text-xs font-bold text-slate-600">Số tuần muốn sinh lịch</Label>
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
                  className="mt-1"
                />
                <p className="mt-1 text-[10px] font-bold text-slate-500">
                  Quy mô: {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} chuyến bay mới sẽ được lập.
                </p>
              </div>

              {apiError && (
                <div className="rounded-xl bg-red-50 border border-red-100 p-3 text-xs text-red-700 font-semibold leading-relaxed">
                  {apiError}
                </div>
              )}

              <div className="flex gap-2 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      setApiError('')
                      setAdminNotice('')
                      
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

                      // STRICT MONDAY-ONLY VALIDATION
                      const dateParts = generateFormData.weekStartDate.split('-')
                      const startDObj = new Date(dateParts[0], dateParts[1] - 1, dateParts[2])
                      if (startDObj.getDay() !== 1) { // 1 = Monday
                        setApiError('⚠️ Ngày bắt đầu phải là Thứ 2 (Monday)! Vui lòng chọn lại.')
                        return
                      }

                      const weekStartDateTime = new Date(generateFormData.weekStartDate + 'T00:00:00Z').toISOString()
                      setAdminNotice('⏳ Đang sinh chuyến bay từ template...')
                      
                      const result = await generateFlightsFromTemplate({
                        templateId: templateId,
                        weekStartDate: weekStartDateTime,
                        numberOfWeeks: numberOfWeeks,
                      })
                      
                      console.log('📊 Result from API:', result)
                      
                      if (result.error || result.message?.includes('trùng') || result.message?.includes('đã tồn tại')) {
                        const errorMsg = result.error || result.message || 'Có lỗi xảy ra khi sinh chuyến bay'
                        setApiError(`❌ ${errorMsg}`)
                        setAdminNotice('')
                        return
                      }
                      
                      setAdminNotice(
                        `✅ Thành công! Đã sinh thành công ${result.totalFlightsGenerated || 0} chuyến bay thực tế! ` +
                        (result.totalFlightsSkipped > 0 ? `(Bỏ qua ${result.totalFlightsSkipped} chuyến trùng lặp)` : '')
                      )
                      setSelectedTemplate(null)
                    } catch (error) {
                      console.error('❌ Lỗi khi sinh chuyến bay:', error)
                      let errorMessage = 'Lỗi khi sinh chuyến bay'
                      
                      if (error.message) {
                        if (error.message.includes('trùng') || error.message.includes('đã tồn tại')) {
                          errorMessage = `❌ ${error.message}`
                        } else if (error.message.includes('ValidationException')) {
                          errorMessage = `⚠️ Dữ liệu không hợp lệ: ${error.message}`
                        } else {
                          errorMessage = `❌ ${error.message}`
                        }
                      }
                      
                      if (error.responseBody) {
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
                  className="flex-1 rounded-xl bg-emerald-600 hover:bg-emerald-700 py-3 text-xs font-bold text-white transition-all shadow-md shadow-emerald-200"
                >
                  Sinh Hàng Loạt {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} Chuyến Bay
                </button>
                <button
                  type="button"
                  onClick={() => setSelectedTemplate(null)}
                  className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-xs font-bold text-slate-700 hover:bg-slate-100 transition"
                >
                  Hủy bỏ
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Viewing template detail overlay modal */}
      {viewingTemplateDetail && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-fadeIn">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl border border-slate-100">
            <div className="mb-4 flex items-start justify-between gap-4 border-b border-slate-100 pb-4">
              <div>
                <h3 className="text-xl font-black text-slate-800 flex items-center gap-2">
                  <span>📋 Bản vẽ chi tiết: {viewingTemplateDetail.name}</span>
                </h3>
                <p className="mt-1 text-xs text-slate-500 font-medium">{viewingTemplateDetail.description}</p>
                <div className="mt-2.5 flex flex-wrap items-center gap-2">
                  <span
                    className={`rounded-full px-2.5 py-0.5 text-[10px] font-bold ${
                      viewingTemplateDetail.isActive
                        ? 'bg-emerald-50 text-emerald-700 border border-emerald-100'
                        : 'bg-slate-100 text-slate-600'
                    }`}
                  >
                    {viewingTemplateDetail.isActive ? '✓ Đang kích hoạt' : '⏸ Tạm dừng'}
                  </span>
                  <span className="rounded-full bg-indigo-50 border border-indigo-100 px-2.5 py-0.5 text-[10px] font-bold text-indigo-700">
                    ✈️ Tần suất: {viewingTemplateDetail.details?.length || 0} chuyến/tuần
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setViewingTemplateDetail(null)}
                className="rounded-xl bg-slate-100 hover:bg-slate-200 px-3 py-2 text-xs font-bold text-slate-600 transition"
              >
                ✕ Đóng lại
              </button>
            </div>

            {/* Weekly calendar layout */}
            <div className="mt-4">
              <h4 className="mb-3 text-xs font-bold text-slate-700 uppercase tracking-wider">Biểu đồ phân bổ tuần</h4>
              <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-7">
                {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                  const flightsForDay = (viewingTemplateDetail.details || []).filter(
                    (d) => d.dayOfWeek === dayIndex
                  )
                  return (
                    <div
                      key={dayIndex}
                      className="rounded-2xl border border-slate-200 bg-slate-50 p-3 flex flex-col"
                    >
                      <div className="mb-2 text-center pb-1.5 border-b border-slate-200">
                        <p className="text-xs font-extrabold text-slate-800">
                          {getWeekdayName(dayIndex)}
                        </p>
                        <p className="text-[10px] font-semibold text-slate-400">({flightsForDay.length} chuyến)</p>
                      </div>
                      <div className="space-y-2 flex-1">
                        {flightsForDay.map((detail, idx) => (
                          <div
                            key={idx}
                            className="rounded-xl border border-indigo-100 bg-indigo-50/50 p-2"
                          >
                            <p className="font-mono text-xs font-extrabold text-indigo-700">
                              ⚡ {detail.flightNumberPrefix || 'FL'}
                            </p>
                            <p className="text-[10px] font-bold text-slate-700 mt-1">
                              Tuyến: {detail.routeId}
                            </p>
                            <p className="text-[9px] font-bold text-slate-500">
                              Aircraft: #{detail.aircraftId}
                            </p>
                            <p className="text-[9px] font-medium text-slate-500 mt-0.5">
                              🕒 {detail.departureTime} → {detail.arrivalTime}
                            </p>
                          </div>
                        ))}
                        {flightsForDay.length === 0 && (
                          <div className="rounded-xl border border-dashed border-slate-200 p-3 text-center text-[10px] font-semibold text-slate-400">
                            Không có chuyến bay
                          </div>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Developer raw JSON preview */}
            <div className="mt-6 border-t border-slate-100 pt-4">
              <h4 className="mb-2 text-xs font-bold text-slate-700 uppercase tracking-wider">📊 Nhật ký cấu hình hệ thống (JSON)</h4>
              <pre className="max-h-48 overflow-auto rounded-xl bg-slate-900 p-4 text-[10px] font-mono text-emerald-400">
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

      {screen !== 'login' &&
        (screen === 'search' || screen === 'list' || screen === 'passenger' || screen === 'payment') && (
        <div className="max-w-4xl mx-auto mb-8 px-4 py-4 rounded-3xl bg-white border border-slate-100/80 shadow-xl shadow-slate-100/40">
          <div className="flex items-center justify-between">
            {[
              { key: 'search', label: 'Tìm kiếm' },
              { key: 'list', label: 'Danh sách' },
              { key: 'passenger', label: 'Hành khách' },
              { key: 'payment', label: 'Thanh toán' },
            ].map((step, idx, arr) => {
              const activeOrder = ['search', 'list', 'passenger', 'payment'].indexOf(screen)
              const stepOrder = ['search', 'list', 'passenger', 'payment'].indexOf(step.key)
              const isCompleted = stepOrder < activeOrder
              const isActive = stepOrder === activeOrder
              
              return (
                <div key={step.key} className="flex-1 flex items-center">
                  {/* Step Item */}
                  <div className="flex flex-col items-center flex-1 relative">
                    <div
                      className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-black transition-all duration-instant shadow-sm ${
                        isCompleted
                          ? 'bg-emerald-500 text-white shadow-emerald-100'
                          : isActive
                          ? 'bg-blue-600 text-white ring-4 ring-blue-100 shadow-blue-200'
                          : 'bg-slate-100 text-slate-400'
                      }`}
                    >
                      {isCompleted ? (
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="3" stroke="currentColor" className="h-4.5 w-4.5">
                          <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                        </svg>
                      ) : (
                        idx + 1
                      )}
                    </div>
                    <span
                      className={`mt-2 text-[11px] font-bold text-center tracking-tight transition-colors duration-instant ${
                        isActive
                          ? 'text-blue-700 font-extrabold'
                          : isCompleted
                          ? 'text-emerald-600'
                          : 'text-slate-400'
                      }`}
                    >
                      {step.label}
                    </span>
                  </div>
                  
                  {/* Connecting Line */}
                  {idx < arr.length - 1 && (
                    <div className="h-1 flex-1 bg-slate-100 rounded-full mx-2 overflow-hidden shrink-0 min-w-[20px] md:min-w-[40px]">
                      <div
                        className="h-full bg-gradient-to-r from-blue-500 to-emerald-500 transition-all duration-300"
                        style={{ width: isCompleted ? '100%' : '0%' }}
                      />
                    </div>
                  )}
                </div>
              )
            })}
          </div>
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





