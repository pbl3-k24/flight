export default function PaymentHistory({
  paymentHistory,
  isLoadingPaymentHistory,
  paymentHistoryError,
  paymentHistoryNotice,
  loadPaymentHistory,
  payBookingFromPaymentHistory,
  payingBookingId,
  formatRouteLabel,
  formatDateTime,
  formatCurrency,
  setScreen,
}) {
  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 pb-4">
        <div>
          <h2 className="title-font text-xl font-extrabold text-slate-800 md:text-2xl">
            Lich su thanh toan
          </h2>
          <p className="text-xs text-slate-400">
            Theo doi trang thai thanh toan cua tung booking va thanh toan lai khi con han.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={loadPaymentHistory}
            className="btn-secondary py-2 text-xs font-bold"
          >
            Lam moi
          </button>
          <button
            type="button"
            onClick={() => setScreen('search')}
            className="btn-primary py-2 text-xs font-bold"
          >
            Dat ve moi
          </button>
        </div>
      </div>

      {paymentHistoryError && (
        <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600">
          {paymentHistoryError}
        </div>
      )}

      {paymentHistoryNotice && (
        <div className="rounded-xl border border-emerald-100 bg-emerald-50/50 p-4 text-xs font-bold text-emerald-700 animate-in fade-in duration-200">
          {paymentHistoryNotice}
        </div>
      )}

      {isLoadingPaymentHistory ? (
        <div className="flex flex-col items-center justify-center py-20 rounded-3xl bg-white border border-slate-100 shadow-sm">
          <svg className="h-6 w-6 animate-spin text-brand-primary" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <span className="mt-4 text-xs text-slate-500">Đang tai lich su thanh toan...</span>
        </div>
      ) : paymentHistory.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-slate-200 bg-white p-12 text-center text-slate-400">
          <span className="text-xs font-bold">Chưa co du lieu thanh toan.</span>
        </div>
      ) : (
        <div className="space-y-6">
          {paymentHistory.map((item) => (
            <article
              key={`${item.bookingId}-${item.paymentId || item.createdAt || item.transactionRef}`}
              className="glass-panel rounded-3xl p-6 shadow-md border border-slate-200/60"
            >
              <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                <div className="space-y-2">
                  <span className="text-xs font-extrabold text-brand-primary uppercase tracking-wider block">
                    Hanh trinh
                  </span>
                  <p className="text-sm font-bold text-slate-800">{formatRouteLabel(item)}</p>
                  <p className="text-xs text-slate-500">Booking: {item.bookingId}</p>
                  {item.transactionRef && (
                    <p className="text-xs text-slate-500 break-all">Ma giao dich: {item.transactionRef}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <span className="text-xs font-extrabold text-slate-500 uppercase tracking-wider block">
                    Thoi gian
                  </span>
                  <p className="text-xs text-slate-600">Tao luc: {formatDateTime(item.createdAt)}</p>
                  {item.paidAt && (
                    <p className="text-xs text-emerald-600">Da thanh toan: {formatDateTime(item.paidAt)}</p>
                  )}
                  {item.expiresAt && (
                    <p className={`text-xs ${item.isExpired ? 'text-red-600' : 'text-amber-600'}`}>
                      Han thanh toan: {formatDateTime(item.expiresAt)}
                    </p>
                  )}
                </div>

                <div className="space-y-2 text-left lg:text-right">
                  <p
                    className={`text-xs font-extrabold uppercase tracking-wider ${
                      item.isPaid ? 'text-emerald-600' : item.isExpired ? 'text-red-600' : 'text-amber-600'
                    }`}
                  >
                    {item.statusLabel}
                  </p>
                  <p className="text-lg font-extrabold text-slate-900">{formatCurrency(Number(item.amount || 0))}</p>
                  <p className="text-xs text-slate-500">{item.provider || 'VNPAY'}</p>

                  {item.canPayNow && (
                    <button
                      type="button"
                      onClick={() => payBookingFromPaymentHistory(item)}
                      disabled={payingBookingId === item.bookingId}
                      className="btn-primary py-2 text-xs font-bold"
                    >
                      {payingBookingId === item.bookingId ? 'Đang chuyen...' : 'Thanh toan ngay'}
                    </button>
                  )}
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}
