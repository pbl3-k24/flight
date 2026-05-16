const Label = ({ children }) => (
  <p className="mb-2 text-sm font-semibold text-slate-700">{children}</p>
)

const Input = (props) => (
  <input
    className="w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
    {...props}
  />
)

const Textarea = (props) => (
  <textarea
    className="min-h-[96px] w-full rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
    {...props}
  />
)

export default function FlightTemplateForm({ value, errors, onChange, readOnly }) {
  return (
    <section className="rounded-2xl bg-white p-5 shadow-lg shadow-slate-200 md:p-7">
      <div className="mb-4">
        <h3 className="text-lg font-bold text-slate-900">Thông tin template</h3>
        <p className="text-sm text-slate-500">
          Cấu hình giai đoạn hiệu lực và trạng thái áp dụng.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <Label>Code *</Label>
          <Input
            placeholder="VD: SUM26"
            value={value.code}
            onChange={(e) => onChange({ ...value, code: e.target.value })}
            disabled={readOnly}
          />
          {errors.code && <p className="mt-1 text-xs text-red-600">{errors.code}</p>}
        </div>
        <div>
          <Label>Tên template *</Label>
          <Input
            placeholder="VD: Summer 2026"
            value={value.name}
            onChange={(e) => onChange({ ...value, name: e.target.value })}
            disabled={readOnly}
          />
          {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name}</p>}
        </div>
        <div className="md:col-span-2">
          <Label>Mô tả</Label>
          <Textarea
            placeholder="Mô tả mục đích sử dụng template"
            value={value.description}
            onChange={(e) => onChange({ ...value, description: e.target.value })}
            disabled={readOnly}
          />
        </div>
        <div>
          <Label>Hiệu lực từ *</Label>
          <Input
            type="date"
            value={value.effectiveFrom}
            onChange={(e) => onChange({ ...value, effectiveFrom: e.target.value })}
            disabled={readOnly}
          />
          {errors.effectiveFrom && (
            <p className="mt-1 text-xs text-red-600">{errors.effectiveFrom}</p>
          )}
        </div>
        <div>
          <Label>Hiệu lực đến *</Label>
          <Input
            type="date"
            value={value.effectiveTo}
            onChange={(e) => onChange({ ...value, effectiveTo: e.target.value })}
            disabled={readOnly}
          />
          {errors.effectiveTo && (
            <p className="mt-1 text-xs text-red-600">{errors.effectiveTo}</p>
          )}
        </div>
        <div className="md:col-span-2">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={value.isActive}
              onChange={(e) => onChange({ ...value, isActive: e.target.checked })}
              disabled={readOnly}
              className="h-4 w-4 rounded border-slate-300 text-[#1E40AF] focus:ring-2 focus:ring-blue-100"
            />
            <span className="text-sm text-slate-700">Kích hoạt template</span>
          </label>
        </div>
      </div>
    </section>
  )
}
