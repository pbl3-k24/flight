import { useEffect, useState } from 'react'

const Label = ({ children }) => (
  <p className="mb-2 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  />
)

const parseConflictMessage = (message) => {
  if (!message) return null
  const getField = (regex) => {
    const match = message.match(regex)
    return match ? match[1].trim() : null
  }

  const aircraft = getField(/aircraft\s*[:\-]\s*([^,|]+)/i)
  const flightNumber = getField(/flight\s*number\s*[:\-]\s*([^,|]+)/i)
  const departure = getField(/departure\s*[:\-]\s*([^,|]+)/i)
  const arrival = getField(/arrival\s*[:\-]\s*([^,|]+)/i)
  const reason = getField(/reason\s*[:\-]\s*([^,|]+)/i)

  if (!aircraft && !flightNumber && !departure && !arrival && !reason) return null
  return {
    aircraft,
    flightNumber,
    departure,
    arrival,
    reason,
  }
}

export default function GenerateFlightsModal({
  open,
  template,
  supportsOverwrite,
  onClose,
  onGenerate,
  isSubmitting,
  result,
  error,
}) {
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [overwriteExisting, setOverwriteExisting] = useState(false)
  const [localError, setLocalError] = useState('')
  const [confirming, setConfirming] = useState(false)

  useEffect(() => {
    if (!open) return
    setFromDate(template?.effectiveFrom || '')
    setToDate(template?.effectiveTo || '')
    setOverwriteExisting(false)
    setLocalError('')
    setConfirming(false)
  }, [open, template])

  if (!open || !template) return null

  const parsedErrors = (result?.errors || [])
    .map((item) => ({ raw: item, parsed: parseConflictMessage(item) }))

  const handleSubmit = () => {
    if (!fromDate || !toDate) {
      setLocalError('Vui lòng chọn từ ngày và đến ngày.')
      return
    }
    if (toDate < fromDate) {
      setLocalError('Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.')
      return
    }
    setLocalError('')

    if (!confirming) {
      setConfirming(true)
      return
    }

    onGenerate({ fromDate, toDate, overwriteExisting })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-3xl rounded-2xl bg-white p-6 shadow-2xl">
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h3 className="text-xl font-bold text-slate-900">Generate flights</h3>
            <p className="text-sm text-slate-500">
              Template: <span className="font-semibold">{template.name}</span>
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

        {(localError || error) && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {localError || error}
          </div>
        )}

        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <Label>Từ ngày</Label>
            <Input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </div>
          <div>
            <Label>Đến ngày</Label>
            <Input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </div>
          {supportsOverwrite && (
            <div className="md:col-span-2">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={overwriteExisting}
                  onChange={(e) => setOverwriteExisting(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
                />
                <span className="text-sm text-slate-700">Ghi đè flight đã tồn tại</span>
              </label>
            </div>
          )}
        </div>

        {confirming && !result && (
          <div className="mt-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
            Xác nhận sinh chuyến bay từ {fromDate} đến {toDate}. Hành động này có thể mất vài phút.
          </div>
        )}

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
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
          >
            {confirming ? 'Xác nhận generate' : 'Generate'}
          </button>
        </div>

        {result && (
          <div className="mt-6 space-y-3">
            <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              Đã tạo {result.totalFlightsGenerated || 0} chuyến bay, bỏ qua {result.totalFlightsSkipped || 0} chuyến.
            </div>
            {result.warnings?.length > 0 && (
              <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
                {result.warnings.join(', ')}
              </div>
            )}
            {parsedErrors.length > 0 && (
              <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                <p className="mb-2 font-semibold">Conflict aircraft</p>
                <div className="space-y-2">
                  {parsedErrors.map((item, index) =>
                    item.parsed ? (
                      <div key={index} className="rounded-lg bg-white/60 p-2">
                        <p>Máy bay: {item.parsed.aircraft || '---'}</p>
                        <p>Chuyến bay: {item.parsed.flightNumber || '---'}</p>
                        <p>Đi: {item.parsed.departure || '---'}</p>
                        <p>Đến: {item.parsed.arrival || '---'}</p>
                        <p>Lý do: {item.parsed.reason || item.raw}</p>
                      </div>
                    ) : (
                      <div key={index} className="rounded-lg bg-white/60 p-2">
                        {item.raw}
                      </div>
                    )
                  )}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
