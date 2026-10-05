import { apiFetch } from "./client";
import type { AppNotification } from "../types";

export function getNotifications(): Promise<AppNotification[]> {
  return apiFetch("/api/notifications");
}

export function markNotificationRead(id: number): Promise<void> {
  return apiFetch(`/api/notifications/${id}/read`, { method: "POST" });
}
