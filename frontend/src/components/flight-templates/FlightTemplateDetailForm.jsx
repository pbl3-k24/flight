import { useEffect, useMemo, useState } from 'react'

const DAY_OPTIONS = [
  { value: 0, label: 'Thứ 2' },
  { value: 1, label: 'Thứ 3' },
  { value: 2, label: 'Thứ 4' },
  { value: 3, label: 'Thứ 5' },
  { value: 4, label: 'Thứ 6' },
  { value: 5, label: 'Thứ 7' },
  { value: 6, label: 'Chủ nhật' },
]

const Label = ({ children, required }) => (
  <p className="mb-1.5 text-sm font-semibold text-slate-700">
    {children}
    {required && <span className="ml-1 text-red-500">*</span>}
  </p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  />
)

const Select = ({ children, ...props }) => (
  <select
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  >
    {children}
  </select>
)

const formatTime = (value) => {
  if (!value) return '--:--'
  if (typeof value === 'string') return value.slice(0, 5)
  return String(value)
}

const toMinutes = (value) => {
  if (!value || typeof value !== 'string') return null
  const [hours, minutes] = value.split(':').map((part) => Number(part))
  if (Number.isNaN(hours) || Number.isNaN(minutes)) return null
  return hours * 60 + minutes
}

const getDetailKey = (detail) => detail?.id ?? detail?.tempId ?? null

export default function FlightTemplateDetailForm({
  open,
  onClose,
  onSubmit,
  existingDetails,
  flightDefinitions,
  aircrafts,
  initialValue,
}) {
  const [form, setForm] = useState({
    flightDefinitionId: '',
    dayOfWeek: '',
    aircraftOverrideId: '',
    departureTimeOverride: '',
    arrivalTimeOverride: '',
    arrivalOffsetDaysOverride: '',
    isActive: true,
    tempId: null,
    id: null,
  })
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')

  useEffect(() => {
    if (!open) return
    if (initialValue) {
      setForm({
        flightDefinitionId: initialValue.flightDefinitionId ?? '',
        dayOfWeek: initialValue.dayOfWeek ?? '',
        aircraftOverrideId: initialValue.aircraftOverrideId ?? '',
        departureTimeOverride: initialValue.departureTimeOverride
          ? String(initialValue.departureTimeOverride).slice(0, 5)
          : '',
        arrivalTimeOverride: initialValue.arrivalTimeOverride
          ? String(initialValue.arrivalTimeOverride).slice(0, 5)
          : '',
        arrivalOffsetDaysOverride: initialValue.arrivalOffsetDaysOverride ?? '',
        isActive: initialValue.isActive ?? true,
        tempId: initialValue.tempId ?? null,
        id: initialValue.id ?? null,
      })
    } else {
      setForm({
        flightDefinitionId: '',
        dayOfWeek: '',
        aircraftOverrideId: '',
        departureTimeOverride: '',
        arrivalTimeOverride: '',
        arrivalOffsetDaysOverride: '',
        isActive: true,
        tempId: null,
        id: null,
      })
    }
    setError('')
    setSearch('')
  }, [open, initialValue])

  const definitionMap = useMemo(
    () => new Map(flightDefinitions.map((item) => [item.id, item])),
    [flightDefinitions]
  )

  const filteredDefinitions = useMemo(() => {
    if (!search) return flightDefinitions
    const normalized = search.toLowerCase()
    return flightDefinitions.filter((item) => {
      const haystack = [
        item.flightNumber,
        item.routeName,
        item.departureAirportCode,
        item.arrivalAirportCode,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
      return haystack.includes(normalized)
    })
  }, [flightDefinitions, search])

  const selectedDefinition = definitionMap.get(Number(form.flightDefinitionId))

  const effectiveDeparture = form.departureTimeOverride || selectedDefinition?.departureTime
  const effectiveArrival = form.arrivalTimeOverride || selectedDefinition?.arrivalTime
  const effectiveOffset =
    form.arrivalOffsetDaysOverride !== ''
      ? Number(form.arrivalOffsetDaysOverride)
      : selectedDefinition?.arrivalOffsetDays ?? 0

  const crossesDay =
    effectiveOffset > 0 ||
    (toMinutes(effectiveArrival) !== null &&
      toMinutes(effectiveDeparture) !== null &&
      toMinutes(effectiveArrival) < toMinutes(effectiveDeparture))

  const selectedAircraftId =
    form.aircraftOverrideId !== ''
      ? Number(form.aircraftOverrideId)
      : selectedDefinition?.defaultAircraftId

  const selectedAircraft = aircrafts.find(
    (item) => (item.aircraftId ?? item.id) === selectedAircraftId
  )

  const hasDuplicate = () => {
    if (!form.flightDefinitionId || form.dayOfWeek === '') return false
    const currentId = getDetailKey(form)
    const currentDefinition = definitionMap.get(Number(form.flightDefinitionId))
    const currentDeparture = form.departureTimeOverride || currentDefinition?.departureTime

    return existingDetails.some((detail) => {
      const detailId = getDetailKey(detail)
      if (currentId && detailId === currentId) return false
      if (Number(detail.flightDefinitionId) !== Number(form.flightDefinitionId)) return false
      if (Number(detail.dayOfWeek) !== Number(form.dayOfWeek)) return false
      const detailDefinition = definitionMap.get(Number(detail.flightDefinitionId))
      const detailDeparture = detail.departureTimeOverride || detailDefinition?.departureTime
      return String(detailDeparture || '') === String(currentDeparture || '')
    })
  }

  const handleSubmit = () => {
    if (!form.flightDefinitionId || form.dayOfWeek === '') {
      setError('Vui lòng chọn Flight Definition và thứ trong tuần.')
      return
    }
    if (hasDuplicate()) {
      setError('Đã có dòng trùng FlightDefinition + DayOfWeek + giờ bay hiệu lực.')
      return
    }
    const departure = form.departureTimeOverride || selectedDefinition?.departureTime
    const arrival = form.arrivalTimeOverride || selectedDefinition?.arrivalTime
    const depMin = toMinutes(departure)
    const arrMin = toMinutes(arrival)
    const offsetDays =
      form.arrivalOffsetDaysOverride === ''
        ? Number(selectedDefinition?.arrivalOffsetDays ?? 0)
        : Number(form.arrivalOffsetDaysOverride)
    if (depMin !== null && arrMin !== null && arrMin < depMin && !(offsetDays > 0)) {
      setError('Giờ đến sớm hơn giờ đi thì arrivalOffsetDaysOverride phải > 0.')
      return
    }
    setError('')
    onSubmit({
      ...form,
      flightDefinitionId: Number(form.flightDefinitionId),
      dayOfWeek: Number(form.dayOfWeek),
      aircraftOverrideId: form.aircraftOverrideId === '' ? null : Number(form.aircraftOverrideId),
      arrivalOffsetDaysOverride:
        form.arrivalOffsetDaysOverride === '' ? null : Number(form.arrivalOffsetDaysOverride),
      departureTimeOverride: form.departureTimeOverride || null,
      arrivalTimeOverride: form.arrivalTimeOverride || null,
      tempId: form.tempId || `tmp-${Date.now()}`,
    })
  }

  if (!open) return null

  const isEditing = Boolean(initialValue)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
      <div className="w-full max-w-4xl rounded-2xl bg-white shadow-2xl overflow-hidden">

        {/* Modal header */}
        <div className="border-b border-slate-100 bg-gradient-to-r from-slate-800 to-slate-700 px-6 py-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/10">
                <svg className="h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
              </div>
              <div>
                <h3 className="text-lg font-bold text-white">
                  {isEditing ? 'Cập nhật dòng lịch' : 'Thêm dòng lịch mới'}
                </h3>
                <p className="text-sm text-slate-300">
                  Chọn flight definition, ngày bay và các thông tin override
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="flex h-8 w-8 items-center justify-center rounded-lg bg-white/10 text-white transition hover:bg-white/20"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* Modal body */}
        <div className="p-6">
          {/* Error */}
          {error && (
            <div className="mb-5 flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <svg className="h-4 w-4 shrink-0 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {error}
            </div>
          )}

          <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
            {/* Left: Form fields */}
            <div className="space-y-4">
              {/* Search definitions */}
              <div>
                <Label>Tìm flight definition</Label>
                <div className="relative">
                  <svg className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                  <input
                    className="w-full rounded-xl border border-slate-200 bg-white py-2.5 pl-9 pr-4 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
                    placeholder="Nhập mã chuyến bay hoặc sân bay..."
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                </div>
              </div>

              {/* Flight definition select */}
              <div>
                <Label required>Flight Definition</Label>
                <Select
                  value={form.flightDefinitionId}
                  onChange={(e) => setForm((prev) => ({ ...prev, flightDefinitionId: e.target.value }))}
                >
                  <option value="">-- Chọn flight definition --</option>
                  {filteredDefinitions.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.flightNumber} • {item.departureAirportCode} → {item.arrivalAirportCode}
                      {item.routeName ? ` (${item.routeName})` : ''}
                    </option>
                  ))}
                </Select>
                {filteredDefinitions.length === 0 && search && (
                  <p className="mt-1 text-xs text-slate-400">Không tìm thấy kết quả phù hợp</p>
                )}
              </div>

              {/* Day of week */}
              <div>
                <Label required>Ngày bay</Label>
                <Select
                  value={form.dayOfWeek}
                  onChange={(e) => setForm((prev) => ({ ...prev, dayOfWeek: e.target.value }))}
                >
                  <option value="">-- Chọn thứ --</option>
                  {DAY_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              </div>

              {/* Aircraft override */}
              <div>
                <Label>Máy bay override <span className="text-xs font-normal text-slate-400">(tuỳ chọn)</span></Label>
                <Select
                  value={form.aircraftOverrideId}
                  onChange={(e) => setForm((prev) => ({ ...prev, aircraftOverrideId: e.target.value }))}
                >
                  <option value="">-- Không override --</option>
                  {aircrafts.map((item) => (
                    <option key={item.aircraftId ?? item.id} value={item.aircraftId ?? item.id}>
                      {item.registrationNumber || item.model || `Aircraft ${item.aircraftId ?? item.id}`}
                    </option>
                  ))}
                </Select>
              </div>

              {/* Time overrides */}
              <div className="grid gap-3 md:grid-cols-2">
                <div>
                  <Label>Giờ đi override</Label>
                  <Input
                    type="time"
                    value={form.departureTimeOverride}
                    onChange={(e) => setForm((prev) => ({ ...prev, departureTimeOverride: e.target.value }))}
                  />
                  {selectedDefinition && (
                    <p className="mt-1 text-xs text-slate-400">
                      Default: {formatTime(selectedDefinition.departureTime)}
                    </p>
                  )}
                </div>
                <div>
                  <Label>Giờ đến override</Label>
                  <Input
                    type="time"
                    value={form.arrivalTimeOverride}
                    onChange={(e) => setForm((prev) => ({ ...prev, arrivalTimeOverride: e.target.value }))}
                  />
                  {selectedDefinition && (
                    <p className="mt-1 text-xs text-slate-400">
                      Default: {formatTime(selectedDefinition.arrivalTime)}
                    </p>
                  )}
                </div>
              </div>

              {/* Offset + Active */}
              <div className="grid gap-3 md:grid-cols-2">
                <div>
                  <Label>Offset ngày đến override</Label>
                  <Input
                    type="number"
                    min="0"
                    max="2"
                    placeholder={String(selectedDefinition?.arrivalOffsetDays ?? '0')}
                    value={form.arrivalOffsetDaysOverride}
                    onChange={(e) => setForm((prev) => ({ ...prev, arrivalOffsetDaysOverride: e.target.value }))}
                  />
                </div>
                <div className="flex items-end pb-0.5">
                  <label className="flex cursor-pointer items-center gap-2.5">
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
                      className="h-4 w-4 rounded border-slate-300 accent-blue-600"
                    />
                    <div>
                      <p className="text-sm font-semibold text-slate-700">Kích hoạt</p>
                      <p className="text-xs text-slate-400">Dòng lịch đang hoạt động</p>
                    </div>
                  </label>
                </div>
              </div>
            </div>

            {/* Right: Preview card */}
            <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-slate-50 to-blue-50/30 p-5">
              <h4 className="mb-4 flex items-center gap-2 text-sm font-bold text-slate-900">
                <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                Preview
              </h4>

              {!selectedDefinition ? (
                <div className="flex flex-col items-center justify-center gap-2 py-8 text-center text-slate-400">
                  <svg className="h-10 w-10 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                  </svg>
                  <p className="text-sm">Chọn flight definition để xem preview</p>
                </div>
              ) : (
                <div className="space-y-4 text-sm">
                  {/* Default info */}
                  <div>
                    <p className="mb-1.5 text-[10px] font-bold uppercase tracking-widest text-slate-400">Mặc định</p>
                    <div className="rounded-xl bg-white p-3 shadow-sm">
                      <p className="font-bold text-slate-900">
                        {selectedDefinition.flightNumber}
                      </p>
                      <p className="text-xs text-slate-500">
                        {selectedDefinition.routeName || `${selectedDefinition.departureAirportCode || '--'} → ${selectedDefinition.arrivalAirportCode || '--'}`}
                      </p>
                      <div className="mt-2 flex items-center gap-2 font-mono text-sm">
                        <span className="font-bold text-emerald-600">{formatTime(selectedDefinition.departureTime)}</span>
                        <svg className="h-3.5 w-3.5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3" />
                        </svg>
                        <span className="font-bold text-blue-600">{formatTime(selectedDefinition.arrivalTime)}</span>
                        <span className="text-xs text-slate-400">+{selectedDefinition.arrivalOffsetDays ?? 0}d</span>
                      </div>
                    </div>
                  </div>

                  {/* Effective info */}
                  <div>
                    <p className="mb-1.5 text-[10px] font-bold uppercase tracking-widest text-slate-400">Thực tế (sau override)</p>
                    <div className="rounded-xl bg-white p-3 shadow-sm">
                      <p className="font-semibold text-slate-700">
                        {selectedAircraft?.registrationNumber || selectedAircraft?.model || (
                          <span className="text-slate-400 font-normal">Không có máy bay</span>
                        )}
                      </p>
                      <div className="mt-2 flex items-center gap-2 font-mono text-sm">
                        <span className={`font-bold ${form.departureTimeOverride ? 'text-amber-600' : 'text-emerald-600'}`}>
                          {formatTime(effectiveDeparture)}
                        </span>
                        <svg className="h-3.5 w-3.5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3" />
                        </svg>
                        <span className={`font-bold ${form.arrivalTimeOverride ? 'text-amber-600' : 'text-blue-600'}`}>
                          {formatTime(effectiveArrival)}
                        </span>
                        <span className="text-xs text-slate-400">+{effectiveOffset}d</span>
                        {crossesDay && (
                          <span className="ml-1 rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-bold text-amber-700">
                            qua ngày
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Selected day */}
                  {form.dayOfWeek !== '' && (
                    <div className="flex items-center gap-2 rounded-xl bg-blue-100 px-3 py-2">
                      <svg className="h-4 w-4 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      <span className="text-sm font-bold text-blue-700">
                        {DAY_OPTIONS.find((o) => String(o.value) === String(form.dayOfWeek))?.label || '---'}
                      </span>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Modal footer */}
        <div className="flex justify-end gap-2.5 border-t border-slate-100 bg-slate-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl border border-slate-200 bg-white px-5 py-2 text-sm font-semibold text-slate-600 shadow-sm transition hover:bg-slate-100"
          >
            Hủy
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            className="flex items-center gap-1.5 rounded-xl bg-[#1E40AF] px-5 py-2 text-sm font-bold text-white shadow-sm transition hover:bg-blue-800"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
            </svg>
            {isEditing ? 'Cập nhật' : 'Lưu dòng lịch'}
          </button>
        </div>
      </div>
    </div>
  )
}
