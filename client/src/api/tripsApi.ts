import { apiFetch } from "./client";
import type { InterestedHiker, Trip, TripParticipantStatus, TripSummary } from "../types";

export function getMyTrips(): Promise<TripSummary[]> {
  return apiFetch("/api/trips");
}

export function getInterestedHikers(peakId: number): Promise<InterestedHiker[]> {
  return apiFetch(`/api/peaks/${peakId}/interested`);
}

export function createTrip(peakId: number, notes?: string, proposedDate?: string): Promise<Trip> {
  return apiFetch("/api/trips", {
    method: "POST",
    body: JSON.stringify({ peakId, notes: notes ?? null, proposedDate: proposedDate ?? null }),
  });
}

export function getTrip(id: number): Promise<Trip> {
  return apiFetch(`/api/trips/${id}`);
}

export function respondToTrip(id: number, status: Extract<TripParticipantStatus, "Joined" | "Declined">): Promise<Trip> {
  return apiFetch(`/api/trips/${id}/respond`, {
    method: "POST",
    body: JSON.stringify({ status }),
  });
}
