export interface PeakSummary {
  id: number;
  name: string;
  countryCode: string;
  elevationMeters: number | null;
}

export interface PeakDetail {
  id: number;
  geoNameId: number;
  name: string;
  alternateNames: string | null;
  latitude: number;
  longitude: number;
  elevationMeters: number | null;
  elevationIsOverridden: boolean;
  elevationSource: string | null;
  countryCode: string;
  countryName: string;
  featureCode: string;
}

export interface PeakSearchResult {
  items: PeakSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface VisitedPeak {
  peakId: number;
  peakName: string;
  latitude: number;
  longitude: number;
  elevationMeters: number | null;
  countryCode: string;
  visitedOn: string;
  notes: string | null;
}

export interface BucketListEntry {
  peakId: number;
  peakName: string;
  latitude: number;
  longitude: number;
  elevationMeters: number | null;
  countryCode: string;
}

export interface CountryOption {
  code: string;
  name: string;
}

export interface Profile {
  email: string;
  firstName: string | null;
  lastName: string | null;
  profilePictureUrl: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  email: string;
}

export interface InterestedHiker {
  userId: string;
  firstName: string | null;
  lastName: string | null;
  distanceKm: number | null;
}

export type TripParticipantStatus = "Invited" | "Joined" | "Declined";

export interface TripParticipant {
  userId: string;
  firstName: string | null;
  lastName: string | null;
  status: TripParticipantStatus;
  email: string | null;
}

export interface Trip {
  id: number;
  peakId: number;
  peakName: string;
  creatorUserId: string;
  creatorFirstName: string | null;
  creatorLastName: string | null;
  creatorEmail: string | null;
  notes: string | null;
  proposedDate: string | null;
  createdAt: string;
  callerStatus: TripParticipantStatus | null;
  participants: TripParticipant[];
}

export interface TripSummary {
  id: number;
  peakId: number;
  peakName: string;
  isCreator: boolean;
  callerStatus: TripParticipantStatus | null;
  participantCount: number;
  proposedDate: string | null;
  createdAt: string;
}

export interface AppNotification {
  id: number;
  type: string;
  tripId: number | null;
  message: string;
  isRead: boolean;
  createdAt: string;
}
