import { apiFetch, API_BASE_URL } from "./client";
import type { Profile } from "../types";

export function toAbsolutePictureUrl(pictureUrl: string | null): string | null {
  return pictureUrl ? `${API_BASE_URL}${pictureUrl}` : null;
}

export function getProfile(): Promise<Profile> {
  return apiFetch("/api/profile");
}

export function updateProfile(firstName: string, lastName: string): Promise<Profile> {
  return apiFetch("/api/profile", {
    method: "PUT",
    body: JSON.stringify({ firstName, lastName }),
  });
}

export function uploadProfilePicture(file: File): Promise<Profile> {
  const formData = new FormData();
  formData.append("file", file);
  return apiFetch("/api/profile/picture", {
    method: "POST",
    body: formData,
  });
}

export function removeProfilePicture(): Promise<Profile> {
  return apiFetch("/api/profile/picture", {
    method: "DELETE",
  });
}

export function updateLocation(latitude: number, longitude: number): Promise<void> {
  return apiFetch("/api/profile/location", {
    method: "PUT",
    body: JSON.stringify({ latitude, longitude }),
  });
}
