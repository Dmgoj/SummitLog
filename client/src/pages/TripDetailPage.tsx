import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getTrip, respondToTrip } from "../api/tripsApi";
import { useAuth } from "../auth/useAuth";
import { ApiError } from "../api/client";
import type { Trip } from "../types";

function personName(firstName: string | null, lastName: string | null, fallback: string): string {
  const name = [firstName, lastName].filter(Boolean).join(" ");
  return name || fallback;
}

export function TripDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { profile } = useAuth();
  const [trip, setTrip] = useState<Trip | null>(null);
  const [loading, setLoading] = useState(true);
  const [responding, setResponding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    getTrip(Number(id))
      .then(setTrip)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Could not load trip."))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleRespond(status: "Joined" | "Declined") {
    if (!trip) return;
    setResponding(true);
    setError(null);
    try {
      const updated = await respondToTrip(trip.id, status);
      setTrip(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not update your response.");
    } finally {
      setResponding(false);
    }
  }

  if (loading) {
    return <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Loading...</p>;
  }

  if (error && !trip) {
    return <p style={{ padding: "56px 40px", color: "var(--color-danger)" }}>{error}</p>;
  }

  if (!trip) {
    return <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Trip not found.</p>;
  }

  const creatorName = personName(trip.creatorFirstName, trip.creatorLastName, trip.creatorEmail ?? "The organizer");
  const joined = trip.participants.filter((p) => p.status === "Joined");
  const isInvited = trip.callerStatus === "Invited";

  return (
    <div style={{ maxWidth: 640, margin: "0 auto", padding: "56px 24px 64px" }}>
      <div
        className="mono link"
        style={{ fontSize: 12, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)", display: "flex", gap: 10, marginBottom: 20 }}
      >
        <Link to={`/peaks/${trip.peakId}`} className="link" style={{ color: "var(--color-text-muted)" }}>
          {trip.peakName}
        </Link>
        <span>/</span>
        <span style={{ color: "var(--color-accent)" }}>Trip</span>
      </div>

      <h1 style={{ margin: "0 0 10px", fontFamily: "var(--font-display)", fontSize: 36, letterSpacing: 0.3, textTransform: "uppercase" }}>
        Trip to {trip.peakName}
      </h1>
      <p style={{ color: "var(--color-text-muted)", fontSize: 14, marginBottom: trip.proposedDate ? 8 : 24 }}>
        Organized by {creatorName}
        {trip.creatorEmail && ` (${trip.creatorEmail})`}
      </p>

      {trip.proposedDate && (
        <p style={{ color: "var(--color-accent-text)", fontSize: 14, marginBottom: 24, fontWeight: 700 }}>
          Proposed date: {new Date(trip.proposedDate).toLocaleDateString(undefined, { dateStyle: "long" })}
        </p>
      )}

      {trip.notes && (
        <div className="card" style={{ padding: 18, marginBottom: 24, fontSize: 14 }}>
          {trip.notes}
        </div>
      )}

      {isInvited && (
        <div style={{ display: "flex", gap: 10, marginBottom: 24 }}>
          <button onClick={() => handleRespond("Joined")} disabled={responding} className="btn btn-primary">
            Join trip
          </button>
          <button onClick={() => handleRespond("Declined")} disabled={responding} className="btn btn-ghost">
            Decline
          </button>
        </div>
      )}

      {trip.callerStatus === "Joined" && (
        <p style={{ color: "var(--color-accent-text)", fontSize: 14, marginBottom: 24 }}>You've joined this trip.</p>
      )}
      {trip.callerStatus === "Declined" && (
        <p style={{ color: "var(--color-text-faint)", fontSize: 14, marginBottom: 24 }}>You declined this trip.</p>
      )}

      {error && <p style={{ color: "var(--color-danger)", fontSize: 14, marginBottom: 16 }}>{error}</p>}

      <div className="mono" style={{ fontSize: 11, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)", marginBottom: 10 }}>
        Joined ({joined.length})
      </div>
      {joined.length > 0 ? (
        <ul style={{ margin: 0, padding: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 8 }}>
          {joined.map((p) => (
            <li key={p.userId} className="card" style={{ padding: 14, display: "flex", justifyContent: "space-between", fontSize: 14 }}>
              <span>
                {personName(p.firstName, p.lastName, "A hiker")}
                {p.email && p.email === profile?.email ? " (you)" : ""}
              </span>
              {p.email && <span style={{ color: "var(--color-text-faint)" }}>{p.email}</span>}
            </li>
          ))}
        </ul>
      ) : (
        <p style={{ color: "var(--color-text-faint)", fontSize: 13 }}>No one has joined yet.</p>
      )}
    </div>
  );
}
