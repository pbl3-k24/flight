import { useEffect, useState } from 'react'

const Label = ({ children }) => (
  <p className="mb-1.5 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
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
  return { aircraft, flightNumber, departure, arrival, reason }
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

  const parsedErrors = (result?.errors || []).map((item) => ({
    raw: item,
    parsed: parseConflictMessage(item),
  }))

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

  const totalGenerated = result?.totalFlightsGenerated || 0
  const totalSkipped = result?.totalFlightsSkipped || 0

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm">
      <div className="w-full max-w-2xl rounded-2xl bg-white shadow-2xl overflow-hidden animate-in">

        {/* Modal header */}
        <div className="bg-gradient-to-r from-emerald-600 to-emerald-500 px-6 py-5">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <svg className="h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </div>
              <div>
                <h3 className="text-lg font-bold text-white">Generate Flights</h3>
                <p className="text-sm text-emerald-100">
                  Template:{' '}
                  <span className="font-semibold text-white">
                    {template.code} – {template.name}
                  </span>
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="flex h-8 w-8 items-center justify-center rounded-lg bg-white/20 text-white transition hover:bg-white/30"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* Modal body */}
        <div className="p-6 space-y-5">
          {/* Error banner */}
          {(localError || error) && (
            <div className="flex items-start gap-2.5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <svg className="mt-0.5 h-4 w-4 shrink-0 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              {localError || error}
            </div>
          )}

          {/* Date range picker */}
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <Label>Từ ngày</Label>
              <Input
                type="date"
                value={fromDate}
                onChange={(e) => { setFromDate(e.target.value); setConfirming(false) }}
              />
            </div>
            <div>
              <Label>Đến ngày</Label>
              <Input
                type="date"
                value={toDate}
                onChange={(e) => { setToDate(e.target.value); setConfirming(false) }}
              />
            </div>
          </div>

          {/* Overwrite toggle */}
          {supportsOverwrite && (
            <label className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 transition
              ${overwriteExisting
                ? 'border-amber-300 bg-amber-50'
                : 'border-slate-200 bg-slate-50 hover:border-slate-300'}`}
            >
              <input
                type="checkbox"
                checked={overwriteExisting}
                onChange={(e) => setOverwriteExisting(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 accent-amber-500"
              />
              <div>
                <p className="text-sm font-semibold text-slate-700">Ghi đè chuyến bay đã tồn tại</p>
                <p className="text-xs text-slate-400">Nếu chuyến bay đã tồn tại trong khoảng thời gian này sẽ bị thay thế</p>
              </div>
            </label>
          )}

          {/* Confirmation notice */}
          {confirming && !result && (
            <div className="flex items-start gap-2.5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
              <svg className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
              <span>
                Xác nhận sinh chuyến bay từ <strong>{fromDate}</strong> đến <strong>{toDate}</strong>.{' '}
                Thao tác này có thể mất vài phút.
              </span>
            </div>
          )}

          {/* Result */}
          {result && (
            <div className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-center">
                  <p className="text-2xl font-bold text-emerald-600">{totalGenerated}</p>
                  <p className="mt-0.5 text-xs font-semibold text-emerald-700">Chuyến bay đã tạo</p>
                </div>
                <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-center">
                  <p className="text-2xl font-bold text-slate-500">{totalSkipped}</p>
                  <p className="mt-0.5 text-xs font-semibold text-slate-600">Chuyến bị bỏ qua</p>
                </div>
              </div>

              {result.warnings?.length > 0 && (
                <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
                  <p className="mb-1 font-semibold">Cảnh báo:</p>
                  <ul className="list-inside list-disc space-y-0.5 text-xs">
                    {result.warnings.map((w, i) => <li key={i}>{w}</li>)}
                  </ul>
                </div>
              )}

              {parsedErrors.length > 0 && (
                <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm">
                  <p className="mb-2 font-semibold text-red-700">Xung đột máy bay ({parsedErrors.length}):</p>
                  <div className="space-y-2 max-h-40 overflow-y-auto">
                    {parsedErrors.map((item, index) =>
                      item.parsed ? (
                        <div key={index} className="rounded-lg border border-red-100 bg-white p-2.5 text-xs text-red-700">
                          <div className="grid grid-cols-2 gap-x-4 gap-y-0.5">
                            {item.parsed.aircraft && <p><span className="font-semibold">Máy bay:</span> {item.parsed.aircraft}</p>}
                            {item.parsed.flightNumber && <p><span className="font-semibold">Chuyến:</span> {item.parsed.flightNumber}</p>}
                            {item.parsed.departure && <p><span className="font-semibold">Đi:</span> {item.parsed.departure}</p>}
                            {item.parsed.arrival && <p><span className="font-semibold">Đến:</span> {item.parsed.arrival}</p>}
                          </div>
                          {item.parsed.reason && <p className="mt-1"><span className="font-semibold">Lý do:</span> {item.parsed.reason}</p>}
                        </div>
                      ) : (
                        <div key={index} className="rounded-lg border border-red-100 bg-white p-2.5 text-xs text-red-600">
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

        {/* Modal footer */}
        <div className="flex justify-end gap-2.5 border-t border-slate-100 bg-slate-50 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl border border-slate-200 bg-white px-5 py-2 text-sm font-semibold text-slate-600 shadow-sm transition hover:bg-slate-100"
          >
            {result ? 'Đóng' : 'Hủy'}
          </button>
          {!result && (
            <button
              type="button"
              onClick={handleSubmit}
              disabled={isSubmitting}
              className={`flex items-center gap-2 rounded-xl px-5 py-2 text-sm font-bold text-white shadow-sm transition
                ${confirming
                  ? 'bg-amber-500 hover:bg-amber-600'
                  : 'bg-emerald-600 hover:bg-emerald-700'}
                disabled:cursor-not-allowed disabled:opacity-60`}
            >
              {isSubmitting && (
                <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
                </svg>
              )}
              {isSubmitting
                ? 'Đang sinh chuyến bay...'
                : confirming
                  ? 'Xác nhận Generate'
                  : 'Generate Flights'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
