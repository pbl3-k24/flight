import { useState } from 'react'

export default function FlightSearchForm({
  tripType,
  setTripType,
  searchData,
  setSearchData,
  passengerCounts,
  setPassengerCounts,
  totalPassengers,
  today,
  isLoadingFlights,
  performSearch,
  apiError,
  airports
}) {
  const [showPassengerDropdown, setShowPassengerDropdown] = useState(false)

  const handleAirportChange = (field, value) => {
    setSearchData((prev) => ({ ...prev, [field]: Number(value) }))
  }

  const updatePassengerCount = (type, increment) => {
    setPassengerCounts((prev) => {
      const nextCount = Math.max(type === 'adult' ? 1 : 0, prev[type] + increment)
      const total = (type === 'adult' ? nextCount : prev.adult) +
                    (type === 'child' ? nextCount : prev.child) +
                    (type === 'infant' ? nextCount : prev.infant)
      
      if (total > 9) return prev // Giới hạn tối đa 9 hành khách
      return { ...prev, [type]: nextCount }
    })
  }

  return (
    <div className="w-full glass-panel rounded-3xl p-6 shadow-xl animate-in fade-in slide-in-from-bottom-4 duration-300">
      <div className="flex flex-col gap-6">
        {/* Title & Trip Type Selector */}
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div>
            <h2 className="title-font text-xl font-extrabold text-slate-800 md:text-2xl">
              Tìm kiếm chuyến bay
            </h2>
            <p className="text-xs text-slate-400">Khám phá các điểm đến mơ ước với mức giá tốt nhất</p>
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
                className={`rounded-lg px-4 py-2 text-xs font-bold transition-all duration-fast ${
                  tripType === option.key
                    ? 'bg-brand-primary text-white shadow-md shadow-blue-200'
                    : 'text-slate-500 hover:text-brand-primary'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>

        {/* Search Parameters Grid */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {/* Outbound Airport */}
          <div className="relative">
            <label className="form-label flex items-center gap-1.5">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-brand-primary">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
              </svg>
              <span>Điểm đi</span>
            </label>
            <select
              value={searchData.fromAirportId}
              onChange={(e) => handleAirportChange('fromAirportId', e.target.value)}
              className="form-input appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2020%22%20fill%3D%22none%22%3E%3Cpath%20d%3D%22M7%209l3%203%203-3%22%20stroke%3D%22%2364748B%22%20stroke-width%3D%221.5%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1.25rem] bg-[right_0.75rem_center] bg-no-repeat pr-10"
            >
              {airports.map((airport) => (
                <option key={airport.Id} value={airport.Id}>
                  {airport.City} ({airport.Code})
                </option>
              ))}
            </select>
          </div>

          {/* Inbound Airport */}
          <div className="relative">
            <label className="form-label flex items-center gap-1.5">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-brand-tertiary">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" transform="rotate(180 12 12)" />
              </svg>
              <span>Điểm đến</span>
            </label>
            <select
              value={searchData.toAirportId}
              onChange={(e) => handleAirportChange('toAirportId', e.target.value)}
              className="form-input appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2020%22%20fill%3D%22none%22%3E%3Cpath%20d%3D%22M7%209l3%203%203-3%22%20stroke%3D%22%2364748B%22%20stroke-width%3D%221.5%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1.25rem] bg-[right_0.75rem_center] bg-no-repeat pr-10"
            >
              {airports.map((airport) => (
                <option key={airport.Id} value={airport.Id}>
                  {airport.City} ({airport.Code})
                </option>
              ))}
            </select>
          </div>

          {/* Departure Date */}
          <div>
            <label className="form-label flex items-center gap-1.5">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-slate-500">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
              </svg>
              <span>Ngày đi</span>
            </label>
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
              className="form-input"
            />
          </div>

          {/* Return Date (only for roundtrip) */}
          {tripType === 'roundtrip' ? (
            <div>
              <label className="form-label flex items-center gap-1.5">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-slate-500">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
                </svg>
                <span>Ngày về</span>
              </label>
              <input
                type="date"
                min={searchData.departDate || today}
                value={searchData.returnDate}
                onChange={(e) => setSearchData((prev) => ({ ...prev, returnDate: e.target.value }))}
                className="form-input"
              />
            </div>
          ) : (
            /* Seat Class Selection (filled in 4th col when Oneway) */
            <div>
              <label className="form-label flex items-center gap-1.5">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-slate-500">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                </svg>
                <span>Hạng ghế</span>
              </label>
              <select
                value={searchData.seatClass}
                onChange={(e) => setSearchData((prev) => ({ ...prev, seatClass: e.target.value }))}
                className="form-input appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2020%22%20fill%3D%22none%22%3E%3Cpath%20d%3D%22M7%209l3%203%203-3%22%20stroke%3D%22%2364748B%22%20stroke-width%3D%221.5%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1.25rem] bg-[right_0.75rem_center] bg-no-repeat pr-10"
              >
                <option value="Economy">Economy</option>
                <option value="Business">Business</option>
              </select>
            </div>
          )}
        </div>

        {/* Second Row Grid */}
        <div className="grid gap-4 md:grid-cols-2">
          {/* Passengers Picker */}
          <div className="relative">
            <label className="form-label flex items-center gap-1.5">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-slate-500">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.109A2.25 2.25 0 0112.75 21.5h-1.5a2.25 2.25 0 01-2.25-2.263V19.13m0 0a9.337 9.337 0 01-2.625-.372 9.337 9.337 0 01-4.121-.952 4.125 4.125 0 017.533-2.493M3.75 19.5h.007v-.003c0-.111-.007-.222-.007-.333 0-2.31 1.87-4.183 4.18-4.183H12" />
              </svg>
              <span>Hành khách</span>
            </label>
            
            <button
              type="button"
              onClick={() => setShowPassengerDropdown((prev) => !prev)}
              className="form-input text-left flex items-center justify-between"
            >
              <span>{totalPassengers} Hành khách</span>
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="h-5 w-5 text-slate-400">
                <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clipRule="evenodd" />
              </svg>
            </button>

            {showPassengerDropdown && (
              <>
                <div className="fixed inset-0 z-10" onClick={() => setShowPassengerDropdown(false)} />
                <div className="absolute left-0 right-0 z-20 mt-2 rounded-2xl border border-slate-100 bg-white p-4 shadow-xl ring-1 ring-black/5 animate-in fade-in slide-in-from-top-2 duration-150">
                  <div className="space-y-4">
                    {/* Adult */}
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-xs font-extrabold text-slate-800">Người lớn</p>
                        <p className="text-[10px] text-slate-400">Từ 12 tuổi trở lên</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <button
                          type="button"
                          disabled={passengerCounts.adult <= 1}
                          onClick={() => updatePassengerCount('adult', -1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50 disabled:opacity-40"
                        >
                          −
                        </button>
                        <span className="w-5 text-center text-xs font-bold text-slate-800">{passengerCounts.adult}</span>
                        <button
                          type="button"
                          onClick={() => updatePassengerCount('adult', 1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50"
                        >
                          +
                        </button>
                      </div>
                    </div>

                    {/* Child */}
                    <div className="flex items-center justify-between border-t border-slate-50 pt-3">
                      <div>
                        <p className="text-xs font-extrabold text-slate-800">Trẻ em</p>
                        <p className="text-[10px] text-slate-400">Từ 2 đến dưới 12 tuổi</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <button
                          type="button"
                          disabled={passengerCounts.child <= 0}
                          onClick={() => updatePassengerCount('child', -1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50 disabled:opacity-40"
                        >
                          −
                        </button>
                        <span className="w-5 text-center text-xs font-bold text-slate-800">{passengerCounts.child}</span>
                        <button
                          type="button"
                          onClick={() => updatePassengerCount('child', 1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50"
                        >
                          +
                        </button>
                      </div>
                    </div>

                    {/* Infant */}
                    <div className="flex items-center justify-between border-t border-slate-50 pt-3">
                      <div>
                        <p className="text-xs font-extrabold text-slate-800">Em bé</p>
                        <p className="text-[10px] text-slate-400">Dưới 2 tuổi (ngồi chung ghế)</p>
                      </div>
                      <div className="flex items-center gap-3">
                        <button
                          type="button"
                          disabled={passengerCounts.infant <= 0}
                          onClick={() => updatePassengerCount('infant', -1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50 disabled:opacity-40"
                        >
                          −
                        </button>
                        <span className="w-5 text-center text-xs font-bold text-slate-800">{passengerCounts.infant}</span>
                        <button
                          type="button"
                          onClick={() => updatePassengerCount('infant', 1)}
                          className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:bg-slate-50"
                        >
                          +
                        </button>
                      </div>
                    </div>
                  </div>
                  
                  <div className="mt-4 border-t border-slate-100 pt-3 text-right">
                    <button
                      type="button"
                      onClick={() => setShowPassengerDropdown(false)}
                      className="rounded-lg bg-brand-primary px-3 py-1.5 text-xs font-bold text-white transition hover:bg-blue-700"
                    >
                      Xác nhận
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>

          {/* Seat Class Selection (only for roundtrip) */}
          {tripType === 'roundtrip' && (
            <div>
              <label className="form-label flex items-center gap-1.5">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4 text-slate-500">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 01-1.043 3.296 3.745 3.745 0 01-3.296 1.043A3.745 3.745 0 0112 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 01-3.296-1.043 3.745 3.745 0 01-1.043-3.296A3.745 3.745 0 013 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 011.043-3.296 3.746 3.746 0 013.296-1.043A3.746 3.746 0 0112 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 013.296 1.043 3.746 3.746 0 011.043 3.296A3.745 3.745 0 0121 12z" />
                </svg>
                <span>Hạng ghế</span>
              </label>
              <select
                value={searchData.seatClass}
                onChange={(e) => setSearchData((prev) => ({ ...prev, seatClass: e.target.value }))}
                className="form-input appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2020%22%20fill%3D%22none%22%3E%3Cpath%20d%3D%22M7%209l3%203%203-3%22%20stroke%3D%22%2364748B%22%20stroke-width%3D%221.5%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%2F%3E%3C%2Fsvg%3E')] bg-[length:1.25rem] bg-[right_0.75rem_center] bg-no-repeat pr-10"
              >
                <option value="Economy">Economy</option>
                <option value="Business">Business</option>
              </select>
            </div>
          )}
        </div>

        {/* Submit Button & API Errors */}
        <div className="flex flex-col gap-4 border-t border-slate-100 pt-6 md:flex-row md:items-center md:justify-between">
          <p className="text-[10px] text-slate-400 max-w-lg">
            * Bằng việc nhấn nút Tìm kiếm, bạn đồng ý với các điều kiện điều khoản vận chuyển của FlyNow.vn và các quy định của Cục Hàng không Việt Nam.
          </p>
          
          <button
            type="button"
            onClick={performSearch}
            disabled={isLoadingFlights}
            className="btn-primary min-w-[160px] flex items-center justify-center gap-2"
          >
            {isLoadingFlights ? (
              <>
                <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                <span>Đang tìm...</span>
              </>
            ) : (
              'Tìm chuyến bay'
            )}
          </button>
        </div>

        {apiError && (
          <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600 animate-in fade-in duration-150">
            {apiError}
          </div>
        )}
      </div>
    </div>
  )
}
