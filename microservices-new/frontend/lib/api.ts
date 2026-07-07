export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

export const apiRequest = async (endpoint: string, options: RequestInit = {}) => {
  const token = localStorage.getItem('token')
  const isFormData = options.body instanceof FormData

  const headers: Record<string, string> = {
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...((options.headers as Record<string, string>) || {}),
  }

  if (!isFormData) {
    headers['Content-Type'] = 'application/json'
  }

  const config: RequestInit = {
    ...options,
    headers,
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, config)

  if (!response.ok) {
    const contentType = response.headers.get('content-type') || ''
    let errorMessage = `HTTP error! status: ${response.status}`

    if (contentType.includes('application/json')) {
      const errorBody = await response.json().catch(() => null)
      if (typeof errorBody === 'string') {
        errorMessage = errorBody
      } else if (errorBody && typeof errorBody === 'object') {
        const message = (errorBody as { message?: unknown }).message
        if (typeof message === 'string') {
          errorMessage = message
        }
      }
    } else {
      const text = await response.text().catch(() => '')
      if (text) {
        errorMessage = text
      }
    }

    throw new Error(errorMessage)
  }

  return response.json()
}