import { useMemo, useState } from 'react'

export default function FlightSearchResults({
  filters,
  setFilters,
  tripType,
  roundtripStep,
  dateOptions,
  currentListDate,
  lowestPriceForCurrentDate,
  isLoadingFlights,
  performSearch,
  loadReturnFlights,
  searchData,
  setSearchData,
  passengerDivisor,
  filteredFlights,
  outboundFlights,
  setSelectedFlight,
  setReturnFlight,
  setRoundtripStep,
  loadServices,
  setScreen,
  seatClassMap,
  setServiceError,
  setServices,
  formatCurrency,
  formatTime,
  formatDuration,
  getFlightId,
  getFlightLabel,
  nextAfterFlightSelect = 'passenger',
}) {
  const [expandedFlightId, setExpandedFlightId] = useState(null)
  const [detailTabs, setDetailTabs] = useState({})

  const resolveFlightKey = (flight) => getFlightId(flight) || flight?.flightNumber || flight?.departureTime

  const handleSelectFlight = (flight) => {
    const seatClassId = seatClassMap[searchData.seatClass]
    if (seatClassId) {
      loadServices(seatClassId)
    } else {
      setServiceError('Không xac dinh duoc hang ghe de tai dich vu.')
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
      setScreen(nextAfterFlightSelect)
    }
  }

  const stepText = useMemo(() => {
    if (tripType === 'roundtrip') {
      return roundtripStep === 'outbound'
        ? 'Bu?c 1: Ch?n chuyến bay di'
        : 'Bu?c 2: Ch?n chuyến bay v?'
    }
    return 'Ch?n chuyến bay c?a b?n'
  }, [tripType, roundtripStep])

  const toggleDetails = (flight) => {
    const key = resolveFlightKey(flight)
    setExpandedFlightId((prev) => (prev === key ? null : key))
    setDetailTabs((prev) => ({ ...prev, [key]: prev[key] || 'flight' }))
  }

  const getSeatClassBenefits = (seatClass) => {
    const normalized = String(seatClass || '').toLowerCase()
    if (normalized === 'business') {
      return {
        carryOn: '12 Kg',
        checkedBag: '30 Kg',
        services: ['Uu tiï¿½n check-in', 'Uu tiï¿½n lï¿½n mï¿½y bay', 'Su?t an cao c?p', 'Gh? r?ng hon'],
      }
    }
    return {
      carryOn: '7 Kg',
      checkedBag: '20 Kg',
      services: ['Check-in tiï¿½u chu?n', 'Su?t an tiï¿½u chu?n'],
    }
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[280px_1fr] animate-in fade-in duration-300">
      <aside className="glass-panel h-fit rounded-3xl p-5 shadow-xl">
        <div className="mb-4 flex items-center justify-between border-b border-slate-100 pb-3">
          <h3 className="title-font text-base font-extrabold text-slate-800">B? l?c tï¿½m ki?m</h3>
          <button
            type="button"
            onClick={() => setFilters({ maxPrice: 3000000, timeSlot: 'all', seatClass: 'all' })}
            className="text-[11px] font-bold text-brand-primary hover:underline focus:outline-none"
          >
            D?t l?i
          </button>
        </div>

        <div className="space-y-5">
          <div>
            <label className="form-label flex items-center justify-between">
              <span>Giï¿½ t?i da</span>
              <span className="text-xs font-extrabold text-brand-primary">{formatCurrency(filters.maxPrice)}</span>
            </label>
            <input
              type="range"
              min="1000000"
              max="6000000"
              step="100000"
              value={filters.maxPrice}
              onChange={(e) => setFilters((prev) => ({ ...prev, maxPrice: Number(e.target.value) }))}
              className="w-full cursor-pointer accent-brand-primary"
            />
          </div>

          <div>
            <label className="form-label">Khung gi? c?t cï¿½nh</label>
            <select
              value={filters.timeSlot}
              onChange={(e) => setFilters((prev) => ({ ...prev, timeSlot: e.target.value }))}
              className="form-input"
            >
              <option value="all">T?t c? th?i gian</option>
              <option value="morning">Sï¿½ng (00:00 - 11:59)</option>
              <option value="afternoon">Chi?u (12:00 - 17:59)</option>
              <option value="evening">T?i (18:00 - 23:59)</option>
            </select>
          </div>
        </div>
      </aside>

      <section className="space-y-4">
        <div className="rounded-2xl border border-slate-100 bg-white px-5 py-4 shadow-sm">
          <div className="flex items-center gap-2.5">
            <span className="flex h-6 w-6 items-center justify-center rounded-full bg-brand-primary text-[11px] font-bold text-white">
              {tripType === 'roundtrip' && roundtripStep === 'return' ? '2' : '1'}
            </span>
            <span className="text-sm font-extrabold text-slate-800">{stepText}</span>
          </div>
        </div>

        {dateOptions.length > 0 && (
          <div className="-mx-2 flex gap-3 overflow-x-auto px-2 pb-2">
            {dateOptions.map((option) => {
              const isActive = option.value === currentListDate
              const priceLabel = isActive && Number.isFinite(lowestPriceForCurrentDate)
                ? formatCurrency(lowestPriceForCurrentDate)
                : 'Xem giï¿½'

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
                  className={`min-w-[130px] rounded-2xl border p-3.5 text-left transition-all duration-fast ${
                    isActive
                      ? 'border-brand-primary bg-brand-primary/5 text-brand-primary ring-2 ring-brand-primary/10'
                      : 'border-slate-200 bg-white text-slate-600 hover:border-brand-primary/50'
                  } ${isLoadingFlights ? 'opacity-50' : ''}`}
                >
                  <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{option.weekday}</p>
                  <p className="mt-1 text-xs font-extrabold text-slate-800">{option.label}</p>
                  <p className="mt-2 text-xs font-extrabold text-brand-primary">{priceLabel}</p>
                </button>
              )
            })}
          </div>
        )}

        {isLoadingFlights ? (
          <div className="flex flex-col items-center justify-center rounded-3xl border border-slate-100 bg-white py-20 shadow-sm">
            <span className="mt-4 text-xs font-bold text-slate-500">Đang tï¿½m ki?m chuyến bay phï¿½ h?p...</span>
          </div>
        ) : (
          <>
            {filteredFlights.map((flight) => {
              const flightKey = resolveFlightKey(flight)
              const isExpanded = expandedFlightId === flightKey
              const activeTab = detailTabs[flightKey] || 'flight'
              const flightPrice = (flight.pricesByClass?.[searchData.seatClass] || 0) / passengerDivisor
              const benefits = getSeatClassBenefits(searchData.seatClass)

              return (
                <article
                  key={flightKey}
                  className="glass-panel rounded-3xl p-6 shadow-md transition-all duration-fast hover:-translate-y-[2px] hover:shadow-lg"
                >
                  <div className="flex flex-col justify-between gap-6 md:flex-row md:items-center">
                    <div className="flex items-center gap-4">
                      <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-primary/5 text-brand-primary">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.8" stroke="currentColor" className="h-6 w-6">
                          <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
                        </svg>
                      </div>
                      <div>
                        <span className="text-xs font-extrabold uppercase tracking-wider text-brand-primary">{flight.airlineCode || 'Airline'}</span>
                        <h4 className="mt-0.5 text-sm font-extrabold text-slate-800">S? hi?u: {getFlightLabel(flight)}</h4>
                        <span className="block text-[10px] font-medium text-slate-400">Mï¿½y bay: {flight.aircraftModel || '---'} ï¿½ {searchData.seatClass}</span>
                      </div>
                    </div>

                    <div className="flex max-w-sm flex-1 items-center justify-between">
                      <div className="text-left">
                        <span className="block text-base font-extrabold text-slate-800">{formatTime(flight.departureTime)}</span>
                        <span className="mt-0.5 block text-[10px] font-medium text-slate-400">{flight.departureAirport || 'Airport'}</span>
                      </div>
                      <div className="relative flex-1 px-4 text-center">
                        <span className="mb-1 block text-[10px] font-bold text-slate-400">{formatDuration(flight.durationMinutes)}</span>
                        <div className="relative h-[2px] w-full bg-slate-200">
                          <div className="absolute right-0 top-1/2 h-1.5 w-1.5 -translate-y-1/2 rounded-full bg-brand-primary" />
                        </div>
                        <span className="mt-1 block text-[9px] font-bold text-brand-tertiary">Bay th?ng</span>
                      </div>
                      <div className="text-right">
                        <span className="block text-base font-extrabold text-slate-800">{formatTime(flight.arrivalTime)}</span>
                        <span className="mt-0.5 block text-[10px] font-medium text-slate-400">{flight.arrivalAirport || 'Airport'}</span>
                      </div>
                    </div>

                    <div className="flex items-center justify-between gap-3 border-t border-slate-100 pt-4 md:justify-center md:border-t-0 md:pt-0 md:text-right md:flex-col">
                      <div>
                        <span className="block text-[10px] font-bold text-slate-400">Giï¿½ cho 1 hï¿½nh khï¿½ch</span>
                        <span className="block text-lg font-extrabold text-slate-800 md:mt-0.5">{formatCurrency(flightPrice)}</span>
                      </div>
                      <button
                        type="button"
                        onClick={() => toggleDetails(flight)}
                        className="text-xs font-bold text-brand-primary hover:underline"
                      >
                        {isExpanded ? '?n chi ti?t' : 'Xem chi ti?t'}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleSelectFlight(flight)}
                        className="btn-primary rounded-xl px-6 py-2.5 text-xs font-bold shadow-md shadow-blue-100"
                      >
                        Ch?n chuyến bay
                      </button>
                    </div>
                  </div>

                  {isExpanded && (
                    <div className="mt-5 rounded-2xl border border-slate-200 bg-white p-4">
                      <div className="mb-4 inline-flex rounded-full bg-slate-100 p-1">
                        {[
                          { key: 'flight', label: 'Chuyến bay' },
                          { key: 'fare', label: 'Giï¿½ vï¿½' },
                          { key: 'rules', label: 'Di?u ki?n vï¿½' },
                        ].map((tab) => (
                          <button
                            key={tab.key}
                            type="button"
                            onClick={() => setDetailTabs((prev) => ({ ...prev, [flightKey]: tab.key }))}
                            className={`rounded-full px-4 py-1.5 text-xs font-bold transition ${
                              activeTab === tab.key ? 'bg-brand-primary text-white' : 'text-slate-600 hover:text-brand-primary'
                            }`}
                          >
                            {tab.label}
                          </button>
                        ))}
                      </div>

                      {activeTab === 'flight' && (
                        <div className="grid gap-3 md:grid-cols-[1fr_auto]">
                          <div className="space-y-1 text-sm text-slate-700">
                            <p className="font-bold text-slate-900">{flight.airlineCode || 'Airline'} - {getFlightLabel(flight)}</p>
                            <p>{formatTime(flight.departureTime)} - {flight.departureAirport || '---'} ? {formatTime(flight.arrivalTime)} - {flight.arrivalAirport || '---'}</p>
                            <p className="text-xs text-slate-500">Th?i lu?ng: {formatDuration(flight.durationMinutes)} ï¿½ Mï¿½y bay: {flight.aircraftModel || '---'}</p>
                          </div>
                          <div className="text-sm text-slate-700">
                            <p>Hï¿½nh lï¿½ xï¿½ch tay: <strong>{benefits.carryOn}</strong></p>
                            <p>Hï¿½nh lï¿½ kï¿½ g?i: <strong>{benefits.checkedBag}</strong></p>
                            <p className="text-xs text-slate-500">Quy?n l?i thay d?i theo hạng ghế dï¿½ ch?n.</p>
                          </div>
                        </div>
                      )}

                      {activeTab === 'fare' && (
                        <div className="space-y-2 text-sm text-slate-700">
                          <div className="flex items-center justify-between">
                            <span>Giï¿½ vï¿½ co b?n ({searchData.seatClass})</span>
                            <strong>{formatCurrency(flightPrice)}</strong>
                          </div>
                          <div className="flex items-center justify-between text-slate-500">
                            <span>Thu? vï¿½ phï¿½</span>
                            <span>Dï¿½ g?m trong giï¿½ hi?n th?</span>
                          </div>
                          <div className="rounded-xl bg-blue-50 px-3 py-2 text-xs text-blue-700">
                            {searchData.seatClass === 'Business'
                              ? 'H?ng Business bao g?m nhi?u quy?n l?i dịch vụ hon so v?i Economy.'
                              : 'B?n cï¿½ th? ch?n h?ng Business d? nh?n thï¿½m quy?n l?i dịch vụ.'}
                          </div>
                        </div>
                      )}

                      {activeTab === 'rules' && (
                        <div className="space-y-2 text-sm text-slate-700">
                          <p className="font-semibold text-slate-800">Dịch vụ bao g?m theo h?ng {searchData.seatClass}:</p>
                          <ul className="list-disc space-y-1 pl-5 text-sm text-slate-700">
                            {benefits.services.map((item) => (
                              <li key={`${flightKey}-${item}`}>{item}</li>
                            ))}
                          </ul>
                          <p>D?i vï¿½: ï¿½p d?ng theo di?u ki?n hạng ghế vï¿½ quy d?nh hï¿½ng.</p>
                          <p>Hoï¿½n/h?y vï¿½: cï¿½ th? phï¿½t sinh phï¿½ theo th?i di?m x? lï¿½.</p>
                          <p className="rounded-xl bg-rose-50 px-3 py-2 text-xs text-rose-600">Vui lï¿½ng ki?m tra di?u ki?n cu?i cï¿½ng t?i bu?c thanh toï¿½n.</p>
                        </div>
                      )}
                    </div>
                  )}
                </article>
              )
            })}

            {!isLoadingFlights && filteredFlights.length === 0 && outboundFlights.length > 0 && (
              <div className="rounded-3xl border border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
                <span className="text-xs font-bold">Khï¿½ng tï¿½m th?y chuyến bay phï¿½ h?p v?i b? l?c hi?n t?i.</span>
              </div>
            )}

            {!isLoadingFlights && outboundFlights.length === 0 && (
              <div className="rounded-3xl border border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
                <span className="text-xs font-bold">Vui lï¿½ng th?c hi?n tï¿½m ki?m chuyến bay tru?c.</span>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  )
}
