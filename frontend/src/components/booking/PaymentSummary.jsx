export default function PaymentSummary({
  bookingAmount,
  paymentData,
  bookingReference,
  saveBookingToHistory,
  historyNotice,
  setScreen,
  selectedFlight,
  returnFlight,
  tripType,
  passengerForms,
  searchData,
  appliedPromotion,
  discountAmount,
  totalPrice,
  getBookingStatusLabel,
  formatCurrency,
  formatDateTime,
  getFlightId,
  getFlightLabel,
  setSelectedFlight,
  setPassengerForms,
  setHistoryNotice,
  getPaymentAmount
}) {
  const paymentSummaryAmount = Number.isFinite(Number(bookingAmount))
    ? Number(bookingAmount)
    : getPaymentAmount ? getPaymentAmount(paymentData, null) : 0

  const handleCancelPayment = () => {
    setSelectedFlight(null)
    setPassengerForms([])
    setHistoryNotice('')
    setScreen('search')
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_340px] animate-in fade-in duration-300">
      {/* Left Main Checkout Panel */}
      <section className="space-y-6">
        <div className="glass-panel rounded-3xl p-6 shadow-xl space-y-6">
          <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-primary/10 text-brand-primary">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-6 w-6">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-5.625 3.51A1.75 1.75 0 001.5 21.75h21a1.75 1.75 0 001.625-1.077L21.43 14.85a3 3 0 00-4.078-4.077L14.6 13.52a3 3 0 00-4.077 0L7.77 10.772a3 3 0 00-4.078 0L1.125 15.603a1.75 1.75 0 00-.75 1.488v2.909z" />
              </svg>
            </div>
            <div>
              <h2 className="title-font text-lg font-extrabold text-slate-800">Thanh toï¿½n vï¿½ mï¿½y bay</h2>
              <p className="text-xs text-slate-400">Phương thức thanh toï¿½n tr?c tuy?n b?o m?t qua VNPAY</p>
            </div>
          </div>

          {/* Payment Link details */}
          <div className="rounded-2xl border border-dashed border-blue-200 bg-blue-50/50 p-6 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <p className="text-xs text-slate-400 font-bold uppercase tracking-wider">Mï¿½ don hï¿½ng</p>
                <p className="text-sm font-extrabold text-slate-800 mt-0.5">
                  {paymentData?.transactionRef || bookingReference}
                </p>
              </div>
              <div>
                <p className="text-xs text-slate-400 font-bold uppercase tracking-wider text-right">T?ng thanh toï¿½n</p>
                <p className="text-lg font-extrabold text-brand-primary mt-0.5">
                  {paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}
                </p>
              </div>
            </div>

            <p className="text-xs text-slate-500 leading-relaxed">
              B?n s? được di?u hu?ng d?n c?ng thanh toï¿½n b?o m?t VNPAY d? hoï¿½n t?t giao d?ch. Sau khi hoï¿½n thï¿½nh thanh toï¿½n thï¿½nh cï¿½ng, vui lï¿½ng nh?n nï¿½t <strong>Xï¿½c nh?n dï¿½ thanh toï¿½n</strong> d? xu?t vï¿½ di?n t?.
            </p>

            <div className="pt-2 flex flex-wrap gap-3">
              {paymentData?.paymentUrl || paymentData?.paymentLink ? (
                <a
                  className="btn-primary flex items-center gap-2 text-xs font-bold px-5 py-3 shadow-md shadow-blue-200"
                  href={paymentData.paymentUrl || paymentData.paymentLink}
                  target="_blank"
                  rel="noreferrer"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4 w-4">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                  </svg>
                  <span>M? c?ng thanh toï¿½n VNPAY</span>
                </a>
              ) : (
                <p className="text-xs font-bold text-rose-600">
                   Chưa t?o được du?ng d?n thanh toï¿½n. Vui lï¿½ng quay l?i tï¿½m ki?m ho?c thử lại sau.
                </p>
              )}
            </div>

            {/* Action buttons */}
            <div className="flex flex-wrap gap-2.5 pt-4 border-t border-blue-100">
              <button
                type="button"
                onClick={saveBookingToHistory}
                className="btn-success px-4 py-2.5 rounded-xl text-xs font-bold"
              >
                Xï¿½c nh?n dï¿½ thanh toï¿½n
              </button>
              <button
                type="button"
                onClick={() => setScreen('history')}
                className="btn-secondary px-4 py-2.5 rounded-xl text-xs font-bold"
              >
                Xem chi ti?t vï¿½ dï¿½ d?t
              </button>
            </div>

            {historyNotice && (
              <div className="rounded-xl bg-emerald-50 border border-emerald-100 px-4 py-3 text-xs font-bold text-emerald-700 animate-in fade-in duration-200">
                {historyNotice}
              </div>
            )}
          </div>

          {/* Sandbox Guidelines */}
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5 space-y-3">
            <h4 className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2.5" stroke="currentColor" className="h-4 w-4 text-slate-500">
                <path strokeLinecap="round" strokeLinejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 111.063.852l-.708 2.836a.75.75 0 001.063.852l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
              </svg>
              <span>Hu?ng d?n thanh toï¿½n th? nghi?m (Sandbox)</span>
            </h4>
            <ol className="list-decimal text-xs text-slate-500 space-y-2 pl-4 leading-relaxed">
              <li>T?i c?ng VNPAY, ch?n phuong th?c: <strong>Th? n?i d?a vï¿½ tï¿½i kho?n ngï¿½n hï¿½ng</strong> (Local ATM Card).</li>
              <li>Ch?n logo ngï¿½n hï¿½ng th? nghi?m <strong>NCB</strong>.</li>
              <li>Nh?p thï¿½ng tin th? test nhu sau:
                <ul className="list-disc pl-5 mt-1 space-y-1 font-mono text-[11px] text-slate-600 bg-white p-2 rounded-xl border border-slate-100">
                  <li>S? th?: <strong className="text-slate-800">9704198526191432198</strong></li>
                  <li>Tï¿½n ch? th?: <strong className="text-slate-800">NGUYEN VAN A</strong></li>
                  <li>Ngï¿½y phï¿½t hï¿½nh: <strong className="text-slate-800">07/15</strong></li>
                  <li>Mï¿½ OTP nh?n: <strong className="text-slate-800">123456</strong></li>
                </ul>
              </li>
              <li>Nh?n <strong>Thanh toï¿½n</strong> vï¿½ d?i chuyến hu?ng tr? l?i.</li>
            </ol>
          </div>

          <button
            type="button"
            onClick={handleCancelPayment}
            className="btn-secondary px-5 py-3 rounded-xl text-xs font-bold text-slate-500"
          >
            H?y thanh toï¿½n & Quay l?i trang ch?
          </button>
        </div>
      </section>

      {/* Right Order Receipt Summary Sidebar */}
      <aside className="space-y-4">
        <div className="glass-panel rounded-3xl p-5 shadow-xl space-y-4">
          <h3 className="title-font text-sm font-extrabold text-slate-800 border-b border-slate-100 pb-3">
            Hï¿½a don d?t ch?
          </h3>

          <div className="space-y-3 text-xs text-slate-600">
            {selectedFlight && (
              <div>
                <span className="block font-bold text-slate-400 text-[10px] uppercase">Ch?ng di</span>
                <p className="font-extrabold text-slate-800 mt-0.5">
                  {selectedFlight.departureAirport} ? {selectedFlight.arrivalAirport}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  S? hi?u: {getFlightLabel(selectedFlight)}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Th?i gian: {formatDateTime(selectedFlight.departureTime)}
                </p>
              </div>
            )}

            {tripType === 'roundtrip' && returnFlight && (
              <div className="pt-2.5 border-t border-slate-100">
                <span className="block font-bold text-slate-400 text-[10px] uppercase">Ch?ng v?</span>
                <p className="font-extrabold text-slate-800 mt-0.5">
                  {returnFlight.departureAirport} ? {returnFlight.arrivalAirport}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  S? hi?u: {getFlightLabel(returnFlight)}
                </p>
                <p className="text-[10px] text-slate-400 font-bold">
                  Th?i gian: {formatDateTime(returnFlight.departureTime)}
                </p>
              </div>
            )}

            <div className="pt-2.5 border-t border-slate-100 space-y-1">
              <div className="flex justify-between">
                <span className="text-slate-400 font-bold">Hï¿½nh khï¿½ch liï¿½n h?:</span>
                <span className="font-extrabold text-slate-700">
                  {passengerForms[0]?.fullName || 'Khï¿½ch hï¿½ng'}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400 font-bold">S? lu?ng vï¿½:</span>
                <span className="font-extrabold text-slate-700">{searchData.passengers} vï¿½</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400 font-bold">H?ng d?t ch?:</span>
                <span className="font-extrabold text-slate-700">{searchData.seatClass}</span>
              </div>
            </div>
          </div>

          <div className="border-t border-slate-100 pt-4 space-y-2">
            <div className="flex justify-between text-xs text-slate-500 font-bold">
              <span>Thï¿½nh ti?n vï¿½:</span>
              <span>{formatCurrency(totalPrice)}</span>
            </div>

            {appliedPromotion && (
              <div className="flex justify-between text-xs text-emerald-600 font-bold">
                <span> Gi?m giï¿½ uu dï¿½i:</span>
                <span>-{formatCurrency(discountAmount)}</span>
              </div>
            )}

            <div className="flex justify-between items-center border-t border-slate-100 pt-3 text-slate-800">
              <span className="text-xs font-extrabold">C?n thanh toï¿½n</span>
              <span className="text-base font-extrabold text-brand-primary">
                {paymentSummaryAmount !== null ? formatCurrency(paymentSummaryAmount) : '--'}
              </span>
            </div>
          </div>
        </div>
      </aside>
    </div>
  )
}
