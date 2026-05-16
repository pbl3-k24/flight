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
  getBestPromotion,
  getFlightDefinitions,
  getAircrafts,
  getFlightTemplates,
  getFlightScheduleTemplate,
  createFlightTemplate,
  deleteFlightTemplate,
  generateFlightsFromTemplate,
} from './api'

const airports = [
  { Id: 1, Code: 'SGN', Name: 'S├ón bay T├ón S╞ín Nhß║Ñt', City: 'Th├ánh phß╗æ Hß╗ô Ch├¡ Minh', Province: 'Hß╗ô Ch├¡ Minh', IsActive: true, IsDeleted: false },
  { Id: 2, Code: 'HAN', Name: 'S├ón bay Nß╗Öi B├ái', City: 'H├á Nß╗Öi', Province: 'H├á Nß╗Öi', IsActive: true, IsDeleted: false },
  { Id: 3, Code: 'DAD', Name: 'S├ón bay Quß╗æc tß║┐ ─É├á Nß║╡ng', City: '─É├á Nß║╡ng', Province: '─É├á Nß║╡ng', IsActive: true, IsDeleted: false },
  { Id: 4, Code: 'CTS', Name: 'S├ón bay Cß║ºn Th╞í', City: 'Cß║ºn Th╞í', Province: 'Cß║ºn Th╞í', IsActive: true, IsDeleted: false },
  { Id: 5, Code: 'VCA', Name: 'S├ón bay Bu├┤n M├¬ Thuß╗Öt', City: 'Bu├┤n M├¬ Thuß╗Öt', Province: '─Éß║»k Lß║»k', IsActive: true, IsDeleted: false },
  { Id: 6, Code: 'HUI', Name: 'S├ón bay Ph├║ B├ái', City: 'Huß║┐', Province: 'Thß╗½a Thi├¬n Huß║┐', IsActive: true, IsDeleted: false },
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

const formatRouteLabel = (item) => {
  const from = item?.fromAirport || '---'
  const to = item?.toAirport || '---'
  const fromCode = item?.fromCode ? ` (${item.fromCode})` : ''
  const toCode = item?.toCode ? ` (${item.toCode})` : ''
  return `${from}${fromCode} ΓåÆ ${to}${toCode}`
}

const formatFlightMeta = (item) => {
  const parts = [item?.airlineCode, item?.flightNumber, item?.seatClass].filter(Boolean)
  return parts.join(' ┬╖ ')
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

const getFlightLabel = (flight) => flight?.flightNumber || getFlightId(flight) || 'Chuyß║┐n bay'

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
  const [filters, setFilters] = useState({
    maxPrice: 3000000,
    timeSlot: 'all',
    seatClass: 'all',
  })
  const [selectedFlight, setSelectedFlight] = useState(null)
  const [passengerInfo, setPassengerInfo] = useState({
    fullName: '',
    dob: '',
    gender: 'Nam',
    document: '',
  })
  const [bookingReference] = useState(() => `FB${Date.now().toString().slice(-8)}`)
  const [apiFlights, setApiFlights] = useState([])
  const [isLoadingFlights, setIsLoadingFlights] = useState(false)
  const [apiError, setApiError] = useState('')
  const [bookingId, setBookingId] = useState(null)
  const [paymentData, setPaymentData] = useState(null)
  const [bestPromotion, setBestPromotion] = useState(null)
  const [isLoadingPromotion, setIsLoadingPromotion] = useState(false)
  const [bookingHistory, setBookingHistory] = useState([])
  const [historyNotice, setHistoryNotice] = useState('')
  const [historyError, setHistoryError] = useState('')
  const [isLoadingHistory, setIsLoadingHistory] = useState(false)
  const [isCancellingBookingId, setIsCancellingBookingId] = useState(null)
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
      console.log('≡ƒöä Kh├┤i phß╗Ñc token tß╗½ localStorage:', token ? token.substring(0, 20) + '...' : 'kh├┤ng c├│')
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
        console.log('Γ£à ─É├ú kh├┤i phß╗Ñc phi├¬n ─æ─âng nhß║¡p:', { email, role: normalizedRole })
      }
    } catch (e) {
      console.error('Γ¥î Lß╗ùi khi kh├┤i phß╗Ñc token:', e)
    }
  }, [])

  const logout = () => {
    try {
      clearAuthToken()
    } catch (e) {}
    setAuthUser(null)
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

    return {
      bookingId: booking?.bookingId ?? booking?.bookingCode ?? '---',
      transactionRef: booking?.bookingCode || '',
      status: booking?.status || '─É├ú ─æß║╖t',
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
      passengerCount: passengers.length || 1,
      totalPrice:
        booking?.finalAmount ?? booking?.totalAmount ?? outbound?.price ?? 0,
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
      setHistoryError(error.message || 'Kh├┤ng thß╗â tß║úi lß╗ïch sß╗¡ ─æß║╖t v├⌐')
    } finally {
      setIsLoadingHistory(false)
    }
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
    const normalized = String(status || '').trim().toLowerCase()
    const ascii = normalized.normalize('NFD').replace(/[\u0300-\u036f]/g, '')
    return ['huy', 'da huy', 'canceled', 'cancelled', 'cancel'].includes(ascii)
  }

  const cancelBookingFromHistory = async (item) => {
    if (!item) return

    const bookingIdValue = Number(item.bookingId)
    if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
      setHistoryError('Kh├┤ng thß╗â hß╗ºy v├⌐ n├áy v├¼ thiß║┐u m├ú booking hß╗úp lß╗ç.')
      return
    }

    if (isBookingCancelled(item.status)) {
      setHistoryNotice('V├⌐ n├áy ─æ├ú ─æ╞░ß╗úc hß╗ºy tr╞░ß╗¢c ─æ├│.')
      return
    }

    const confirmed = window.confirm('Bß║ín chß║»c chß║»n muß╗æn hß╗ºy v├⌐ n├áy?')
    if (!confirmed) return

    const reason = window.prompt('L├╜ do hß╗ºy v├⌐ (kh├┤ng bß║»t buß╗Öc):', '')
    if (reason === null) return

    setIsCancellingBookingId(item.bookingId)
    setHistoryError('')
    setHistoryNotice('')
    try {
      await cancelBooking(bookingIdValue, reason.trim())
      const next = bookingHistory.map((entry) =>
        entry.bookingId === item.bookingId
          ? { ...entry, status: '─É├ú hß╗ºy' }
          : entry
      )
      persistBookingHistory(next)
      setHistoryNotice('─É├ú hß╗ºy v├⌐ th├ánh c├┤ng.')
    } catch (error) {
      setHistoryError(error.message || 'Hß╗ºy v├⌐ thß║Ñt bß║íi. Vui l├▓ng thß╗¡ lß║íi.')
    } finally {
      setIsCancellingBookingId(null)
    }
  }

  const saveBookingToHistory = () => {
    if (!selectedFlight) {
      setHistoryNotice('Ch╞░a c├│ chuyß║┐n bay ─æß╗â l╞░u.')
      return
    }

    const entry = {
      bookingId: bookingId || bookingReference,
      transactionRef: paymentData?.transactionRef || bookingReference,
      status: paymentData?.status || '─É├ú thanh to├ín',
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
      passengerName: passengerInfo.fullName,
      passengerCount: Number(searchData.passengers || 1),
      totalPrice,
    }

    const exists = bookingHistory.some(
      (item) =>
        item.bookingId === entry.bookingId ||
        (entry.transactionRef && item.transactionRef === entry.transactionRef)
    )

    if (exists) {
      setHistoryNotice('V├⌐ n├áy ─æ├ú ─æ╞░ß╗úc l╞░u trong lß╗ïch sß╗¡.')
      return
    }

    const next = [entry, ...bookingHistory]
    persistBookingHistory(next)
    setHistoryNotice('─É├ú l╞░u v├⌐ v├áo lß╗ïch sß╗¡ ─æß║╖t chß╗ù.')
  }

  const filteredFlights = useMemo(() => {
    return apiFlights.filter((flight) => {
      const pricesByClass = flight.pricesByClass || {}
      const flightPrice = pricesByClass[searchData.seatClass] || 0
      const byPrice = flightPrice <= filters.maxPrice
      const byClass =
        filters.seatClass === 'all' ||
        flight.availableSeatsByClass?.[filters.seatClass] > 0

      let byTime = true
      const departHour = new Date(flight.departureTime).getHours()
      if (filters.timeSlot === 'morning') byTime = departHour < 12
      if (filters.timeSlot === 'afternoon') byTime = departHour >= 12 && departHour < 18
      if (filters.timeSlot === 'evening') byTime = departHour >= 18

      return byPrice && byClass && byTime
    })
  }, [filters, apiFlights, searchData.seatClass])

  const totalPrice = (selectedFlight?.price || selectedFlight?.pricesByClass?.[searchData.seatClass] || 0) * Number(searchData.passengers || 1)
  
  const discountAmount = bestPromotion?.calculatedDiscount || 0
  const finalPrice = totalPrice - discountAmount

  useEffect(() => {
    if (!selectedFlight || totalPrice === 0) {
      setBestPromotion(null)
      return
    }

    const fetchBestPromotion = async () => {
      setIsLoadingPromotion(true)
      try {
        const promotion = await getBestPromotion(totalPrice)
        setBestPromotion(promotion)
        if (promotion) {
          console.log('≡ƒÄü ├üp dß╗Ñng m├ú giß║úm gi├í:', promotion.promoCode, '- Giß║úm:', promotion.calculatedDiscount)
        }
      } catch (error) {
        console.error('Lß╗ùi khi t├¼m promotion:', error)
        setBestPromotion(null)
      } finally {
        setIsLoadingPromotion(false)
      }
    }

    fetchBestPromotion()
  }, [selectedFlight, totalPrice])

  const selectedFromAirport = airports.find((airport) => airport.Id === searchData.fromAirportId)
  const selectedToAirport = airports.find((airport) => airport.Id === searchData.toAirportId)

  const isAdmin = authUser?.role === 'admin'

  const getWeekdayName = (dayIndex) => {
    const names = ['Thß╗⌐ 2', 'Thß╗⌐ 3', 'Thß╗⌐ 4', 'Thß╗⌐ 5', 'Thß╗⌐ 6', 'Thß╗⌐ 7', 'Chß╗º nhß║¡t']
    return names[dayIndex] || 'Thß╗⌐'
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
        if (!cancelled) setApiError(error.message || 'Lß╗ùi khß╗ƒi tß║ío thanh to├ín')
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

  // Load flight definitions khi v├áo m├án h├¼nh templates
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
          setApiError(error.message || 'Kh├┤ng thß╗â tß║úi danh s├ích flight definitions')
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

  // Load aircrafts khi v├áo m├án h├¼nh templates
  useEffect(() => {
    let cancelled = false

    if (screen !== 'templates' || !isAdmin) return undefined

    const fetchAircrafts = async () => {
      console.log('≡ƒöä Loading aircrafts...')
      setIsLoadingAircrafts(true)
      try {
        const aircraftList = await getAircrafts()
        console.log('Γ£à Aircrafts loaded:', aircraftList)
        if (!cancelled) {
          setAircrafts(Array.isArray(aircraftList) ? aircraftList : [])
        }
      } catch (error) {
        if (!cancelled) {
          console.error('Γ¥î Kh├┤ng thß╗â tß║úi danh s├ích m├íy bay:', error)
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

  // Load templates khi v├áo m├án h├¼nh templates
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
          setApiError(error.message || 'Kh├┤ng thß╗â tß║úi danh s├ích templates')
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
          <h1 className="title-font mt-1 text-2xl font-bold md:text-3xl">Hß╗ç thß╗æng ─æß║╖t v├⌐ m├íy bay</h1>
          <p className="mt-1 text-sm text-blue-100">Demo luß╗ông ─æß║╖t v├⌐ c╞í bß║ún vß╗¢i dß╗» liß╗çu giß║ú lß║¡p</p>
        </div>
        <div>
          {authUser ? (
            <div className="flex items-center gap-3">
              <div className="text-sm text-blue-100">Xin ch├áo, {authUser.fullName || authUser.email}</div>
              <button
                type="button"
                onClick={logout}
                className="rounded-xl bg-red-500 px-3 py-2 text-sm font-semibold text-white hover:bg-red-600"
              >
                ─É─âng xuß║Ñt
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  )

  const renderLogin = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-6 shadow-lg shadow-slate-200">
      <h2 className="title-font mb-6 text-center text-2xl font-bold text-slate-900">─É─âng nhß║¡p</h2>
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
            placeholder="ΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇó"
            value={loginData.password}
            onChange={(e) => setLoginData((prev) => ({ ...prev, password: e.target.value }))}
          />
        </div>
      </div>
      <button
        type="button"
        onClick={async () => {
          if (!loginData.email || !loginData.password) {
            setApiError('Vui l├▓ng nhß║¡p email v├á mß║¡t khß║⌐u')
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
            setApiError(error.message || '─É─âng nhß║¡p thß║Ñt bß║íi')
          } finally {
            setIsLoggingIn(false)
          }
        }}
        disabled={isLoggingIn}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
      >
        {isLoggingIn ? '─Éang ─æ─âng nhß║¡p...' : '─É─âng nhß║¡p'}
      </button>
      {apiError && (
        <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">{apiError}</div>
      )}
      <p className="mt-4 text-center text-sm text-slate-500">
        Ch╞░a c├│ t├ái khoß║ún?{' '}
        <button
          type="button"
          onClick={() => {
            setApiError('')
            setScreen('register')
          }}
          className="font-semibold text-[#1E40AF] hover:underline"
        >
          ─É─âng k├╜
        </button>
      </p>
    </div>
  )

  const renderRegister = () => (
    <div className="mx-auto w-full max-w-md rounded-2xl bg-white p-6 shadow-lg shadow-slate-200">
      <h2 className="title-font mb-6 text-center text-2xl font-bold text-slate-900">Tß║ío t├ái khoß║ún</h2>
      <div className="space-y-4">
        <div>
          <Label>Hß╗ì v├á t├¬n</Label>
          <Input
            type="text"
            placeholder="Nguyß╗àn V─ân A"
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
          <Label>Sß╗æ ─æiß╗çn thoß║íi</Label>
          <Input
            type="tel"
            placeholder="0900000000"
            value={registerData.phone}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, phone: e.target.value }))}
          />
        </div>
        <div>
          <Label>Mß║¡t khß║⌐u</Label>
          <Input
            type="password"
            placeholder="ΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇó"
            value={registerData.password}
            onChange={(e) => setRegisterData((prev) => ({ ...prev, password: e.target.value }))}
          />
        </div>
        <div>
          <Label>X├íc nhß║¡n mß║¡t khß║⌐u</Label>
          <Input
            type="password"
            placeholder="ΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇóΓÇó"
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
            setRegisterError('Vui l├▓ng nhß║¡p ─æß║ºy ─æß╗º th├┤ng tin')
            return
          }

          if (registerData.password !== registerData.confirmPassword) {
            setRegisterError('Mß║¡t khß║⌐u x├íc nhß║¡n kh├┤ng khß╗¢p')
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
            setRegisterError(error.message || '─É─âng k├╜ thß║Ñt bß║íi')
          } finally {
            setIsRegistering(false)
          }
        }}
        disabled={isRegistering}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50"
      >
        {isRegistering ? '─Éang tß║ío t├ái khoß║ún...' : 'Tß║ío t├ái khoß║ún'}
      </button>
      {registerError && (
        <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">{registerError}</div>
      )}
      <p className="mt-4 text-center text-sm text-slate-500">
        ─É├ú c├│ t├ái khoß║ún?{' '}
        <button
          type="button"
          onClick={() => {
            setRegisterError('')
            setScreen('login')
          }}
          className="font-semibold text-[#1E40AF] hover:underline"
        >
          ─É─âng nhß║¡p
        </button>
      </p>
    </div>
  )

  const renderSearch = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <h2 className="title-font mb-6 text-xl font-bold text-slate-900 md:text-2xl">
        T├¼m kiß║┐m chuyß║┐n bay
      </h2>
      <div className="mb-5 inline-flex rounded-xl bg-slate-100 p-1">
        {[
          { key: 'oneway', label: 'Mß╗Öt chiß╗üu' },
          { key: 'roundtrip', label: 'Khß╗⌐ hß╗ôi' },
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
          <Label>─Éiß╗âm ─æi</Label>
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
          <Label>─Éiß╗âm ─æß║┐n</Label>
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
          <Label>Ng├áy ─æi</Label>
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
            <Label>Ng├áy vß╗ü</Label>
            <Input
              type="date"
              min={searchData.departDate || today}
              value={searchData.returnDate}
              onChange={(e) => setSearchData((prev) => ({ ...prev, returnDate: e.target.value }))}
            />
          </div>
        )}

        <div>
          <Label>Sß╗æ h├ánh kh├ích</Label>
          <Select
            value={searchData.passengers}
            onChange={(e) => setSearchData((prev) => ({ ...prev, passengers: e.target.value }))}
          >
            <option value="1">1 h├ánh kh├ích</option>
            <option value="2">2 h├ánh kh├ích</option>
            <option value="3">3 h├ánh kh├ích</option>
            <option value="4">4 h├ánh kh├ích</option>
          </Select>
        </div>
        <div>
          <Label>Hß║íng ghß║┐</Label>
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
        onClick={async () => {
          if (searchData.fromAirportId === searchData.toAirportId) {
            setApiError('─Éiß╗âm ─æi v├á ─æiß╗âm ─æß║┐n phß║úi kh├íc nhau')
            return
          }

          if (!searchData.departDate || searchData.departDate < today) {
            setApiError('Ng├áy ─æi phß║úi tß╗½ h├┤m nay trß╗ƒ ─æi')
            return
          }

          if (tripType === 'roundtrip' && !searchData.returnDate) {
            setApiError('Vui l├▓ng chß╗ìn ng├áy vß╗ü')
            return
          }

          if (tripType === 'roundtrip' && searchData.returnDate < searchData.departDate) {
            setApiError('Ng├áy vß╗ü phß║úi sau hoß║╖c bß║▒ng ng├áy ─æi')
            return
          }

          setIsLoadingFlights(true)
          setApiError('')
          try {
            const results = await searchFlights({
              departureAirportId: searchData.fromAirportId,
              arrivalAirportId: searchData.toAirportId,
              departureDate: searchData.departDate,
              returnDate: tripType === 'roundtrip' ? searchData.returnDate : null,
              passengerCount: parseInt(searchData.passengers, 10),
              seatPreference: seatClassMap[searchData.seatClass] || null,
            })

            setSelectedFlight(null)
            setBookingId(null)
            setPaymentData(null)
            setApiFlights(Array.isArray(results) ? results : [])
            setFilters((prev) => ({ ...prev, seatClass: searchData.seatClass }))
            setScreen('list')
          } catch (error) {
            setApiError(error.message || 'Lß╗ùi t├¼m kiß║┐m chuyß║┐n bay. Vui l├▓ng thß╗¡ lß║íi.')
          } finally {
            setIsLoadingFlights(false)
          }
        }}
        disabled={isLoadingFlights}
        className="mt-6 w-full rounded-xl bg-[#1E40AF] px-4 py-3 text-sm font-semibold text-white transition hover:bg-blue-800 disabled:opacity-50 md:w-auto"
      >
        {isLoadingFlights ? '─Éang t├¼m kiß║┐m...' : 'T├¼m kiß║┐m'}
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
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">Bß╗Ö lß╗ìc</h3>
        <div className="space-y-4">
          <div>
            <Label>Gi├í tß╗æi ─æa: {formatCurrency(filters.maxPrice)}</Label>
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
            <Label>Khung giß╗¥ bay</Label>
            <Select
              value={filters.timeSlot}
              onChange={(e) => setFilters((prev) => ({ ...prev, timeSlot: e.target.value }))}
            >
              <option value="all">Tß║Ñt cß║ú</option>
              <option value="morning">S├íng (00:00 - 11:59)</option>
              <option value="afternoon">Chiß╗üu (12:00 - 17:59)</option>
              <option value="evening">Tß╗æi (18:00 - 23:59)</option>
            </Select>
          </div>
          <div>
            <Label>Hß║íng ghß║┐</Label>
            <Select
              value={filters.seatClass}
              onChange={(e) => setFilters((prev) => ({ ...prev, seatClass: e.target.value }))}
            >
              <option value="all">Tß║Ñt cß║ú</option>
              <option value="Economy">Economy</option>
              <option value="Business">Business</option>
            </Select>
          </div>
        </div>
      </aside>

      <section className="space-y-4">
        {filteredFlights.map((flight) => {
          const flightPrice = flight.pricesByClass?.[searchData.seatClass] || 0

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
                    ID chuyß║┐n bay: {getFlightId(flight)} ┬╖ M├ú hiß╗ân thß╗ï: {getFlightLabel(flight)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-xl font-bold text-slate-900">{formatCurrency(flightPrice)}</p>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedFlight(flight)
                      setScreen('passenger')
                    }}
                    className="mt-2 rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
                  >
                    Chß╗ìn
                  </button>
                </div>
              </div>
            </article>
          )
        })}

        {!isLoadingFlights && filteredFlights.length === 0 && apiFlights.length > 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
            Kh├┤ng t├¼m thß║Ñy chuyß║┐n bay ph├╣ hß╗úp bß╗Ö lß╗ìc.
          </div>
        )}

        {!isLoadingFlights && apiFlights.length === 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
            Vui l├▓ng t├¼m kiß║┐m chuyß║┐n bay tr╞░ß╗¢c.
          </div>
        )}
      </section>
    </div>
  )

  const renderPassenger = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h2 className="title-font mb-6 text-xl font-bold text-slate-900">Th├┤ng tin h├ánh kh├ích</h2>
        <div className="grid gap-4 md:grid-cols-2">
          <div className="md:col-span-2">
            <Label>Hß╗ì t├¬n</Label>
            <Input
              placeholder="Nguyß╗àn V─ân A"
              value={passengerInfo.fullName}
              onChange={(e) =>
                setPassengerInfo((prev) => ({ ...prev, fullName: e.target.value }))
              }
            />
          </div>
          <div>
            <Label>Ng├áy sinh</Label>
            <Input
              type="date"
              value={passengerInfo.dob}
              onChange={(e) => setPassengerInfo((prev) => ({ ...prev, dob: e.target.value }))}
            />
          </div>
          <div>
            <Label>Giß╗¢i t├¡nh</Label>
            <Select
              value={passengerInfo.gender}
              onChange={(e) =>
                setPassengerInfo((prev) => ({ ...prev, gender: e.target.value }))
              }
            >
              <option>Nam</option>
              <option>Nß╗»</option>
              <option>Kh├íc</option>
            </Select>
          </div>
          <div className="md:col-span-2">
            <Label>CCCD / Passport</Label>
            <Input
              placeholder="012345678901"
              value={passengerInfo.document}
              onChange={(e) =>
                setPassengerInfo((prev) => ({ ...prev, document: e.target.value }))
              }
            />
          </div>
        </div>

        <button
          type="button"
          onClick={async () => {
            if (!passengerInfo.fullName || !passengerInfo.dob || !passengerInfo.document) {
              setApiError('Vui l├▓ng ─æiß╗ün ─æß║ºy ─æß╗º th├┤ng tin h├ánh kh├ích')
              return
            }

            if (!authUser?.email) {
              setApiError('Vui l├▓ng ─æ─âng nhß║¡p ─æß╗â d├╣ng email t├ái khoß║ún khi ─æß║╖t v├⌐')
              return
            }

            setIsLoadingFlights(true)
            setApiError('')
            try {
              const seatClassId = seatClassMap[searchData.seatClass] || 1
              const booking = await createBooking({
                outboundFlightId: getFlightId(selectedFlight),
                outboundFlightNumber: selectedFlight.flightNumber,
                outboundDepartureDate: selectedFlight.departureTime,
                returnFlightId: null,
                returnFlightNumber: null,
                returnDepartureDate: null,
                passengerCount: parseInt(searchData.passengers, 10),
                seatClassId,
                passengers: [
                  {
                    firstName: passengerInfo.fullName.split(' ')[0],
                    lastName: passengerInfo.fullName.split(' ').slice(1).join(' '),
                    email: authUser.email,
                    phone: '0900000000',
                    dateOfBirth: new Date(passengerInfo.dob).toISOString(),
                    nationality: 'VN',
                    passportNumber: passengerInfo.document,
                  },
                ],
                promotionId: bestPromotion?.promotionId || null,
                contactEmail: authUser.email,
              })
              setBookingId(booking.bookingId)
              setScreen('payment')
            } catch (error) {
              setApiError(error.message || 'Lß╗ùi tß║ío booking. Vui l├▓ng thß╗¡ lß║íi.')
            } finally {
              setIsLoadingFlights(false)
            }
          }}
          disabled={isLoadingFlights}
          className="mt-6 rounded-xl bg-[#1E40AF] px-5 py-3 text-sm font-semibold text-white hover:bg-blue-800 disabled:opacity-50"
        >
          {isLoadingFlights ? '─Éang xß╗¡ l├╜...' : 'Tiß║┐p tß╗Ñc'}
        </button>
        {apiError && (
          <div className="mt-4 rounded-xl bg-red-50 p-4 text-sm text-red-700">
            {apiError}
          </div>
        )}
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">T├│m tß║»t chuyß║┐n bay</h3>
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
              ID chuyß║┐n bay: {getFlightId(selectedFlight)} ┬╖ Sß╗æ hiß╗çu: {getFlightLabel(selectedFlight)}
            </p>
          </>
        )}
        
        <div className="mt-3 border-t border-slate-100 pt-3">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Gi├í v├⌐ ({searchData.passengers} ng╞░ß╗¥i):</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {isLoadingPromotion && (
            <div className="mt-2 text-xs text-slate-500">
              ≡ƒöì ─Éang t├¼m m├ú giß║úm gi├í tß╗æt nhß║Ñt...
            </div>
          )}
          
          {bestPromotion && (
            <div className="mt-2 rounded-lg bg-green-50 p-2">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-semibold text-green-700">≡ƒÄü {bestPromotion.promoCode}</p>
                  <p className="text-xs text-green-600">{bestPromotion.description}</p>
                </div>
                <p className="text-sm font-bold text-green-700">-{formatCurrency(discountAmount)}</p>
              </div>
            </div>
          )}
          
          <div className="mt-2 flex justify-between border-t border-slate-100 pt-2 text-base font-bold text-[#1E40AF]">
            <span>Tß╗òng cß╗Öng:</span>
            <span>{formatCurrency(finalPrice)}</span>
          </div>
        </div>
      </aside>
    </div>
  )

  const renderPayment = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h2 className="title-font mb-4 text-xl font-bold text-slate-900">Thanh to├ín</h2>
        <p className="mb-5 text-sm text-slate-500">Ph╞░╞íng thß╗⌐c thanh to├ín: VNPAY</p>
        {paymentData && (
          <div className="mb-4 rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">
            <p>Trß║íng th├íi: {paymentData.status}</p>
            <p>Sß╗æ tiß╗ün: {formatCurrency(paymentData.amount || totalPrice)}</p>
          </div>
        )}

        <div className="mb-5 rounded-2xl border border-dashed border-blue-300 bg-blue-50 p-6">
          <p className="text-sm text-slate-600">
            Mß╗ƒ trang thanh to├ín VNPAY trong tr├¼nh duyß╗çt ─æß╗â thß╗▒c hiß╗çn giao dß╗ïch.
          </p>
          <p className="mt-2 break-all text-xs text-slate-500">
            M├ú thanh to├ín: {paymentData?.transactionRef || bookingReference}
          </p>
          <div className="mt-4">
            {paymentData?.paymentLink ? (
              <a
                className="inline-flex items-center justify-center rounded-xl bg-[#1E40AF] px-5 py-3 text-sm font-semibold text-white hover:bg-blue-800"
                href={paymentData.paymentLink}
                target="_blank"
                rel="noreferrer"
              >
                Mß╗ƒ trang thanh to├ín
              </a>
            ) : (
              <p className="text-sm text-red-600">
                Ch╞░a c├│ ─æ╞░ß╗¥ng dß║½n thanh to├ín. Vui l├▓ng thß╗¡ lß║íi.
              </p>
            )}
          </div>
          <div className="mt-4 flex flex-wrap gap-3">
            <button
              type="button"
              onClick={saveBookingToHistory}
              className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              X├íc nhß║¡n ─æ├ú thanh to├ín
            </button>
            <button
              type="button"
              onClick={() => setScreen('history')}
              className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Xem v├⌐ ─æ├ú ─æß║╖t
            </button>
          </div>
          {historyNotice && (
            <div className="mt-3 rounded-xl bg-emerald-50 px-4 py-3 text-xs text-emerald-700">
              {historyNotice}
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-5 text-sm text-slate-700">
          <p className="font-semibold text-slate-900">Quy tr├¼nh thanh to├ín VNPAY (Sandbox)</p>
          <ol className="mt-3 list-decimal space-y-2 pl-5">
            <li>Chß╗ìn ph╞░╞íng thß╗⌐c: Thß║╗ nß╗Öi ─æß╗ïa v├á t├ái khoß║ún ng├ón h├áng (Local ATM Card).</li>
            <li>Chß╗ìn logo ng├ón h├áng NCB.</li>
            <li>
              Nhß║¡p thß║╗ test: Sß╗æ thß║╗ 9704198526191432198, T├¬n chß╗º thß║╗ NGUYEN VAN A,
              Ng├áy ph├ít h├ánh 07/15, OTP 123456.
            </li>
            <li>Bß║Ñm "Thanh to├ín".</li>
          </ol>
        </div>

        <button
          type="button"
          onClick={() => {
            setSelectedFlight(null)
            setPassengerInfo({ fullName: '', dob: '', gender: 'Nam', document: '' })
            setHistoryNotice('')
            setScreen('search')
          }}
          className="mt-4 rounded-xl border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
        >
          Huß╗╖
        </button>
      </section>

      <aside className="h-fit rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <h3 className="title-font mb-4 text-lg font-bold text-slate-900">T├│m tß║»t ─æ╞ín h├áng</h3>
        <div className="space-y-2 text-sm text-slate-600">
          <p>ID chuyß║┐n bay: {getFlightId(selectedFlight)}</p>
          <p>Sß╗æ hiß╗çu chuyß║┐n bay: {getFlightLabel(selectedFlight)}</p>
          <p>H├úng bay: {selectedFlight?.airlineCode}</p>
          <p>H├ánh kh├ích: {passengerInfo.fullName || 'Ch╞░a nhß║¡p'}</p>
          <p>Sß╗æ l╞░ß╗úng: {searchData.passengers}</p>
          {paymentData && (
            <p className="break-all text-xs text-slate-500">M├ú giao dß╗ïch: {paymentData.transactionRef}</p>
          )}
        </div>
        
        <div className="mt-4 border-t border-slate-100 pt-4 space-y-2">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Gi├í v├⌐:</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {bestPromotion && (
            <div className="flex justify-between text-sm text-green-600">
              <span>≡ƒÄü Giß║úm gi├í ({bestPromotion.promoCode}):</span>
              <span>-{formatCurrency(discountAmount)}</span>
            </div>
          )}
          
          <div className="flex justify-between border-t border-slate-100 pt-2 text-base font-bold text-[#1E40AF]">
            <span>Tß╗òng tiß╗ün:</span>
            <span>{formatCurrency(finalPrice)}</span>
          </div>
        </div>
      </aside>
    </div>
  )

  const renderHistory = () => (
    <div className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Lß╗ïch sß╗¡</p>
          <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">V├⌐ ─æ├ú ─æß║╖t</h2>
          <p className="mt-1 text-sm text-slate-500">Danh s├ích c├íc booking ─æ├ú x├íc nhß║¡n thanh to├ín.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={loadBookingHistory}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            L├ám mß╗¢i
          </button>
          <button
            type="button"
            onClick={() => setScreen('search')}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            ─Éß║╖t v├⌐ mß╗¢i
          </button>
        </div>
      </div>

      {isLoadingHistory && (
        <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
          ─Éang tß║úi lß╗ïch sß╗¡ ─æß║╖t v├⌐...
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
          Ch╞░a c├│ v├⌐ n├áo ─æ╞░ß╗úc l╞░u. H├úy ho├án tß║Ñt thanh to├ín v├á l╞░u v├⌐.
        </div>
      )}

      {!isLoadingHistory && bookingHistory.length > 0 && (
        <div className="space-y-4">
          {bookingHistory.map((item) => (
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
                  {formatFlightMeta(item) && (
                    <p className="text-sm text-slate-500">{formatFlightMeta(item)}</p>
                  )}
                  <p className="mt-1 text-xs text-slate-500">
                    M├ú booking: {item.bookingId}
                    {item.transactionRef ? ` ┬╖ M├ú giao dß╗ïch: ${item.transactionRef}` : ''}
                  </p>
                </div>
                <div className="text-right">
                  <p
                    className={`text-sm font-semibold ${
                      isBookingCancelled(item.status) 
                        ? 'text-red-600' 
                        : item.status?.toLowerCase().includes('pending') || item.status?.toLowerCase().includes('chß╗¥')
                        ? 'text-yellow-600'
                        : 'text-emerald-600'
                    }`}
                  >
                    {item.status}
                  </p>
                  <p className="text-sm text-slate-500">{formatDateTime(item.createdAt)}</p>
                  <p className="mt-2 text-lg font-bold text-slate-900">
                    {formatCurrency(item.totalPrice)}
                  </p>
                  <p className="text-xs text-slate-500">
                    H├ánh kh├ích: {item.passengerName || '---'} ┬╖ {item.passengerCount} v├⌐
                  </p>
                  <div className="mt-3 flex flex-wrap gap-2 justify-end">
                    {/* N├║t thanh to├ín cho booking ch╞░a thanh to├ín */}
                    {!isBookingCancelled(item.status) && 
                     (item.status?.toLowerCase().includes('pending') || 
                      item.status?.toLowerCase().includes('chß╗¥') ||
                      item.status?.toLowerCase().includes('unpaid')) && (
                      <button
                        type="button"
                        onClick={async () => {
                          try {
                            setHistoryError('')
                            setHistoryNotice('')
                            const bookingIdValue = Number(item.bookingId)
                            if (!Number.isFinite(bookingIdValue) || bookingIdValue <= 0) {
                              setHistoryError('M├ú booking kh├┤ng hß╗úp lß╗ç')
                              return
                            }
                            
                            setHistoryNotice('─Éang chuyß╗ân ─æß║┐n trang thanh to├ín...')
                            const paymentResponse = await initiatePayment(bookingIdValue, 'VNPAY')
                            
                            if (paymentResponse?.paymentUrl) {
                              // Chuyß╗ân h╞░ß╗¢ng trß╗▒c tiß║┐p sang VNPay
                              window.location.href = paymentResponse.paymentUrl
                            } else {
                              setHistoryError('Kh├┤ng nhß║¡n ─æ╞░ß╗úc link thanh to├ín tß╗½ server')
                            }
                          } catch (error) {
                            setHistoryError(error.message || 'Lß╗ùi khi khß╗ƒi tß║ío thanh to├ín')
                          }
                        }}
                        className="rounded-xl bg-green-600 px-4 py-2 text-xs font-semibold text-white hover:bg-green-700"
                      >
                        ≡ƒÆ│ Thanh to├ín ngay
                      </button>
                    )}
                    {/* N├║t hß╗ºy v├⌐ */}
                    {!isBookingCancelled(item.status) && (
                      <button
                        type="button"
                        onClick={() => cancelBookingFromHistory(item)}
                        disabled={isCancellingBookingId === item.bookingId}
                        className="rounded-xl bg-red-500 px-4 py-2 text-xs font-semibold text-white hover:bg-red-600 disabled:opacity-60"
                      >
                        {isCancellingBookingId === item.bookingId ? '─Éang hß╗ºy...' : 'Hß╗ºy v├⌐'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  )

  const renderTemplateManagement = () => (
    <div className="space-y-5">
      {/* Header */}
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="mb-6">
          <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Admin</p>
          <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Quß║ún l├╜ Flight Templates</h2>
          <p className="mt-1 text-sm text-slate-500">
            Tß║ío template chuyß║┐n bay theo thß╗⌐ trong tuß║ºn v├á tß╗▒ ─æß╗Öng sinh chuyß║┐n bay tß╗½ template
          </p>
        </div>

        {adminNotice && (
          <div className="mb-4 rounded-xl bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-[#1E40AF]">
            <div className="flex items-start gap-2">
              <span className="text-lg">Γä╣∩╕Å</span>
              <div className="flex-1">{adminNotice}</div>
            </div>
          </div>
        )}

        {apiError && (
          <div className="mb-4 rounded-xl bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
            <div className="flex items-start gap-2">
              <span className="text-lg">ΓÜá∩╕Å</span>
              <div className="flex-1 whitespace-pre-wrap">{apiError}</div>
            </div>
          </div>
        )}

        {/* Form tß║ío template */}
        <div className="mb-6 rounded-xl border border-slate-200 bg-slate-50 p-5">
          <h3 className="mb-4 text-lg font-bold text-slate-900">≡ƒôï Tß║ío Template Mß╗¢i</h3>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <Label>T├¬n Template</Label>
              <Input
                placeholder="VD: Template Tuß║ºn Th╞░ß╗¥ng"
                value={templateFormData.name}
                onChange={(e) =>
                  setTemplateFormData((prev) => ({ ...prev, name: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>M├┤ tß║ú</Label>
              <Input
                placeholder="VD: Lß╗ïch bay cho c├íc ng├áy th╞░ß╗¥ng"
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
              <span className="text-sm text-slate-700">K├¡ch hoß║ít template</span>
            </label>
          </div>

          <button
            type="button"
            onClick={async () => {
              if (!templateFormData.name) {
                setApiError('Vui l├▓ng nhß║¡p t├¬n template')
                return
              }

              if (templateSlots.length === 0) {
                setApiError('Vui l├▓ng th├¬m ├¡t nhß║Ñt 1 chuyß║┐n bay v├áo template')
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

                    // ─Éß║úm bß║úo time format l├á HH:mm:ss
                    const formatTime = (time) => {
                      if (!time) return '08:00:00'
                      // Nß║┐u c├│ format HH:mm:ss.sssssss, chß╗ë lß║Ñy HH:mm:ss
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

                console.log('≡ƒôñ Creating template:', templateData)

                await createFlightTemplate(templateData)
                setAdminNotice(`Γ£à ─É├ú tß║ío template "${templateFormData.name}" vß╗¢i ${templateSlots.length} chuyß║┐n bay/tuß║ºn!`)
                setTemplateFormData({ name: '', description: '', isActive: true })
                setTemplateSlots([])

                // Reload templates
                const templates = await getFlightTemplates()
                setFlightTemplates(Array.isArray(templates) ? templates : [])
              } catch (error) {
                setApiError(error.message || 'Lß╗ùi khi tß║ío template')
              }
            }}
            className="mt-4 rounded-xl bg-[#1E40AF] px-6 py-3 text-sm font-semibold text-white hover:bg-blue-800"
          >
            ≡ƒÆ╛ L╞░u Template ({templateSlots.length} chuyß║┐n bay/tuß║ºn)
          </button>
        </div>

        {/* Danh s├ích templates */}
        <div className="mb-6">
          <h3 className="mb-4 text-lg font-bold text-slate-900">≡ƒôÜ Danh S├ích Templates</h3>
          {isLoadingTemplates && (
            <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
              ─Éang tß║úi danh s├ích templates...
            </div>
          )}
          {!isLoadingTemplates && flightTemplates.length === 0 && (
            <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
              Ch╞░a c├│ template n├áo. H├úy tß║ío template ─æß║ºu ti├¬n!
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
                        ≡ƒôà {template.details?.length || 0} chuyß║┐n/tuß║ºn
                      </span>
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                          template.isActive
                            ? 'bg-green-100 text-green-700'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {template.isActive ? 'Γ£ô Hoß║ít ─æß╗Öng' : 'ΓÅ╕ Tß║ím dß╗½ng'}
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
                        // Thß╗¡ nhiß╗üu field c├│ thß╗â c├│
                        const templateId = template.templateId || template.id || template.Id
                        console.log('≡ƒöì Template object:', template)
                        console.log('≡ƒöì Template ID:', templateId)
                        
                        if (!templateId) {
                          setApiError('Kh├┤ng t├¼m thß║Ñy ID cß╗ºa template')
                          return
                        }
                        
                        const templateDetail = await getFlightScheduleTemplate(templateId)
                        console.log('≡ƒôï Template Detail:', templateDetail)
                        setViewingTemplateDetail(templateDetail)
                      } catch (error) {
                        setApiError(error.message || 'Lß╗ùi khi tß║úi chi tiß║┐t template')
                      }
                    }}
                    className="flex-1 rounded-lg bg-slate-600 px-3 py-2 text-sm font-semibold text-white hover:bg-slate-700"
                  >
                    ≡ƒæü∩╕Å Xem chi tiß║┐t
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
                    ≡ƒÜÇ Sinh chuyß║┐n bay
                  </button>
                  <button
                    type="button"
                    onClick={async () => {
                      if (window.confirm(`X├│a template "${template.name}"?`)) {
                        try {
                          const templateId = template.templateId || template.id || template.Id
                          await deleteFlightTemplate(templateId)
                          setAdminNotice(`Γ£à ─É├ú x├│a template "${template.name}"`)
                          const templates = await getFlightTemplates()
                          setFlightTemplates(Array.isArray(templates) ? templates : [])
                        } catch (error) {
                          setApiError(error.message || 'Lß╗ùi khi x├│a template')
                        }
                      }
                    }}
                    className="rounded-lg bg-red-500 px-3 py-2 text-sm font-semibold text-white hover:bg-red-600"
                  >
                    ≡ƒùæ∩╕Å
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Form sinh chuyß║┐n bay tß╗½ template */}
        {selectedTemplate && (
          <div className="rounded-xl border-2 border-green-500 bg-green-50 p-5">
            <h3 className="mb-2 text-lg font-bold text-slate-900">
              ≡ƒÜÇ Sinh Chuyß║┐n Bay tß╗½ Template
            </h3>
            <p className="mb-4 text-sm text-green-700">
              Template: <strong>{selectedTemplate.name}</strong> ({selectedTemplate.details?.length || 0} chuyß║┐n/tuß║ºn)
            </p>

            <div className="rounded-lg bg-white p-4 mb-4">
              <p className="text-sm text-slate-700 mb-2">
                <strong>Template n├áy sß║╜ tß║ío chuyß║┐n bay theo thß╗⌐:</strong>
              </p>
              <div className="flex flex-wrap gap-2">
                {[0, 1, 2, 3, 4, 5, 6].map((dayIndex) => {
                  const count = selectedTemplate.details?.filter(d => d.dayOfWeek === dayIndex).length || 0
                  return count > 0 ? (
                    <span key={dayIndex} className="rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-700">
                      {getWeekdayName(dayIndex)}: {count} chuyß║┐n
                    </span>
                  ) : null
                })}
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <div>
                <Label>Ng├áy bß║»t ─æß║ºu tuß║ºn (Thß╗⌐ 2)</Label>
                <Input
                  type="date"
                  value={generateFormData.weekStartDate}
                  onChange={(e) =>
                    setGenerateFormData((prev) => ({ ...prev, weekStartDate: e.target.value }))
                  }
                />
                <p className="mt-1 text-xs text-slate-500">Chß╗ìn ng├áy Thß╗⌐ 2 ─æß╗â bß║»t ─æß║ºu</p>
              </div>
              <div>
                <Label>Sß╗æ tuß║ºn muß╗æn sinh</Label>
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
                  Tß╗òng: {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} chuyß║┐n bay
                </p>
              </div>
              <div className="flex items-end">
                <button
                  type="button"
                  onClick={async () => {
                    try {
                      setApiError('')
                      setAdminNotice('')
                      
                      // Validate dß╗» liß╗çu tr╞░ß╗¢c khi gß╗¡i
                      const templateId = Number(generateFormData.templateId)
                      const numberOfWeeks = Number(generateFormData.numberOfWeeks)
                      
                      if (!templateId || templateId <= 0) {
                        setApiError('Template ID kh├┤ng hß╗úp lß╗ç')
                        return
                      }
                      
                      if (!numberOfWeeks || numberOfWeeks <= 0) {
                        setApiError('Sß╗æ tuß║ºn phß║úi lß╗¢n h╞ín 0')
                        return
                      }
                      
                      if (!generateFormData.weekStartDate) {
                        setApiError('Vui l├▓ng chß╗ìn ng├áy bß║»t ─æß║ºu')
                        return
                      }

                      // Chuyß╗ân date sang ISO datetime vß╗¢i timezone UTC
                      const weekStartDateTime = new Date(generateFormData.weekStartDate + 'T00:00:00Z').toISOString()

                      setAdminNotice('ΓÅ│ ─Éang sinh chuyß║┐n bay tß╗½ template...')
                      
                      const result = await generateFlightsFromTemplate({
                        templateId: templateId,
                        weekStartDate: weekStartDateTime, // ISO datetime vß╗¢i timezone
                        numberOfWeeks: numberOfWeeks,
                      })
                      
                      console.log('≡ƒôè Result from API:', result)
                      
                      // Kiß╗âm tra nß║┐u c├│ lß╗ùi trong response (backend trß║ú 200 nh╞░ng c├│ error)
                      if (result.error || result.message?.includes('tr├╣ng') || result.message?.includes('─æ├ú tß╗ôn tß║íi')) {
                        const errorMsg = result.error || result.message || 'C├│ lß╗ùi xß║úy ra khi sinh chuyß║┐n bay'
                        setApiError(`Γ¥î ${errorMsg}`)
                        setAdminNotice('')
                        return
                      }
                      
                      // Th├ánh c├┤ng
                      setAdminNotice(
                        `Γ£à Th├ánh c├┤ng! ─É├ú sinh ${result.totalFlightsGenerated || 0} chuyß║┐n bay! ` +
                        (result.totalFlightsSkipped > 0 ? `(Bß╗Å qua ${result.totalFlightsSkipped} chuyß║┐n tr├╣ng)` : '')
                      )
                      setSelectedTemplate(null)
                    } catch (error) {
                      console.error('Γ¥î Lß╗ùi khi sinh chuyß║┐n bay:', error)
                      
                      // Xß╗¡ l├╜ c├íc loß║íi lß╗ùi kh├íc nhau
                      let errorMessage = 'Lß╗ùi khi sinh chuyß║┐n bay'
                      
                      if (error.message) {
                        // Kiß╗âm tra lß╗ùi tr├╣ng chuyß║┐n bay
                        if (error.message.includes('tr├╣ng') || error.message.includes('─æ├ú tß╗ôn tß║íi')) {
                          errorMessage = `Γ¥î ${error.message}`
                        } 
                        // Kiß╗âm tra lß╗ùi validation
                        else if (error.message.includes('ValidationException') || error.message.includes('validation')) {
                          errorMessage = `ΓÜá∩╕Å Lß╗ùi dß╗» liß╗çu: ${error.message}`
                        }
                        // Lß╗ùi kh├íc
                        else {
                          errorMessage = `Γ¥î ${error.message}`
                        }
                      }
                      
                      // Hiß╗ân thß╗ï chi tiß║┐t lß╗ùi tß╗½ response body nß║┐u c├│
                      if (error.responseBody) {
                        console.log('≡ƒôï Chi tiß║┐t lß╗ùi:', error.responseBody)
                        if (error.responseBody.detail) {
                          errorMessage = `Γ¥î ${error.responseBody.detail}`
                        } else if (error.responseBody.title) {
                          errorMessage = `Γ¥î ${error.responseBody.title}`
                        }
                      }
                      
                      setApiError(errorMessage)
                      setAdminNotice('')
                    }
                  }}
                  className="w-full rounded-xl bg-green-600 px-4 py-3 text-sm font-semibold text-white hover:bg-green-700"
                >
                  ≡ƒÜÇ Sinh {(selectedTemplate.details?.length || 0) * generateFormData.numberOfWeeks} Chuyß║┐n Bay
                </button>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setSelectedTemplate(null)}
              className="mt-3 text-sm text-slate-600 hover:text-slate-900 underline"
            >
              ΓåÉ Hß╗ºy v├á chß╗ìn template kh├íc
            </button>
          </div>
        )}
      </section>

      {/* Khung template theo thß╗⌐ */}
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200">
        <div className="mb-4">
          <h3 className="text-lg font-bold text-slate-900">≡ƒôà Khung Template Theo Thß╗⌐</h3>
          <p className="text-sm text-slate-500">
            Chß╗ìn flight definition v├á th├¬m v├áo thß╗⌐ t╞░╞íng ß╗⌐ng (Thß╗⌐ 2 - Chß╗º nhß║¡t)
          </p>
        </div>

        <div className="grid gap-5 lg:grid-cols-[300px_1fr]">
          {/* Danh s├ích flight definitions */}
          <aside className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <h4 className="mb-3 text-sm font-bold text-slate-900">Flight Definitions</h4>
            <div className="max-h-[600px] space-y-2 overflow-y-auto">
              {isLoadingFlightDefinitions && (
                <div className="text-xs text-slate-500">─Éang tß║úi...</div>
              )}
              {!isLoadingFlightDefinitions && flightDefinitions.length === 0 && (
                <div className="text-xs text-slate-500">Ch╞░a c├│ flight definitions</div>
              )}
              {flightDefinitions.map((def) => (
                <div
                  key={def.id}
                  className="rounded-lg border border-slate-200 bg-white p-3 text-xs"
                >
                  <p className="font-semibold text-[#1E40AF]">{def.flightNumber}</p>
                  <p className="mt-1 text-slate-700">
                    {def.departureAirportCode} ΓåÆ {def.arrivalAirportCode}
                  </p>
                  <p className="text-slate-500">
                    {def.departureTime} - {def.arrivalTime}
                  </p>
                  
                  {/* Chß╗ìn m├íy bay */}
                  <select
                    id={`aircraft-${def.id}`}
                    className="mt-2 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                    defaultValue=""
                  >
                    <option value="">
                      {isLoadingAircrafts ? '─Éang tß║úi...' : `Chß╗ìn m├íy bay... (${aircrafts.length})`}
                    </option>
                    {aircrafts.map((aircraft) => (
                      <option key={aircraft.id || aircraft.aircraftId} value={aircraft.id || aircraft.aircraftId}>
                        {aircraft.registrationNumber || aircraft.model || `Aircraft ${aircraft.id || aircraft.aircraftId}`}
                      </option>
                    ))}
                  </select>
                  
                  {/* Chß╗ìn thß╗⌐ */}
                  <select
                    className="mt-2 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                    onChange={(e) => {
                      const dayOfWeek = parseInt(e.target.value, 10)
                      if (dayOfWeek >= 0) {
                        // Lß║Ñy aircraft ─æ╞░ß╗úc chß╗ìn
                        const aircraftSelect = document.getElementById(`aircraft-${def.id}`)
                        const selectedAircraftId = aircraftSelect ? parseInt(aircraftSelect.value, 10) : null
                        
                        if (!selectedAircraftId) {
                          setApiError('Vui l├▓ng chß╗ìn m├íy bay tr╞░ß╗¢c!')
                          setTimeout(() => setApiError(''), 3000)
                          e.target.value = ''
                          return
                        }
                        
                        // Kiß╗âm tra tr├╣ng
                        const isDuplicate = templateSlots.some(
                          (slot) => slot.flightDefinition.id === def.id && slot.dayOfWeek === dayOfWeek
                        )
                        if (isDuplicate) {
                          setApiError(`Flight ${def.flightNumber} ─æ├ú tß╗ôn tß║íi trong ${getWeekdayName(dayOfWeek)}!`)
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
                        setAdminNotice(`Γ£à ─É├ú th├¬m ${def.flightNumber} v├áo ${getWeekdayName(dayOfWeek)}`)
                        setTimeout(() => setAdminNotice(''), 2000)
                        e.target.value = ''
                        aircraftSelect.value = ''
                      }
                    }}
                  >
                    <option value="">Chß╗ìn thß╗⌐...</option>
                    <option value="0">Thß╗⌐ 2</option>
                    <option value="1">Thß╗⌐ 3</option>
                    <option value="2">Thß╗⌐ 4</option>
                    <option value="3">Thß╗⌐ 5</option>
                    <option value="4">Thß╗⌐ 6</option>
                    <option value="5">Thß╗⌐ 7</option>
                    <option value="6">Chß╗º nhß║¡t</option>
                  </select>
                </div>
              ))}
            </div>
          </aside>

          {/* Khung template theo thß╗⌐ */}
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
                    <p className="text-xs text-slate-500">({slotsForDay.length} chuyß║┐n)</p>
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
                          {slot.flightDefinition.departureAirportCode} ΓåÆ {slot.flightDefinition.arrivalAirportCode}
                        </p>
                        <p className="text-xs text-slate-500">
                          {slot.flightDefinition.departureTime?.substring(0, 5)}
                        </p>
                        {slot.flightDefinition.selectedAircraftId && (
                          <p className="text-xs text-green-700 font-medium">
                            Γ£ê∩╕Å Aircraft ID: {slot.flightDefinition.selectedAircraftId}
                          </p>
                        )}
                        <button
                          type="button"
                          onClick={() => {
                            setTemplateSlots((prev) => prev.filter((s) => s.id !== slot.id))
                            setAdminNotice('─É├ú x├│a chuyß║┐n bay khß╗Åi template')
                            setTimeout(() => setAdminNotice(''), 2000)
                          }}
                          className="mt-1 w-full rounded bg-red-100 px-2 py-1 text-xs text-red-700 hover:bg-red-200"
                        >
                          X├│a
                        </button>
                      </div>
                    ))}
                    {slotsForDay.length === 0 && (
                      <div className="rounded-lg border border-dashed border-slate-300 p-3 text-center text-xs text-slate-400">
                        Ch╞░a c├│ chuyß║┐n bay
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </section>

      {/* Modal xem chi tiß║┐t template */}
      {viewingTemplateDetail && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-y-auto rounded-2xl bg-white p-6 shadow-2xl">
            <div className="mb-4 flex items-start justify-between">
              <div>
                <h3 className="text-2xl font-bold text-slate-900">
                  ≡ƒôï {viewingTemplateDetail.name}
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
                    {viewingTemplateDetail.isActive ? 'Γ£ô Hoß║ít ─æß╗Öng' : 'ΓÅ╕ Tß║ím dß╗½ng'}
                  </span>
                  <span className="rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-700">
                    {viewingTemplateDetail.details?.length || 0} chuyß║┐n bay/tuß║ºn
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setViewingTemplateDetail(null)}
                className="rounded-lg bg-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-300"
              >
                Γ£ò ─É├│ng
              </button>
            </div>

            {/* Hiß╗ân thß╗ï theo thß╗⌐ */}
            <div className="mt-6">
              <h4 className="mb-3 text-lg font-bold text-slate-900">Lß╗ïch bay theo thß╗⌐</h4>
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
                        <p className="text-xs text-slate-500">({flightsForDay.length} chuyß║┐n)</p>
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
                              {detail.departureTime} ΓåÆ {detail.arrivalTime}
                            </p>
                          </div>
                        ))}
                        {flightsForDay.length === 0 && (
                          <div className="rounded-lg border border-dashed border-slate-300 p-3 text-center text-xs text-slate-400">
                            Kh├┤ng c├│ chuyß║┐n bay
                          </div>
                        )}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Hiß╗ân thß╗ï raw data */}
            <div className="mt-6">
              <h4 className="mb-2 text-sm font-bold text-slate-900">≡ƒôè Raw Data (JSON)</h4>
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
            Kh├ích h├áng
          </button>
          <button
            type="button"
            onClick={() => setScreen('history')}
            className={`rounded-full px-3 py-1 text-xs font-semibold ${
              screen === 'history' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
            }`}
          >
            V├⌐ cß╗ºa t├┤i
          </button>
          {isAdmin && (
            <button
              type="button"
              onClick={() => setScreen('templates')}
              className={`rounded-full px-3 py-1 text-xs font-semibold ${
                screen === 'templates' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
              }`}
            >
              ≡ƒôï Quß║ún l├╜ Templates
            </button>
          )}
        </div>
      )}
      {screen !== 'login' &&
        (screen === 'search' || screen === 'list' || screen === 'passenger' || screen === 'payment') && (
        <div className="mb-5 flex flex-wrap gap-2">
          {[
            { key: 'search', label: 'T├¼m kiß║┐m' },
            { key: 'list', label: 'Danh s├ích' },
            { key: 'passenger', label: 'H├ánh kh├ích' },
            { key: 'payment', label: 'Thanh to├ín' },
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
      {screen === 'register' && renderRegister()}
      {screen === 'search' && renderSearch()}
      {screen === 'list' && renderFlightList()}
      {screen === 'passenger' && selectedFlight && renderPassenger()}
      {screen === 'payment' && selectedFlight && renderPayment()}
      {screen === 'history' && renderHistory()}
      {screen === 'templates' && isAdmin && renderTemplateManagement()}
    </main>
  )
}

export default App
