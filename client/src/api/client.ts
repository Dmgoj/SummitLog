export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
const TOKEN_STORAGE_KEY = "summitlog.token";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string | null): void {
  if (token) {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  }
}

function extractErrorMessage(body: string): string | null {
  if (!body) {
    return null;
  }

  try {
    const parsed = JSON.parse(body);
    if (Array.isArray(parsed)) {
      return parsed.join(" ");
    }
    if (typeof parsed === "string") {
      return parsed;
    }
    if (parsed && typeof parsed === "object") {
      if (typeof parsed.title === "string") {
        return parsed.title;
      }
      if (parsed.errors && typeof parsed.errors === "object") {
        return Object.values(parsed.errors).flat().join(" ");
      }
    }
  } catch {
    return body;
  }

  return body;
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers = new Headers(options.headers);
  if (!(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });

  if (response.status === 401) {
    setToken(null);
    throw new ApiError(401, "Session expired. Please log in again.");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new ApiError(response.status, extractErrorMessage(text) || response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}
