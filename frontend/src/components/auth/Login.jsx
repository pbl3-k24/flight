import { useState } from 'react'

export default function Login({
  loginData,
  setLoginData,
  isLoggingIn,
  setIsLoggingIn,
  apiError,
  setApiError,
  login,
  setAuthToken,
  setAuthUser,
  setScreen,
  getRoleFromToken,
  setForgotEmail,
  setForgotStep,
  setForgotError,
  setForgotLoading
}) {
  const [localError, setLocalError] = useState('')

  const handleDemoLogin = (role) => {
    setLocalError('')
    setApiError('')
    
    // Generate standard mock JWT structure to satisfy getRoleFromToken decoder
    try {
      const payloadObj = {
        role: role,
        email: role === 'admin' ? 'admin@flynow.vn' : 'user@flynow.vn',
        fullName: role === 'admin' ? 'Quản Trị Viên Demo' : 'Khách Hàng Demo',
        exp: Math.floor(Date.now() / 1000) + (60 * 60 * 24),
      }
      const utf8String = unescape(encodeURIComponent(JSON.stringify(payloadObj)))
      const base64Payload = btoa(utf8String)
      const mockToken = `mockHeader.${base64Payload}.mockSignature`

      setAuthToken(mockToken)
      setAuthUser({
        email: payloadObj.email,
        fullName: payloadObj.fullName,
        role: role,
      })
      setScreen(role === 'admin' ? 'flights' : 'search')
    } catch (e) {
      setLocalError('Lỗi giả lập đăng nhập: ' + e.message)
    }
  }

  const handleLoginSubmit = async (e) => {
    e.preventDefault()
    setLocalError('')
    setApiError('')

    if (!loginData.email || !loginData.password) {
      setLocalError('Vui lòng nhập đầy đủ email và mật khẩu.')
      return
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(loginData.email)) {
      setLocalError('Định dạng email không hợp lệ.')
      return
    }

    setIsLoggingIn(true)
    try {
      const auth = await login(loginData.email, loginData.password)
      const token = auth?.token || ''
      const roleFromToken = getRoleFromToken(token)
      const normalizedRole = roleFromToken === 'admin' ? 'admin' : 'user'

      if (token) {
        setAuthToken(token)
      }

      setAuthUser({
        email: auth?.email || loginData.email,
        fullName: auth?.fullName || '',
        role: normalizedRole,
      })
      
      setScreen(normalizedRole === 'admin' ? 'flights' : 'search')
    } catch (error) {
      setApiError(error.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.')
    } finally {
      setIsLoggingIn(false)
    }
  }

  const navigateToForgotPassword = () => {
    setApiError('')
    setForgotEmail(loginData.email || '')
    setForgotStep('email')
    setForgotError('')
    setForgotLoading(false)
    setScreen('forgot-password')
  }

  return (
    <div className="mx-auto w-full max-w-md animate-in fade-in slide-in-from-bottom-4 duration-200">
      <div className="glass-panel rounded-2xl p-8 shadow-xl">
        <div className="mb-8 text-center">
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
                d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z"
              />
            </svg>
          </div>
          <h2 className="title-font text-2xl font-extrabold text-slate-900">Đăng nhập tài khoản</h2>
          <p className="mt-2 text-xs text-slate-400">Chào mừng bạn quay trở lại với FlyNow.vn</p>
        </div>

        <form onSubmit={handleLoginSubmit} className="space-y-4">
          <div>
            <label className="form-label">Địa chỉ Email</label>
            <div className="relative">
              <span className="absolute inset-y-0 left-0 flex items-center pl-3.5 text-slate-400">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.8" stroke="currentColor" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25H4.5a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5H4.5a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                </svg>
              </span>
              <input
                type="email"
                required
                placeholder="email@example.com"
                value={loginData.email}
                onChange={(e) => setLoginData((prev) => ({ ...prev, email: e.target.value }))}
                className="form-input pl-10"
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-semibold text-slate-700">Mật khẩu</label>
              <button
                type="button"
                onClick={navigateToForgotPassword}
                className="text-xs font-bold text-brand-primary hover:text-blue-800 hover:underline focus:outline-none"
              >
                Quên mật khẩu?
              </button>
            </div>
            <div className="relative">
              <span className="absolute inset-y-0 left-0 flex items-center pl-3.5 text-slate-400">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="1.8" stroke="currentColor" className="h-5 w-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
                </svg>
              </span>
              <input
                type="password"
                required
                placeholder="••••••••"
                value={loginData.password}
                onChange={(e) => setLoginData((prev) => ({ ...prev, password: e.target.value }))}
                className="form-input pl-10"
              />
            </div>
          </div>

          {(localError || apiError) && (
            <div className="rounded-xl border border-red-100 bg-red-50/50 p-4 text-xs font-bold text-red-600 animate-in fade-in duration-150">
              <div className="flex flex-col gap-2">
                <div className="flex gap-2">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 flex-none">
                    <path fillRule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clipRule="evenodd" />
                  </svg>
                  <span>{localError || apiError}</span>
                </div>
                {String(localError || apiError).toLowerCase().includes('failed to fetch') && (
                  <div className="mt-1 border-t border-red-200/50 pt-2 text-[10px] font-medium text-slate-600">
                    <p className="font-bold text-red-700">Gợi ý sửa lỗi:</p>
                    <p className="mt-0.5">Có vẻ Backend server (.NET Core) chưa được khởi động. Bạn có thể bấm hai nút Demo bên dưới để đăng nhập tức thì bằng giả lập.</p>
                  </div>
                )}
              </div>
            </div>
          )}

          <button
            type="submit"
            disabled={isLoggingIn}
            className="w-full btn-primary mt-2 flex justify-center items-center gap-2"
          >
            {isLoggingIn ? (
              <>
                <svg className="h-4 w-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                <span>Đang đăng nhập...</span>
              </>
            ) : (
              'Đăng nhập'
            )}
          </button>
        </form>

        <div className="mt-6 border-t border-slate-100 pt-5">
          <p className="text-center text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-2.5">Đăng nhập nhanh giả lập (Bypass)</p>
          <div className="grid grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => handleDemoLogin('user')}
              className="flex items-center justify-center gap-1.5 rounded-xl border border-blue-200/60 bg-blue-50/20 py-2.5 text-xs font-bold text-blue-700 hover:bg-blue-50 transition-all duration-instant active:scale-[0.97]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.501 20.118a7.5 7.5 0 0 1 14.998 0A17.933 17.933 0 0 1 12 21.75c-2.676 0-5.216-.584-7.499-1.632Z" />
              </svg>
              Khách Demo
            </button>
            <button
              type="button"
              onClick={() => handleDemoLogin('admin')}
              className="flex items-center justify-center gap-1.5 rounded-xl border border-purple-200/60 bg-purple-50/20 py-2.5 text-xs font-bold text-purple-700 hover:bg-purple-50 transition-all duration-instant active:scale-[0.97]"
            >
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth="2" stroke="currentColor" className="h-4 w-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.57-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z" />
              </svg>
              Admin Demo
            </button>
          </div>
        </div>

        <div className="mt-6 border-t border-slate-100 pt-5 text-center text-xs text-slate-500">
          Chưa có tài khoản?{' '}
          <button
            type="button"
            onClick={() => {
              setApiError('')
              setScreen('register')
            }}
            className="font-bold text-brand-primary hover:text-blue-800 hover:underline focus:outline-none"
          >
            Đăng ký tài khoản mới
          </button>
        </div>
      </div>
    </div>
  )
}
