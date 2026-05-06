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
  getFlightTemplates,
  getFlightScheduleTemplate,
  createFlightTemplate,
  deleteFlightTemplate,
  generateFlightsFromTemplate,
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
      status: booking?.status || 'Đã đặt',
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
      setHistoryError(error.message || 'Không thể tải lịch sử đặt vé')
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
          ? { ...entry, status: 'Đã hủy' }
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
      status: paymentData?.status || 'Đã thanh toán',
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
      setHistoryNotice('Vé này đã được lưu trong lịch sử.')
      return
    }

    const next = [entry, ...bookingHistory]
    persistBookingHistory(next)
    setHistoryNotice('Đã lưu vé vào lịch sử đặt chỗ.')
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
          console.log('🎁 Áp dụng mã giảm giá:', promotion.promoCode, '- Giảm:', promotion.calculatedDiscount)
        }
      } catch (error) {
        console.error('Lỗi khi tìm promotion:', error)
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
    const names = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']
    return names[dayIndex] || 'Thứ'
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
          <Label>Số hành khách</Label>
          <Select
            value={searchData.passengers}
            onChange={(e) => setSearchData((prev) => ({ ...prev, passengers: e.target.value }))}
          >
            <option value="1">1 hành khách</option>
            <option value="2">2 hành khách</option>
            <option value="3">3 hành khách</option>
            <option value="4">4 hành khách</option>
          </Select>
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
        onClick={async () => {
          if (searchData.fromAirportId === searchData.toAirportId) {
            setApiError('Điểm đi và điểm đến phải khác nhau')
            return
          }

          if (!searchData.departDate || searchData.departDate < today) {
            setApiError('Ngày đi phải từ hôm nay trở đi')
            return
          }

          if (tripType === 'roundtrip' && !searchData.returnDate) {
            setApiError('Vui lòng chọn ngày về')
            return
          }

          if (tripType === 'roundtrip' && searchData.returnDate < searchData.departDate) {
            setApiError('Ngày về phải sau hoặc bằng ngày đi')
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
            setApiError(error.message || 'Lỗi tìm kiếm chuyến bay. Vui lòng thử lại.')
          } finally {
            setIsLoadingFlights(false)
          }
        }}
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
                    ID chuyến bay: {getFlightId(flight)} · Mã hiển thị: {getFlightLabel(flight)}
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
                    Chọn
                  </button>
                </div>
              </div>
            </article>
          )
        })}

        {!isLoadingFlights && filteredFlights.length === 0 && apiFlights.length > 0 && (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
            Không tìm thấy chuyến bay phù hợp bộ lọc.
          </div>
        )}

        {!isLoadingFlights && apiFlights.length === 0 && (
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
        <div className="grid gap-4 md:grid-cols-2">
          <div className="md:col-span-2">
            <Label>Họ tên</Label>
            <Input
              placeholder="Nguyễn Văn A"
              value={passengerInfo.fullName}
              onChange={(e) =>
                setPassengerInfo((prev) => ({ ...prev, fullName: e.target.value }))
              }
            />
          </div>
          <div>
            <Label>Ngày sinh</Label>
            <Input
              type="date"
              value={passengerInfo.dob}
              onChange={(e) => setPassengerInfo((prev) => ({ ...prev, dob: e.target.value }))}
            />
          </div>
          <div>
            <Label>Giới tính</Label>
            <Select
              value={passengerInfo.gender}
              onChange={(e) =>
                setPassengerInfo((prev) => ({ ...prev, gender: e.target.value }))
              }
            >
              <option>Nam</option>
              <option>Nữ</option>
              <option>Khác</option>
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
              setApiError('Vui lòng điền đầy đủ thông tin hành khách')
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
        
        <div className="mt-3 border-t border-slate-100 pt-3">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Giá vé ({searchData.passengers} người):</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {isLoadingPromotion && (
            <div className="mt-2 text-xs text-slate-500">
              🔍 Đang tìm mã giảm giá tốt nhất...
            </div>
          )}
          
          {bestPromotion && (
            <div className="mt-2 rounded-lg bg-green-50 p-2">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-semibold text-green-700">🎁 {bestPromotion.promoCode}</p>
                  <p className="text-xs text-green-600">{bestPromotion.description}</p>
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

  const renderPayment = () => (
    <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <h2 className="title-font mb-4 text-xl font-bold text-slate-900">Thanh toán</h2>
        <p className="mb-5 text-sm text-slate-500">Phương thức thanh toán: VNPAY</p>
        {paymentData && (
          <div className="mb-4 rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">
            <p>Trạng thái: {paymentData.status}</p>
            <p>Số tiền: {formatCurrency(paymentData.amount || totalPrice)}</p>
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
            {paymentData?.paymentLink ? (
              <a
                className="inline-flex items-center justify-center rounded-xl bg-[#1E40AF] px-5 py-3 text-sm font-semibold text-white hover:bg-blue-800"
                href={paymentData.paymentLink}
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
            setPassengerInfo({ fullName: '', dob: '', gender: 'Nam', document: '' })
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
          <p>Hãng bay: {selectedFlight?.airlineCode}</p>
          <p>Hành khách: {passengerInfo.fullName || 'Chưa nhập'}</p>
          <p>Số lượng: {searchData.passengers}</p>
          {paymentData && (
            <p className="break-all text-xs text-slate-500">Mã giao dịch: {paymentData.transactionRef}</p>
          )}
        </div>
        
        <div className="mt-4 border-t border-slate-100 pt-4 space-y-2">
          <div className="flex justify-between text-sm text-slate-600">
            <span>Giá vé:</span>
            <span>{formatCurrency(totalPrice)}</span>
          </div>
          
          {bestPromotion && (
            <div className="flex justify-between text-sm text-green-600">
              <span>🎁 Giảm giá ({bestPromotion.promoCode}):</span>
              <span>-{formatCurrency(discountAmount)}</span>
            </div>
          )}
          
          <div className="flex justify-between border-t border-slate-100 pt-2 text-base font-bold text-[#1E40AF]">
            <span>Tổng tiền:</span>
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
                    Mã booking: {item.bookingId}
                    {item.transactionRef ? ` · Mã giao dịch: ${item.transactionRef}` : ''}
                  </p>
                </div>
                <div className="text-right">
                  <p
                    className={`text-sm font-semibold ${
                      isBookingCancelled(item.status) ? 'text-red-600' : 'text-emerald-600'
                    }`}
                  >
                    {item.status}
                  </p>
                  <p className="text-sm text-slate-500">{formatDateTime(item.createdAt)}</p>
                  <p className="mt-2 text-lg font-bold text-slate-900">
                    {formatCurrency(item.totalPrice)}
                  </p>
                  <p className="text-xs text-slate-500">
                    Hành khách: {item.passengerName || '---'} · {item.passengerCount} vé
                  </p>
                  {!isBookingCancelled(item.status) && (
                    <button
                      type="button"
                      onClick={() => cancelBookingFromHistory(item)}
                      disabled={isCancellingBookingId === item.bookingId}
                      className="mt-3 rounded-xl bg-red-500 px-4 py-2 text-xs font-semibold text-white hover:bg-red-600 disabled:opacity-60"
                    >
                      {isCancellingBookingId === item.bookingId ? 'Đang hủy...' : 'Hủy vé'}
                    </button>
                  )}
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
          <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">Quản lý Flight Templates</h2>
          <p className="mt-1 text-sm text-slate-500">
            Tạo template chuyến bay theo thứ trong tuần và tự động sinh chuyến bay từ template
          </p>
        </div>

        {adminNotice && (
          <div className="mb-4 rounded-xl bg-blue-50 px-4 py-3 text-sm text-[#1E40AF]">
            {adminNotice}
          </div>
        )}

        {apiError && (
          <div className="mb-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">
            {apiError}
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
                      aircraftId: slot.flightDefinition.defaultAircraftId || 1,
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

                      const result = await generateFlightsFromTemplate({
                        templateId: templateId,
                        weekStartDate: weekStartDateTime, // ISO datetime với timezone
                        numberOfWeeks: numberOfWeeks,
                      })
                      setAdminNotice(
                        `✅ Thành công! Đã sinh ${result.totalFlightsGenerated} chuyến bay! ` +
                        (result.totalFlightsSkipped > 0 ? `(Bỏ qua ${result.totalFlightsSkipped} chuyến trùng)` : '')
                      )
                      setSelectedTemplate(null)
                    } catch (error) {
                      setApiError(error.message || 'Lỗi khi sinh chuyến bay')
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
                  <select
                    className="mt-2 w-full rounded border border-slate-300 px-2 py-1 text-xs"
                    onChange={(e) => {
                      const dayOfWeek = parseInt(e.target.value, 10)
                      if (dayOfWeek >= 0) {
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
                            flightDefinition: def,
                            dayOfWeek,
                          },
                        ])
                        setAdminNotice(`✅ Đã thêm ${def.flightNumber} vào ${getWeekdayName(dayOfWeek)}`)
                        setTimeout(() => setAdminNotice(''), 2000)
                        e.target.value = ''
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
          {isAdmin && (
            <button
              type="button"
              onClick={() => setScreen('templates')}
              className={`rounded-full px-3 py-1 text-xs font-semibold ${
                screen === 'templates' ? 'bg-[#1E40AF] text-white' : 'bg-slate-200 text-slate-500'
              }`}
            >
              📋 Quản lý Templates
            </button>
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
