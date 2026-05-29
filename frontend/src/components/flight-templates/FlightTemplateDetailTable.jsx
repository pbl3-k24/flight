import { Fragment, useState } from 'react'

const DAY_LABELS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']
const DAY_OPTIONS = [
  { value: 0, label: 'Thứ 2' },
  { value: 1, label: 'Thứ 3' },
  { value: 2, label: 'Thứ 4' },
  { value: 3, label: 'Thứ 5' },
  { value: 4, label: 'Thứ 6' },
  { value: 5, label: 'Thứ 7' },
  { value: 6, label: 'Chủ nhật' },
]

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

const getDetailKey = (detail) =>
  detail.id ?? detail.tempId ?? `${detail.flightDefinitionId}-${detail.dayOfWeek}`

const emptyRow = () => ({
  tempId: `tmp-${Date.now()}-${Math.random().toString(16).slice(2)}`,
  id: null,
  flightDefinitionId: '',
  dayOfWeek: '',
  aircraftOverrideId: '',
  departureTimeOverride: '',
  arrivalTimeOverride: '',
  arrivalOffsetDaysOverride: '',
  isActive: true,
  _editing: true,
})

const CellSelect = ({ value, onChange, children, disabled }) => (
  <select
    value={value}
    onChange={onChange}
    disabled={disabled}
    className="w-full min-w-[130px] rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs outline-none transition
      focus:border-blue-400 focus:ring-2 focus:ring-blue-100
      disabled:bg-slate-50 disabled:text-slate-400"
  >
    {children}
  </select>
)

const CellInput = ({ value, onChange, type = 'text', placeholder = '', disabled }) => (
  <input
    type={type}
    value={value}
    onChange={onChange}
    placeholder={placeholder}
    disabled={disabled}
    className="w-full min-w-[90px] rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs outline-none transition
      focus:border-blue-400 focus:ring-2 focus:ring-blue-100
      disabled:bg-slate-50 disabled:text-slate-400"
  />
)

export default function FlightTemplateDetailTable({
  details,
  definitions,
  aircrafts,
  onAdd,
  onEdit,
  onDelete,
  readOnly,
}) {
  const [editingRows, setEditingRows] = useState([])
  const [rowErrors, setRowErrors] = useState({})

  const definitionMap = new Map(definitions.map((item) => [item.id, item]))
  const aircraftMap = new Map(
    aircrafts.map((item) => [item.aircraftId ?? item.id, item])
  )

  const resolveDetail = (detail) => {
    const definition = definitionMap.get(detail.flightDefinitionId)
    const defaultDeparture = detail.departureTime || definition?.departureTime
    const defaultArrival = detail.arrivalTime || definition?.arrivalTime
    const defaultOffset = detail.arrivalOffsetDays ?? definition?.arrivalOffsetDays ?? 0

    const effectiveDeparture = detail.departureTimeOverride || defaultDeparture
    const effectiveArrival = detail.arrivalTimeOverride || defaultArrival
    const effectiveOffset = detail.arrivalOffsetDaysOverride ?? defaultOffset

    const aircraftId = detail.aircraftOverrideId || definition?.defaultAircraftId
    const aircraft = aircraftMap.get(aircraftId)

    const crossesDay =
      effectiveOffset > 0 ||
      (toMinutes(effectiveArrival) !== null &&
        toMinutes(effectiveDeparture) !== null &&
        toMinutes(effectiveArrival) < toMinutes(effectiveDeparture))

    return {
      ...detail,
      definition,
      aircraft,
      effectiveDeparture,
      effectiveArrival,
      effectiveOffset,
      crossesDay,
    }
  }

  const handleAddRow = () => {
    const row = emptyRow()
    setEditingRows((prev) => [...prev, row])
    setRowErrors((prev) => ({ ...prev, [row.tempId]: '' }))
  }

  const updateEditingRow = (tempId, field, value) => {
    setEditingRows((prev) =>
      prev.map((row) => (row.tempId === tempId ? { ...row, [field]: value } : row))
    )
  }

  const validateRow = (row) => {
    if (!row.flightDefinitionId && row.flightDefinitionId !== 0)
      return 'Vui lòng chọn Flight Definition.'
    if (row.dayOfWeek === '' || row.dayOfWeek === null || row.dayOfWeek === undefined)
      return 'Vui lòng chọn ngày bay.'
    const isDuplicate = details.some(
      (d) =>
        Number(d.flightDefinitionId) === Number(row.flightDefinitionId) &&
        Number(d.dayOfWeek) === Number(row.dayOfWeek)
    )
    if (isDuplicate) return 'Đã tồn tại dòng với Flight Definition và ngày bay này.'
    const departure = row.departureTimeOverride || definitionMap.get(Number(row.flightDefinitionId))?.departureTime
    const arrival = row.arrivalTimeOverride || definitionMap.get(Number(row.flightDefinitionId))?.arrivalTime
    const depMin = toMinutes(departure)
    const arrMin = toMinutes(arrival)
    const offsetDays =
      row.arrivalOffsetDaysOverride === '' ||
      row.arrivalOffsetDaysOverride === null ||
      row.arrivalOffsetDaysOverride === undefined
        ? Number(definitionMap.get(Number(row.flightDefinitionId))?.arrivalOffsetDays ?? 0)
        : Number(row.arrivalOffsetDaysOverride)
    if (depMin !== null && arrMin !== null && arrMin < depMin && !(offsetDays > 0)) {
      return 'Giờ đến sớm hơn giờ đi thì arrivalOffsetDaysOverride phải > 0.'
    }
    return ''
  }

  const handleSaveRow = (row) => {
    const error = validateRow(row)
    if (error) {
      setRowErrors((prev) => ({ ...prev, [row.tempId]: error }))
      return
    }

    const payload = {
      tempId: row.tempId,
      id: null,
      flightDefinitionId: Number(row.flightDefinitionId),
      dayOfWeek: Number(row.dayOfWeek),
      aircraftOverrideId: row.aircraftOverrideId !== '' ? Number(row.aircraftOverrideId) : null,
      departureTimeOverride: row.departureTimeOverride || null,
      arrivalTimeOverride: row.arrivalTimeOverride || null,
      arrivalOffsetDaysOverride:
        row.arrivalOffsetDaysOverride !== '' ? Number(row.arrivalOffsetDaysOverride) : null,
      isActive: Boolean(row.isActive),
    }

    onEdit && onEdit(payload, true)
    setEditingRows((prev) => prev.filter((r) => r.tempId !== row.tempId))
    setRowErrors((prev) => {
      const next = { ...prev }
      delete next[row.tempId]
      return next
    })
  }

  const handleCancelRow = (tempId) => {
    setEditingRows((prev) => prev.filter((r) => r.tempId !== tempId))
    setRowErrors((prev) => {
      const next = { ...prev }
      delete next[tempId]
      return next
    })
  }

  const resolvedDetails = details.map(resolveDetail)
  const hasAnyRow = resolvedDetails.length > 0 || editingRows.length > 0

  return (
    <section className="rounded-2xl bg-white shadow-lg shadow-slate-100 overflow-hidden">
      {/* Section header */}
      <div className="border-b border-slate-100 bg-slate-50 px-6 py-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#1E40AF]/10">
              <svg className="h-4 w-4 text-[#1E40AF]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064" />
              </svg>
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Chi tiết lịch bay</h3>
              <p className="text-xs text-slate-500">
                Quản lý các dòng lịch theo flight definition và thứ trong tuần
                {resolvedDetails.length > 0 && (
                  <span className="ml-2 inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-xs font-semibold text-blue-700">
                    {resolvedDetails.length} dòng
                  </span>
                )}
              </p>
            </div>
          </div>

          {!readOnly && (
            <button
              type="button"
              onClick={handleAddRow}
              className="flex items-center gap-1.5 rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-800 active:scale-95"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
              </svg>
              Thêm dòng lịch
            </button>
          )}
        </div>
      </div>

      {/* Empty state */}
      {!hasAnyRow && (
        <div className="flex flex-col items-center justify-center gap-3 py-16 text-slate-400">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100">
            <svg className="h-7 w-7 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064" />
            </svg>
          </div>
          <div className="text-center">
            <p className="font-semibold text-slate-600">Chưa có dòng lịch nào</p>
            <p className="mt-0.5 text-sm text-slate-400">Thêm dòng lịch để cấu hình chuyến bay định kỳ</p>
          </div>
          {!readOnly && (
            <button
              type="button"
              onClick={handleAddRow}
              className="mt-1 rounded-xl border-2 border-dashed border-blue-300 px-5 py-2 text-sm font-semibold text-blue-600 transition hover:border-blue-400 hover:bg-blue-50"
            >
              + Thêm dòng đầu tiên
            </button>
          )}
        </div>
      )}

      {/* Table */}
      {hasAnyRow && (
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50/70">
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Flight Definition</th>
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Ngày bay</th>
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Máy bay</th>
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Giờ đi</th>
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Giờ đến</th>
                <th className="px-4 py-3 text-center text-xs font-bold uppercase tracking-wide text-slate-500">Offset</th>
                <th className="px-4 py-3 text-xs font-bold uppercase tracking-wide text-slate-500">Trạng thái</th>
                {!readOnly && (
                  <th className="px-4 py-3 text-right text-xs font-bold uppercase tracking-wide text-slate-500">Hành động</th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50">

              {/* Saved rows */}
              {resolvedDetails.map((detail) => (
                <tr key={getDetailKey(detail)} className="group transition-colors hover:bg-blue-50/30">
                  {/* Flight Definition */}
                  <td className="px-4 py-3">
                    <div className="font-semibold text-slate-900">
                      {detail.flightNumber || detail.definition?.flightNumber || `ID: ${detail.flightDefinitionId}`}
                    </div>
                    <div className="mt-0.5 text-xs text-slate-400">
                      {detail.definition?.routeName ||
                        `${detail.definition?.departureAirportCode || '--'} → ${detail.definition?.arrivalAirportCode || '--'}`}
                    </div>
                  </td>

                  {/* Day of week */}
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-1 text-xs font-bold text-blue-700">
                      {DAY_LABELS[detail.dayOfWeek] || '---'}
                    </span>
                  </td>

                  {/* Aircraft */}
                  <td className="px-4 py-3">
                    <div className="text-xs font-semibold text-slate-800">
                      {detail.aircraft?.registrationNumber || detail.aircraft?.model || (
                        <span className="text-slate-400">---</span>
                      )}
                    </div>
                    {detail.aircraftOverrideId && (
                      <div className="mt-0.5 text-[10px] text-amber-600 font-semibold">override</div>
                    )}
                    {!detail.aircraftOverrideId && detail.definition?.defaultAircraftId && (
                      <div className="mt-0.5 text-[10px] text-slate-400">default</div>
                    )}
                  </td>

                  {/* Departure */}
                  <td className="px-4 py-3">
                    <span className="font-mono text-sm font-bold text-emerald-600">
                      {formatTime(detail.effectiveDeparture)}
                    </span>
                    {detail.departureTimeOverride && (
                      <span className="ml-1.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-600">
                        override
                      </span>
                    )}
                  </td>

                  {/* Arrival */}
                  <td className="px-4 py-3">
                    <span className="font-mono text-sm font-bold text-blue-600">
                      {formatTime(detail.effectiveArrival)}
                    </span>
                    {detail.crossesDay && (
                      <span className="ml-1.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold text-amber-600">
                        +1
                      </span>
                    )}
                    {detail.arrivalTimeOverride && (
                      <span className="ml-1.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-600">
                        override
                      </span>
                    )}
                  </td>

                  {/* Offset */}
                  <td className="px-4 py-3 text-center">
                    <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-600">
                      {detail.effectiveOffset}
                    </span>
                  </td>

                  {/* Status */}
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold
                      ${detail.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}
                    >
                      <span className={`h-1.5 w-1.5 rounded-full ${detail.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                      {detail.isActive ? 'Hoạt động' : 'Tạm dừng'}
                    </span>
                  </td>

                  {/* Actions */}
                  {!readOnly && (
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-1.5">
                        <button
                          type="button"
                          onClick={() => onEdit(detail)}
                          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm transition hover:bg-slate-50"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => onDelete(detail)}
                          className="rounded-lg bg-red-500 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-red-600"
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}

              {/* Inline editing rows */}
              {editingRows.map((row) => {
                const selectedDef = definitionMap.get(Number(row.flightDefinitionId))
                const rowError = rowErrors[row.tempId]
                return (
                  <Fragment key={row.tempId}>
                    <tr className="border-l-4 border-l-blue-500 bg-blue-50/60">
                      {/* Flight Definition select */}
                      <td className="px-3 py-2.5">
                        <CellSelect
                          value={row.flightDefinitionId}
                          onChange={(e) => updateEditingRow(row.tempId, 'flightDefinitionId', e.target.value)}
                        >
                          <option value="">-- Chọn Flight Definition --</option>
                          {definitions.map((item) => (
                            <option key={item.id} value={item.id}>
                              [{item.id}] {item.flightNumber} •{' '}
                              {item.departureAirportCode || '--'} → {item.arrivalAirportCode || '--'}
                            </option>
                          ))}
                        </CellSelect>
                        {selectedDef && (
                          <div className="mt-1 text-[10px] text-blue-600">
                            {selectedDef.routeName || ''} • {formatTime(selectedDef.departureTime)} → {formatTime(selectedDef.arrivalTime)}
                          </div>
                        )}
                      </td>

                      {/* Day select */}
                      <td className="px-3 py-2.5">
                        <CellSelect
                          value={row.dayOfWeek}
                          onChange={(e) => updateEditingRow(row.tempId, 'dayOfWeek', e.target.value)}
                        >
                          <option value="">-- Chọn ngày --</option>
                          {DAY_OPTIONS.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                              {opt.label}
                            </option>
                          ))}
                        </CellSelect>
                      </td>

                      {/* Aircraft override */}
                      <td className="px-3 py-2.5">
                        <CellSelect
                          value={row.aircraftOverrideId}
                          onChange={(e) => updateEditingRow(row.tempId, 'aircraftOverrideId', e.target.value)}
                        >
                          <option value="">-- Không override --</option>
                          {aircrafts.map((item) => {
                            const id = item.aircraftId ?? item.id
                            return (
                              <option key={id} value={id}>
                                [{id}] {item.registrationNumber || item.model || `Aircraft ${id}`}
                              </option>
                            )
                          })}
                        </CellSelect>
                        {selectedDef?.defaultAircraftId && !row.aircraftOverrideId && (
                          <div className="mt-1 text-[10px] text-slate-400">
                            Default: {selectedDef.defaultAircraftId}
                          </div>
                        )}
                      </td>

                      {/* Departure override */}
                      <td className="px-3 py-2.5">
                        <CellInput
                          type="time"
                          value={row.departureTimeOverride}
                          onChange={(e) => updateEditingRow(row.tempId, 'departureTimeOverride', e.target.value)}
                        />
                        {selectedDef && !row.departureTimeOverride && (
                          <div className="mt-1 text-[10px] text-slate-400">
                            Default: {formatTime(selectedDef.departureTime)}
                          </div>
                        )}
                      </td>

                      {/* Arrival override */}
                      <td className="px-3 py-2.5">
                        <CellInput
                          type="time"
                          value={row.arrivalTimeOverride}
                          onChange={(e) => updateEditingRow(row.tempId, 'arrivalTimeOverride', e.target.value)}
                        />
                        {selectedDef && !row.arrivalTimeOverride && (
                          <div className="mt-1 text-[10px] text-slate-400">
                            Default: {formatTime(selectedDef.arrivalTime)}
                          </div>
                        )}
                      </td>

                      {/* Offset override */}
                      <td className="px-3 py-2.5">
                        <CellInput
                          type="number"
                          value={row.arrivalOffsetDaysOverride}
                          onChange={(e) => updateEditingRow(row.tempId, 'arrivalOffsetDaysOverride', e.target.value)}
                          placeholder={selectedDef?.arrivalOffsetDays ?? '0'}
                        />
                      </td>

                      {/* Status toggle */}
                      <td className="px-3 py-2.5">
                        <label className="flex cursor-pointer items-center gap-2">
                          <input
                            type="checkbox"
                            checked={row.isActive}
                            onChange={(e) => updateEditingRow(row.tempId, 'isActive', e.target.checked)}
                            className="h-4 w-4 rounded border-slate-300 accent-blue-600"
                          />
                          <span className="text-xs text-slate-600">Hoạt động</span>
                        </label>
                      </td>

                      {/* Row actions */}
                      <td className="px-3 py-2.5 text-right">
                        <div className="flex justify-end gap-1.5">
                          <button
                            type="button"
                            onClick={() => handleSaveRow(row)}
                            className="flex items-center gap-1 rounded-lg bg-blue-600 px-3 py-1.5 text-xs font-bold text-white shadow-sm transition hover:bg-blue-700"
                          >
                            <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                              <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                            </svg>
                            Lưu
                          </button>
                          <button
                            type="button"
                            onClick={() => handleCancelRow(row.tempId)}
                            className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm transition hover:bg-slate-50"
                          >
                            Hủy
                          </button>
                        </div>
                      </td>
                    </tr>

                    {/* Row error */}
                    {rowError && (
                      <tr key={`error-${row.tempId}`} className="bg-red-50">
                        <td colSpan={8} className="px-4 py-2 text-xs font-semibold text-red-600">
                          <div className="flex items-center gap-1.5">
                            <svg className="h-3.5 w-3.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                              <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                            </svg>
                            {rowError}
                          </div>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Pending rows hint */}
      {editingRows.length > 0 && (
        <div className="border-t border-slate-100 bg-amber-50 px-5 py-2.5">
          <p className="flex items-center gap-1.5 text-xs text-amber-700">
            <svg className="h-3.5 w-3.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
            Đang có <strong>{editingRows.length}</strong> dòng chưa lưu. Nhấn ✓ Lưu để xác nhận từng dòng.
          </p>
        </div>
      )}
    </section>
  )
}
