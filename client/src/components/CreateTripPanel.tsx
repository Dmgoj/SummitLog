import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getInterestedHikers, createTrip } from "../api/tripsApi";
import { requestAndShareLocation } from "../api/geolocation";
import { ApiError } from "../api/client";
import type { InterestedHiker } from "../types";

interface Props {
  peakId: number;
  peakName: string;
}

function hikerName(h: InterestedHiker): string {
  const name = [h.firstName, h.lastName].filter(Boolean).join(" ");
  return name || "A hiker";
}

export function CreateTripPanel({ peakId, peakName }: Props) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [hikers, setHikers] = useState<InterestedHiker[] | null>(null);
  const [notes, setNotes] = useState("");
  const [proposedDate, setProposedDate] = useState("");
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleOpen() {
    setOpen(true);
    setError(null);
    setLoading(true);
    try {
      try {
        await requestAndShareLocation();
      } catch {
        // Location is optional — proceed without distance sorting if denied/unavailable.
      }
      const list = await getInterestedHikers(peakId);
      setHikers(list);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load interested hikers.");
    } finally {
      setLoading(false);
    }
  }

  async function handleCreate() {
    setCreating(true);
    setError(null);
    try {
      const trip = await createTrip(peakId, notes.trim() || undefined, proposedDate || undefined);
      navigate(`/trips/${trip.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not create trip.");
    } finally {
      setCreating(false);
    }
  }

  if (!open) {
    return (
      <button onClick={handleOpen} className="btn btn-ghost">
        Create a trip
      </button>
    );
  }

  return (
    <div className="card" style={{ padding: 22, maxWidth: 420 }}>
      <div style={{ fontWeight: 700, marginBottom: 12 }}>Create a trip to {peakName}</div>

      {loading && <p style={{ color: "var(--color-text-faint)", fontSize: 13 }}>Finding hikers interested in this peak...</p>}

      {!loading && hikers && (
        <>
          {hikers.length > 0 ? (
            <div style={{ marginBottom: 16 }}>
              <div className="mono" style={{ fontSize: 11, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)", marginBottom: 8 }}>
                {hikers.length} hiker{hikers.length === 1 ? "" : "s"} interested
              </div>
              <ul style={{ margin: 0, padding: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 6 }}>
                {hikers.map((h) => (
                  <li key={h.userId} style={{ display: "flex", justifyContent: "space-between", fontSize: 14 }}>
                    <span>{hikerName(h)}</span>
                    <span style={{ color: "var(--color-text-faint)", fontSize: 13 }}>
                      {h.distanceKm !== null ? `${Math.round(h.distanceKm)} km away` : "distance unknown"}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ) : (
            <p style={{ color: "var(--color-text-faint)", fontSize: 13, marginBottom: 16 }}>
              No one else has this peak on their bucket list yet. You can still create the trip.
            </p>
          )}

          <label
            className="mono"
            style={{ display: "block", fontSize: 11, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)", marginBottom: 6 }}
          >
            Proposed date (optional)
          </label>
          <input
            type="date"
            className="input"
            value={proposedDate}
            onChange={(e) => setProposedDate(e.target.value)}
            style={{ width: "100%", marginBottom: 12 }}
          />

          <textarea
            className="input"
            placeholder="Notes (optional)"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
            style={{ width: "100%", marginBottom: 12, resize: "vertical" }}
          />

          {error && <p style={{ color: "var(--color-danger)", fontSize: 13, marginBottom: 12 }}>{error}</p>}

          <div style={{ display: "flex", gap: 10 }}>
            <button onClick={handleCreate} disabled={creating} className="btn btn-primary">
              {creating ? "Creating..." : "Create trip"}
            </button>
            <button onClick={() => setOpen(false)} disabled={creating} className="btn btn-ghost">
              Cancel
            </button>
          </div>
        </>
      )}

      {!loading && !hikers && error && (
        <>
          <p style={{ color: "var(--color-danger)", fontSize: 13, marginBottom: 12 }}>{error}</p>
          <button onClick={() => setOpen(false)} className="btn btn-ghost">
            Close
          </button>
        </>
      )}
    </div>
  );
}
