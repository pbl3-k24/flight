export default function BookingExtras({
  passengerForms,
  tripType,
  services,
  serviceError,
  isLoadingServices,
  selectedServicesByPassengerDraft,
  setSelectedServicesByPassengerDraft,
  onBack,
  onContinue,
  formatCurrency,
}) {
  const updateServiceDraft = (leg, passengerIndex, serviceId, quantity) => {
    setSelectedServicesByPassengerDraft((prev) => {
      const nextLeg = { ...(prev?.[leg] || {}) }
      nextLeg[passengerIndex] = {
        ...(nextLeg[passengerIndex] || {}),
        [serviceId]: quantity,
      }
      return {
        ...prev,
        [leg]: nextLeg,
      }
    })
  }

  const getServicePriceLabel = (price) => {
    const value = Number(price || 0)
    return value > 0 ? `+ ${formatCurrency(value)}` : 'Mien phi'
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
      <aside className="glass-panel h-fit rounded-3xl p-5 shadow-xl">
        <h3 className="title-font text-lg font-extrabold text-slate-800">Chi tiet gia</h3>
        <p className="mt-2 text-sm text-slate-500">Them hanh ly va dich vu truoc khi nhap thong tin.</p>
        <div className="mt-5 flex flex-col gap-2">
          <button type="button" onClick={onBack} className="btn-secondary w-full">
            Quay lai
          </button>
          <button type="button" onClick={onContinue} className="btn-primary w-full">
            Tiep tuc
          </button>
        </div>
      </aside>

      <section className="space-y-4">
        <div className="glass-panel rounded-3xl p-6 shadow-xl">
          <h2 className="title-font text-2xl font-extrabold text-slate-800">Mua hanh ly va dich vu</h2>
          {serviceError && <p className="mt-3 text-sm text-rose-600">{serviceError}</p>}
          {isLoadingServices && <p className="mt-3 text-sm text-slate-500">Đang tai dich vu...</p>}
          {!isLoadingServices && services.length === 0 && (
            <p className="mt-3 text-sm text-slate-500">Không co dich vu kha dung cho hang ghe da chon.</p>
          )}
        </div>

        {passengerForms.map((passenger, index) => {
          const isInfant = passenger.type === 'infant'
          return (
            <article key={`extras-${index}`} className="glass-panel rounded-3xl p-6 shadow-md">
              <p className="text-sm font-bold text-slate-800">
                Hanh khach {index + 1} - {isInfant ? 'Tre so sinh' : 'Nguoi lon/tre em'}
              </p>
              {isInfant ? (
                <p className="mt-2 text-sm text-slate-500">Tre so sinh khong ap dung dich vu bo sung.</p>
              ) : (
                <div className="mt-4 space-y-4">
                  {['outbound', tripType === 'roundtrip' ? 'return' : null]
                    .filter(Boolean)
                    .map((leg) => (
                      <div key={`${index}-${leg}`} className="rounded-2xl border border-slate-200 bg-white p-4">
                        <p className="mb-3 text-sm font-semibold text-slate-700">
                          Dich vu {leg === 'outbound' ? 'chieu di' : 'chieu ve'}
                        </p>
                        {services.map((service) => {
                          const serviceId = service.serviceId || service.id
                          const currentQty =
                            selectedServicesByPassengerDraft?.[leg]?.[index]?.[serviceId] || 0
                          return (
                            <div
                              key={`${index}-${leg}-${serviceId}`}
                              className="mb-2 flex items-center justify-between rounded-xl border border-slate-100 p-3"
                            >
                              <div>
                                <p className="text-sm font-semibold text-slate-800">{service.serviceName}</p>
                                <p className="text-xs text-slate-500">{getServicePriceLabel(service.price)}</p>
                              </div>
                              <div className="flex items-center gap-2">
                                <button
                                  type="button"
                                  onClick={() => {
                                    if (currentQty > 0) updateServiceDraft(leg, index, serviceId, currentQty - 1)
                                  }}
                                  className="rounded-lg border border-slate-200 px-3 py-1 text-sm font-semibold text-slate-700"
                                >
                                  -
                                </button>
                                <span className="w-6 text-center text-sm font-bold">{currentQty}</span>
                                <button
                                  type="button"
                                  onClick={() => updateServiceDraft(leg, index, serviceId, currentQty + 1)}
                                  className="rounded-lg bg-brand-primary px-3 py-1 text-sm font-semibold text-white"
                                >
                                  +
                                </button>
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    ))}
                </div>
              )}
            </article>
          )
        })}
      </section>
    </div>
  )
}
