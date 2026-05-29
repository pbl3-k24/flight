import { useState } from 'react'
import FlightTemplateManagement from '../flight-templates/FlightTemplateManagement'
import FlightListWithPagination from './FlightListWithPagination'

export default function AdminDashboard({
  // Flights management props
  adminFlights,
  adminRoutes,
  aircrafts,
  isLoadingFlightsAdmin,
  flightAdminError,
  flightAdminNotice,
  flightFormData,
  setFlightFormData,
  editingFlightId,
  isCancellingAdminFlightId,
  adminFlightFilters,
  setAdminFlightFilters,
  filteredAdminFlights,
  loadAdminFlights,
  startCreateFlight,
  startEditFlight,
  handleCancelAdminFlight,
  handleDeleteFlight,
  handleFlightSubmit,
  getSeatInventorySummary,
  
  // Promotions management props
  adminPromotions,
  isLoadingPromotionsAdmin,
  promotionAdminError,
  promotionAdminNotice,
  promotionFormData,
  setPromotionFormData,
  editingPromotionId,
  loadAdminPromotions,
  startCreatePromotion,
  startEditPromotion,
  handleDeletePromotion,
  handlePromotionSubmit,
  getPromotionTypeLabel,

  // Shared utils
  formatCurrency,
  formatDateTime
}) {
  const [activeSubTab, setActiveSubTab] = useState('flights')

  return (
    <div className="grid gap-6 lg:grid-cols-[240px_1fr] animate-in fade-in duration-300">
      {/* Sidebar for Admin Panel */}
      <aside className="glass-panel h-fit rounded-3xl p-5 shadow-xl space-y-4">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-widest text-slate-400">Trang qu?n tr?</span>
          <h3 className="title-font text-base font-extrabold text-slate-800 mt-0.5">Admin Dashboard</h3>
        </div>

        <div className="flex flex-col gap-1">
          {[
            { id: 'flights', label: ' Qu?n lï¿½ chuyến bay' },
            { id: 'promotions', label: '? Qu?n lï¿½ khuy?n mï¿½i' },
            { id: 'templates', label: ' M?u l?ch trï¿½nh bay' },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveSubTab(tab.id)}
              className={`w-full text-left rounded-xl px-4 py-3 text-xs font-bold transition-all duration-instant ${
                activeSubTab === tab.id
                  ? 'bg-brand-primary text-white shadow-md shadow-blue-100'
                  : 'text-slate-600 hover:bg-slate-50'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="space-y-6">
        {/* SUBTAB 1: Flight Management */}
        {activeSubTab === 'flights' && (
          <div className="grid gap-6 lg:grid-cols-[1fr_360px] animate-in fade-in duration-200">
            {/* Left: Listing & Filters */}
            <section className="glass-panel rounded-3xl p-6 shadow-xl space-y-5">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
                <div>
                  <h3 className="title-font text-lg font-extrabold text-slate-800">Danh sï¿½ch chuyến bay</h3>
                  <p className="text-xs text-slate-400">Danh sï¿½ch chuyến bay chi ti?t trong h? th?ng</p>
                </div>

                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={loadAdminFlights}
                    className="btn-secondary py-2 text-xs font-bold"
                  >
                    Lï¿½m m?i
                  </button>
                  <button
                    type="button"
                    onClick={startCreateFlight}
                    className="btn-primary py-2 text-xs font-bold"
                  >
                    + T?o chuyến bay
                  </button>
                </div>
              </div>

              {/* Filters Panel */}
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 p-3 bg-slate-50 rounded-2xl border border-slate-100 text-xs">
                <div>
                  <label className="form-label text-[10px]">Ngï¿½y bay</label>
                  <input
                    type="date"
                    value={adminFlightFilters.date}
                    onChange={(e) => setAdminFlightFilters((prev) => ({ ...prev, date: e.target.value }))}
                    className="form-input text-xs"
                  />
                </div>
                <div>
                  <label className="form-label text-[10px]">T? sï¿½n bay</label>
                  <input
                    type="text"
                    value={adminFlightFilters.from}
                    onChange={(e) => setAdminFlightFilters((prev) => ({ ...prev, from: e.target.value }))}
                    placeholder="VD: SGN"
                    className="form-input text-xs"
                  />
                </div>
                <div>
                  <label className="form-label text-[10px]">D?n sï¿½n bay</label>
                  <input
                    type="text"
                    value={adminFlightFilters.to}
                    onChange={(e) => setAdminFlightFilters((prev) => ({ ...prev, to: e.target.value }))}
                    placeholder="VD: HAN"
                    className="form-input text-xs"
                  />
                </div>
                <div>
                  <label className="form-label text-[10px]">Mï¿½ chuyến bay</label>
                  <input
                    type="text"
                    value={adminFlightFilters.code}
                    onChange={(e) => setAdminFlightFilters((prev) => ({ ...prev, code: e.target.value }))}
                    placeholder="VD: VN211"
                    className="form-input text-xs"
                  />
                </div>
              </div>

              {/* Notice boards */}
              {flightAdminError && (
                <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600">
                  {flightAdminError}
                </div>
              )}
              {flightAdminNotice && (
                <div className="rounded-xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-bold text-emerald-700">
                  {flightAdminNotice}
                </div>
              )}

              {/* Flights list with group-by-date + pagination */}
              {isLoadingFlightsAdmin ? (
                <div className="py-12 text-center text-xs text-slate-400 font-bold">Đang tải danh sï¿½ch chuyến bay...</div>
              ) : adminFlights.length === 0 ? (
                <div className="py-12 text-center text-xs text-slate-400 font-bold">Chưa cï¿½ chuyến bay nï¿½o trong h? th?ng.</div>
              ) : filteredAdminFlights.length === 0 ? (
                <div className="py-12 text-center text-xs text-slate-400 font-bold">Khï¿½ng tï¿½m th?y chuyến bay nï¿½o phï¿½ h?p.</div>
              ) : (
                <FlightListWithPagination
                  flights={filteredAdminFlights}
                  getSeatInventorySummary={getSeatInventorySummary}
                  formatCurrency={formatCurrency}
                  formatDateTime={formatDateTime}
                  startEditFlight={startEditFlight}
                  handleCancelAdminFlight={handleCancelAdminFlight}
                  handleDeleteFlight={handleDeleteFlight}
                  isCancellingAdminFlightId={isCancellingAdminFlightId}
                />
              )}
            </section>

            {/* Right: Create/Edit Form */}
            <aside className="glass-panel rounded-3xl p-5 shadow-xl h-fit space-y-4 text-xs">
              <h3 className="title-font text-sm font-extrabold text-slate-800 border-b border-slate-100 pb-3">
                {editingFlightId ? 'Cập nhật chuyến bay' : 'T?o chuyến bay m?i'}
              </h3>
              
              <form onSubmit={handleFlightSubmit} className="space-y-3">
                <div>
                  <label className="form-label text-[10px]">S? hi?u chuyến bay</label>
                  <input
                    type="text"
                    required
                    placeholder="VD: VN211"
                    value={flightFormData.flightNumber}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, flightNumber: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Tuy?n bay</label>
                  <select
                    value={flightFormData.routeId}
                    required
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, routeId: e.target.value }))}
                    disabled={!!editingFlightId}
                    className="form-input text-xs"
                  >
                    <option value="">Ch?n tuy?n bay</option>
                    {adminRoutes.map((route) => (
                      <option key={route.routeId} value={route.routeId}>
                        {route.departureAirport} ? {route.arrivalAirport}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="form-label text-[10px]">Ch?n tï¿½u bay</label>
                  <select
                    value={flightFormData.aircraftId}
                    required
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, aircraftId: e.target.value }))}
                    className="form-input text-xs"
                  >
                    <option value="">Ch?n mï¿½y bay</option>
                    {aircrafts.map((aircraft) => (
                      <option key={aircraft.aircraftId} value={aircraft.aircraftId}>
                        {aircraft.model} ({aircraft.registrationNumber})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="form-label text-[10px]">Th?i gian c?t cï¿½nh</label>
                  <input
                    type="datetime-local"
                    required
                    value={flightFormData.departureTime}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, departureTime: e.target.value }))}
                    className="form-input text-xs"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Th?i gian h? cï¿½nh</label>
                  <input
                    type="datetime-local"
                    required
                    value={flightFormData.arrivalTime}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, arrivalTime: e.target.value }))}
                    className="form-input text-xs"
                  />
                </div>

                <label className="flex items-center gap-2 text-xs text-slate-700 cursor-pointer pt-1">
                  <input
                    type="checkbox"
                    checked={flightFormData.isActive}
                    onChange={(e) => setFlightFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
                    className="accent-brand-primary h-4 w-4"
                  />
                  <span>Kï¿½ch ho?t chuyến bay nï¿½y</span>
                </label>

                <div className="flex gap-2 pt-2 border-t border-slate-100">
                  <button
                    type="submit"
                    className="btn-primary py-2 text-xs font-bold flex-1"
                  >
                    {editingFlightId ? 'Cập nhật' : 'T?o m?i'}
                  </button>
                  {editingFlightId && (
                    <button
                      type="button"
                      onClick={startCreateFlight}
                      className="btn-secondary py-2 text-xs font-bold"
                    >
                      H?y
                    </button>
                  )}
                </div>
              </form>
            </aside>
          </div>
        )}

        {/* SUBTAB 2: Promotion Management */}
        {activeSubTab === 'promotions' && (
          <div className="grid gap-6 lg:grid-cols-[1fr_360px] animate-in fade-in duration-200">
            {/* Left: Promotions List */}
            <section className="glass-panel rounded-3xl p-6 shadow-xl space-y-5">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
                <div>
                  <h3 className="title-font text-lg font-extrabold text-slate-800">Danh sï¿½ch khuy?n mï¿½i</h3>
                  <p className="text-xs text-slate-400">Cï¿½c chuong trï¿½nh khuy?n mï¿½i hi?n cï¿½ trï¿½n h? th?ng</p>
                </div>

                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={loadAdminPromotions}
                    className="btn-secondary py-2 text-xs font-bold"
                  >
                    Lï¿½m m?i
                  </button>
                  <button
                    type="button"
                    onClick={startCreatePromotion}
                    className="btn-primary py-2 text-xs font-bold"
                  >
                    + T?o mï¿½
                  </button>
                </div>
              </div>

              {/* Notice boards */}
              {promotionAdminError && (
                <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600">
                  {promotionAdminError}
                </div>
              )}
              {promotionAdminNotice && (
                <div className="rounded-xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-bold text-emerald-700">
                  {promotionAdminNotice}
                </div>
              )}

              {isLoadingPromotionsAdmin ? (
                <div className="py-12 text-center text-xs text-slate-400 font-bold">Đang tải danh sï¿½ch khuy?n mï¿½i...</div>
              ) : adminPromotions.length === 0 ? (
                <div className="py-12 text-center text-xs text-slate-400 font-bold">Chưa cï¿½ chuong trï¿½nh khuy?n mï¿½i nï¿½o được t?o.</div>
              ) : (
                <div className="space-y-4 max-h-[500px] overflow-y-auto pr-1">
                  {adminPromotions.map((promo) => (
                    <article
                      key={promo.promotionId}
                      className="rounded-2xl border border-slate-100 p-4 bg-white shadow-sm space-y-3"
                    >
                      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <h4 className="text-sm font-extrabold text-brand-primary uppercase tracking-wider">
                              {promo.code || '---'}
                            </h4>
                            <span className={`rounded-full px-2.5 py-0.5 text-[9px] font-bold ${
                              promo.isActive ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-slate-50 text-slate-500 border border-slate-200'
                            }`}>
                              {promo.isActive ? 'Đang kï¿½ch ho?t' : 'T?m d?ng'}
                            </span>
                          </div>
                          
                          <p className="text-xs text-slate-600 font-medium">{promo.description || 'Khï¿½ng cï¿½ mï¿½ t? chi ti?t'}</p>
                          <p className="text-xs font-bold text-slate-700">
                            Lo?i gi?m: {getPromotionTypeLabel(promo.discountType)} ï¿½ Tr? giï¿½: {formatCurrency(promo.discountValue)}
                          </p>
                          <p className="text-[10px] text-slate-400 font-medium">
                            Gi?m t?i da: {promo.maxDiscountAmount ? formatCurrency(promo.maxDiscountAmount) : 'Khï¿½ng gi?i h?n'} ï¿½ Don hï¿½ng t?i thi?u: {formatCurrency(promo.minimumAmount || 0)}
                          </p>
                          <p className="text-[10px] text-slate-400 font-medium">
                            Th?i gian: {formatDateTime(promo.validFrom)} ? {formatDateTime(promo.validTo)}
                          </p>
                        </div>

                        <div className="text-right flex flex-col gap-2 items-end">
                          <div className="text-xs font-bold text-slate-700">
                            <span>{promo.usageCount} lu?t dï¿½ng</span>
                            <span className="block text-[10px] text-slate-400 font-medium mt-0.5">
                              Gi?i h?n: {promo.usageLimit  'Khï¿½ng gi?i h?n'}
                            </span>
                          </div>

                          <div className="flex items-center gap-2">
                            <button
                              type="button"
                              onClick={() => startEditPromotion(promo)}
                              className="btn-secondary px-3 py-1.5 text-[10px] font-bold rounded-lg"
                            >
                              S?a
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDeletePromotion(promo)}
                              className="btn-danger px-3 py-1.5 text-[10px] font-bold rounded-lg"
                            >
                              Xï¿½a
                            </button>
                          </div>
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>

            {/* Right: Create/Edit Form */}
            <aside className="glass-panel rounded-3xl p-5 shadow-xl h-fit space-y-4 text-xs">
              <h3 className="title-font text-sm font-extrabold text-slate-800 border-b border-slate-100 pb-3">
                {editingPromotionId ? 'Cập nhật khuy?n mï¿½i' : 'T?o khuy?n mï¿½i m?i'}
              </h3>
              
              <form onSubmit={handlePromotionSubmit} className="space-y-3">
                <div>
                  <label className="form-label text-[10px]">Mï¿½ khuy?n mï¿½i</label>
                  <input
                    type="text"
                    required
                    placeholder="VD: BANMAI2026"
                    value={promotionFormData.code}
                    disabled={!!editingPromotionId}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, code: e.target.value.toUpperCase() }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Mï¿½ t? chuong trï¿½nh</label>
                  <input
                    type="text"
                    required
                    placeholder="Gi?m giï¿½ d?t vï¿½ d?p T?t"
                    value={promotionFormData.description}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, description: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Lo?i gi?m giï¿½</label>
                  <select
                    value={promotionFormData.discountType}
                    required
                    disabled={!!editingPromotionId}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, discountType: Number(e.target.value) }))}
                    className="form-input text-xs"
                  >
                    <option value={0}>Gi?m theo t? l? (%)</option>
                    <option value={1}>Gi?m tr?c ti?p s? ti?n (d)</option>
                  </select>
                </div>

                <div>
                  <label className="form-label text-[10px]">Giï¿½ tr? gi?m (% ho?c VND)</label>
                  <input
                    type="number"
                    required
                    placeholder="VD: 10 ho?c 200000"
                    value={promotionFormData.discountValue}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, discountValue: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">S? ti?n gi?m t?i da (VND)</label>
                  <input
                    type="number"
                    placeholder="VD: 500000 (d? tr?ng n?u khï¿½ng gi?i h?n)"
                    value={promotionFormData.maxDiscountAmount}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, maxDiscountAmount: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Don hï¿½ng t?i thi?u ï¿½p d?ng (VND)</label>
                  <input
                    type="number"
                    required
                    disabled={!!editingPromotionId}
                    placeholder="VD: 1000000"
                    value={promotionFormData.minimumAmount}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, minimumAmount: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div>
                  <label className="form-label text-[10px]">Gi?i h?n s? lu?t s? d?ng</label>
                  <input
                    type="number"
                    placeholder="VD: 100"
                    value={promotionFormData.usageLimit}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, usageLimit: e.target.value }))}
                    className="form-input"
                  />
                </div>

                <div className="grid gap-2 grid-cols-2">
                  <div>
                    <label className="form-label text-[10px]">Hi?u l?c t?</label>
                    <input
                      type="date"
                      required
                      disabled={!!editingPromotionId}
                      value={promotionFormData.validFrom}
                      onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validFrom: e.target.value }))}
                      className="form-input text-xs"
                    />
                  </div>
                  <div>
                    <label className="form-label text-[10px]">Hi?u l?c d?n</label>
                    <input
                      type="date"
                      required
                      value={promotionFormData.validTo}
                      onChange={(e) => setPromotionFormData((prev) => ({ ...prev, validTo: e.target.value }))}
                      className="form-input text-xs"
                    />
                  </div>
                </div>

                <label className="flex items-center gap-2 text-xs text-slate-700 cursor-pointer pt-1">
                  <input
                    type="checkbox"
                    checked={promotionFormData.isActive}
                    onChange={(e) => setPromotionFormData((prev) => ({ ...prev, isActive: e.target.checked }))}
                    className="accent-brand-primary h-4 w-4"
                  />
                  <span>Kï¿½ch ho?t mï¿½ uu dï¿½i nï¿½y</span>
                </label>

                <div className="flex gap-2 pt-2 border-t border-slate-100">
                  <button
                    type="submit"
                    className="btn-primary py-2 text-xs font-bold flex-1"
                  >
                    {editingPromotionId ? 'Cập nhật' : 'T?o m?i'}
                  </button>
                  {editingPromotionId && (
                    <button
                      type="button"
                      onClick={startCreatePromotion}
                      className="btn-secondary py-2 text-xs font-bold"
                    >
                      H?y
                    </button>
                  )}
                </div>
              </form>
            </aside>
          </div>
        )}

        {/* SUBTAB 3: Flight Schedule Templates */}
        {activeSubTab === 'templates' && (
          <div className="glass-panel rounded-3xl p-6 shadow-xl animate-in fade-in duration-200">
            <FlightTemplateManagement />
          </div>
        )}
      </div>
    </div>
  )
}
