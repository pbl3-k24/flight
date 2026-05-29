import { useState } from 'react'
import QRCode from 'react-qr-code'

// Safe interop for react-qr-code CJS/ESM in Vite/Rollup
const SafeQRCode = (() => {
  if (!QRCode) return 'div'
  if (QRCode.default && (QRCode.default.render || typeof QRCode.default === 'function' || QRCode.default.$$typeof)) return QRCode.default
  if (QRCode.render || typeof QRCode === 'function' || QRCode.$$typeof) return QRCode
  if (typeof QRCode === 'object' && QRCode.QRCode) return QRCode.QRCode
  return QRCode
})()

export default function BookingHistory({
  bookingHistory,
  isLoadingHistory,
  historyError,
  historyNotice,
  loadBookingHistory,
  setScreen,
  disruptionOptionsMap,
  disruptionLoadingMap,
  disruptionErrorMap,
  disruptionNoticeMap,
  selectedDisruptionDecisionMap,
  setSelectedDisruptionDecisionMap,
  selectedDisruptionActionMap,
  setSelectedDisruptionActionMap,
  loadTicketsForBooking,
  bookingTicketsMap,
  loadingTicketsMap,
  ticketErrorMap,
  resolveTicketStatusCode,
  getTicketStatusLabel,
  isTicketActionable,
  openUpgradeModal,
  cancelTicketFromHistory,
  initiatePayment,
  setHistoryError,
  setHistoryNotice,
  seatClassMap,
  setCurrentBookingForServices,
  setSelectedServicesByPassenger,
  setShowServicesModal,
  loadServices,
  openChangeFlightModal,
  cancelBookingFromHistory,
  loadDisruptionOptionsForBooking,
  getDisruptionLegLabel,
  handleDisruptionRefund,
  openRebookModal,
  formatRouteLabel,
  formatTime,
  formatDuration,
  formatDateTime,
  formatFlightMeta,
  getBookingStatusClassName,
  getBookingStatusLabel,
  formatCurrency,
  isBookingCancelled,
  isBookingPendingPayment,
  isPendingDisruptionDecision,
  isCancellingBookingId,
  isCancellingTicketId,
  
  // Modal states & actions
  upgradeModal,
  closeUpgradeModal,
  setUpgradeModal,
  handleGetUpgradeQuote,
  handleCreateUpgradeRequest,
  handleInitiateUpgradePayment,
  upgradeLoading,
  
  changeFlightModal,
  closeChangeFlightModal,
  setChangeFlightModal,
  handleGetChangeFlightOptions,
  handleGetChangeFlightQuote,
  handleConfirmChangeFlight,
  changeFlightLoading,
  
  showServicesModal,
  currentBookingForServices,
  selectedServicesByPassenger,
  services,
  isLoadingServices,
  getServicePriceLabel,
  addServiceToBooking,
  
  rebookModalState,
  closeRebookModal,
  setRebookModalState,
  getDisruptionDecisionOptions,
  filterDisruptionFlightsByDate,
  handleDisruptionRebook
}) {
  const [activeTab, setActiveTab] = useState('all')
  const [expandedBookings, setExpandedBookings] = useState({})

  const handleToggleTickets = (bookingId) => {
    const isExpanded = !!expandedBookings[bookingId]
    setExpandedBookings((prev) => ({
      ...prev,
      [bookingId]: !isExpanded,
    }))
    if (!isExpanded && !bookingTicketsMap[bookingId]) {
      loadTicketsForBooking(bookingId)
    }
  }

  const filteredHistory = bookingHistory.filter((item) => {
    if (activeTab === 'all') return true
    if (activeTab === 'paid') return !isBookingCancelled(item.status) && !isBookingPendingPayment(item.status)
    if (activeTab === 'pending') return isBookingPendingPayment(item.status)
    if (activeTab === 'disrupted') return isPendingDisruptionDecision(item.status) || isBookingCancelled(item.status)
    return true
  })

  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      {/* Header and controls */}
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-4">
        <div>
          <h2 className="title-font text-xl font-extrabold text-slate-800 md:text-2xl">
            Lịch sử d?t vï¿½ mï¿½y bay
          </h2>
          <p className="text-xs text-slate-400">Xem vï¿½ qu?n lï¿½ t?t c? cï¿½c giao d?ch d?t gi? ch? c?a b?n</p>
        </div>
        
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={loadBookingHistory}
            className="btn-secondary py-2 text-xs font-bold"
          >
            Lï¿½m m?i
          </button>
          <button
            type="button"
            onClick={() => setScreen('search')}
            className="btn-primary py-2 text-xs font-bold"
          >
            D?t vï¿½ m?i
          </button>
        </div>
      </div>

      {/* Tabs Menu */}
      <div className="flex border-b border-slate-200 gap-1 overflow-x-auto pb-[1px]">
        {[
          { id: 'all', label: 'T?t c? d?t ch?' },
          { id: 'paid', label: 'Dï¿½ thanh toï¿½n / Hoï¿½n t?t' },
          { id: 'pending', label: 'Ch? thanh toï¿½n' },
          { id: 'disrupted', label: 'S? c? chuyến bay / Dï¿½ h?y' },
        ].map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveTab(tab.id)}
            className={`whitespace-nowrap px-4 py-2.5 text-xs font-bold border-b-2 transition ${
              activeTab === tab.id
                ? 'border-brand-primary text-brand-primary'
                : 'border-transparent text-slate-500 hover:text-slate-800 hover:border-slate-300'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Notifications */}
      {historyError && (
        <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600">
          {historyError}
        </div>
      )}

      {historyNotice && (
        <div className="rounded-xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-bold text-emerald-700 animate-in fade-in duration-200">
          {historyNotice}
        </div>
      )}

      {/* History Lists */}
      {isLoadingHistory ? (
        <div className="flex flex-col items-center justify-center py-20 rounded-3xl bg-white border border-slate-100 shadow-sm">
          <svg className="h-6 w-6 animate-spin text-brand-primary" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <span className="mt-4 text-xs text-slate-500">Đang tải lịch sử vï¿½ dï¿½ d?t...</span>
        </div>
      ) : filteredHistory.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
          <svg className="h-10 w-10 mx-auto text-slate-300 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
          </svg>
          <span className="text-xs font-bold">Chưa cï¿½ thï¿½ng tin giao d?ch nï¿½o cho phï¿½n m?c nï¿½y.</span>
        </div>
      ) : (
        <div className="space-y-6">
          {filteredHistory.map((item) => {
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
                className="glass-panel rounded-3xl p-6 shadow-md border border-slate-200/60"
              >
                <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                  {/* Flight Info Block */}
                  <div className="space-y-2">
                    <span className="text-xs font-extrabold text-brand-primary uppercase tracking-wider block">
                      {formatRouteLabel(item)}
                    </span>
                    <h3 className="text-base font-extrabold text-slate-800">
                      {formatTime(item.departTime)} - {formatTime(item.arriveTime)}
                    </h3>
                    <p className="text-xs text-slate-400 font-medium">
                      Ngï¿½y bay: {formatDateTime(item.departTime)}
                    </p>
                    {formatFlightMeta(item) && (
                      <span className="inline-block rounded-lg bg-slate-100 px-2.5 py-1 text-[10px] font-bold text-slate-500">
                        {formatFlightMeta(item)}
                      </span>
                    )}
                    <div className="text-[10px] text-slate-400 font-bold pt-1.5 border-t border-slate-50">
                      <span>Mï¿½ Booking: <strong className="text-slate-600">{item.bookingId}</strong></span>
                      {item.transactionRef && (
                        <span className="block mt-0.5">Giao d?ch: <strong className="text-slate-600">{item.transactionRef}</strong></span>
                      )}
                    </div>
                  </div>

                  {/* Summary Block */}
                  <div className="space-y-2">
                    <span className="text-xs font-bold text-slate-400 uppercase tracking-wider block">
                      Thï¿½ng tin hï¿½a don
                    </span>
                    <div className="flex items-center gap-2">
                      <span className={`rounded-full px-3 py-1 text-[10px] font-bold ${
                        getBookingStatusClassName(item.status) === 'text-emerald-600' ? 'bg-emerald-50 text-emerald-700' :
                        getBookingStatusClassName(item.status) === 'text-red-600' ? 'bg-red-50 text-red-700' : 'bg-yellow-50 text-yellow-700'
                      }`}>
                        {getBookingStatusLabel(item.status)}
                      </span>
                    </div>
                    <p className="text-[11px] text-slate-400 font-medium mt-1">
                      D?t lï¿½c: {formatDateTime(item.createdAt)}
                    </p>
                    <h4 className="text-base font-extrabold text-slate-800 pt-1">
                      {formatCurrency(item.totalPrice)}
                    </h4>
                    <p className="text-[10px] text-slate-400 font-bold">
                      Hï¿½nh khï¿½ch: {item.passengerName || '---'} ï¿½ {item.passengerCount} gh?
                    </p>
                  </div>

                  {/* Service list if exists */}
                  {Array.isArray(item.passengers) &&
                    item.passengers.some(
                      (passenger) => Array.isArray(passenger?.services) && passenger.services.length > 0
                    ) && (
                      <div className="rounded-2xl border border-slate-100 bg-slate-50/50 p-4 text-xs space-y-2.5">
                        <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">Dịch vụ di kï¿½m</span>
                        <div className="space-y-2 max-h-24 overflow-y-auto pr-1">
                          {item.passengers.map((passenger, pIdx) => {
                            const svcs = Array.isArray(passenger?.services) ? passenger.services : []
                            if (svcs.length === 0) return null
                            const pName = [passenger?.lastName, passenger?.firstName].filter(Boolean).join(' ') || `Hï¿½nh khï¿½ch ${pIdx + 1}`

                            return (
                              <div key={passenger?.passengerId || pIdx} className="space-y-1">
                                <p className="font-extrabold text-[10px] text-slate-600">{pName}</p>
                                <ul className="space-y-0.5 text-[10px] text-slate-500 pl-2 list-disc">
                                  {svcs.map((service, sIdx) => (
                                    <li key={sIdx}>
                                      {service?.serviceName} {service?.quantity > 1 ? `x${service.quantity}` : ''} ({formatCurrency(service.price)})
                                    </li>
                                  ))}
                                </ul>
                              </div>
                            )
                          })}
                        </div>
                      </div>
                    )}
                </div>

                {/* Actions Row */}
                <div className="flex flex-wrap items-center justify-end gap-2.5 mt-5 border-t border-slate-100 pt-4">
                  <button
                    type="button"
                    onClick={() => handleToggleTickets(item.bookingId)}
                    className="btn-secondary px-3.5 py-2 text-xs font-bold rounded-xl"
                  >
                    {expandedBookings[item.bookingId] ? '?n chi ti?t vï¿½' : 'Xem chi ti?t vï¿½'}
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
                            setHistoryError('Mï¿½ booking khï¿½ng h?p l?')
                            return
                          }
                          setHistoryNotice('Đang k?t n?i c?ng thanh toï¿½n...')
                          const res = await initiatePayment(bookingIdValue, 'VNPAY')
                          const paymentUrl = res?.paymentUrl || res?.paymentLink || res?.PaymentUrl || res?.payment?.paymentUrl
                          if (paymentUrl) {
                            window.location.href = paymentUrl
                          } else {
                            setHistoryError('Khï¿½ng th? l?y liï¿½n k?t thanh toï¿½n t? c?ng.')
                          }
                        } catch (err) {
                          setHistoryError(err.message || 'Lỗi k?t n?i c?ng thanh toï¿½n')
                        }
                      }}
                      className="btn-success px-4 py-2 text-xs font-bold rounded-xl flex items-center gap-1.5"
                    >
                       Thanh toï¿½n ngay
                    </button>
                  )}

                  {!isBookingCancelled(item.status) && (
                    <button
                      type="button"
                      onClick={() => {
                        const passengers = item?.passengers || []
                        const seatClassId = seatClassMap[item?.seatClass] || null
                        setCurrentBookingForServices({ ...item })
                        const initSel = passengers.reduce((acc, passenger) => {
                          if (passenger?.passengerId) acc[passenger.passengerId] = {}
                          return acc
                        }, {})
                        setSelectedServicesByPassenger(initSel)
                        setShowServicesModal(true)
                        loadServices(seatClassId)
                      }}
                      className="btn-primary bg-indigo-600 hover:bg-indigo-700 px-4 py-2 text-xs font-bold rounded-xl"
                    >
                      ? Thï¿½m dịch vụ
                    </button>
                  )}

                  {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                    <button
                      type="button"
                      onClick={() => openChangeFlightModal(item)}
                      className="btn-secondary border-emerald-400 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 px-4 py-2 text-xs font-bold rounded-xl"
                    >
                       D?i chuyến
                    </button>
                  )}

                  {!isBookingCancelled(item.status) && !isPendingDisruptionDecision(item.status) && (
                    <button
                      type="button"
                      onClick={() => cancelBookingFromHistory(item)}
                      disabled={isCancellingBookingId === item.bookingId}
                      className="btn-danger px-4 py-2 text-xs font-bold rounded-xl disabled:opacity-50"
                    >
                      {isCancellingBookingId === item.bookingId ? 'Đang h?y...' : 'H?y d?t ch?'}
                    </button>
                  )}

                  {(isBookingCancelled(item.status) || isPendingDisruptionDecision(item.status)) && (
                    <button
                      type="button"
                      onClick={() => loadDisruptionOptionsForBooking(item.bookingId)}
                      className="btn-primary bg-amber-500 hover:bg-amber-600 px-4 py-2 text-xs font-bold rounded-xl"
                    >
                      T?i phuong ï¿½n x? lï¿½ h?y chuyến
                    </button>
                  )}
                </div>

                {/* Disruption Alert and Form */}
                {disruptionOptions.length > 0 && (
                  <div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50/50 p-4 space-y-3 animate-in fade-in duration-200">
                    <p className="font-extrabold text-amber-800 text-xs flex items-center gap-1.5">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="w-5 h-5 text-amber-600 animate-bounce">
                        <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clipRule="evenodd" />
                      </svg>
                      <span>TRUNG Tï¿½M X? Lï¿½ S? C?: Chuyến bay c?a b?n cï¿½ thay d?i l?ch trï¿½nh.</span>
                    </p>

                    <div className="grid gap-4 sm:grid-cols-2 text-xs">
                      <div>
                        <label className="form-label text-[10px]">Ch?n chuyến bay b? ?nh hu?ng</label>
                        <select
                          value={selectedDecisionId}
                          onChange={(e) =>
                            setSelectedDisruptionDecisionMap((prev) => ({
                              ...prev,
                              [item.bookingId]: e.target.value,
                            }))
                          }
                          className="form-input text-xs"
                        >
                          <option value="">-- Ch?n ch?ng x? lï¿½ --</option>
                          {disruptionOptions.map((opt, optIdx) => (
                            <option key={optIdx} value={opt?.decisionId ?? ''}>
                              {getDisruptionLegLabel(opt?.legType)} (Mï¿½ quy?t d?nh #{opt?.decisionId})
                            </option>
                          ))}
                        </select>
                      </div>

                      <div>
                        <label className="form-label text-[10px]">L?a ch?n phuong th?c x? lï¿½</label>
                        <div className="flex gap-4 pt-3.5">
                          <label className="inline-flex items-center gap-1.5 font-semibold text-slate-700 cursor-pointer">
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
                              className="accent-amber-600 h-4 w-4"
                            />
                            <span>Hoï¿½n ti?n 100% khï¿½ng m?t phï¿½</span>
                          </label>
                          <label className="inline-flex items-center gap-1.5 font-semibold text-slate-700 cursor-pointer">
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
                              className="accent-amber-600 h-4 w-4"
                            />
                            <span>D?i sang chuyến bay m?i mi?n phï¿½</span>
                          </label>
                        </div>
                      </div>
                    </div>

                    <div className="flex justify-end pt-2">
                      <button
                        type="button"
                        disabled={!selectedDecisionId}
                        onClick={() => {
                          if (!selectedDecisionId) return
                          if (selectedAction === 'refund') {
                            handleDisruptionRefund(item.bookingId, selectedDecisionId)
                          } else {
                            openRebookModal(item.bookingId, selectedDecisionId, item.departTime)
                          }
                        }}
                        className="rounded-xl bg-amber-600 px-5 py-2 text-xs font-bold text-white hover:bg-amber-700 disabled:opacity-50"
                      >
                        Xï¿½c nh?n phuong ï¿½n x? lï¿½
                      </button>
                    </div>
                  </div>
                )}

                {/* Tickets Lists */}
                {expandedBookings[item.bookingId] && (
                  <div className="mt-4 pt-4 border-t border-slate-100 space-y-4">
                    {loadingTicketsMap[item.bookingId] && (
                      <p className="text-xs text-slate-400 font-bold animate-pulse">Đang tải danh sï¿½ch vï¿½ di?n t?...</p>
                    )}
                    {ticketErrorMap[item.bookingId] && (
                      <p className="text-xs font-bold text-rose-500">{ticketErrorMap[item.bookingId]}</p>
                    )}
                    {!loadingTicketsMap[item.bookingId] && !ticketErrorMap[item.bookingId] && (
                      Array.isArray(bookingTicketsMap[item.bookingId]) && (
                        bookingTicketsMap[item.bookingId].length > 0 ? (
                          <>
                            <h4 className="text-xs font-bold text-slate-800">Vï¿½ di?n t? c?a b?n</h4>
                            
                            <div className="grid gap-4 md:grid-cols-2">
                              {bookingTicketsMap[item.bookingId].map((ticket) => {
                                const code = resolveTicketStatusCode(ticket.status)
                                let bg = 'bg-yellow-50 text-yellow-800 border-yellow-200'
                                let icon = '.'
                                
                                if (code === 0) {
                                  bg = 'bg-emerald-50 text-emerald-800 border-emerald-200'
                                  icon = '?'
                                } else if (code === 1) {
                                  bg = 'bg-slate-50 text-slate-700 border-slate-200'
                                  icon = ''
                                } else if (code === 2) {
                                  bg = 'bg-blue-50 text-blue-800 border-blue-200'
                                  icon = '?'
                                } else if (code === 3 || code === 4 || code === 5) {
                                  bg = 'bg-red-50 text-red-800 border-red-200'
                                  icon = '?'
                                }
                                
                                const label = getTicketStatusLabel(ticket.status)
                                
                                return (
                                  <div
                                    key={ticket.ticketId}
                                    className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm space-y-4 relative overflow-hidden"
                                  >
                                    {/* Boarding Pass layout */}
                                    <div className="flex justify-between items-start">
                                      <div>
                                        <span className="text-[10px] text-slate-400 font-extrabold uppercase">Vï¿½ s?</span>
                                        <h5 className="text-xs font-extrabold text-slate-800">{ticket.ticketNumber || `Vï¿½ #${ticket.ticketId}`}</h5>
                                      </div>
                                      <span className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[9px] font-bold ${bg}`}>
                                        <span>{icon}</span>
                                        <span>{label}</span>
                                      </span>
                                    </div>

                                    <div className="grid grid-cols-2 gap-2 text-[10px] text-slate-500">
                                      <div>
                                        <span className="block font-bold text-slate-400 text-[8px] uppercase">Hï¿½nh khï¿½ch</span>
                                        <span className="font-extrabold text-slate-700">{ticket.passengerName || '---'}</span>
                                      </div>
                                      <div>
                                        <span className="block font-bold text-slate-400 text-[8px] uppercase">S? hi?u chuyến bay</span>
                                        <span className="font-extrabold text-slate-700">{ticket.flightNumber || '---'}</span>
                                      </div>
                                      <div>
                                        <span className="block font-bold text-slate-400 text-[8px] uppercase">Hï¿½nh trï¿½nh</span>
                                        <span className="font-extrabold text-slate-700">{ticket.departureAirport} ? {ticket.arrivalAirport}</span>
                                      </div>
                                      <div>
                                        <span className="block font-bold text-slate-400 text-[8px] uppercase">Gi? c?t cï¿½nh</span>
                                        <span className="font-extrabold text-slate-700">{formatDateTime(ticket.departureTime)}</span>
                                      </div>
                                      <div>
                                        <span className="block font-bold text-slate-400 text-[8px] uppercase">Hạng ghế</span>
                                        <span className={`inline-block px-2 py-0.5 rounded-full text-[10px] font-bold mt-0.5 ${
                                          (ticket.seatClassName || '').toLowerCase().includes('first') || (ticket.seatClassCode || '').toLowerCase().includes('f')
                                            ? 'bg-amber-100 text-amber-700'
                                            : (ticket.seatClassName || '').toLowerCase().includes('business') || (ticket.seatClassCode || '').toLowerCase().includes('b')
                                            ? 'bg-indigo-100 text-indigo-700'
                                            : 'bg-emerald-100 text-emerald-700'
                                        }`}>
                                          {ticket.seatClassName || seatClassMap?.[ticket.seatClassId] || ticket.seatClassCode || 'Economy'}
                                        </span>
                                      </div>
                                    </div>

                                    {/* Rendering Boarding QR code */}
                                    <div className="flex justify-center bg-slate-50 p-3 rounded-2xl border border-slate-100">
                                      <SafeQRCode
                                        value={`FLIGHT-PASS:${ticket.ticketNumber || ticket.ticketId}:${ticket.passengerName}:${ticket.flightNumber}`}
                                        size={70}
                                        className="h-16 w-16"
                                      />
                                    </div>

                                    {/* Ticket actions */}
                                    {isTicketActionable(ticket.status) && (
                                      <div className="flex gap-2 pt-2 border-t border-slate-50">
                                        <button
                                          type="button"
                                          onClick={() => openUpgradeModal(item, ticket)}
                                          className="btn-secondary border-indigo-200 bg-indigo-50/50 text-indigo-700 hover:bg-indigo-50 px-3 py-1.5 text-[10px] font-bold rounded-lg flex-1"
                                        >
                                           Nï¿½ng h?ng
                                        </button>
                                        <button
                                          type="button"
                                          disabled={isCancellingTicketId === ticket.ticketId}
                                          onClick={() => cancelTicketFromHistory(item.bookingId, ticket)}
                                          className="btn-danger px-3 py-1.5 text-[10px] font-bold rounded-lg flex-1"
                                        >
                                          {isCancellingTicketId === ticket.ticketId ? 'Đang h?y...' : 'H?y vï¿½'}
                                        </button>
                                      </div>
                                    )}

                                    {/* Service listed under ticket */}
                                    {Array.isArray(ticket.services) && ticket.services.length > 0 && (
                                      <div className="mt-2 rounded-xl bg-slate-50 p-2 text-[9px] text-slate-500">
                                        <p className="font-bold text-slate-600 mb-1">Dịch vụ di kï¿½m:</p>
                                        <ul className="space-y-0.5 list-disc pl-3">
                                          {ticket.services.map((svc, svcIdx) => (
                                            <li key={svcIdx}>
                                              {svc?.serviceName} {svc?.quantity > 1 ? `x${svc.quantity}` : ''}
                                            </li>
                                          ))}
                                        </ul>
                                      </div>
                                    )}
                                  </div>
                                )
                              })}
                            </div>
                          </>
                        ) : (
                          <p className="text-xs text-slate-400 font-bold text-center py-4 bg-slate-50 rounded-2xl border border-slate-200">
                            Chưa cï¿½ thï¿½ng tin vï¿½ cho giao d?ch nï¿½y.
                          </p>
                        )
                      )
                    )}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      )}

      {/* Upgrade Seats Class Modal */}
      {upgradeModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="max-h-[90vh] w-full max-w-md overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-150">
            <div className="flex items-start justify-between border-b border-slate-100 pb-3">
              <div>
                <h3 className="title-font text-base font-extrabold text-slate-800">
                  Nï¿½ng hạng ghế cabin
                </h3>
                <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                  Vï¿½: {upgradeModal.ticket?.ticketNumber} ï¿½ Hï¿½nh khï¿½ch: {upgradeModal.ticket?.passengerName}
                </p>
              </div>
              <button
                type="button"
                onClick={closeUpgradeModal}
                className="text-slate-400 hover:text-slate-700 text-lg font-bold"
              >
                ?
              </button>
            </div>

            {/* Steps indicator */}
            <div className="grid grid-cols-3 gap-2 border-b border-slate-50 pb-3.5">
              {['select', 'quote', 'payment'].map((s, idx) => {
                const stepLabels = ['Ch?n h?ng', 'Bï¿½o giï¿½', 'Thanh toï¿½n']
                const isActive = upgradeModal.step === s
                const isCompleted = ['select', 'quote', 'payment'].indexOf(upgradeModal.step) > idx
                
                return (
                  <div key={s} className="text-center">
                    <span className={`inline-block rounded-full h-5 w-5 text-[10px] font-bold leading-5 ${
                      isActive ? 'bg-brand-primary text-white' : isCompleted ? 'bg-blue-100 text-brand-primary' : 'bg-slate-100 text-slate-400'
                    }`}>
                      {idx + 1}
                    </span>
                    <p className={`text-[8px] font-bold uppercase mt-1 ${isActive ? 'text-slate-800' : 'text-slate-400'}`}>
                      {stepLabels[idx]}
                    </p>
                  </div>
                )
              })}
            </div>

            {/* Step 1: Select */}
            {upgradeModal.step === 'select' && (
              <div className="space-y-4">
                <p className="text-xs text-slate-500 leading-normal">
                  Vui lï¿½ng l?a ch?n hạng ghế mu?n nï¿½ng c?p lï¿½n. Chï¿½ng tï¿½i s? ki?m tra ch? tr?ng vï¿½ bï¿½o phï¿½ chï¿½nh l?ch cho b?n.
                </p>

                <div className="space-y-2.5">
                  {[
                    { id: 1, name: 'Economy' },
                    { id: 2, name: 'Business' },
                  ].map((cls) => {
                    const currentClass = upgradeModal.ticket?.seatClass || ''
                    const isSelected = upgradeModal.selectedClassId === cls.id
                    const isCurrent = currentClass.toLowerCase() === cls.name.toLowerCase()

                    return (
                      <button
                        key={cls.id}
                        type="button"
                        disabled={isCurrent}
                        onClick={() => setUpgradeModal((prev) => ({ ...prev, selectedClassId: cls.id }))}
                        className={`w-full text-left rounded-2xl border p-4 transition-all ${
                          isCurrent
                            ? 'bg-slate-50 border-slate-200 opacity-60 cursor-not-allowed'
                            : isSelected
                            ? 'border-brand-primary bg-brand-primary/5 text-brand-primary'
                            : 'border-slate-200 hover:border-brand-primary/50'
                        }`}
                      >
                        <div className="flex justify-between items-center text-xs font-bold">
                          <span>{cls.name} Class</span>
                          {isCurrent ? (
                            <span className="text-[9px] font-bold text-slate-400">H?ng vï¿½ hi?n t?i</span>
                          ) : isSelected ? (
                            <span className="text-[9px] font-bold text-brand-primary">? Dï¿½ ch?n</span>
                          ) : null}
                        </div>
                      </button>
                    )
                  })}
                </div>

                <button
                  type="button"
                  disabled={!upgradeModal.selectedClassId || upgradeLoading}
                  onClick={handleGetUpgradeQuote}
                  className="w-full btn-primary py-2.5 text-xs font-bold flex justify-center items-center"
                >
                  {upgradeLoading ? 'Đang ki?m tra gh?...' : 'Ki?m tra chï¿½nh l?ch giï¿½ ?'}
                </button>
              </div>
            )}

            {/* Step 2: Quote */}
            {upgradeModal.step === 'quote' && upgradeModal.quote && (
              <div className="space-y-4">
                <h4 className="text-xs font-bold text-slate-800">Thï¿½ng tin chï¿½nh l?ch phï¿½</h4>
                
                <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 space-y-2 text-xs text-slate-600">
                  <div className="flex justify-between">
                    <span>Vï¿½ cu dï¿½ mua:</span>
                    <span className="font-bold">
                      {formatCurrency(upgradeModal.quote.currentTicketPrice || upgradeModal.quote.paidAmountOfOldTicket || 0)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span>Giï¿½ vï¿½ h?ng m?i:</span>
                    <span className="font-bold">
                      {formatCurrency(upgradeModal.quote.newClassPrice || upgradeModal.quote.newTicketAmount || 0)}
                    </span>
                  </div>
                  <div className="flex justify-between text-orange-600 font-bold">
                    <span>Chï¿½nh l?ch ch?ng:</span>
                    <span>
                      {formatCurrency(upgradeModal.quote.fareDifference || upgradeModal.quote.priceDifference || 0)}
                    </span>
                  </div>
                  {upgradeModal.quote.upgradeFee > 0 && (
                    <div className="flex justify-between text-orange-600 font-bold">
                      <span>Phï¿½ dịch vụ nï¿½ng h?ng:</span>
                      <span>{formatCurrency(upgradeModal.quote.upgradeFee)}</span>
                    </div>
                  )}
                  <div className="flex justify-between items-center border-t border-slate-200 pt-2 text-slate-800">
                    <span className="font-extrabold">T?ng ti?n nï¿½ng h?ng:</span>
                    <span className="font-extrabold text-brand-primary text-sm">
                      {formatCurrency(upgradeModal.quote.upgradeAmount || 0)}
                    </span>
                  </div>
                </div>

                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setUpgradeModal((prev) => ({ ...prev, step: 'select' }))}
                    className="btn-secondary py-2 text-xs font-bold flex-1"
                  >
                    Quay l?i
                  </button>
                  <button
                    type="button"
                    disabled={upgradeLoading}
                    onClick={handleCreateUpgradeRequest}
                    className="btn-primary py-2 text-xs font-bold flex-1"
                  >
                    {upgradeLoading ? 'Đang t?o yï¿½u c?u...' : 'T?o yï¿½u c?u nï¿½ng c?p'}
                  </button>
                </div>
              </div>
            )}

            {/* Step 3: Payment */}
            {upgradeModal.step === 'payment' && (
              <div className="space-y-4">
                <div className="rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 space-y-2 text-xs text-slate-600 text-center">
                  <div className="text-2xl mb-1"></div>
                  <p className="font-extrabold text-emerald-800 text-xs">T?o yï¿½u c?u nï¿½ng h?ng thï¿½nh cï¿½ng!</p>
                  <p className="text-[10px] text-slate-400">Yï¿½u c?u c?a b?n: #{upgradeModal.request?.requestId}</p>
                  {upgradeModal.request?.expiresAt && (
                    <p className="text-[9px] text-rose-500 font-bold">
                      H?n thanh toï¿½n: {new Date(upgradeModal.request.expiresAt).toLocaleString('vi-VN')}
                    </p>
                  )}
                  <div className="border-t border-emerald-100 pt-2.5 mt-2.5 flex justify-between items-center text-slate-700 font-bold">
                    <span>C?n thanh toï¿½n thï¿½m:</span>
                    <span className="text-brand-primary font-extrabold text-sm">
                      {formatCurrency(upgradeModal.request?.priceDifference || 0)}
                    </span>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={handleInitiateUpgradePayment}
                  className="w-full btn-primary py-2.5 text-xs font-bold flex items-center justify-center gap-1.5"
                >
                   Thanh toï¿½n chï¿½nh l?ch nï¿½ng h?ng
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Change Flight (Rebooking) Modal */}
      {changeFlightModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-150">
            <div className="flex items-start justify-between border-b border-slate-100 pb-3">
              <div>
                <h3 className="title-font text-base font-extrabold text-slate-800">
                  D?i ngï¿½y / D?i chuyến bay
                </h3>
                <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                  Booking ID: #{changeFlightModal.booking?.bookingId}
                </p>
              </div>
              <button
                type="button"
                onClick={closeChangeFlightModal}
                className="text-slate-400 hover:text-slate-700 text-lg font-bold"
              >
                ?
              </button>
            </div>

            {/* Steps indicator */}
            <div className="grid grid-cols-3 gap-2 border-b border-slate-50 pb-3.5">
              {['select-flight', 'quote', 'confirm'].map((s, idx) => {
                const stepLabels = ['Ch?n chuyến', 'Bï¿½o giï¿½ chï¿½nh l?ch', 'Hoï¿½n t?t']
                const isActive = changeFlightModal.step === s
                const isCompleted = ['select-flight', 'quote', 'confirm'].indexOf(changeFlightModal.step) > idx
                
                return (
                  <div key={s} className="text-center">
                    <span className={`inline-block rounded-full h-5 w-5 text-[10px] font-bold leading-5 ${
                      isActive ? 'bg-brand-primary text-white' : isCompleted ? 'bg-blue-100 text-brand-primary' : 'bg-slate-100 text-slate-400'
                    }`}>
                      {idx + 1}
                    </span>
                    <p className={`text-[8px] font-bold uppercase mt-1 ${isActive ? 'text-slate-800' : 'text-slate-400'}`}>
                      {stepLabels[idx]}
                    </p>
                  </div>
                )
              })}
            </div>

            {/* Step 1: Select new flight */}
            {changeFlightModal.step === 'select-flight' && (
              <div className="space-y-4">
                <div className="grid gap-3 sm:grid-cols-2 text-xs">
                  <div>
                    <label className="form-label text-[10px]">Ch?ng mu?n d?i</label>
                    <select
                      value={changeFlightModal.legType}
                      onChange={(e) => setChangeFlightModal((prev) => ({ ...prev, legType: Number(e.target.value), options: null }))}
                      className="form-input text-xs"
                    >
                      <option value={0}>Ch?ng di (Outbound)</option>
                      {!!(changeFlightModal.booking?.returnFlightNumber || changeFlightModal.booking?.returnFlightId) && (
                        <option value={1}>Ch?ng v? (Return)</option>
                      )}
                    </select>
                  </div>

                  <div>
                    <label className="form-label text-[10px]">Ch?n ngï¿½y bay m?i</label>
                    <input
                      type="date"
                      value={changeFlightModal.departureDate}
                      onChange={(e) => setChangeFlightModal((prev) => ({ ...prev, departureDate: e.target.value, options: null }))}
                      className="form-input text-xs"
                    />
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => handleGetChangeFlightOptions()}
                  className="w-full btn-secondary text-brand-primary border-brand-primary hover:bg-brand-primary/5 py-2.5 text-xs font-bold flex items-center justify-center"
                >
                   Tï¿½m chuyến bay cï¿½ s?n
                </button>

                {/* Candidate list options */}
                {changeFlightModal.options !== null && (
                  <div className="space-y-2">
                    <p className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                      {changeFlightModal.options.length > 0
                        ? `Tï¿½m th?y ${changeFlightModal.options.length} chuyến bay kh? d?ng:`
                        : 'Khï¿½ng tï¿½m th?y chuyến bay nï¿½o trong ngï¿½y dï¿½ ch?n.'}
                    </p>

                    <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                      {changeFlightModal.options.map((flight) => {
                        const isSel = changeFlightModal.selectedFlightId === flight.flightId
                        return (
                          <button
                            key={flight.flightId}
                            type="button"
                            onClick={() => setChangeFlightModal((prev) => ({ ...prev, selectedFlightId: flight.flightId }))}
                            className={`w-full text-left rounded-xl border p-3 text-xs transition-all ${
                              isSel ? 'border-brand-primary bg-brand-primary/5 text-brand-primary' : 'border-slate-100 hover:border-brand-primary/30'
                            }`}
                          >
                            <div className="flex justify-between items-center font-bold">
                              <span>{flight.flightNumber} {flight.aircraftModel ? `ï¿½ ${flight.aircraftModel}` : ''}</span>
                              <span className="text-[10px] text-slate-400 font-bold">{formatDuration ? formatDuration(flight.durationMinutes) : `${Math.floor((flight.durationMinutes||0)/60)}h${(flight.durationMinutes||0)%60}m`}</span>
                            </div>
                            <div className="flex justify-between items-center mt-1">
                              <p className="font-extrabold text-slate-700">
                                {formatTime(flight.departureTime)} - {formatTime(flight.arrivalTime)}
                              </p>
                              <p className={`font-extrabold text-sm ${isSel ? 'text-brand-primary' : 'text-emerald-600'}`}>
                                {formatCurrency ? formatCurrency(flight.unitFare) : `${(flight.unitFare||0).toLocaleString('vi-VN')}?`}
                              </p>
                            </div>
                            <p className="text-[10px] text-slate-400 mt-0.5">
                              Cï¿½n {flight.availableSeats ?? '?'} ch? tr?ng
                            </p>
                          </button>
                        )
                      })}
                    </div>
                  </div>
                )}

                {changeFlightModal.selectedFlightId && (
                  <button
                    type="button"
                    disabled={changeFlightLoading}
                    onClick={handleGetChangeFlightQuote}
                    className="w-full btn-primary py-2.5 text-xs font-bold flex items-center justify-center"
                  >
                    {changeFlightLoading ? 'Đang tï¿½nh chï¿½nh l?ch...' : 'Ti?p t?c ki?m tra bï¿½o giï¿½ ?'}
                  </button>
                )}
              </div>
            )}

            {/* Step 2: Quote change flight */}
            {changeFlightModal.step === 'quote' && changeFlightModal.quote && (
              <div className="space-y-4 animate-in fade-in duration-150">
                <h4 className="text-xs font-bold text-slate-800">Thï¿½ng tin bï¿½o giï¿½ d?i ch?ng</h4>
                
                <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 space-y-2 text-xs text-slate-600">
                  <div className="flex justify-between">
                    <span>Vï¿½ ch?ng cu:</span>
                    <span className="font-bold">{formatCurrency(changeFlightModal.quote.oldAmount || 0)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Vï¿½ ch?ng m?i:</span>
                    <span className="font-bold">{formatCurrency(changeFlightModal.quote.newAmount || 0)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Chï¿½nh l?ch vï¿½:</span>
                    <span className={`font-bold ${(changeFlightModal.quote.fareDifference || 0) >= 0 ? 'text-orange-600' : 'text-green-600'}`}>
                      {(changeFlightModal.quote.fareDifference || 0) >= 0 ? '+' : ''}
                      {formatCurrency(changeFlightModal.quote.fareDifference || 0)}
                    </span>
                  </div>
                  {changeFlightModal.quote.changeFee > 0 && (
                    <div className="flex justify-between text-orange-600 font-bold">
                      <span>Phï¿½ hï¿½nh chï¿½nh d?i vï¿½:</span>
                      <span>{formatCurrency(changeFlightModal.quote.changeFee)}</span>
                    </div>
                  )}
                  <div className="flex justify-between items-center border-t border-slate-200 pt-2 text-slate-800">
                    <span className="font-extrabold">T?ng chï¿½nh l?ch c?n thanh toï¿½n:</span>
                    <span className={`font-extrabold text-sm ${(changeFlightModal.quote.netAmount || 0) > 0 ? 'text-orange-600' : 'text-green-600'}`}>
                      {(changeFlightModal.quote.netAmount || 0) > 0 ? '+' : ''}
                      {formatCurrency(changeFlightModal.quote.netAmount || 0)}
                    </span>
                  </div>
                </div>

                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => setChangeFlightModal((prev) => ({ ...prev, step: 'select-flight' }))}
                    className="btn-secondary py-2 text-xs font-bold flex-1"
                  >
                    Quay l?i
                  </button>
                  <button
                    type="button"
                    disabled={changeFlightLoading}
                    onClick={handleConfirmChangeFlight}
                    className="btn-primary py-2 text-xs font-bold flex-1"
                  >
                    {changeFlightLoading ? 'Đang th?c hi?n d?i...' : 'Xï¿½c nh?n d?i vï¿½'}
                  </button>
                </div>
              </div>
            )}

            {/* Step 3: Confirmation */}
            {changeFlightModal.step === 'confirm' && (
              <div className="space-y-4 animate-in fade-in duration-150">
                <div className="rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 space-y-2 text-xs text-slate-600 text-center">
                  <div className="text-3xl mb-1"></div>
                  <p className="font-extrabold text-emerald-800 text-xs">Yï¿½u c?u d?i chuyến được ch?p nh?n!</p>
                  
                  {changeFlightModal.confirmResult?.paymentRequired ? (
                    <div className="pt-2 border-t border-emerald-100 mt-2 space-y-2">
                      <p className="text-[10px] text-slate-400">B?n c?n thanh toï¿½n chï¿½nh l?ch ch?ng bay tru?c khi vï¿½ m?i được xu?t.</p>
                      <button
                        type="button"
                        onClick={async () => {
                          const res = await initiatePayment(changeFlightModal.confirmResult.bookingId || changeFlightModal.booking.bookingId, 'VNPAY')
                          const paymentUrl = res?.paymentUrl || res?.paymentLink || res?.PaymentUrl || res?.payment?.paymentUrl
                          if (paymentUrl) window.location.href = paymentUrl
                        }}
                        className="w-full btn-primary py-2 text-xs font-bold"
                      >
                         Thanh toï¿½n chï¿½nh l?ch d?i vï¿½
                      </button>
                    </div>
                  ) : (
                    <p className="text-[10px] text-slate-400">Chuyến bay m?i dï¿½ được d?i mi?n phï¿½ thï¿½nh cï¿½ng. Vï¿½ di?n t? m?i dang được xu?t.</p>
                  )}
                </div>

                <button
                  type="button"
                  onClick={() => {
                    closeChangeFlightModal()
                    window.location.reload()
                  }}
                  className="w-full btn-secondary py-2.5 text-xs font-bold"
                >
                  Hoï¿½n t?t vï¿½ Quay l?i danh sï¿½ch vï¿½
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Auxiliary Add Services Modal inside History */}
      {showServicesModal && currentBookingForServices && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-150">
            <div className="flex items-start justify-between border-b border-slate-100 pb-3">
              <div>
                <h3 className="title-font text-base font-extrabold text-slate-800">
                  Thï¿½m dịch vụ ch?ng bay
                </h3>
                <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                  Booking ID: #{currentBookingForServices.bookingId} ï¿½ Mï¿½ ch?ng: {currentBookingForServices.flightNumber}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowServicesModal(false)
                  setCurrentBookingForServices(null)
                  setSelectedServicesByPassenger({})
                }}
                className="text-slate-400 hover:text-slate-700 text-lg font-bold"
              >
                ?
              </button>
            </div>

            <div className="space-y-4">
              {currentBookingForServices.passengers.map((passenger) => {
                const passengerName = [passenger.lastName, passenger.firstName].filter(Boolean).join(' ')
                const passengerId = passenger.passengerId
                const passengerSelection = selectedServicesByPassenger[passengerId] || {}

                return (
                  <div key={passengerId} className="rounded-2xl border border-slate-100 bg-slate-50 p-4 space-y-3">
                    <h4 className="text-xs font-extrabold text-slate-800">{passengerName}</h4>
                    
                    <div className="space-y-2">
                      {services.map((service) => {
                        const serviceId = service.serviceId || service.id
                        const currentQty = passengerSelection[serviceId] || 0
                        
                        return (
                          <div
                            key={serviceId}
                            className="flex items-center justify-between rounded-xl bg-white border border-slate-100 p-3 text-xs"
                          >
                            <div>
                              <p className="font-bold text-slate-800">{service.serviceName}</p>
                              <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                                {getServicePriceLabel(service.price)}
                              </p>
                            </div>
                            <div className="flex items-center gap-2">
                              <button
                                type="button"
                                disabled={currentQty <= 0}
                                onClick={() => {
                                  setSelectedServicesByPassenger((prev) => ({
                                    ...prev,
                                    [passengerId]: {
                                      ...passengerSelection,
                                      [serviceId]: currentQty - 1,
                                    },
                                  }))
                                }}
                                className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50 disabled:opacity-40"
                              >
                                ?
                              </button>
                              <span className="w-5 text-center text-xs font-bold text-slate-800">{currentQty}</span>
                              <button
                                type="button"
                                onClick={() => {
                                  setSelectedServicesByPassenger((prev) => ({
                                    ...prev,
                                    [passengerId]: {
                                      ...passengerSelection,
                                      [serviceId]: currentQty + 1,
                                    },
                                  }))
                                }}
                                className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50"
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

            <div className="border-t border-slate-100 pt-4 flex items-center justify-between">
              <div>
                <p className="text-[10px] text-slate-400 font-bold uppercase">T?ng phï¿½ dịch vụ</p>
                <p className="text-lg font-extrabold text-brand-primary mt-0.5">
                  {formatCurrency(
                    Object.entries(selectedServicesByPassenger).reduce((total, [pId, selections]) => {
                      return (
                        total +
                        Object.entries(selections || {}).reduce((sum, [sId, qty]) => {
                          const service = services.find((s) => (s.serviceId || s.id) === Number(sId))
                          return sum + (service?.price || 0) * Number(qty || 0)
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
                    const passengers = currentBookingForServices?.passengers || []
                    
                    let addedCount = 0
                    for (const passenger of passengers) {
                      const passengerIdValue = Number(passenger?.passengerId)
                      const selections = selectedServicesByPassenger[passengerIdValue] || {}
                      for (const [serviceId, quantity] of Object.entries(selections)) {
                        if (quantity > 0) {
                          await addServiceToBooking(bookingIdValue, passengerIdValue, Number(serviceId), quantity)
                          addedCount += quantity
                        }
                      }
                    }

                    if (addedCount > 0) {
                      setHistoryNotice(`? Dï¿½ thï¿½m thï¿½nh cï¿½ng ${addedCount} dịch vụ ph? tr? vï¿½o vï¿½ c?a b?n!`)
                    }
                    setShowServicesModal(false)
                    setCurrentBookingForServices(null)
                    setSelectedServicesByPassenger({})
                  } catch (err) {
                    setHistoryError(err.message || 'Lỗi thï¿½m dịch vụ')
                  }
                }}
                className="btn-success px-5 py-2.5 text-xs font-bold rounded-xl"
              >
                ? Xï¿½c nh?n thï¿½m dịch vụ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Disruption Rebook Modal */}
      {rebookModalState.isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-150">
            <div className="flex items-start justify-between border-b border-slate-100 pb-3">
              <div>
                <h3 className="title-font text-base font-extrabold text-slate-800">
                  D?i chuyến bay h? tr? s? c?
                </h3>
                <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                  Quy?t d?nh s? c? ID: #{rebookModalState.decisionId}
                </p>
              </div>
              <button
                type="button"
                onClick={closeRebookModal}
                className="text-slate-400 hover:text-slate-700 text-lg font-bold"
              >
                ?
              </button>
            </div>

            {(() => {
              const decision = getDisruptionDecisionOptions(
                rebookModalState.bookingId,
                rebookModalState.decisionId
              )
              const allOptions = Array.isArray(decision?.flightOptions) ? decision.flightOptions : []
              const filteredOptions = filterDisruptionFlightsByDate(allOptions, rebookModalState.date)

              return (
                <div className="space-y-4">
                  <div>
                    <label className="form-label text-xs">Ch?n ngï¿½y kh?i hï¿½nh m?i</label>
                    <input
                      type="date"
                      value={rebookModalState.date}
                      onChange={async (e) => {
                        const nextDate = e.target.value
                        const bId = Number(rebookModalState.bookingId)
                        const dId = Number(rebookModalState.decisionId)

                        setRebookModalState((prev) => ({ ...prev, date: nextDate, selectedFlightId: '' }))

                        if (!nextDate || !Number.isFinite(bId)) return

                        try {
                          const options = await getDisruptionOptions(bId, nextDate)
                          const matchedDecision = Array.isArray(options)
                            ? options.find((opt) => Number(opt?.decisionId) === dId)
                            : null
                          const flightOpts = Array.isArray(matchedDecision?.flightOptions) ? matchedDecision.flightOptions : []
                          const firstMatch = flightOpts[0]

                          setRebookModalState((prev) => ({
                            ...prev,
                            selectedFlightId: firstMatch?.flightId ? String(firstMatch.flightId) : '',
                          }))
                        } catch (err) {
                          // ignore errors
                        }
                      }}
                      className="form-input text-xs max-w-xs"
                    />
                  </div>

                  <div className="space-y-2">
                    <p className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Cï¿½c chuyến bay mi?n phï¿½ thay th?:</p>

                    <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                      {filteredOptions.map((flight) => {
                        const isSel = String(flight.flightId) === rebookModalState.selectedFlightId
                        return (
                          <div
                            key={flight.flightId}
                            className={`flex flex-col sm:flex-row justify-between gap-4 items-start sm:items-center rounded-2xl border p-4 bg-white transition-all ${
                              isSel ? 'border-brand-primary bg-brand-primary/5' : 'border-slate-100 hover:border-brand-primary/30'
                            }`}
                          >
                            <div className="text-xs">
                              <p className="font-extrabold text-brand-primary">
                                {flight.flightNumber || `Chuyến #${flight.flightId}`}
                              </p>
                              <p className="mt-1 font-bold text-slate-800 text-sm">
                                {formatTime(flight.departureTime)} - {formatTime(flight.arrivalTime)}
                              </p>
                              <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                                Gi? bay: {formatDateTime(flight.departureTime)}
                              </p>
                              <p className="text-[10px] text-slate-400 font-bold">
                                Gh? tr?ng: {flight.availableSeats} gh?
                              </p>
                            </div>

                            <button
                              type="button"
                              onClick={() => setRebookModalState((prev) => ({ ...prev, selectedFlightId: String(flight.flightId) }))}
                              className={`btn-primary px-4 py-2 text-[10px] font-bold rounded-lg ${
                                isSel ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-brand-primary'
                              }`}
                            >
                              {isSel ? 'Đang ch?n' : 'D?i chuyến nï¿½y'}
                            </button>
                          </div>
                        )
                      })}

                      {filteredOptions.length === 0 && (
                        <p className="text-xs text-slate-400 text-center py-6">Khï¿½ng cï¿½ chuyến bay thay th? nï¿½o trong ngï¿½y dï¿½ ch?n.</p>
                      )}
                    </div>
                  </div>

                  <div className="flex gap-2 pt-4 border-t border-slate-100">
                    <button
                      type="button"
                      onClick={closeRebookModal}
                      className="btn-secondary py-2 text-xs font-bold flex-1"
                    >
                      H?y
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
                      className="btn-primary py-2 text-xs font-bold flex-1"
                    >
                      Xï¿½c nh?n d?i chuyến mi?n phï¿½
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
}
