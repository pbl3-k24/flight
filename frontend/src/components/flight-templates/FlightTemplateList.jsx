import { useMemo } from 'react'

const Label = ({ children }) => (
  <p className="mb-2 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  />
)

const Select = ({ children, ...props }) => (
  <select
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  >
    {children}
  </select>
)

const formatDate = (value) => {
  if (!value) return '--'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(date)
}

const StatCard = ({ label, value, color }) => (
  <div className={`rounded-2xl border px-5 py-4 ${color}`}>
    <p className="text-xs font-semibold uppercase tracking-widest opacity-70">{label}</p>
    <p className="mt-1 text-2xl font-bold">{value}</p>
  </div>
)

export default function FlightTemplateList({
  templates,
  loading,
  error,
  filters,
  onFilterChange,
  onCreate,
  onView,
  onEdit,
  onDelete,
  onGenerate,
  isDeletingId,
}) {
  const filteredCount = useMemo(() => templates.length, [templates.length])
  const activeCount = useMemo(() => templates.filter((t) => t.isActive).length, [templates])
  const inactiveCount = useMemo(() => templates.filter((t) => !t.isActive).length, [templates])

  return (
    <div className="space-y-5">

      {/* ── Header Section ── */}
      <section className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-[#1E40AF] via-[#1E3A8A] to-[#1E3A8A] p-6 shadow-xl shadow-blue-100 md:p-8">
        {/* decorative circles */}
        <div className="pointer-events-none absolute -right-10 -top-10 h-48 w-48 rounded-full bg-white/10" />
        <div className="pointer-events-none absolute -bottom-8 right-24 h-32 w-32 rounded-full bg-white/10" />

        <div className="relative flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            {/* icon */}
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20 backdrop-blur-sm">
              <svg className="h-6 w-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-blue-200">
                • Quản trị hệ thống
              </p>
              <h2 className="mt-0.5 text-xl font-extrabold text-white md:text-2xl">
                Flight Schedule Templates
              </h2>
              <p className="mt-0.5 text-sm font-medium text-white/90">
                Lập kế hoạch bay định kỳ theo thứ trong tuần và tự động hóa việc sinh lịch bay hằng tuần.
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onCreate}
            className="flex items-center gap-2 rounded-xl bg-white px-5 py-2.5 text-sm font-extrabold text-[#1E40AF] shadow-md transition hover:bg-slate-100 hover:text-blue-900 active:scale-95 border border-slate-200"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
            </svg>
            Tạo template mới
          </button>
        </div>

        {/* stat chips */}
        <div className="relative mt-5 flex flex-wrap gap-3">
          <div className="flex items-center gap-1.5 rounded-full bg-emerald-950/80 px-3 py-1.5 text-xs font-bold text-emerald-300 border border-emerald-500/30">
            <span className="h-2 w-2 rounded-full bg-emerald-400" />
            {activeCount} đang hoạt động
          </div>
          <div className="flex items-center gap-1.5 rounded-full bg-slate-900/80 px-3 py-1.5 text-xs font-bold text-slate-300 border border-slate-700/30">
            <span className="h-2 w-2 rounded-full bg-slate-400" />
            {inactiveCount} tạm dừng
          </div>
          <div className="flex items-center gap-1.5 rounded-full bg-blue-950/80 px-3 py-1.5 text-xs font-bold text-blue-200 border border-blue-500/30">
            <span className="h-2 w-2 rounded-full bg-blue-400" />
            {filteredCount} / {templates.length} hiển thị
          </div>
        </div>
      </section>

      {/* ── Filter Section ── */}
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-100 md:p-6">
        <div className="mb-4 flex items-center gap-2">
          <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
          </svg>
          <h3 className="text-sm font-bold text-slate-700">Bộ lọc</h3>
        </div>
        <div className="grid gap-3 md:grid-cols-4">
          <div className="md:col-span-2">
            <Label>Tìm kiếm</Label>
            <div className="relative">
              <svg className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                className="w-full rounded-xl border border-slate-200 bg-white py-2.5 pl-9 pr-4 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
                placeholder="Tìm theo code, tên, mô tả..."
                value={filters.keyword}
                onChange={(e) => onFilterChange({ ...filters, keyword: e.target.value })}
              />
            </div>
          </div>
          <div>
            <Label>Trạng thái</Label>
            <Select
              value={filters.isActive}
              onChange={(e) => onFilterChange({ ...filters, isActive: e.target.value })}
            >
              <option value="all">Tất cả</option>
              <option value="active">Đang hoạt động</option>
              <option value="inactive">Tạm dừng</option>
            </Select>
          </div>
          <div>
            <Label>Từ ngày</Label>
            <Input
              type="date"
              value={filters.fromDate}
              onChange={(e) => onFilterChange({ ...filters, fromDate: e.target.value })}
            />
          </div>
        </div>
        <div className="mt-3 flex items-center justify-between">
          <p className="text-xs text-slate-400">
            Hiển thị <span className="font-semibold text-slate-600">{filteredCount}</span> template phù hợp bộ lọc
          </p>
          {(filters.keyword || filters.isActive !== 'all' || filters.fromDate || filters.toDate) && (
            <button
              type="button"
              onClick={() => onFilterChange({ keyword: '', isActive: 'all', fromDate: '', toDate: '' })}
              className="text-xs font-semibold text-blue-600 hover:underline"
            >
              Xoá bộ lọc
            </button>
          )}
        </div>
      </section>

      {/* ── Table Section ── */}
      <section className="rounded-2xl bg-white shadow-lg shadow-slate-100 overflow-hidden">
        {error && (
          <div className="mx-5 mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        {loading && (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-slate-400">
            <svg className="h-8 w-8 animate-spin text-blue-400" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
            </svg>
            <p className="text-sm">Đang tải danh sách templates...</p>
          </div>
        )}
        {!loading && templates.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-3 py-20 text-slate-400">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100">
              <svg className="h-8 w-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
            </div>
            <div className="text-center">
              <p className="font-semibold text-slate-600">Không có template phù hợp</p>
              <p className="mt-0.5 text-sm text-slate-400">Thử thay đổi bộ lọc hoặc tạo template mới</p>
            </div>
            <button
              type="button"
              onClick={onCreate}
              className="mt-1 rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
            >
              + Tạo template mới
            </button>
          </div>
        )}

        {!loading && templates.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50">
                  <th className="px-5 py-3.5 text-xs font-bold uppercase tracking-wide text-slate-500">Code</th>
                  <th className="px-5 py-3.5 text-xs font-bold uppercase tracking-wide text-slate-500">Tên template</th>
                  <th className="px-5 py-3.5 text-xs font-bold uppercase tracking-wide text-slate-500">Hiệu lực</th>
                  <th className="px-5 py-3.5 text-xs font-bold uppercase tracking-wide text-slate-500">Trạng thái</th>
                  <th className="px-5 py-3.5 text-xs font-bold uppercase tracking-wide text-slate-500">Chi tiết</th>
                  <th className="px-5 py-3.5 text-right text-xs font-bold uppercase tracking-wide text-slate-500">Hành động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {templates.map((template) => (
                  <tr key={template.id} className="group transition-colors hover:bg-blue-50/40">
                    {/* Code */}
                    <td className="px-5 py-4">
                      <span className="inline-flex items-center rounded-lg bg-slate-100 px-2.5 py-1 text-xs font-bold tracking-wider text-slate-700">
                        {template.code || '--'}
                      </span>
                    </td>

                    {/* Name + Description */}
                    <td className="px-5 py-4 max-w-xs">
                      <p className="font-semibold text-slate-900 group-hover:text-[#1E40AF] transition-colors">
                        {template.name}
                      </p>
                      <p className="mt-0.5 line-clamp-2 text-xs text-slate-400">
                        {template.description || 'Không có mô tả'}
                      </p>
                    </td>

                    {/* Dates */}
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-1.5 text-xs text-slate-700">
                        <svg className="h-3.5 w-3.5 text-slate-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                        {formatDate(template.effectiveFrom)}
                      </div>
                      <div className="mt-0.5 flex items-center gap-1.5 text-xs text-slate-400">
                        <svg className="h-3.5 w-3.5 shrink-0 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M17 8l4 4m0 0l-4 4m4-4H3" />
                        </svg>
                        {formatDate(template.effectiveTo)}
                      </div>
                    </td>

                    {/* Status */}
                    <td className="px-5 py-4">
                      <span
                        className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold ${
                          template.isActive
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-slate-100 text-slate-500'
                        }`}
                      >
                        <span className={`h-1.5 w-1.5 rounded-full ${template.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                        {template.isActive ? 'Đang hoạt động' : 'Tạm dừng'}
                      </span>
                    </td>

                    {/* Details count */}
                    <td className="px-5 py-4">
                      <span className="text-sm font-semibold text-slate-700">
                        {template.detailsCount || 0}
                      </span>
                      <span className="ml-1 text-xs text-slate-400">dòng lịch</span>
                    </td>

                    {/* Actions */}
                    <td className="px-5 py-4 text-right">
                      <div className="flex flex-wrap justify-end gap-1.5">
                        <button
                          type="button"
                          onClick={() => onView(template)}
                          title="Xem chi tiết"
                          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-600 shadow-sm transition hover:border-slate-300 hover:bg-slate-50"
                        >
                          Xem
                        </button>
                        <button
                          type="button"
                          onClick={() => onEdit(template)}
                          title="Chỉnh sửa"
                          className="rounded-lg bg-blue-600 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-blue-700"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => onGenerate(template)}
                          title="Sinh chuyến bay"
                          className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-emerald-700"
                        >
                          Generate
                        </button>
                        <button
                          type="button"
                          onClick={() => onDelete(template)}
                          disabled={isDeletingId === template.id}
                          title="Xoá template"
                          className="rounded-lg bg-red-500 px-3 py-1.5 text-xs font-semibold text-white shadow-sm transition hover:bg-red-600 disabled:opacity-50"
                        >
                          {isDeletingId === template.id ? (
                            <span className="flex items-center gap-1">
                              <svg className="h-3 w-3 animate-spin" fill="none" viewBox="0 0 24 24">
                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
                              </svg>
                              Đang xóa
                            </span>
                          ) : 'Xóa'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
