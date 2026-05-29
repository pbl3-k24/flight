import { useState } from 'react'

export default function Header({
  authUser,
  unreadNotificationCount,
  isLoadingNotifications,
  notificationError,
  sortedNotifications,
  isNotificationPanelOpen,
  setIsNotificationPanelOpen,
  handleMarkAllNotificationsRead,
  handleMarkNotificationRead,
  logout,
  screen,
  setScreen,
  isAdmin,
  formatDateTime
}) {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const navItems = [
    { id: 'search', label: 'TÃ¯Â¿Â½m chuyáº¿n bay', roles: ['user', 'admin'] },
    { id: 'history', label: 'Lá»‹ch sá»­ d?t vÃ¯Â¿Â½', roles: ['user', 'admin'] },
    { id: 'payment-history', label: 'Lá»‹ch sá»­ payment', roles: ['user', 'admin'] },
    { id: 'saved-passengers', label: 'Danh b? hÃ¯Â¿Â½nh khÃ¯Â¿Â½ch', roles: ['user', 'admin'] },
  ]

  const adminNavItems = [
    { id: 'flights', label: 'Qu?n lÃ¯Â¿Â½ chuyáº¿n bay' },
    { id: 'templates', label: 'M?u l?ch trÃ¯Â¿Â½nh' },
    { id: 'promotions', label: 'Qu?n lÃ¯Â¿Â½ khuy?n mÃ¯Â¿Â½i' },
  ]

  const handleNavClick = (id) => {
    setScreen(id)
    setMobileMenuOpen(false)
  }

  return (
    <header className="sticky top-0 z-40 w-full glass-panel border-b border-slate-200/80 px-4 py-4 md:px-8">
      <div className="mx-auto flex max-w-7xl items-center justify-between">
        {/* Brand Logo */}
        <button
          type="button"
          onClick={() => handleNavClick(isAdmin ? 'flights' : 'search')}
          className="flex items-center gap-3 text-left focus:outline-none"
        >
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-primary text-white shadow-md shadow-blue-200">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              strokeWidth="2.5"
              stroke="currentColor"
              className="h-6 w-6"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5"
              />
            </svg>
          </div>
          <div>
            <span className="title-font block text-lg font-extrabold tracking-tight text-brand-primary md:text-xl">
              FlyNow<span className="text-brand-tertiary">.vn</span>
            </span>
            <span className="block text-[10px] font-medium uppercase tracking-widest text-slate-400">
              Sky is the Limit
            </span>
          </div>
        </button>

        {/* Desktop Navigation */}
        {authUser && (
          <nav className="hidden lg:flex items-center gap-1">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => handleNavClick(item.id)}
                className={`rounded-lg px-4 py-2 text-xs font-bold transition-all duration-instant ${
                  screen === item.id || (item.id === 'search' && screen === 'list')
                    ? 'bg-brand-primary/10 text-brand-primary'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                }`}
              >
                {item.label}
              </button>
            ))}

            {isAdmin && (
              <div className="ml-2 flex items-center gap-1 border-l border-slate-200 pl-2">
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 px-2">
                  Admin:
                </span>
                {adminNavItems.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => handleNavClick(item.id)}
                    className={`rounded-lg px-4 py-2 text-xs font-bold transition-all duration-instant ${
                      screen === item.id
                        ? 'bg-slate-900 text-white'
                        : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                    }`}
                  >
                    {item.label}
                  </button>
                ))}
              </div>
            )}
          </nav>
        )}

        {/* Right Section: Notification & User Panel */}
        <div className="flex items-center gap-3">
          {authUser ? (
            <>
              {/* Notification Center */}
              <div className="relative">
                <button
                  type="button"
                  onClick={() => setIsNotificationPanelOpen((prev) => !prev)}
                  className="relative flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 transition hover:bg-slate-50 hover:text-slate-900"
                  aria-label="ThÃ¯Â¿Â½ng bÃ¯Â¿Â½o"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    strokeWidth="2"
                    stroke="currentColor"
                    className="h-5 w-5"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0"
                    />
                  </svg>
                  {unreadNotificationCount > 0 && (
                    <span className="absolute -right-1 -top-1 flex h-5 min-w-[20px] items-center justify-center rounded-full bg-rose-500 px-1.5 text-[10px] font-bold text-white shadow-sm ring-2 ring-white animate-pulse">
                      {unreadNotificationCount > 99 ? '99+' : unreadNotificationCount}
                    </span>
                  )}
                </button>

                {/* Notifications Dropdown Panel */}
                {isNotificationPanelOpen && (
                  <>
                    <div
                      className="fixed inset-0 z-30"
                      onClick={() => setIsNotificationPanelOpen(false)}
                    />
                    <div className="absolute right-0 top-full z-40 mt-3 w-80 rounded-2xl border border-slate-100 bg-white p-2 text-slate-900 shadow-2xl ring-1 ring-black/5 animate-in fade-in slide-in-from-top-2 duration-150">
                      <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2.5">
                        <span className="text-sm font-extrabold text-slate-800">ThÃ¯Â¿Â½ng bÃ¯Â¿Â½o</span>
                        {unreadNotificationCount > 0 && (
                          <button
                            type="button"
                            onClick={handleMarkAllNotificationsRead}
                            className="text-xs font-semibold text-brand-primary hover:text-blue-700 hover:underline"
                          >
                            D?c t?t c?
                          </button>
                        )}
                      </div>
                      <div className="max-h-80 overflow-y-auto py-1">
                        {isLoadingNotifications && (
                          <div className="flex flex-col items-center justify-center py-8 text-slate-400">
                            <svg className="h-5 w-5 animate-spin text-brand-primary" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                            </svg>
                            <span className="mt-2 text-xs">Äang táº£i thÃ¯Â¿Â½ng bÃ¯Â¿Â½o...</span>
                          </div>
                        )}
                        {!isLoadingNotifications && notificationError && (
                          <div className="px-4 py-4 text-center text-xs text-rose-500">{notificationError}</div>
                        )}
                        {!isLoadingNotifications && !notificationError && sortedNotifications.length === 0 && (
                          <div className="flex flex-col items-center justify-center py-10 text-slate-400">
                            <svg className="h-8 w-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                            </svg>
                            <span className="mt-2 text-xs font-medium">H?p thu tr?ng</span>
                          </div>
                        )}
                        {!isLoadingNotifications && !notificationError && sortedNotifications.length > 0 && (
                          <div className="divide-y divide-slate-50">
                            {sortedNotifications.map((item) => {
                              const title = item?.subject || item?.category || 'ThÃ¯Â¿Â½ng bÃ¯Â¿Â½o'
                              const message = item?.message || item?.errorMessage || item?.type || ''
                              const timestamp = formatDateTime(item?.createdAt || item?.sentAt)
                              const isRead = Boolean(item?.isRead)

                              return (
                                <button
                                  type="button"
                                  key={item?.notificationId || `${title}-${timestamp}`}
                                  onClick={() => {
                                    if (!isRead) {
                                      handleMarkNotificationRead(item?.notificationId)
                                    }
                                  }}
                                  className={`flex w-full flex-col gap-1 rounded-lg p-2.5 text-left transition hover:bg-slate-50 ${
                                    isRead ? 'opacity-60' : 'bg-blue-50/20'
                                  }`}
                                >
                                  <div className="flex items-start justify-between gap-2">
                                    <span className={`text-xs font-bold ${isRead ? 'text-slate-700' : 'text-slate-900'}`}>
                                      {title}
                                    </span>
                                    {!isRead && (
                                      <span className="mt-1 h-2 w-2 flex-none rounded-full bg-brand-primary animate-pulse" />
                                    )}
                                  </div>
                                  {message && <div className="text-[11px] text-slate-500 line-clamp-2">{message}</div>}
                                  {timestamp && <div className="text-[9px] text-slate-400">{timestamp}</div>}
                                </button>
                              )
                            })}
                          </div>
                        )}
                      </div>
                    </div>
                  </>
                )}
              </div>

              {/* User Dropdown */}
              <div className="flex items-center gap-2 border-l border-slate-200 pl-3">
                <div className="hidden md:block text-right">
                  <span className="block text-xs font-bold text-slate-800">
                    {authUser.fullName || authUser.email}
                  </span>
                  <span className="block text-[10px] font-bold uppercase tracking-wider text-slate-400">
                    {isAdmin ? 'Qu?n tr? viÃ¯Â¿Â½n' : 'KhÃ¯Â¿Â½ch hÃ¯Â¿Â½ng'}
                  </span>
                </div>
                <button
                  type="button"
                  onClick={logout}
                  className="rounded-xl border border-red-100 bg-red-50/50 px-3 py-2 text-xs font-bold text-red-600 transition hover:bg-red-50 active:scale-[0.98]"
                >
                  Äang xu?t
                </button>
              </div>

              {/* Mobile Menu Toggle */}
              <button
                type="button"
                onClick={() => setMobileMenuOpen((prev) => !prev)}
                className="flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 bg-white text-slate-600 transition hover:bg-slate-50 lg:hidden"
                aria-label="Menu"
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  fill="none"
                  viewBox="0 0 24 24"
                  strokeWidth="2"
                  stroke="currentColor"
                  className="h-5 w-5"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d={
                      mobileMenuOpen
                        ? 'M6 18L18 6M6 6l12 12'
                        : 'M4 6h16M4 12h16M4 18h16'
                    }
                  />
                </svg>
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={() => setScreen('login')}
              className="btn-primary flex items-center gap-2"
            >
              ÄÄƒng nháº­p
            </button>
          )}
        </div>
      </div>

      {/* Mobile Navigation Drawer */}
      {mobileMenuOpen && authUser && (
        <div className="mt-4 border-t border-slate-100 pt-4 lg:hidden animate-in slide-in-from-top duration-200">
          <div className="flex flex-col gap-1">
            {navItems.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() => handleNavClick(item.id)}
                className={`flex w-full items-center rounded-xl px-4 py-3 text-sm font-bold transition ${
                  screen === item.id || (item.id === 'search' && screen === 'list')
                    ? 'bg-brand-primary/10 text-brand-primary'
                    : 'text-slate-600 hover:bg-slate-50'
                }`}
              >
                {item.label}
              </button>
            ))}

            {isAdmin && (
              <>
                <div className="mt-2 border-t border-slate-100 pt-2 px-4 text-[10px] font-bold uppercase tracking-wider text-slate-400">
                  PhÃ¯Â¿Â½n h? Admin
                </div>
                {adminNavItems.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => handleNavClick(item.id)}
                    className={`flex w-full items-center rounded-xl px-4 py-3 text-sm font-bold transition ${
                      screen === item.id
                        ? 'bg-slate-900 text-white'
                        : 'text-slate-600 hover:bg-slate-50'
                    }`}
                  >
                    {item.label}
                  </button>
                ))}
              </>
            )}
          </div>
        </div>
      )}
    </header>
  )
}
