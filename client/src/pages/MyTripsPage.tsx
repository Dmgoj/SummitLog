import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyTrips } from "../api/tripsApi";
import type { TripSummary } from "../types";

function statusLabel(trip: TripSummary): string {
  if (trip.isCreator) {
    return "Organizer";
  }
  return trip.callerStatus ?? "Invited";
}

export function MyTripsPage() {
  const [trips, setTrips] = useState<TripSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyTrips()
      .then(setTrips)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Loading...</p>;
  }

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: "56px 40px 64px" }}>
      <div className="mono" style={{ fontSize: 12, letterSpacing: 3, textTransform: "uppercase", color: "var(--color-accent)", marginBottom: 14 }}>
        Group hikes
      </div>
      <h1 style={{ margin: "0 0 32px", fontFamily: "var(--font-display)", fontSize: 44, letterSpacing: 0.3, textTransform: "uppercase" }}>
        My Trips
      </h1>

      {trips.length === 0 && (
        <p style={{ color: "var(--color-text-muted)" }}>
          No trips yet. Add a peak to your bucket list and create a trip from its page to get started.
        </p>
      )}

      {trips.map((trip) => (
        <Link
          key={trip.id}
          to={`/trips/${trip.id}`}
          className="link"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 20,
            padding: "18px 12px",
            borderTop: "1px solid var(--color-border)",
          }}
        >
          <div>
            <div style={{ fontWeight: 700, fontSize: 17, color: "var(--color-text)" }}>{trip.peakName}</div>
            <div style={{ fontSize: 13, color: "var(--color-text-muted)", marginTop: 4 }}>
              {trip.participantCount} joined · created {new Date(trip.createdAt).toLocaleDateString()}
            </div>
          </div>
          <div
            className="mono"
            style={{
              fontSize: 11,
              letterSpacing: 1,
              textTransform: "uppercase",
              color: trip.isCreator || trip.callerStatus === "Joined" ? "var(--color-accent-text)" : "var(--color-text-faint)",
              padding: "6px 12px",
              borderRadius: 999,
              border: "1px solid var(--color-border)",
              flexShrink: 0,
            }}
          >
            {statusLabel(trip)}
          </div>
        </Link>
      ))}
    </div>
  );
}
