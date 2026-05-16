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
    const apiError = new Error(error.detail || error.message || `API Error: ${response.statusText}`)
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
    body: JSON.stringify(payload),
  })

const updateFlightTemplate = (id, payload) =>
  makeRequest(endpoints.templateById(id), {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const deleteFlightTemplate = (id) =>
  makeRequest(endpoints.templateById(id), {
    method: 'DELETE',
  })

const getFlightDefinitions = async (activeOnly = true) => {
  const data = await makeRequest(`${endpoints.flightDefinitions}${buildQuery({ activeOnly })}`)
  return normalizeList(data)
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
