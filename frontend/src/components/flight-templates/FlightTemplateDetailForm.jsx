import { useEffect, useMemo, useState } from 'react'

const dayOptions = [
  { value: 0, label: 'Thứ 2' },
  { value: 1, label: 'Thứ 3' },
  { value: 2, label: 'Thứ 4' },
  { value: 3, label: 'Thứ 5' },
  { value: 4, label: 'Thứ 6' },
  { value: 5, label: 'Thứ 7' },
  { value: 6, label: 'Chủ nhật' },
]

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
        arrivalOffsetDaysOverride:
          initialValue.arrivalOffsetDaysOverride ?? '',
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

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-4xl rounded-2xl bg-white p-6 shadow-2xl">
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h3 className="text-xl font-bold text-slate-900">
              {initialValue ? 'Cập nhật detail' : 'Thêm detail mới'}
            </h3>
            <p className="text-sm text-slate-500">
              Chọn flight definition, ngày bay và các thông tin override.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg bg-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-300"
          >
            ✕ Đóng
          </button>
        </div>

        {error && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-[1.3fr_1fr]">
          <div className="space-y-4">
            <div>
              <Label>Tìm flight definition</Label>
              <Input
                placeholder="Nhập mã chuyến bay hoặc sân bay..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div>
              <Label>Flight definition *</Label>
              <Select
                value={form.flightDefinitionId}
                onChange={(e) =>
                  setForm((prev) => ({
                    ...prev,
                    flightDefinitionId: e.target.value,
                  }))
                }
              >
                <option value="">Chọn flight definition</option>
                {filteredDefinitions.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.flightNumber} · {item.departureAirportCode} → {item.arrivalAirportCode}
                  </option>
                ))}
              </Select>
            </div>
            <div>
              <Label>Ngày bay *</Label>
              <Select
                value={form.dayOfWeek}
                onChange={(e) => setForm((prev) => ({ ...prev, dayOfWeek: e.target.value }))}
              >
                <option value="">Chọn thứ</option>
                {dayOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            </div>
            <div>
              <Label>Máy bay override (optional)</Label>
              <Select
                value={form.aircraftOverrideId}
                onChange={(e) =>
                  setForm((prev) => ({
                    ...prev,
                    aircraftOverrideId: e.target.value,
                  }))
                }
              >
                <option value="">Không override</option>
                {aircrafts.map((item) => (
                  <option key={item.aircraftId ?? item.id} value={item.aircraftId ?? item.id}>
                    {item.registrationNumber || item.model || `Aircraft ${item.aircraftId ?? item.id}`}
                  </option>
                ))}
              </Select>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <Label>Giờ đi override</Label>
                <Input
                  type="time"
                  value={form.departureTimeOverride}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, departureTimeOverride: e.target.value }))
                  }
                />
              </div>
              <div>
                <Label>Giờ đến override</Label>
                <Input
                  type="time"
                  value={form.arrivalTimeOverride}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, arrivalTimeOverride: e.target.value }))
                  }
                />
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <Label>Offset ngày đến override</Label>
                <Input
                  type="number"
                  min="0"
                  value={form.arrivalOffsetDaysOverride}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, arrivalOffsetDaysOverride: e.target.value }))
                  }
                />
              </div>
              <div className="flex items-end">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) =>
                      setForm((prev) => ({ ...prev, isActive: e.target.checked }))
                    }
                    className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
                  />
                  <span className="text-sm text-slate-700">Đang hoạt động</span>
                </label>
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <h4 className="mb-3 text-sm font-bold text-slate-900">Preview</h4>
            {!selectedDefinition && (
              <p className="text-sm text-slate-500">Chọn flight definition để xem preview.</p>
            )}
            {selectedDefinition && (
              <div className="space-y-3 text-sm text-slate-700">
                <div>
                  <p className="text-xs uppercase tracking-wide text-slate-400">Mặc định</p>
                  <p className="font-semibold text-slate-900">
                    {selectedDefinition.flightNumber} · {selectedDefinition.routeName || `${
                      selectedDefinition.departureAirportCode || '--'
                    } → ${selectedDefinition.arrivalAirportCode || '--'}`}
                  </p>
                  <p>Máy bay: {selectedDefinition.defaultAircraftId || '--'}</p>
                  <p>
                    {formatTime(selectedDefinition.departureTime)} → {formatTime(selectedDefinition.arrivalTime)}
                  </p>
                  <p>Offset ngày: {selectedDefinition.arrivalOffsetDays ?? 0}</p>
                </div>
                <div>
                  <p className="text-xs uppercase tracking-wide text-slate-400">Thực tế</p>
                  <p className="font-semibold text-slate-900">
                    Máy bay: {selectedAircraft?.registrationNumber || selectedAircraft?.model || selectedAircraftId || '--'}
                  </p>
                  <p>
                    {formatTime(effectiveDeparture)} → {formatTime(effectiveArrival)}
                  </p>
                  <p>Offset ngày: {effectiveOffset}</p>
                  {crossesDay && (
                    <span className="inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700">
                      Qua ngày
                    </span>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="mt-6 flex flex-wrap justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Hủy
          </button>
          <button
            type="button"
            onClick={() => {
              if (!form.flightDefinitionId || form.dayOfWeek === '') {
                setError('Vui lòng chọn flight definition và thứ trong tuần.')
                return
              }

              if (hasDuplicate()) {
                setError('Đã có dòng trùng FlightDefinition + DayOfWeek + giờ bay hiệu lực.')
                return
              }

              setError('')
              onSubmit({
                ...form,
                flightDefinitionId: Number(form.flightDefinitionId),
                dayOfWeek: Number(form.dayOfWeek),
                aircraftOverrideId:
                  form.aircraftOverrideId === '' ? null : Number(form.aircraftOverrideId),
                arrivalOffsetDaysOverride:
                  form.arrivalOffsetDaysOverride === ''
                    ? null
                    : Number(form.arrivalOffsetDaysOverride),
                departureTimeOverride: form.departureTimeOverride || null,
                arrivalTimeOverride: form.arrivalTimeOverride || null,
                tempId: form.tempId || `tmp-${Date.now()}`,
              })
            }}
            className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
          >
            Lưu detail
          </button>
        </div>
      </div>
    </div>
  )
}
