const API_ROOT_URL = import.meta.env?.VITE_API_ROOT_URL || 'http://localhost:5042'
const API_BASE_URL = `${API_ROOT_URL}/api/v1`

const endpoints = {
  templates: '/admin/flight-templates',
  templateById: (id) => `/admin/flight-templates/${id}`,
  generate: '/admin/flight-templates/generate',
  flightDefinitions: '/admin/flight-definitions',
  aircrafts: '/admin/aircraft',
}

const supportsOverwrite = false

const getAuthToken = () => {
  try {
    return localStorage.getItem('authToken')
  } catch {
    return null
  }
}

const normalizeList = (value) => {
  if (Array.isArray(value)) return value
  if (Array.isArray(value?.items)) return value.items
  if (Array.isArray(value?.data)) return value.data
  if (Array.isArray(value?.$values)) return value.$values
  return []
}

const normalizeTimeValue = (value) => {
  if (!value) return null
  if (typeof value !== 'string') return value
  if (value.length >= 8) return value.slice(0, 8)
  if (value.length === 5) return `${value}:00`
  return value
}

const toNullableNumber = (value) => {
  if (value === '' || value === null || value === undefined) return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

const toBoolean = (value) => Boolean(value)

const normalizeTemplateDetailPayload = (detail = {}) => {
  const fdId = Number(
    detail.flightDefinitionId ??
    detail.FlightDefinitionId ??
    0
  )
  return {
    FlightDefinitionId: fdId,
    DayOfWeek: Number(detail.DayOfWeek ?? detail.dayOfWeek ?? 0),
    AircraftOverrideId: toNullableNumber(detail.AircraftOverrideId ?? detail.aircraftOverrideId),
    DepartureTimeOverride: normalizeTimeValue(detail.DepartureTimeOverride ?? detail.departureTimeOverride ?? detail.departureTime),
    ArrivalTimeOverride: normalizeTimeValue(detail.ArrivalTimeOverride ?? detail.arrivalTimeOverride ?? detail.arrivalTime),
    ArrivalOffsetDaysOverride: toNullableNumber(detail.ArrivalOffsetDaysOverride ?? detail.arrivalOffsetDaysOverride),
    IsActive: toBoolean(detail.IsActive ?? detail.isActive ?? true),
  }
}

const normalizeTemplatePayload = (payload = {}) => ({
  Code: String(payload.Code ?? payload.code ?? '').trim().toUpperCase(),
  Name: String(payload.Name ?? payload.name ?? '').trim(),
  Description: payload.Description ?? payload.description ?? null,
  EffectiveFrom: payload.EffectiveFrom ?? payload.effectiveFrom ?? null,
  EffectiveTo: payload.EffectiveTo ?? payload.effectiveTo ?? null,
  IsActive: toBoolean(payload.IsActive ?? payload.isActive ?? true),
  Details: Array.isArray(payload.Details ?? payload.details)
    ? (payload.Details ?? payload.details).map(normalizeTemplateDetailPayload)
    : [],
})

const makeRequest = async (endpoint, options = {}) => {
  const url = `${API_BASE_URL}${endpoint}`
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  }
  const token = getAuthToken()
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(url, {
    ...options,
    headers,
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    const modelErrors = error?.errors && typeof error.errors === 'object'
      ? Object.values(error.errors).flat().filter(Boolean)
      : []
    const apiError = new Error(
      modelErrors[0]
        || error.detail
        || error.message
        || `API Error: ${response.statusText}`
    )
    apiError.status = response.status
    apiError.responseBody = error
    throw apiError
  }

  const data = await response.json().catch(() => null)
  if (data?.error || data?.message) {
    const apiError = new Error(data.error || data.message)
    apiError.status = response.status
    apiError.responseBody = data
    throw apiError
  }

  return data
}

const buildQuery = (params = {}) => {
  const searchParams = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return
    searchParams.set(key, String(value))
  })
  const query = searchParams.toString()
  return query ? `?${query}` : ''
}

const getFlightTemplates = async () => {
  const data = await makeRequest(endpoints.templates)
  return normalizeList(data)
}

const getFlightTemplateById = (id) => makeRequest(endpoints.templateById(id))

const createFlightTemplate = (payload) =>
  makeRequest(endpoints.templates, {
    method: 'POST',
    body: JSON.stringify(normalizeTemplatePayload(payload)),
  })

const updateFlightTemplate = (id, payload) =>
  makeRequest(endpoints.templateById(id), {
    method: 'PUT',
    body: JSON.stringify(normalizeTemplatePayload(payload)),
  })

const deleteFlightTemplate = (id) =>
  makeRequest(endpoints.templateById(id), {
    method: 'DELETE',
  })

const getFlightDefinitions = async (activeOnly = true) => {
  const data = await makeRequest(`${endpoints.flightDefinitions}${buildQuery({ activeOnly })}`)
  const list = normalizeList(data)
  return list.map((item) => ({
    ...item,
    id: item?.id ?? item?.flightDefinitionId ?? null,
  })).filter(item => item.id !== null)
}

const getAircrafts = async () => {
  const data = await makeRequest(`${endpoints.aircrafts}?page=1&pageSize=100&includeDeleted=false`)
  return normalizeList(data)
}

const generateFlightsFromTemplate = (payload) =>
  makeRequest(endpoints.generate, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const flightTemplateApi = {
  endpoints,
  supportsOverwrite,
  normalizeTimeValue,
  normalizeTemplatePayload,
  normalizeList,
  getFlightTemplates,
  getFlightTemplateById,
  createFlightTemplate,
  updateFlightTemplate,
  deleteFlightTemplate,
  getFlightDefinitions,
  getAircrafts,
  generateFlightsFromTemplate,
}
