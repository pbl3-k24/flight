import { useEffect, useState } from 'react'

const PAGE_SIZE = 10

/**
 * Parse a date string to YYYY-MM-DD local date key
 */
const toDateKey = (isoString) => {
  if (!isoString) return 'KhÃ´ng rÃµ ngÃ y'
  const d = new Date(isoString)
  if (isNaN(d.getTime())) return 'KhÃ´ng rÃµ ngÃ y'
  return d.toLocaleDateString('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

const toSortKey = (isoString) => {
  if (!isoString) return '9999-12-31'
  const d = new Date(isoString)
  if (isNaN(d.getTime())) return '9999-12-31'
  return d.toISOString().slice(0, 10)
}

/**
 * Resolve seat availability info from seatInventory array.
 * Returns array of { label, available, total, price }
 */
const resolveSeatInfo = (seatInventory) => {
  if (!Array.isArray(seatInventory) || seatInventory.length === 0) return []
  return seatInventory.map((seat) => ({
    label: seat.seatClassName || seat.className || seat.class || seat.label || '---',
    available: seat.availableSeats  seat.available  seat.remainingSeats  null,
    total: seat.totalSeats  seat.total  null,
    price: seat.price  seat.basePrice  null,
  }))
}

/**
 * Get color classes for seat availability badge
 */
const getSeatBadgeColor = (available, total) => {
  if (available === null) return 'bg-slate-100 text-slate-500'
  if (total === null || total === 0) return 'bg-slate-100 text-slate-500'
  const ratio = available / total
  if (ratio === 0) return 'bg-red-100 text-red-700'
  if (ratio < 0.25) return 'bg-orange-100 text-orange-700'
  if (ratio < 0.6) return 'bg-amber-100 text-amber-700'
  return 'bg-emerald-100 text-emerald-700'
}

export default function FlightListWithPagination({
  flights,
  getSeatInventorySummary,
  formatCurrency,
  formatDateTime,
  startEditFlight,
  handleCancelAdminFlight,
  handleDeleteFlight,
  isCancellingAdminFlightId,
}) {
  const [currentPage, setCurrentPage] = useState(1)

  // Reset to page 1 whenever the filtered flight list changes
  useEffect(() => {
    setCurrentPage(1)
  }, [flights.length])

  const totalPages = Math.ceil(flights.length / PAGE_SIZE)

  const paged = flights.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE)

  // Group paged flights by date
  const groupsByDate = {}
  paged.forEach((flight) => {
    const key = toSortKey(flight.departureTime)
    const label = toDateKey(flight.departureTime)
    if (!groupsByDate[key]) groupsByDate[key] = { label, flights: [] }
    groupsByDate[key].flights.push(flight)
  })
  const sortedDateKeys = Object.keys(groupsByDate).sort()

  const handlePage = (page) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)))
  }

  return (
    <div className="space-y-4">
      {/* Summary row */}
      <div className="flex items-center justify-between text-xs text-slate-500">
        <span>
          Hiá»ƒn thá»‹ <span className="font-bold text-slate-700">{(currentPage - 1) * PAGE_SIZE + 1}â€“{Math.min(currentPage * PAGE_SIZE, flights.length)}</span> / <span className="font-bold text-slate-700">{flights.length}</span> chuyáº¿n bay
        </span>
        {totalPages > 1 && (
          <span className="text-slate-400">Trang {currentPage} / {totalPages}</span>
        )}
      </div>

      {/* Flight groups by date */}
      <div className="space-y-5">
        {sortedDateKeys.map((dateKey) => {
          const group = groupsByDate[dateKey]
          return (
            <div key={dateKey}>
              {/* Date header */}
              <div className="mb-2 flex items-center gap-3">
                <div className="flex items-center gap-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-3 py-1.5 shadow-sm">
                  <span className="text-[10px] font-extrabold uppercase tracking-wider text-white/80">ðŸ“…</span>
                  <span className="text-xs font-extrabold text-white capitalize">{group.label}</span>
                </div>
                <div className="flex-1 h-px bg-slate-100" />
                <span className="text-[10px] font-bold text-slate-400">{group.flights.length} chuyáº¿n</span>
              </div>

              {/* Flight cards */}
              <div className="space-y-2">
                {group.flights.map((flight) => {
                  const seatSummary = getSeatInventorySummary(flight.seatInventory)
                  const seatInfo = resolveSeatInfo(flight.seatInventory)

                  return (
                    <article
                      key={flight.flightId}
                      className="rounded-2xl border border-slate-100 bg-white p-4 shadow-sm hover:shadow-md hover:border-blue-100 transition-all duration-150"
                    >
                      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                        {/* Left: flight info */}
                        <div className="flex-1 space-y-2">
                          {/* Header row */}
                          <div className="flex flex-wrap items-center gap-2">
                            <h4 className="text-sm font-extrabold text-slate-900">{flight.flightNumber || '---'}</h4>
                            <span className={`rounded-full px-2.5 py-0.5 text-[9px] font-bold ${
                              flight.isActive
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                                : 'bg-slate-50 text-slate-500 border border-slate-200'
                            }`}>
                              {flight.isActive ? 'Hoáº¡t Ä‘á»™ng' : 'Táº¡m dá»«ng'}
                            </span>
                          </div>

                          {/* Route + aircraft */}
                          <p className="text-[11px] font-bold text-slate-600">
                            {flight.routeCode || '---'} &nbsp;Â·&nbsp; âœˆ {flight.aircraftModel || '---'}
                          </p>

                          {/* Times */}
                          <div className="flex flex-wrap gap-x-4 gap-y-0.5">
                            <span className="text-[10px] text-slate-400 font-medium">
                              ðŸ›« Cáº¥t cÃ¡nh: <span className="font-bold text-slate-600">{formatDateTime(flight.departureTime)}</span>
                            </span>
                            <span className="text-[10px] text-slate-400 font-medium">
                              ðŸ›¬ Háº¡ cÃ¡nh: <span className="font-bold text-slate-600">{formatDateTime(flight.arrivalTime)}</span>
                            </span>
                          </div>

                          {/* Seat availability */}
                          {(seatInfo.length > 0 || seatSummary.length > 0) && (
                            <div className="pt-1.5">
                              <div className="mb-1 text-[9px] font-extrabold uppercase tracking-widest text-slate-400">
                                Gháº¿ trá»‘ng theo háº¡ng
                              </div>
                              <div className="flex flex-wrap gap-1.5">
                                {seatInfo.length > 0 ? (
                                  seatInfo.map((seat, idx) => {
                                    const badgeColor = getSeatBadgeColor(seat.available, seat.total)
                                    return (
                                      <div
                                        key={idx}
                                        className={`rounded-xl px-2.5 py-1.5 text-[10px] font-bold flex flex-col items-center gap-0.5 min-w-[72px] ${badgeColor} border border-current/10`}
                                      >
                                        <span className="font-extrabold uppercase tracking-wide text-[9px] opacity-70">{seat.label}</span>
                                        <div className="flex items-baseline gap-1">
                                          <span className="text-base font-black leading-none">
                                            {seat.available  '?'}
                                          </span>
                                          {seat.total !== null && (
                                            <span className="text-[9px] opacity-60">/ {seat.total}</span>
                                          )}
                                        </div>
                                        {seat.price !== null && (
                                          <span className="text-[8px] opacity-60 font-semibold">
                                            {formatCurrency(seat.price)}
                                          </span>
                                        )}
                                      </div>
                                    )
                                  })
                                ) : (
                                  // Fallback: only price, no available count
                                  seatSummary.map((seat, idx) => (
                                    <span
                                      key={idx}
                                      className="rounded-lg bg-blue-50 px-2.5 py-1 text-[9px] font-bold text-blue-700"
                                    >
                                      {seat.label}: {formatCurrency(seat.price)}
                                    </span>
                                  ))
                                )}
                              </div>
                            </div>
                          )}
                        </div>

                        {/* Right: actions */}
                        <div className="flex items-center gap-2 self-end sm:self-start sm:pt-1">
                          <button
                            type="button"
                            onClick={() => startEditFlight(flight)}
                            className="btn-secondary px-3 py-1.5 text-[10px] font-bold rounded-lg"
                          >
                            Sá»­a
                          </button>
                          <button
                            type="button"
                            onClick={() => handleCancelAdminFlight(flight)}
                            disabled={isCancellingAdminFlightId === flight.flightId}
                            className="btn-secondary border-amber-300 bg-amber-50 text-amber-700 hover:bg-amber-100 px-3 py-1.5 text-[10px] font-bold rounded-lg disabled:opacity-50"
                          >
                            Há»§y chuyáº¿n
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDeleteFlight(flight)}
                            className="btn-danger px-3 py-1.5 text-[10px] font-bold rounded-lg"
                          >
                            XÃ³a
                          </button>
                        </div>
                      </div>
                    </article>
                  )
                })}
              </div>
            </div>
          )
        })}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-1.5 pt-2 border-t border-slate-100">
          {/* Prev */}
          <button
            type="button"
            onClick={() => handlePage(currentPage - 1)}
            disabled={currentPage === 1}
            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            â† TrÆ°á»›c
          </button>

          {/* Page numbers */}
          {Array.from({ length: totalPages }, (_, i) => i + 1)
            .filter((page) => {
              // Show: first, last, current Â±2
              return page === 1 || page === totalPages || Math.abs(page - currentPage) <= 2
            })
            .reduce((acc, page, idx, arr) => {
              if (idx > 0 && arr[idx - 1] !== page - 1) {
                acc.push('...')
              }
              acc.push(page)
              return acc
            }, [])
            .map((item, idx) =>
              item === '...' ? (
                <span key={`ellipsis-${idx}`} className="px-1 text-xs text-slate-400">â€¦</span>
              ) : (
                <button
                  key={item}
                  type="button"
                  onClick={() => handlePage(item)}
                  className={`min-w-[32px] rounded-lg border px-2.5 py-1.5 text-xs font-bold transition-colors ${
                    currentPage === item
                      ? 'border-blue-600 bg-blue-600 text-white shadow-sm'
                      : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  {item}
                </button>
              )
            )}

          {/* Next */}
          <button
            type="button"
            onClick={() => handlePage(currentPage + 1)}
            disabled={currentPage === totalPages}
            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Tiáº¿p â†’
          </button>
        </div>
      )}
    </div>
  )
}
