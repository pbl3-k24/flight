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
  id: template.id ?? template.templateId ?? template.Id ?? null,
  code: template.code || '',
  name: template.name || '',
  description: template.description || '',
  effectiveFrom: template.effectiveFrom || '',
  effectiveTo: template.effectiveTo || '',
  isActive: Boolean(template.isActive),
  detailsCount: template.details?.length || 0,
})

const mapDetailFromApi = (detail) => ({
  id: detail.id ?? null,
  tempId: detail.id ? null : `tmp-${Date.now()}-${Math.random().toString(16).slice(2)}`,
  flightDefinitionId: detail.flightDefinitionId,
  dayOfWeek: detail.dayOfWeek,
  aircraftOverrideId: detail.aircraftOverrideId ?? null,
  departureTimeOverride: detail.departureTimeOverride ?? null,
  arrivalTimeOverride: detail.arrivalTimeOverride ?? null,
  arrivalOffsetDaysOverride: detail.arrivalOffsetDaysOverride ?? null,
  isActive: detail.isActive ?? true,
  flightNumber: detail.flightNumber,
  routeName: detail.routeName,
  departureTime: detail.departureTime,
  arrivalTime: detail.arrivalTime,
  arrivalOffsetDays: detail.arrivalOffsetDays,
})

const mapDetailToPayload = (detail) => ({
  flightDefinitionId: detail.flightDefinitionId,
  dayOfWeek: detail.dayOfWeek,
  aircraftOverrideId: detail.aircraftOverrideId ?? null,
  departureTimeOverride: flightTemplateApi.normalizeTimeValue(detail.departureTimeOverride),
  arrivalTimeOverride: flightTemplateApi.normalizeTimeValue(detail.arrivalTimeOverride),
  arrivalOffsetDaysOverride: detail.arrivalOffsetDaysOverride ?? null,
  isActive: Boolean(detail.isActive),
})

const mapTemplatePayload = (template, details) => ({
  code: template.code?.trim(),
  name: template.name?.trim(),
  description: template.description || null,
  effectiveFrom: template.effectiveFrom || null,
  effectiveTo: template.effectiveTo || null,
  isActive: Boolean(template.isActive),
  details: details.map(mapDetailToPayload),
})

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
      setTemplatesError(error.message || 'Không thể tải danh sách templates.')
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
      setActionError(error.message || 'Không thể tải dữ liệu flight definition / aircraft.')
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
      setActionError(error.message || 'Không thể tải chi tiết template.')
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
      if (detailMode === 'create') {
        await flightTemplateApi.createFlightTemplate(payload)
        setNotice('Đã tạo template thành công.')
      } else {
        await flightTemplateApi.updateFlightTemplate(templateForm.id, payload)
        setNotice('Đã cập nhật template thành công.')
      }
      await loadTemplates()
      setMode('list')
    } catch (error) {
      setActionError(error.message || 'Không thể lưu template.')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteTemplate = async (template) => {
    if (!template?.id) return
    if (!window.confirm(`Xóa template "${template.name}"?`)) return

    setDeletingId(template.id)
    setActionError('')
    try {
      await flightTemplateApi.deleteFlightTemplate(template.id)
      setNotice(`Đã xóa template "${template.name}".`)
      await loadTemplates()
    } catch (error) {
      setActionError(error.message || 'Không thể xóa template.')
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
      setGenerateModal((prev) => ({ ...prev, error: error.message || 'Không thể generate flights.' }))
    } finally {
      setGenerating(false)
    }
  }

  if (mode === 'list') {
    return (
      <div className="space-y-4">
        {(notice || actionError) && (
          <div
            className={`rounded-2xl px-4 py-3 text-sm ${
              actionError
                ? 'border border-red-200 bg-red-50 text-red-700'
                : 'border border-emerald-200 bg-emerald-50 text-emerald-700'
            }`}
          >
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

  return (
    <div className="space-y-4">
      {(notice || actionError) && (
        <div
          className={`rounded-2xl px-4 py-3 text-sm ${
            actionError
              ? 'border border-red-200 bg-red-50 text-red-700'
              : 'border border-emerald-200 bg-emerald-50 text-emerald-700'
          }`}
        >
          {actionError || notice}
        </div>
      )}

      <div className="flex flex-wrap items-center justify-between gap-2">
        <button
          type="button"
          onClick={() => {
            setMode('list')
            setDetailMode('view')
          }}
          className="rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
        >
          ← Quay lại danh sách
        </button>

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => openGenerateModal(templateForm)}
            disabled={!templateForm.id}
            className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
          >
            Generate flights
          </button>
          {detailMode === 'view' && (
            <button
              type="button"
              onClick={() => setDetailMode('edit')}
              className="rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
            >
              Chuyển sang sửa
            </button>
          )}
          {detailMode !== 'view' && (
            <button
              type="button"
              onClick={handleSaveTemplate}
              disabled={saving}
              className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {saving ? 'Đang lưu...' : 'Lưu template'}
            </button>
          )}
        </div>
      </div>

      <FlightTemplateForm
        value={templateForm}
        errors={templateErrors}
        onChange={setTemplateForm}
        readOnly={detailMode === 'view'}
      />

      <FlightTemplateDetailTable
        details={detailItems}
        definitions={flightDefinitions}
        aircrafts={aircrafts}
        onAdd={() => {
          setEditingDetail(null)
          setDetailModalOpen(true)
        }}
        onEdit={(detail) => {
          setEditingDetail(detail)
          setDetailModalOpen(true)
        }}
        onDelete={handleDetailDelete}
        readOnly={detailMode === 'view'}
      />

      {(loadingDefinitions || loadingAircrafts) && (
        <div className="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500">
          Đang tải danh sách flight definition / aircraft...
        </div>
      )}

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
