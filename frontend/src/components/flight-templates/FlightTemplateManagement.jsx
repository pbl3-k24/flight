import { useEffect, useMemo, useState } from 'react'
import { flightTemplateApi } from '../../services/flightTemplateApi'
import FlightTemplateList from './FlightTemplateList'
import FlightTemplateForm from './FlightTemplateForm'
import FlightTemplateDetailTable from './FlightTemplateDetailTable'
import FlightTemplateDetailForm from './FlightTemplateDetailForm'
import GenerateFlightsModal from './GenerateFlightsModal'

const emptyTemplate = {
  id: null,
  code: '',
  name: '',
  description: '',
  effectiveFrom: '',
  effectiveTo: '',
  isActive: true,
}

const normalizeTemplate = (template) => ({
  id: template?.id ?? template?.templateId ?? template?.Id ?? null,
  code: template.code || '',
  name: template.name || '',
  description: template.description || '',
  effectiveFrom: template.effectiveFrom || '',
  effectiveTo: template.effectiveTo || '',
  isActive: Boolean(template.isActive),
  detailsCount: template.details?.length || 0,
})

const mapDetailFromApi = (detail) => ({
  id: detail?.id ?? null,
  tempId: detail.id ? null : `tmp-${Date.now()}-${Math.random().toString(16).slice(2)}`,
  flightDefinitionId: detail.flightDefinitionId,
  dayOfWeek: detail.dayOfWeek,
  aircraftOverrideId: detail?.aircraftOverrideId ?? null,
  departureTimeOverride: detail?.departureTimeOverride ?? null,
  arrivalTimeOverride: detail?.arrivalTimeOverride ?? null,
  arrivalOffsetDaysOverride: detail?.arrivalOffsetDaysOverride ?? null,
  isActive: detail?.isActive ?? true,
  flightNumber: detail.flightNumber,
  routeName: detail.routeName,
  departureTime: detail.departureTime,
  arrivalTime: detail.arrivalTime,
  arrivalOffsetDays: detail.arrivalOffsetDays,
})

const toNullableNumber = (value) => {
  if (value === '' || value === null || value === undefined) return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

const toTimeOrNull = (value) => {
  const normalized = flightTemplateApi.normalizeTimeValue(value)
  if (!normalized || typeof normalized !== 'string') return null
  return /^\d{2}:\d{2}:\d{2}$/.test(normalized) ? normalized : null
}

const mapDetailToPayload = (detail) => ({
  FlightDefinitionId: Number(detail.flightDefinitionId),
  DayOfWeek: Number(detail.dayOfWeek),
  AircraftOverrideId: toNullableNumber(detail.aircraftOverrideId),
  DepartureTimeOverride: toTimeOrNull(detail.departureTimeOverride),
  ArrivalTimeOverride: toTimeOrNull(detail.arrivalTimeOverride),
  ArrivalOffsetDaysOverride: toNullableNumber(detail.arrivalOffsetDaysOverride),
  IsActive: Boolean(detail.isActive),
})

const mapTemplatePayload = (template, details) => ({
  Code: String(template.code ?? '').trim().toUpperCase(),
  Name: String(template.name ?? '').trim(),
  Description: template.description || null,
  EffectiveFrom: template.effectiveFrom || null,
  EffectiveTo: template.effectiveTo || null,
  IsActive: Boolean(template.isActive),
  Details: details.map(mapDetailToPayload),
})

const validateDetailPayload = (detail) => {
  if (!Number.isInteger(detail.FlightDefinitionId) || detail.FlightDefinitionId <= 0) {
    return 'flightDefinitionId phai la ID FlightDefinition hop le (> 0).'
  }
  if (!Number.isInteger(detail.DayOfWeek) || detail.DayOfWeek < 0 || detail.DayOfWeek > 6) {
    return 'dayOfWeek phai nam trong khoang 0..6.'
  }
  if (detail.AircraftOverrideId !== null && (!Number.isInteger(detail.AircraftOverrideId) || detail.AircraftOverrideId <= 0)) {
    return 'aircraftOverrideId neu co phai > 0.'
  }
  if (detail.ArrivalOffsetDaysOverride !== null && (!Number.isInteger(detail.ArrivalOffsetDaysOverride) || detail.ArrivalOffsetDaysOverride < 0 || detail.ArrivalOffsetDaysOverride > 2)) {
    return 'arrivalOffsetDaysOverride phai trong khoang 0..2.'
  }
  return ''
}

const validateTemplate = (template) => {
  const errors = {}
  if (!template.code?.trim()) errors.code = 'Vui lòng nhập code.'
  if (!template.name?.trim()) errors.name = 'Vui lòng nhập tên template.'
  if (!template.effectiveFrom) errors.effectiveFrom = 'Chọn ngày hiệu lực từ.'
  if (!template.effectiveTo) errors.effectiveTo = 'Chọn ngày hiệu lực đến.'
  if (template.effectiveFrom && template.effectiveTo && template.effectiveTo < template.effectiveFrom) {
    errors.effectiveTo = 'Ngày hiệu lực đến phải sau hoặc bằng ngày bắt đầu.'
  }
  return errors
}

export default function FlightTemplateManagement() {
  const [templates, setTemplates] = useState([])
  const [loadingTemplates, setLoadingTemplates] = useState(false)
  const [templatesError, setTemplatesError] = useState('')
  const [filters, setFilters] = useState({
    keyword: '',
    isActive: 'all',
    fromDate: '',
    toDate: '',
  })

  const [mode, setMode] = useState('list')
  const [detailMode, setDetailMode] = useState('view')
  const [templateForm, setTemplateForm] = useState({ ...emptyTemplate })
  const [templateErrors, setTemplateErrors] = useState({})
  const [detailItems, setDetailItems] = useState([])

  const [flightDefinitions, setFlightDefinitions] = useState([])
  const [aircrafts, setAircrafts] = useState([])
  const [loadingDefinitions, setLoadingDefinitions] = useState(false)
  const [loadingAircrafts, setLoadingAircrafts] = useState(false)

  const [notice, setNotice] = useState('')
  const [actionError, setActionError] = useState('')
  const [saving, setSaving] = useState(false)
  const [deletingId, setDeletingId] = useState(null)

  const [detailModalOpen, setDetailModalOpen] = useState(false)
  const [editingDetail, setEditingDetail] = useState(null)

  const [generateModal, setGenerateModal] = useState({
    open: false,
    template: null,
    result: null,
    error: '',
  })
  const [generating, setGenerating] = useState(false)

  const loadTemplates = async () => {
    setLoadingTemplates(true)
    setTemplatesError('')
    try {
      const data = await flightTemplateApi.getFlightTemplates()
      setTemplates(data.map(normalizeTemplate))
    } catch (error) {
      setTemplatesError(error.message || 'Khï¿½ng th? t?i danh sï¿½ch templates.')
    } finally {
      setLoadingTemplates(false)
    }
  }

  const loadDefinitionsAndAircrafts = async () => {
    setLoadingDefinitions(true)
    setLoadingAircrafts(true)
    try {
      const [definitions, aircraftList] = await Promise.all([
        flightTemplateApi.getFlightDefinitions(true),
        flightTemplateApi.getAircrafts(),
      ])
      setFlightDefinitions(definitions)
      setAircrafts(aircraftList)
    } catch (error) {
      setActionError(error.message || 'Khï¿½ng th? t?i d? li?u flight definition / aircraft.')
    } finally {
      setLoadingDefinitions(false)
      setLoadingAircrafts(false)
    }
  }

  useEffect(() => {
    loadTemplates()
    loadDefinitionsAndAircrafts()
  }, [])

  const filteredTemplates = useMemo(() => {
    return templates.filter((template) => {
      const keyword = filters.keyword.trim().toLowerCase()
      const matchesKeyword = !keyword
        || [template.code, template.name, template.description]
          .filter(Boolean)
          .join(' ')
          .toLowerCase()
          .includes(keyword)

      const matchesStatus =
        filters.isActive === 'all'
          ? true
          : filters.isActive === 'active'
          ? template.isActive
          : !template.isActive

      const matchesFrom = filters.fromDate
        ? template.effectiveFrom && template.effectiveFrom >= filters.fromDate
        : true

      const matchesTo = filters.toDate
        ? template.effectiveTo && template.effectiveTo <= filters.toDate
        : true

      return matchesKeyword && matchesStatus && matchesFrom && matchesTo
    })
  }, [templates, filters])

  const openDetailView = async (template, modeType) => {
    setActionError('')
    setNotice('')
    setTemplateErrors({})
    setDetailItems([])

    if (!template?.id) return

    try {
      const detail = await flightTemplateApi.getFlightTemplateById(template.id)
      setTemplateForm({ ...emptyTemplate, ...normalizeTemplate(detail) })
      setDetailItems((detail.details || []).map(mapDetailFromApi))
      setMode('detail')
      setDetailMode(modeType)
    } catch (error) {
      setActionError(error.message || 'Khï¿½ng th? t?i chi ti?t template.')
    }
  }

  const startCreate = () => {
    setTemplateForm({ ...emptyTemplate })
    setDetailItems([])
    setTemplateErrors({})
    setActionError('')
    setNotice('')
    setMode('detail')
    setDetailMode('create')
  }

  const handleSaveTemplate = async () => {
    const errors = validateTemplate(templateForm)
    setTemplateErrors(errors)
    if (Object.keys(errors).length > 0) return

    setSaving(true)
    setActionError('')
    setNotice('')

    try {
      const payload = mapTemplatePayload(templateForm, detailItems)
      if (!payload.Code) {
        setActionError('Code la bat buoc.')
        return
      }
      if (!Array.isArray(payload.Details) || payload.Details.length === 0) {
        setActionError('Template phai co it nhat 1 detail.')
        return
      }
      for (let i = 0; i < payload.Details.length; i += 1) {
        const err = validateDetailPayload(payload.Details[i])
        if (err) {
          setActionError(`Detail #${i + 1}: ${err}`)
          return
        }
      }
      if (detailMode === 'create') {
        await flightTemplateApi.createFlightTemplate(payload)
        setNotice('Dï¿½ t?o template thï¿½nh cï¿½ng.')
      } else {
        await flightTemplateApi.updateFlightTemplate(templateForm.id, payload)
        setNotice('Dï¿½ cập nhật template thï¿½nh cï¿½ng.')
      }
      await loadTemplates()
      setMode('list')
    } catch (error) {
      setActionError(error.message || 'Khï¿½ng th? luu template.')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteTemplate = async (template) => {
    if (!template?.id) return
    if (!window.confirm(`Xï¿½a template "${template.name}"?`)) return

    setDeletingId(template.id)
    setActionError('')
    try {
      await flightTemplateApi.deleteFlightTemplate(template.id)
      setNotice(`Dï¿½ xï¿½a template "${template.name}".`)
      await loadTemplates()
    } catch (error) {
      setActionError(error.message || 'Khï¿½ng th? xï¿½a template.')
    } finally {
      setDeletingId(null)
    }
  }

  const handleDetailSave = (detail) => {
    setDetailItems((prev) => {
      const detailKey = detail.id ?? detail.tempId
      const existingIndex = prev.findIndex(
        (item) => (item.id ?? item.tempId) === detailKey
      )
      if (existingIndex >= 0) {
        const next = prev.slice()
        next[existingIndex] = detail
        return next
      }
      return [...prev, detail]
    })
    setDetailModalOpen(false)
  }

  const handleDetailDelete = (detail) => {
    setDetailItems((prev) => prev.filter((item) => (item.id ?? item.tempId) !== (detail.id ?? detail.tempId)))
  }

  const openGenerateModal = (template) => {
    setGenerateModal({ open: true, template, result: null, error: '' })
  }

  const handleGenerateFlights = async ({ fromDate, toDate, overwriteExisting }) => {
    if (!fromDate || !toDate || !generateModal.template?.id) return

    const start = new Date(`${fromDate}T00:00:00`)
    const end = new Date(`${toDate}T00:00:00`)
    const diffDays = Math.floor((end - start) / (1000 * 60 * 60 * 24))
    const numberOfWeeks = Math.max(1, Math.ceil((diffDays + 1) / 7))

    setGenerating(true)
    setGenerateModal((prev) => ({ ...prev, result: null, error: '' }))

    try {
      const payload = {
        templateId: Number(generateModal.template.id),
        weekStartDate: new Date(`${fromDate}T00:00:00Z`).toISOString(),
        numberOfWeeks,
      }

      if (flightTemplateApi.supportsOverwrite) {
        payload.overwriteExisting = Boolean(overwriteExisting)
      }

      const result = await flightTemplateApi.generateFlightsFromTemplate(payload)
      setGenerateModal((prev) => ({ ...prev, result }))
    } catch (error) {
      setGenerateModal((prev) => ({ ...prev, error: error.message || 'Khï¿½ng th? generate flights.' }))
    } finally {
      setGenerating(false)
    }
  }

  if (mode === 'list') {
    return (
      <div className="space-y-4">
        {(notice || actionError) && (
          <div
            className={`flex items-center gap-2.5 rounded-2xl px-4 py-3 text-sm ${
              actionError
                ? 'border border-red-200 bg-red-50 text-red-700'
                : 'border border-emerald-200 bg-emerald-50 text-emerald-700'
            }`}
          >
            <svg className="h-4 w-4 shrink-0" fill="currentColor" viewBox="0 0 20 20">
              {actionError
                ? <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                : <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />}
            </svg>
            {actionError || notice}
          </div>
        )}
        <FlightTemplateList
          templates={filteredTemplates}
          loading={loadingTemplates}
          error={templatesError}
          filters={filters}
          onFilterChange={setFilters}
          onCreate={startCreate}
          onView={(template) => openDetailView(template, 'view')}
          onEdit={(template) => openDetailView(template, 'edit')}
          onDelete={handleDeleteTemplate}
          onGenerate={openGenerateModal}
          isDeletingId={deletingId}
        />
        <GenerateFlightsModal
          open={generateModal.open}
          template={generateModal.template}
          supportsOverwrite={flightTemplateApi.supportsOverwrite}
          onClose={() => setGenerateModal({ open: false, template: null, result: null, error: '' })}
          onGenerate={handleGenerateFlights}
          isSubmitting={generating}
          result={generateModal.result}
          error={generateModal.error}
        />
      </div>
    )
  }

  const isModeCreate = detailMode === 'create'
  const isModeEdit = detailMode === 'edit'
  const isModeView = detailMode === 'view'

  return (
    <div className="space-y-4">
      {/* Notice / Error banner */}
      {(notice || actionError) && (
        <div
          className={`flex items-center gap-2.5 rounded-2xl px-4 py-3 text-sm ${
            actionError
              ? 'border border-red-200 bg-red-50 text-red-700'
              : 'border border-emerald-200 bg-emerald-50 text-emerald-700'
          }`}
        >
          <svg className="h-4 w-4 shrink-0" fill="currentColor" viewBox="0 0 20 20">
            {actionError
              ? <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              : <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />}
          </svg>
          {actionError || notice}
        </div>
      )}

      {/* Action bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-white px-5 py-3.5 shadow-lg shadow-slate-100">
        {/* Back button */}
        <button
          type="button"
          onClick={() => {
            setMode('list')
            setDetailMode('view')
          }}
          className="flex items-center gap-1.5 rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-600 shadow-sm transition hover:bg-slate-50"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Quay lại danh sách
        </button>

        {/* Mode badge */}
        <span className={`rounded-full px-3 py-1 text-xs font-bold ${
          isModeCreate ? 'bg-blue-100 text-blue-700'
          : isModeEdit ? 'bg-amber-100 text-amber-700'
          : 'bg-slate-100 text-slate-600'
        }`}>
          {isModeCreate ? '✦ Tạo mới' : isModeEdit ? '✎ Đang chỉnh sửa' : '◎ Chế độ xem'}
        </span>

        {/* Right actions */}
        <div className="flex flex-wrap gap-2">
          {templateForm.id && (
            <button
              type="button"
              onClick={() => openGenerateModal(templateForm)}
              className="flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
              Generate Flights
            </button>
          )}

          {isModeView && (
            <button
              type="button"
              onClick={() => setDetailMode('edit')}
              className="flex items-center gap-1.5 rounded-xl bg-[#1E40AF] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-800"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
              Chỉnh sửa
            </button>
          )}

          {!isModeView && (
            <button
              type="button"
              onClick={handleSaveTemplate}
              disabled={saving}
              className="flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {saving ? (
                <>
                  <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
                  </svg>
                  Đang lưu...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                  </svg>
                  Lưu template
                </>
              )}
            </button>
          )}
        </div>
      </div>

      {/* Loading definitions */}
      {(loadingDefinitions || loadingAircrafts) && (
        <div className="flex items-center justify-center gap-2.5 rounded-2xl border border-dashed border-slate-300 bg-white py-4 text-sm text-slate-500">
          <svg className="h-4 w-4 animate-spin text-blue-400" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
          </svg>
          Đang tải dữ liệu flight definition / aircraft...
        </div>
      )}

      <FlightTemplateForm
        value={templateForm}
        errors={templateErrors}
        onChange={setTemplateForm}
        readOnly={isModeView}
      />

      <FlightTemplateDetailTable
        details={detailItems}
        definitions={flightDefinitions}
        aircrafts={aircrafts}
        onAdd={() => {
          setEditingDetail(null)
          setDetailModalOpen(true)
        }}
        onEdit={(detail, fromInline) => {
          if (fromInline) {
            handleDetailSave(detail)
            return
          }
          setEditingDetail(detail)
          setDetailModalOpen(true)
        }}
        onDelete={handleDetailDelete}
        readOnly={isModeView}
      />

      <FlightTemplateDetailForm
        open={detailModalOpen}
        onClose={() => setDetailModalOpen(false)}
        onSubmit={handleDetailSave}
        existingDetails={detailItems}
        flightDefinitions={flightDefinitions}
        aircrafts={aircrafts}
        initialValue={editingDetail}
      />

      <GenerateFlightsModal
        open={generateModal.open}
        template={generateModal.template}
        supportsOverwrite={flightTemplateApi.supportsOverwrite}
        onClose={() => setGenerateModal({ open: false, template: null, result: null, error: '' })}
        onGenerate={handleGenerateFlights}
        isSubmitting={generating}
        result={generateModal.result}
        error={generateModal.error}
      />
    </div>
  )
}
