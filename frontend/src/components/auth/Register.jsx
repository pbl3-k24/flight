import { useState } from 'react'

export default function Register({
  registerData,
  setRegisterData,
  isRegistering,
  setIsRegistering,
  registerError,
  setRegisterError,
  registerAccount,
  setAuthToken,
  setAuthUser,
  setScreen,
  getRoleFromToken
}) {
  const [localError, setLocalError] = useState('')

  const handleRegisterSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')
    setRegisterError('')

    const { fullName, email, phone, password, confirmPassword } = registerData

    if (!fullName || !email || !phone || !password || !confirmPassword) {
      setLocalError('Vui lï¿½ng di?n d?y d? t?t c? cï¿½c tru?ng.')
      return
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(email)) {
      setLocalError('D?a ch? email khï¿½ng dï¿½ng d?nh d?ng.')
      return
    }

    const phoneRegex = /^[0-9]{10}$/
    if (!phoneRegex.test(phone)) {
      setLocalError('S? di?n tho?i ph?i g?m dï¿½ng 10 ch? s?.')
      return
    }

    if (password.length < 6) {
      setLocalError('Mật khẩu ph?i ch?a ï¿½t nh?t 6 kï¿½ t?.')
      return
    }

    if (password !== confirmPassword) {
      setLocalError('Mật khẩu xï¿½c nh?n khï¿½ng kh?p.')
      return
    }

    setIsRegistering(true)
    try {
      const auth = await registerAccount({
        email,
        password,
        fullName,
        phone,
      })
      const token = auth?.token || ''
      const roleFromToken = getRoleFromToken(token)
      const normalizedRole = roleFromToken === 'admin' ? 'admin' : 'user'

      if (token) {
        setAuthToken(token)
      }

      setAuthUser({
        email: auth?.email || email,
        fullName: auth?.fullName || fullName,
        role: normalizedRole,
      })
      setScreen('search')
    } catch (error) {
      setRegisterError(error.message || 'Đang kï¿½ th?t b?i. Email cï¿½ th? dï¿½ t?n t?i.')
    } finally {
      setIsRegistering(false)
    }
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
              strokeWidth="2.5"
              stroke="currentColor"
              className="h-7 w-7"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-9-3.5h.008v.008H3.75V4.5zm0 3h.008v.008H3.75V7.5zm0 3h.008v.008H3.75v-.008zm0 3h.008v.008H3.75v-.008zm0 3h.008v.008H3.75v-.008zm0 3h.008v.008H3.75v-.008zM9.75 4.5h.008v.008H9.75V4.5zm0 3h.008v.008H9.75V7.5zm0 3h.008v.008H9.75v-.008zm0 3h.008v.008H9.75v-.008zm0 3h.008v.008H9.75v-.008zm0 3h.008v.008H9.75v-.008zm5.25-13.5h.008v.008H15V4.5zm0 3h.008v.008H15V7.5z"
              />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-extrabold text-slate-900">T?o tï¿½i kho?n m?i</h2>
          <p className="mt-2 text-xs text-slate-400">Tham gia FlyNow.vn d? nh?n ng?p trï¿½n uu dï¿½i bay</p>
        </div>

        <form onSubmit={handleRegisterSubmit} className="space-y-4">
          <div>
            <label className="form-label">H? vï¿½ tï¿½n</label>
            <input
              type="text"
              required
              placeholder="Nguy?n Van A"
              value={registerData.fullName}
              onChange={(e) => setRegisterData((prev) => ({ ...prev, fullName: e.target.value }))}
              className="form-input"
            />
          </div>

          <div>
            <label className="form-label">D?a ch? Email</label>
            <input
              type="email"
              required
              placeholder="email@example.com"
              value={registerData.email}
              onChange={(e) => setRegisterData((prev) => ({ ...prev, email: e.target.value }))}
              className="form-input"
            />
          </div>

          <div>
            <label className="form-label">S? di?n tho?i</label>
            <input
              type="tel"
              required
              placeholder="0901234567"
              value={registerData.phone}
              onChange={(e) => setRegisterData((prev) => ({ ...prev, phone: e.target.value }))}
              className="form-input"
            />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <label className="form-label">Mật khẩu</label>
              <input
                type="password"
                required
                placeholder="........"
                value={registerData.password}
                onChange={(e) => setRegisterData((prev) => ({ ...prev, password: e.target.value }))}
                className="form-input"
              />
            </div>
            <div>
              <label className="form-label">Xï¿½c nh?n mật khẩu</label>
              <input
                type="password"
                required
                placeholder="........"
                value={registerData.confirmPassword}
                onChange={(e) => setRegisterData((prev) => ({ ...prev, confirmPassword: e.target.value }))}
                className="form-input"
              />
            </div>
          </div>

          {(localError || registerError) && (
            <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600 animate-in fade-in duration-150">
              <div className="flex gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 flex-none">
                  <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clipRule="evenodd" />
                </svg>
                <span>{localError || registerError}</span>
              </div>
            </div>
          )}

          <button
            type="submit"
            disabled={isRegistering}
            className="w-full btn-primary mt-2 flex justify-center items-center gap-2"
          >
            {isRegistering ? (
              <>
                <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                <span>Đang dang kï¿½...</span>
              </>
            ) : (
              'T?o tï¿½i kho?n'
            )}
          </button>
        </form>

        <div className="mt-6 border-t border-slate-100 pt-6 text-center text-xs text-slate-500">
          Dï¿½ cï¿½ tï¿½i kho?n?{' '}
          <button
            type="button"
            onClick={() => {
              setRegisterError('')
              setScreen('login')
            }}
            className="font-bold text-brand-primary hover:text-blue-800 hover:underline focus:outline-none"
          >
            Đăng nhập ngay
          </button>
        </div>
      </div>
    </div>
  )
}
