import { useState } from 'react'
import { resetPassword } from '../../api'

export default function ResetPassword({
  resetCode,
  setResetCode,
  resetNewPassword,
  setResetNewPassword,
  resetConfirmPassword,
  setResetConfirmPassword,
  resetSuccess,
  setResetSuccess,
  forgotError,
  setForgotError,
  setScreen
}) {
  const [loading, setLoading] = useState(false)
  const [localError, setLocalError] = useState('')

  const handleResetSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')
    setForgotError('')

    if (!resetCode.trim()) {
      setLocalError('Vui lï¿½ng nh?p mï¿½ xï¿½c nh?n nh?n được qua email.')
      return
    }

    if (!resetNewPassword || resetNewPassword.length < 6) {
      setLocalError('Mật khẩu m?i ph?i dï¿½i ï¿½t nh?t 6 kï¿½ t?.')
      return
    }

    if (resetNewPassword !== resetConfirmPassword) {
      setLocalError('Mật khẩu xï¿½c nh?n khï¿½ng kh?p.')
      return
    }

    setLoading(true)
    try {
      await resetPassword(resetCode.trim(), resetNewPassword)
      setResetSuccess(true)
    } catch (err) {
      setLocalError(err.message || 'Mï¿½ xï¿½c nh?n khï¿½ng h?p l? ho?c dï¿½ h?t h?n.')
    } finally {
      setLoading(false)
    }
  }

  const navigateToLogin = () => {
    setResetSuccess(false)
    setForgotError('')
    setResetCode('')
    setResetNewPassword('')
    setResetConfirmPassword('')
    setScreen('login')
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
                d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z"
              />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-extrabold text-slate-900">D?t l?i mật khẩu</h2>
          <p className="mt-2 text-xs text-slate-400">
            Nh?p mï¿½ xï¿½c nh?n cï¿½ng v?i mật khẩu m?i d? khï¿½i ph?c tï¿½i kho?n
          </p>
        </div>

        {!resetSuccess ? (
          <form onSubmit={handleResetSubmit} className="space-y-4">
            <div>
              <label className="form-label">Mï¿½ xï¿½c nh?n (OTP)</label>
              <input
                type="text"
                required
                placeholder="Nh?p mï¿½ t? email..."
                value={resetCode}
                onChange={(e) => setResetCode(e.target.value)}
                className="form-input tracking-widest font-mono text-center text-base"
              />
            </div>

            <div>
              <label className="form-label">Mật khẩu m?i</label>
              <input
                type="password"
                required
                placeholder="........"
                value={resetNewPassword}
                onChange={(e) => setResetNewPassword(e.target.value)}
                className="form-input"
              />
            </div>

            <div>
              <label className="form-label">Xï¿½c nh?n mật khẩu m?i</label>
              <input
                type="password"
                required
                placeholder="........"
                value={resetConfirmPassword}
                onChange={(e) => setResetConfirmPassword(e.target.value)}
                className="form-input"
              />
            </div>

            {(localError || forgotError) && (
              <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600 animate-in fade-in duration-150">
                {localError || forgotError}
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
                  <span>Đang cập nhật mật khẩu...</span>
                </>
              ) : (
                'D?t l?i mật khẩu'
              )}
            </button>
          </form>
        ) : (
          <div className="space-y-6 text-center animate-in fade-in duration-200">
            <div className="rounded-2xl bg-emerald-50/50 border border-emerald-100 p-5">
              <div className="text-3xl mb-2">?</div>
              <p className="font-extrabold text-emerald-800 text-sm">Cập nhật mật khẩu thï¿½nh cï¿½ng!</p>
              <p className="mt-2 text-xs text-emerald-600 leading-relaxed">
                Mật khẩu tï¿½i kho?n c?a b?n dï¿½ được thay d?i. Gi? dï¿½y b?n dï¿½ cï¿½ th? đăng nhập b?ng mật khẩu m?i nï¿½y.
              </p>
            </div>

            <button
              type="button"
              onClick={navigateToLogin}
              className="w-full btn-primary"
            >
              Đăng nhập ngay
            </button>
          </div>
        )}

        <div className="mt-6 border-t border-slate-100 pt-6 text-center text-xs text-slate-500">
          Chưa nh?n được mï¿½?{' '}
          <button
            type="button"
            onClick={() => {
              setForgotError('')
              setResetSuccess(false)
              setScreen('forgot-password')
            }}
            className="font-bold text-brand-primary hover:text-blue-800 hover:underline focus:outline-none"
          >
            G?i l?i mï¿½ xï¿½c nh?n
          </button>
        </div>
      </div>
    </div>
  )
}
