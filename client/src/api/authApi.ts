import { apiFetch } from "./client";
import type { AuthResponse } from "../types";

export function register(email: string, password: string): Promise<{ userId: string; email: string }> {
  return apiFetch("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function login(email: string, password: string): Promise<AuthResponse> {
  return apiFetch("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export function confirmEmail(userId: string, token: string): Promise<void> {
  return apiFetch("/api/auth/confirm-email", {
    method: "POST",
    body: JSON.stringify({ userId, token }),
  });
}

export function resendConfirmation(email: string): Promise<string> {
  return apiFetch("/api/auth/resend-confirmation", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function forgotPassword(email: string): Promise<string> {
  return apiFetch("/api/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(userId: string, token: string, newPassword: string): Promise<void> {
  return apiFetch("/api/auth/reset-password", {
    method: "POST",
    body: JSON.stringify({ userId, token, newPassword }),
  });
}
