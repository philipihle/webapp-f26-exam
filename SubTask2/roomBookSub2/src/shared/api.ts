export const API_URL = import.meta.env.VITE_API_URL;

// Adds the JWT from localStorage so protected endpoints ([Authorize]) accept the request
export function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem('token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

// Reads the error message from the API: plain text (e.g. domain rules) or JSON validation title
async function getErrorMessage(response: Response): Promise<string> {
  const text = await response.text();
  try {
    const json = JSON.parse(text);
    return json.title ?? text;
  } catch {
    return text || `Request failed (status ${response.status})`;
  }
}

export async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
  return response.json();
}

export async function ensureOk(response: Response): Promise<void> {
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}
