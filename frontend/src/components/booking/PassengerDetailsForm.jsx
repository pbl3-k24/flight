import { useEffect, useState } from 'react'

export default function PassengerDetailsForm({
  passengerForms,
  setPassengerForms,
  savedPassengers,
  applySavedPassengerToForm,
  services,
  isLoadingServices,
  serviceError,
  selectedServicesByPassengerDraft,
  setSelectedServicesByPassengerDraft,
  availablePromotions,
  selectedPromotionId,
  setSelectedPromotionId,
  isLoadingPromotion,
  appliedPromotion,
  getPromotionDisplayText,
  getPromotionId,
  tripType,
  selectedFlight,
  returnFlight,
  searchData,
  passengerDivisor,
  performBookingCreation,
  setApiError,
  apiError,
  isLoadingBooking,
  formatCurrency,
  formatTime,
  formatDateTime,
  getFlightLabel,
  getAgeFromDob,
  formatRouteLabel,
  authUser
}) {
  const [localValidationMessage, setLocalValidationMessage] = useState('')

  const getServicePriceLabel = (price) => {
    return price > 0 ? `+ ${formatCurrency(price)}` : 'Mi?n phï¿½'
  }

  const updateServiceDraft = (leg, passengerIndex, serviceId, quantity) => {
    setSelectedServicesByPassengerDraft((prev) => {
      const nextLeg = { ...prev[leg] }
      nextLeg[passengerIndex] = {
        ...nextLeg[passengerIndex],
        [serviceId]: quantity,
      }
      return {
        ...prev,
        [leg]: nextLeg,
      }
    })
  }

  const calculateTotalBaggageMealCost = () => {
    let cost = 0
    passengerForms.forEach((passenger, idx) => {
      if (passenger.type === 'infant') return
      
      const outboundSvc = selectedServicesByPassengerDraft?.outbound?.[idx] || {}
      Object.entries(outboundSvc).forEach(([svcId, qty]) => {
        const match = services.find((s) => (s.serviceId || s.id) === Number(svcId))
        if (match && qty > 0) cost += match.price * qty
      })

      if (tripType === 'roundtrip') {
        const returnSvc = selectedServicesByPassengerDraft?.return?.[idx] || {}
        Object.entries(returnSvc).forEach(([svcId, qty]) => {
          const match = services.find((s) => (s.serviceId || s.id) === Number(svcId))
          if (match && qty > 0) cost += match.price * qty
        })
      }
    })
    return cost
  }

  const baseOutboundPrice = selectedFlight ? (selectedFlight.pricesByClass?.[searchData.seatClass] || 0) : 0
  const baseReturnPrice = (tripType === 'roundtrip' && returnFlight) ? (returnFlight.pricesByClass?.[searchData.seatClass] || 0) : 0
  
  // Calculate fare breakdown
  const calculateFareBreakdown = () => {
    let outboundTotal = 0
    let returnTotal = 0
    
    passengerForms.forEach((p) => {
      const rate = p.type === 'infant' ? 0 : p.type === 'child' ? 0.75 : 1 // Child fare is 75%
      outboundTotal += baseOutboundPrice * rate
      returnTotal += baseReturnPrice * rate
    })
    return { outboundTotal, returnTotal }
  }

  const { outboundTotal, returnTotal } = calculateFareBreakdown()
  const servicesCost = calculateTotalBaggageMealCost()
  const subtotal = outboundTotal + returnTotal + servicesCost
  
  // Apply discount if promotion is selected
  const discount = appliedPromotion ? (() => {
    const value = appliedPromotion.discountValue || 0
    if (appliedPromotion.discountType === 'Percentage' || appliedPromotion.discountType === 0) {
      const calc = (subtotal * value) / 100
      return appliedPromotion.maxDiscountAmount && calc > appliedPromotion.maxDiscountAmount
        ? appliedPromotion.maxDiscountAmount
        : calc
    }
    return value
  })() : 0

  const finalCost = Math.max(0, subtotal - discount)

  const handleBookingSubmit = async (e) => {
    e.preventDefault()
    setLocalValidationMessage('')
    setApiError('')

    // Client-side validations
    if (tripType === 'roundtrip' && !returnFlight) {
      setLocalValidationMessage('Vui lï¿½ng ch?n chuyến bay kh? h?i tru?c khi ti?p t?c.')
      return
    }

    if (passengerForms.length === 0) {
      setLocalValidationMessage('Danh sï¿½ch hï¿½nh khï¿½ch tr?ng.')
      return
    }

    let hasAdult = false
    let hasValidationError = false

    passengerForms.forEach((passenger, idx) => {
      if (!passenger.fullName.trim()) {
        hasValidationError = true
      }
      if (!passenger.dob) {
        hasValidationError = true
      }
      
      const age = getAgeFromDob(passenger.dob)
      if (age === null || age < 0) {
        hasValidationError = true
        return
      }

      if (age >= 14) {
        hasAdult = true
      }

      if (passenger.type === 'adult') {
        if (age < 12) {
          hasValidationError = true
        }
        if (!passenger.document || !passenger.email || !passenger.phone) {
          hasValidationError = true
        }
      } else if (passenger.type === 'child') {
        if (age < 2 || age > 12) {
          hasValidationError = true
        }
      } else if (passenger.type === 'infant') {
        if (age >= 2) {
          hasValidationError = true
        }
      }
    })

    if (hasValidationError) {
      setLocalValidationMessage('Vui lï¿½ng di?n dï¿½ng vï¿½ d?y d? thï¿½ng tin c?a t?t c? hï¿½nh khï¿½ch.')
      return
    }

    if (!hasAdult) {
      setLocalValidationMessage('Giao d?ch ph?i cï¿½ ï¿½t nh?t m?t hï¿½nh khï¿½ch t? 14 tu?i tr? lï¿½n di kï¿½m.')
      return
    }

    if (!authUser?.email) {
      setLocalValidationMessage('Vui lï¿½ng đăng nhập tï¿½i kho?n tru?c khi th?c hi?n d?t vï¿½.')
      return
    }

    // Validation success, trigger parent booking creator
    performBookingCreation()
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_340px] animate-in fade-in duration-300">
      {/* Left Form Column */}
      <section className="space-y-6">
        <div className="glass-panel rounded-3xl p-6 shadow-xl">
          <div className="flex items-center gap-3 border-b border-slate-100 pb-4 mb-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-primary/10 text-brand-primary">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-6 w-6">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 9h3.75M15 12h3.75M15 15h3.75M4.5 19.5h15a2.25 2.25 0 002.25-2.25V6.75A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25v10.5A2.25 2.25 0 004.5 19.5zm6-10.125a1.875 1.875 0 11-3.75 0 1.875 1.875 0 013.75 0zm1.294 6.336a6.721 6.721 0 00-6.088 0A2.25 2.25 0 003 17.935v.065h9v-.065a2.25 2.25 0 00-1.206-1.999z" />
              </svg>
            </div>
            <div>
              <h2 className="title-font text-lg font-extrabold text-slate-800">Thï¿½ng tin hï¿½nh khï¿½ch</h2>
              <p className="text-xs text-slate-400">Vui lï¿½ng cung c?p thï¿½ng tin trï¿½ng kh?p v?i tï¿½i li?u cï¿½ nhï¿½n</p>
            </div>
          </div>

          <form onSubmit={handleBookingSubmit} className="space-y-6">
            {passengerForms.map((passenger, index) => {
              const isAdult = passenger.type === 'adult'
              const isChild = passenger.type === 'child'
              const isInfant = passenger.type === 'infant'
              
              return (
                <div
                  key={`passenger-${index}`}
                  className="rounded-2xl border border-slate-200 p-5 bg-white space-y-4 hover:border-brand-primary/30 transition duration-instant"
                >
                  <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                    <span className="text-xs font-extrabold text-slate-800">
                      Hï¿½NH KHï¿½CH {index + 1}
                    </span>
                    <span className={`rounded-full px-3 py-1 text-[10px] font-bold ${
                      isAdult ? 'bg-blue-50 text-blue-700' : isChild ? 'bg-orange-50 text-orange-700' : 'bg-emerald-50 text-emerald-700'
                    }`}>
                      {isAdult ? 'Ngu?i l?n (>= 12 tu?i)' : isChild ? 'Tr? em (2 - 12 tu?i)' : 'Em bï¿½ (< 2 tu?i)'}
                    </span>
                  </div>

                  {/* Apply Saved Passenger */}
                  {savedPassengers.length > 0 && (
                    <div>
                      <label className="form-label text-xs">Di?n nhanh t? danh b?</label>
                      <select
                        value={passenger.savedPassengerId || ''}
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
                        className="form-input text-xs"
                      >
                        <option value="">-- Ch?n thï¿½nh viï¿½n danh b? --</option>
                        {savedPassengers.map((item) => (
                          <option key={item.id} value={item.id}>
                            {[item.lastName, item.firstName].filter(Boolean).join(' ')} {item.email ? `(${item.email})` : ''}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}

                  {/* Fields Grid */}
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="sm:col-span-2">
                      <label className="form-label text-xs">H? vï¿½ tï¿½n (ch? in hoa khï¿½ng d?u)</label>
                      <input
                        type="text"
                        required
                        placeholder="NGUYEN VAN A"
                        value={passenger.fullName}
                        onChange={(e) =>
                          setPassengerForms((prev) =>
                            prev.map((item, idx) =>
                              idx === index ? { ...item, fullName: e.target.value.toUpperCase() } : item
                            )
                          )
                        }
                        className="form-input"
                      />
                    </div>

                    <div>
                      <label className="form-label text-xs">Ngï¿½y sinh</label>
                      <input
                        type="date"
                        required
                        value={passenger.dob}
                        onChange={(e) =>
                          setPassengerForms((prev) =>
                            prev.map((item, idx) =>
                              idx === index ? { ...item, dob: e.target.value } : item
                            )
                          )
                        }
                        className="form-input"
                      />
                    </div>

                    <div>
                      <label className="form-label text-xs">Gi?i tï¿½nh</label>
                      <div className="flex gap-4 pt-3.5">
                        {['Nam', 'N?', 'Khï¿½c'].map((g) => (
                          <label key={g} className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-700 cursor-pointer">
                            <input
                              type="radio"
                              name={`gender-${index}`}
                              value={g}
                              checked={passenger.gender === g}
                              onChange={(e) =>
                                setPassengerForms((prev) =>
                                  prev.map((item, idx) =>
                                    idx === index ? { ...item, gender: e.target.value } : item
                                  )
                                )
                              }
                              className="accent-brand-primary h-4 w-4"
                            />
                            <span>{g}</span>
                          </label>
                        ))}
                      </div>
                    </div>

                    {isAdult && (
                      <div className="sm:col-span-2">
                        <label className="form-label text-xs">S? CCCD / H? chi?u</label>
                        <input
                          type="text"
                          required
                          placeholder="Nh?p s? can cu?c cï¿½ng dï¿½n ho?c s? h? chi?u"
                          value={passenger.document}
                          onChange={(e) =>
                            setPassengerForms((prev) =>
                              prev.map((item, idx) =>
                                idx === index ? { ...item, document: e.target.value } : item
                              )
                            )
                          }
                          className="form-input"
                        />
                      </div>
                    )}

                    {isAdult && (
                      <>
                        <div>
                          <label className="form-label text-xs">D?a ch? Email liï¿½n h?</label>
                          <input
                            type="email"
                            required
                            placeholder="email@example.com"
                            value={passenger.email}
                            onChange={(e) =>
                              setPassengerForms((prev) =>
                                prev.map((item, idx) =>
                                  idx === index ? { ...item, email: e.target.value } : item
                                )
                              )
                            }
                            className="form-input"
                          />
                        </div>
                        <div>
                          <label className="form-label text-xs">S? di?n tho?i liï¿½n l?c</label>
                          <input
                            type="tel"
                            required
                            placeholder="0901234567"
                            value={passenger.phone}
                            onChange={(e) =>
                              setPassengerForms((prev) =>
                                prev.map((item, idx) =>
                                  idx === index ? { ...item, phone: e.target.value } : item
                                )
                              )
                            }
                            className="form-input"
                          />
                        </div>
                      </>
                    )}

                    {/* Auxiliary Services (Not for Infant) */}
                    {!isInfant && (
                      <div className="sm:col-span-2 space-y-3 pt-2">
                        {['outbound', tripType === 'roundtrip' ? 'return' : null]
                          .filter(Boolean)
                          .map((leg) => (
                            <div
                              key={`${index}-${leg}`}
                              className="rounded-xl border border-slate-100 bg-slate-50/50 p-4"
                            >
                              <p className="mb-3 text-xs font-bold text-slate-800 flex items-center gap-1.5">
                                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4 w-4 text-brand-primary">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z" />
                                </svg>
                                <span>Dịch vụ ch?ng {leg === 'outbound' ? 'di' : 'v?'}</span>
                              </p>
                              
                              {serviceError && (
                                <p className="mb-2 text-xs font-bold text-rose-600">{serviceError}</p>
                              )}
                              {isLoadingServices && (
                                <p className="text-xs text-slate-400">Đang tải danh sï¿½ch dịch vụ...</p>
                              )}
                              {!isLoadingServices && services.length === 0 && (
                                <p className="text-xs text-slate-400">Khï¿½ng cï¿½ dịch vụ kh? d?ng cho ch?ng bay nï¿½y.</p>
                              )}
                              {!isLoadingServices && services.length > 0 && (
                                <div className="space-y-2">
                                  {services.map((service) => {
                                    const serviceId = service.serviceId || service.id
                                    const currentQty = selectedServicesByPassengerDraft?.[leg]?.[index]?.[serviceId] || 0
                                    
                                    return (
                                      <div
                                        key={`${index}-${leg}-${serviceId}`}
                                        className="flex items-center justify-between rounded-xl bg-white border border-slate-100 p-3"
                                      >
                                        <div>
                                          <p className="text-xs font-bold text-slate-800">{service.serviceName}</p>
                                          <p className="text-[10px] text-slate-400 font-bold mt-0.5">
                                            {getServicePriceLabel(service.price)}
                                          </p>
                                        </div>
                                        <div className="flex items-center gap-2">
                                          <button
                                            type="button"
                                            disabled={currentQty <= 0}
                                            onClick={() => updateServiceDraft(leg, index, serviceId, currentQty - 1)}
                                            className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50 disabled:opacity-40"
                                          >
                                            ?
                                          </button>
                                          <span className="w-5 text-center text-xs font-bold text-slate-800">{currentQty}</span>
                                          <button
                                            type="button"
                                            onClick={() => updateServiceDraft(leg, index, serviceId, currentQty + 1)}
                                            className="flex h-7 w-7 items-center justify-center rounded-lg border border-slate-200 text-slate-500 hover:bg-slate-50"
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

            {/* Promotion Box */}
            <div className="rounded-2xl border border-slate-200 p-5 bg-slate-50/50 space-y-3">
              <label className="form-label text-xs flex items-center gap-1.5">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4 w-4 text-emerald-600">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9.568 3H5.25A2.25 2.25 0 003 5.25v4.318c0 .597.237 1.17.659 1.591l9.581 9.581a2.25 2.25 0 003.182 0l5.178-5.178a2.25 2.25 0 000-3.182l-9.581-9.58a2.25 2.25 0 00-1.591-.66z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 6h.008v.008H6V6z" />
                </svg>
                <span>ï¿½p d?ng mï¿½ gi?m giï¿½</span>
              </label>
              
              <select
                value={selectedPromotionId || ''}
                onChange={(e) => setSelectedPromotionId(e.target.value)}
                className="form-input"
              >
                <option value="">Khï¿½ng s? d?ng mï¿½</option>
                {availablePromotions.map((promo) => (
                  <option key={String(getPromotionId(promo))} value={String(getPromotionId(promo))}>
                    {getPromotionDisplayText(promo)}
                  </option>
                ))}
              </select>
              
              {isLoadingPromotion && <p className="text-[10px] text-slate-400">Đang ki?m tra mï¿½ gi?m giï¿½...</p>}
              {!isLoadingPromotion && availablePromotions.length === 0 && <p className="text-[10px] text-slate-400">Hi?n t?i khï¿½ng cï¿½ mï¿½ gi?m giï¿½ nï¿½o hoạt động.</p>}
              {appliedPromotion && (
                <p className="text-xs font-bold text-emerald-600">
                  ? Dï¿½ ï¿½p d?ng uu dï¿½i: {getPromotionDisplayText(appliedPromotion)}
                </p>
              )}
            </div>

            {/* Error notifications */}
            {(localValidationMessage || apiError) && (
              <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600 animate-in fade-in duration-150">
                {localValidationMessage || apiError}
              </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-4 pt-2">
              <button
                type="submit"
                disabled={isLoadingBooking}
                className="btn-primary w-full flex items-center justify-center gap-2"
              >
                {isLoadingBooking ? (
                  <>
                    <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    <span>Đang kh?i t?o d?t gi? ch?...</span>
                  </>
                ) : (
                  'Ti?p t?c thanh toï¿½n'
                )}
              </button>
            </div>
          </form>
        </div>
      </section>

      {/* Right Ticket Summary Sidebar */}
      <aside className="space-y-4">
        <div className="glass-panel rounded-3xl p-5 shadow-xl space-y-4">
          <h3 className="title-font text-sm font-extrabold text-slate-800 border-b border-slate-100 pb-3">
            Tï¿½m t?t chuyến bay
          </h3>

          {/* Outbound Leg */}
          {selectedFlight && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs">
                <span className="font-extrabold text-brand-primary">Ch?ng di</span>
                <span className="font-bold text-slate-500">{formatDateTime(selectedFlight.departureTime)}</span>
              </div>
              <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 space-y-1">
                <p className="text-xs font-bold text-slate-800">
                  {selectedFlight.departureAirport} ? {selectedFlight.arrivalAirport}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Chuyến bay: {getFlightLabel(selectedFlight)} ï¿½ {selectedFlight.aircraftModel}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Hạng ghế: {searchData.seatClass}
                </p>
              </div>
            </div>
          )}

          {/* Return Leg */}
          {tripType === 'roundtrip' && returnFlight && (
            <div className="space-y-2 pt-2 border-t border-slate-100">
              <div className="flex items-center justify-between text-xs">
                <span className="font-extrabold text-brand-tertiary">Ch?ng v?</span>
                <span className="font-bold text-slate-500">{formatDateTime(returnFlight.departureTime)}</span>
              </div>
              <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 space-y-1">
                <p className="text-xs font-bold text-slate-800">
                  {returnFlight.departureAirport} ? {returnFlight.arrivalAirport}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Chuyến bay: {getFlightLabel(returnFlight)} ï¿½ {returnFlight.aircraftModel}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Hạng ghế: {searchData.seatClass}
                </p>
              </div>
            </div>
          )}

          {/* Price Breakdown */}
          <div className="border-t border-slate-100 pt-3 space-y-2">
            <h4 className="text-xs font-bold text-slate-800">Chi ti?t thanh toï¿½n</h4>
            
            <div className="space-y-1.5 text-xs">
              <div className="flex justify-between text-slate-500">
                <span>Vï¿½ ch?ng di ({passengerForms.length} khï¿½ch)</span>
                <span className="font-bold">{formatCurrency(outboundTotal)}</span>
              </div>

              {tripType === 'roundtrip' && (
                <div className="flex justify-between text-slate-500">
                  <span>Vï¿½ ch?ng v? ({passengerForms.length} khï¿½ch)</span>
                  <span className="font-bold">{formatCurrency(returnTotal)}</span>
                </div>
              )}

              {servicesCost > 0 && (
                <div className="flex justify-between text-slate-500">
                  <span>Dịch vụ ph? tr?</span>
                  <span className="font-bold">{formatCurrency(servicesCost)}</span>
                </div>
              )}

              {discount > 0 && (
                <div className="flex justify-between text-emerald-600 font-bold">
                  <span>Uu dï¿½i ï¿½p d?ng</span>
                  <span>-{formatCurrency(discount)}</span>
                </div>
              )}
            </div>

            <div className="flex justify-between items-center border-t border-slate-100 pt-3 text-slate-800">
              <span className="text-xs font-extrabold">T?ng s? ti?n</span>
              <span className="text-base font-extrabold text-brand-primary">{formatCurrency(finalCost)}</span>
            </div>
          </div>
        </div>
      </aside>
    </div>
  )
}
