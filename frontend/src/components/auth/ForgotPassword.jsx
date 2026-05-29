import { useState } from 'react'
import { forgotPassword } from '../../api'

export default function ForgotPassword({
  forgotEmail,
  setForgotEmail,
  setScreen,
  setResetCode,
  setResetNewPassword,
  setResetConfirmPassword,
  setResetSuccess,
  setForgotError
}) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')

    if (!forgotEmail || !forgotEmail.includes('@')) {
      setError('Vui lï¿½ng nh?p d?a ch? email h?p l?.')
      return
    }

    setLoading(true)
    try {
      await forgotPassword(forgotEmail)
      setSuccess(true)
    } catch (err) {
      // Backend luï¿½n tr? 200 OK ngay c? khi email khï¿½ng t?n t?i d? b?o m?t
      setSuccess(true)
    } finally {
      setLoading(false)
    }
  }

  const navigateToReset = () => {
    setResetCode('')
    setResetNewPassword('')
    setResetConfirmPassword('')
    setResetSuccess(false)
    setForgotError('')
    setScreen('reset-password')
  }

  return (
    <div className="mx-auto w-full max-w-md animate-in fade-in slide-in-from-bottom-4 duration-200">
      <div className="glass-panel rounded-2xl p-8 shadow-xl">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-primary/10 text-brand-primary">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              strokeWidth="2"
              stroke="currentColor"
              className="h-7 w-7"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M15.75 5.25a3 3 0 013 3m3 0a6 6 0 01-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25A2.25 2.25 0 010 19.5V17.25l6.75-6.75a3 3 0 011.563-.43L10.5 10.5M21 3h.008v.008H21V3zm0 3h.008v.008H21V6zm0 3h.008v.008H21V9zm0 3h.008v.008H21v-.008zm0 3h.008v.008H21v-.008zm0 3h.008v.008H21v-.008z"
              />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-extrabold text-slate-900">Quï¿½n mật khẩu?</h2>
          <p className="mt-2 text-xs text-slate-400">
            Nh?p email tï¿½i kho?n c?a b?n d? nh?n mï¿½ khï¿½i ph?c mật khẩu.
          </p>
        </div>

        {!success ? (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="form-label">D?a ch? Email</label>
              <input
                type="email"
                required
                placeholder="you@email.com"
                value={forgotEmail}
                onChange={(e) => setForgotEmail(e.target.value)}
                className="form-input"
              />
            </div>

            {error && (
              <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600">
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full btn-primary flex justify-center items-center gap-2"
            >
              {loading ? (
                <>
                  <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <span>Đang g?i yï¿½u c?u...</span>
                </>
              ) : (
                'G?i mï¿½ xï¿½c nh?n'
              )}
            </button>

            <p className="text-[10px] text-center leading-normal text-slate-400">
              Vï¿½ lï¿½ do b?o m?t, n?u email t?n t?i trong h? th?ng, b?n s? nh?n được m?t thu ch?a mï¿½ xï¿½c nh?n trong vï¿½i phï¿½t.
            </p>
          </form>
        ) : (
          <div className="space-y-6 text-center animate-in fade-in duration-200">
            <div className="rounded-2xl bg-emerald-50/50 border border-emerald-100 p-5">
              <div className="text-3xl mb-2"></div>
              <p className="font-extrabold text-emerald-800 text-sm">G?i mï¿½ thï¿½nh cï¿½ng!</p>
              <p className="mt-2 text-xs text-emerald-600 leading-relaxed">
                Vui lï¿½ng ki?m tra h?p thu d?n ho?c thu rï¿½c t?i d?a ch? <strong>{forgotEmail}</strong>.
              </p>
              <p className="mt-1 text-[10px] text-emerald-500">Mï¿½ cï¿½ hi?u l?c s? d?ng trong 1 gi?.</p>
            </div>

            <button
              type="button"
              onClick={navigateToReset}
              className="w-full btn-primary flex justify-center items-center gap-2"
            >
              Nh?p mï¿½ d?t l?i mật khẩu ?
            </button>
          </div>
        )}

        <div className="mt-6 border-t border-slate-100 pt-6 text-center">
          <button
            type="button"
            onClick={() => setScreen('login')}
            className="text-xs font-bold text-slate-500 hover:text-brand-primary transition hover:underline"
          >
            ? Quay l?i trang đăng nhập
          </button>
        </div>
      </div>
    </div>
  )
}
