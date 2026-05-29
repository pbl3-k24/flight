const Label = ({ children, required }) => (
  <p className="mb-1.5 text-sm font-semibold text-slate-700">
    {children}
    {required && <span className="ml-1 text-red-500">*</span>}
  </p>
)

const Input = ({ error, ...props }) => (
  <input
    className={`w-full rounded-xl border bg-white px-4 py-2.5 text-sm outline-none transition
      ${error
        ? 'border-red-300 focus:border-red-400 focus:ring-2 focus:ring-red-100'
        : 'border-slate-200 focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100'}
      disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400`}
    {...props}
  />
)

const Textarea = ({ error, ...props }) => (
  <textarea
    className={`min-h-[88px] w-full resize-none rounded-xl border bg-white px-4 py-2.5 text-sm outline-none transition
      ${error
        ? 'border-red-300 focus:border-red-400 focus:ring-2 focus:ring-red-100'
        : 'border-slate-200 focus:border-[#1E40AF] focus:ring-2 focus:ring-blue-100'}
      disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400`}
    {...props}
  />
)

const FieldError = ({ message }) =>
  message ? (
    <p className="mt-1 flex items-center gap-1 text-xs text-red-600">
      <svg className="h-3 w-3 shrink-0" fill="currentColor" viewBox="0 0 20 20">
        <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
      </svg>
      {message}
    </p>
  ) : null

export default function FlightTemplateForm({ value, errors = {}, onChange, readOnly }) {
  return (
    <section className="rounded-2xl bg-white shadow-lg shadow-slate-100 overflow-hidden">
      {/* Section header */}
      <div className="border-b border-slate-100 bg-slate-50 px-6 py-4">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#1E40AF]/10">
            <svg className="h-4 w-4 text-[#1E40AF]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">Thông tin template</h3>
            <p className="text-xs text-slate-500">Cấu hình giai đoạn hiệu lực và trạng thái áp dụng</p>
          </div>
          {readOnly && (
            <span className="ml-auto inline-flex items-center gap-1 rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-500">
              <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
              Chế độ xem
            </span>
          )}
        </div>
      </div>

      {/* Form body */}
      <div className="p-6">
        <div className="grid gap-5 md:grid-cols-2">
          {/* Code */}
          <div>
            <Label required>Code</Label>
            <Input
              placeholder="VD: SUM26"
              value={value.code}
              onChange={(e) => onChange({ ...value, code: e.target.value.toUpperCase() })}
              disabled={readOnly}
              error={errors.code}
              maxLength={20}
            />
            <FieldError message={errors.code} />
          </div>

          {/* Name */}
          <div>
            <Label required>Tên template</Label>
            <Input
              placeholder="VD: Summer 2026"
              value={value.name}
              onChange={(e) => onChange({ ...value, name: e.target.value })}
              disabled={readOnly}
              error={errors.name}
            />
            <FieldError message={errors.name} />
          </div>

          {/* Description */}
          <div className="md:col-span-2">
            <Label>Mô tả</Label>
            <Textarea
              placeholder="Mô tả mục đích sử dụng template..."
              value={value.description}
              onChange={(e) => onChange({ ...value, description: e.target.value })}
              disabled={readOnly}
            />
          </div>

          {/* Effective From */}
          <div>
            <Label required>Hiệu lực từ</Label>
            <Input
              type="date"
              value={value.effectiveFrom}
              onChange={(e) => onChange({ ...value, effectiveFrom: e.target.value })}
              disabled={readOnly}
              error={errors.effectiveFrom}
            />
            <FieldError message={errors.effectiveFrom} />
          </div>

          {/* Effective To */}
          <div>
            <Label required>Hiệu lực đến</Label>
            <Input
              type="date"
              value={value.effectiveTo}
              onChange={(e) => onChange({ ...value, effectiveTo: e.target.value })}
              disabled={readOnly}
              error={errors.effectiveTo}
            />
            <FieldError message={errors.effectiveTo} />
          </div>

          {/* IsActive */}
          <div className="md:col-span-2">
            <label className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 transition
              ${readOnly ? 'cursor-not-allowed border-slate-100 bg-slate-50' : 'border-slate-200 bg-white hover:border-blue-300 hover:bg-blue-50/30'}
              ${value.isActive ? 'border-emerald-200 bg-emerald-50/30' : ''}`}
            >
              <div className="relative">
                <input
                  type="checkbox"
                  checked={value.isActive}
                  onChange={(e) => onChange({ ...value, isActive: e.target.checked })}
                  disabled={readOnly}
                  className="peer sr-only"
                />
                {/* Custom toggle */}
                <div className={`h-5 w-9 rounded-full border-2 transition-colors
                  ${value.isActive ? 'border-emerald-500 bg-emerald-500' : 'border-slate-300 bg-slate-200'}`}
                />
                <div className={`absolute left-0.5 top-0.5 h-3.5 w-3.5 rounded-full bg-white shadow-sm transition-transform
                  ${value.isActive ? 'translate-x-4' : 'translate-x-0'}`}
                />
              </div>
              <div>
                <p className={`text-sm font-semibold ${value.isActive ? 'text-emerald-700' : 'text-slate-600'}`}>
                  {value.isActive ? 'Kích hoạt' : 'Tạm dừng'}
                </p>
                <p className="text-xs text-slate-400">
                  {value.isActive ? 'Template đang được áp dụng' : 'Template tạm thời không hoạt động'}
                </p>
              </div>
            </label>
          </div>
        </div>
      </div>
    </section>
  )
}
