const dayLabels = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật']

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

const getDetailKey = (detail) => detail.id ?? detail.tempId ?? `${detail.flightDefinitionId}-${detail.dayOfWeek}`

export default function FlightTemplateDetailTable({
  details,
  definitions,
  aircrafts,
  onAdd,
  onEdit,
  onDelete,
  readOnly,
}) {
  const definitionMap = new Map(definitions.map((item) => [item.id, item]))
  const aircraftMap = new Map(
    aircrafts.map((item) => [item.aircraftId ?? item.id, item])
  )

  const resolvedDetails = details.map((detail) => {
    const definition = definitionMap.get(detail.flightDefinitionId)
    const defaultDeparture = detail.departureTime || definition?.departureTime
    const defaultArrival = detail.arrivalTime || definition?.arrivalTime
    const defaultOffset =
      detail.arrivalOffsetDays ?? definition?.arrivalOffsetDays ?? 0

    const effectiveDeparture = detail.departureTimeOverride || defaultDeparture
    const effectiveArrival = detail.arrivalTimeOverride || defaultArrival
    const effectiveOffset =
      detail.arrivalOffsetDaysOverride ?? defaultOffset

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
  })

  return (
    <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-lg font-bold text-slate-900">Chi tiết lịch bay</h3>
          <p className="text-sm text-slate-500">
            Quản lý các dòng lịch theo flight definition và thứ trong tuần.
          </p>
        </div>
        {!readOnly && (
          <button
            type="button"
            onClick={onAdd}
            className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
          >
            + Thêm detail
          </button>
        )}
      </div>

      {details.length === 0 && (
        <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Chưa có dòng lịch nào.
        </div>
      )}

      {details.length > 0 && (
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-3 py-3">Chuyến bay</th>
                <th className="px-3 py-3">Ngày bay</th>
                <th className="px-3 py-3">Giờ bay</th>
                <th className="px-3 py-3">Máy bay</th>
                <th className="px-3 py-3">Offset</th>
                <th className="px-3 py-3">Trạng thái</th>
                {!readOnly && <th className="px-3 py-3 text-right">Hành động</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {resolvedDetails.map((detail) => (
                <tr key={getDetailKey(detail)} className="text-slate-700">
                  <td className="px-3 py-3">
                    <div className="font-semibold text-slate-900">
                      {detail.flightNumber || detail.definition?.flightNumber || '--'}
                    </div>
                    <div className="text-xs text-slate-500">
                      {detail.definition?.routeName ||
                        `${detail.definition?.departureAirportCode || '--'} → ${
                          detail.definition?.arrivalAirportCode || '--'
                        }`}
                    </div>
                  </td>
                  <td className="px-3 py-3">
                    <span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-700">
                      {dayLabels[detail.dayOfWeek] || '---'}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-xs">
                    <div>
                      {formatTime(detail.effectiveDeparture)} → {formatTime(detail.effectiveArrival)}
                    </div>
                    {(detail.departureTimeOverride || detail.arrivalTimeOverride) && (
                      <div className="text-slate-400">Có override giờ bay</div>
                    )}
                    {detail.crossesDay && (
                      <span className="mt-1 inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-semibold text-amber-700">
                        Qua ngày
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-3 text-xs">
                    <div className="font-semibold text-slate-900">
                      {detail.aircraft?.registrationNumber || detail.aircraft?.model || '---'}
                    </div>
                    {detail.aircraftOverrideId && (
                      <div className="text-slate-400">Override máy bay</div>
                    )}
                  </td>
                  <td className="px-3 py-3 text-xs">
                    {detail.effectiveOffset}
                    {detail.arrivalOffsetDaysOverride !== null &&
                      detail.arrivalOffsetDaysOverride !== undefined && (
                        <span className="ml-1 rounded-full bg-blue-100 px-2 py-0.5 text-[11px] font-semibold text-blue-700">
                          Override
                        </span>
                      )}
                  </td>
                  <td className="px-3 py-3">
                    <span
                      className={`rounded-full px-2 py-1 text-xs font-semibold ${
                        detail.isActive
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-slate-100 text-slate-600'
                      }`}
                    >
                      {detail.isActive ? 'Hoạt động' : 'Tạm dừng'}
                    </span>
                  </td>
                  {!readOnly && (
                    <td className="px-3 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => onEdit(detail)}
                          className="rounded-lg bg-slate-200 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-300"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => onDelete(detail)}
                          className="rounded-lg bg-red-500 px-3 py-1 text-xs font-semibold text-white hover:bg-red-600"
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
