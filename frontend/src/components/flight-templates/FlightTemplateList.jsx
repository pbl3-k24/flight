import { useMemo } from 'react'

const Label = ({ children }) => (
  <p className="mb-2 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
    {...props}
  />
)

const Select = ({ children, ...props }) => (
  <select
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100"
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

  return (
    <div className="space-y-5">
      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Admin</p>
            <h2 className="title-font mt-1 text-2xl font-bold text-slate-900">
              Quản lý Flight Schedule Templates
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Theo dõi, cập nhật lịch bay theo giai đoạn và sinh chuyến bay thực tế.
            </p>
          </div>
          <button
            type="button"
            onClick={onCreate}
            className="rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800"
          >
            + Tạo template mới
          </button>
        </div>
      </section>

      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        <div className="grid gap-4 md:grid-cols-4">
          <div className="md:col-span-2">
            <Label>Tìm kiếm</Label>
            <Input
              placeholder="Tìm theo code, tên, mô tả..."
              value={filters.keyword}
              onChange={(e) => onFilterChange({ ...filters, keyword: e.target.value })}
            />
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
          <div>
            <Label>Đến ngày</Label>
            <Input
              type="date"
              value={filters.toDate}
              onChange={(e) => onFilterChange({ ...filters, toDate: e.target.value })}
            />
          </div>
          <div className="md:col-span-4">
            <p className="text-xs text-slate-500">
              {filteredCount} template phù hợp bộ lọc
            </p>
          </div>
        </div>
      </section>

      <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
        {error && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        {loading && (
          <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
            Đang tải danh sách templates...
          </div>
        )}
        {!loading && templates.length === 0 && (
          <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
            Không có template phù hợp bộ lọc.
          </div>
        )}

        {!loading && templates.length > 0 && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-3 py-3">Code</th>
                  <th className="px-3 py-3">Tên</th>
                  <th className="px-3 py-3">Hiệu lực</th>
                  <th className="px-3 py-3">Trạng thái</th>
                  <th className="px-3 py-3">Chi tiết</th>
                  <th className="px-3 py-3 text-right">Hành động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {templates.map((template) => (
                  <tr key={template.id} className="text-slate-700">
                    <td className="px-3 py-3 font-semibold text-slate-900">
                      {template.code || '--'}
                    </td>
                    <td className="px-3 py-3">
                      <div className="font-semibold text-slate-900">{template.name}</div>
                      <div className="text-xs text-slate-500 line-clamp-2">
                        {template.description || 'Không có mô tả'}
                      </div>
                    </td>
                    <td className="px-3 py-3 text-xs">
                      <div>{formatDate(template.effectiveFrom)}</div>
                      <div className="text-slate-400">→ {formatDate(template.effectiveTo)}</div>
                    </td>
                    <td className="px-3 py-3">
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-semibold ${
                          template.isActive
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        {template.isActive ? 'Đang hoạt động' : 'Tạm dừng'}
                      </span>
                    </td>
                    <td className="px-3 py-3 text-xs text-slate-600">
                      {template.detailsCount || 0} dòng lịch
                    </td>
                    <td className="px-3 py-3 text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => onView(template)}
                          className="rounded-lg bg-slate-200 px-3 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-300"
                        >
                          Xem
                        </button>
                        <button
                          type="button"
                          onClick={() => onEdit(template)}
                          className="rounded-lg bg-blue-600 px-3 py-1 text-xs font-semibold text-white hover:bg-blue-700"
                        >
                          Sửa
                        </button>
                        <button
                          type="button"
                          onClick={() => onGenerate(template)}
                          className="rounded-lg bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700"
                        >
                          Generate
                        </button>
                        <button
                          type="button"
                          onClick={() => onDelete(template)}
                          disabled={isDeletingId === template.id}
                          className="rounded-lg bg-red-500 px-3 py-1 text-xs font-semibold text-white hover:bg-red-600 disabled:opacity-50"
                        >
                          {isDeletingId === template.id ? 'Đang xóa' : 'Xóa'}
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
