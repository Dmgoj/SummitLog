import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getPeak } from "../api/peaksApi";
import { getVisitedPeaks } from "../api/visitedPeaksApi";
import { getBucketList } from "../api/bucketListApi";
import { PeakMap } from "../components/PeakMap";
import { VisitedToggleButton } from "../components/VisitedToggleButton";
import { BucketListToggleButton } from "../components/BucketListToggleButton";
import { CreateTripPanel } from "../components/CreateTripPanel";
import { useAuth } from "../auth/useAuth";
import type { PeakDetail } from "../types";

function StatCard({ label, children, accent }: { label: string; children: React.ReactNode; accent?: boolean }) {
  return (
    <div
      className="card"
      style={{
        padding: 22,
        ...(accent
          ? { background: "var(--color-accent-soft)", borderColor: "var(--color-accent)" }
          : {}),
      }}
    >
      <div
        className="mono"
        style={{
          fontSize: 11,
          letterSpacing: 2,
          textTransform: "uppercase",
          color: accent ? "var(--color-accent-text)" : "var(--color-text-faint)",
          marginBottom: 10,
        }}
      >
        {label}
      </div>
      {children}
    </div>
  );
}

export function PeakDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [peak, setPeak] = useState<PeakDetail | null>(null);
  const [visited, setVisited] = useState(false);
  const [onBucketList, setOnBucketList] = useState(false);
  const [loading, setLoading] = useState(true);
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    if (!id) return;
    setLoading(true);

    const peakPromise = getPeak(Number(id));
    const visitedPromise = isAuthenticated
      ? getVisitedPeaks().then((list) => list.some((v) => v.peakId === Number(id)))
      : Promise.resolve(false);
    const bucketListPromise = isAuthenticated
      ? getBucketList().then((list) => list.some((b) => b.peakId === Number(id)))
      : Promise.resolve(false);

    Promise.all([peakPromise, visitedPromise, bucketListPromise])
      .then(([peakResult, visitedResult, bucketListResult]) => {
        setPeak(peakResult);
        setVisited(visitedResult);
        setOnBucketList(bucketListResult);
      })
      .finally(() => setLoading(false));
  }, [id, isAuthenticated]);

  if (loading) {
    return (
      <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Loading...</p>
    );
  }

  if (!peak) {
    return (
      <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Peak not found.</p>
    );
  }

  const elevationLooksUnderstated =
    !peak.elevationIsOverridden &&
    peak.featureCode === "MTS" &&
    peak.elevationMeters !== null &&
    peak.elevationMeters < 200;

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: "40px 40px 64px" }}>
      <div
        className="mono link"
        style={{ fontSize: 12, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)", display: "flex", gap: 10, marginBottom: 20 }}
      >
        <Link to="/" className="link" style={{ color: "var(--color-text-muted)" }}>
          Search
        </Link>
        <span>/</span>
        <span style={{ color: "var(--color-accent)" }}>{peak.name}</span>
      </div>

      <div style={{ display: "flex", alignItems: "flex-end", justifyContent: "space-between", paddingBottom: 28, borderBottom: "1px solid var(--color-border)" }}>
        <div>
          <div className="mono" style={{ fontSize: 12, letterSpacing: 2, textTransform: "uppercase", color: "var(--color-accent)", marginBottom: 10 }}>
            {peak.countryName}
          </div>
          <h1 style={{ margin: 0, fontFamily: "var(--font-display)", fontSize: 48, letterSpacing: 0.3, textTransform: "uppercase" }}>
            {peak.name}
          </h1>
          {peak.alternateNames && (
            <div style={{ fontSize: 13, color: "var(--color-text-faint)", marginTop: 12, maxWidth: 640 }}>
              Also known as {peak.alternateNames}
            </div>
          )}
        </div>
        <div style={{ display: "flex", gap: 12 }}>
          <BucketListToggleButton
            key={`bucket-${peak.id}-${onBucketList}`}
            peakId={peak.id}
            initialOnList={onBucketList}
            onChange={setOnBucketList}
          />
          <VisitedToggleButton
            key={`visited-${peak.id}`}
            peakId={peak.id}
            initialVisited={visited}
            onChange={(isVisited) => {
              setVisited(isVisited);
              if (isVisited) {
                setOnBucketList(false);
              }
            }}
          />
        </div>
      </div>

      <div style={{ display: "flex", gap: 32, marginTop: 32 }}>
        <div style={{ width: 260, flexShrink: 0, display: "flex", flexDirection: "column", gap: 14 }}>
          <StatCard label="Elevation">
            <div style={{ fontFamily: "var(--font-display)", fontSize: 34, color: "var(--color-accent-text)", lineHeight: 1 }}>
              {peak.elevationMeters ? peak.elevationMeters.toLocaleString() : "—"}
              <span style={{ fontSize: 16, color: "var(--color-text-faint)", fontFamily: "var(--font-body)" }}> m</span>
            </div>
            {peak.elevationIsOverridden && (
              <div style={{ fontSize: 11, color: "var(--color-accent-text)", marginTop: 8, lineHeight: 1.4 }}>
                Corrected — GeoNames' own figure for this feature was implausible.
                {peak.elevationSource && ` Source: ${peak.elevationSource}.`}
              </div>
            )}
            {elevationLooksUnderstated && (
              <div style={{ fontSize: 11, color: "var(--color-text-faint)", marginTop: 8, lineHeight: 1.4 }}>
                Approximate — GeoNames marks large ranges with a single point, which can sit on low
                ground far below the range's true high point.
              </div>
            )}
          </StatCard>

          <StatCard label="Coordinates">
            <div className="mono" style={{ fontSize: 15, fontWeight: 700 }}>
              <span style={{ color: "var(--color-text-faint)", fontWeight: 600 }}>Lat </span>
              {peak.latitude.toFixed(4)}°
            </div>
            <div className="mono" style={{ fontSize: 15, fontWeight: 700 }}>
              <span style={{ color: "var(--color-text-faint)", fontWeight: 600 }}>Lon </span>
              {peak.longitude.toFixed(4)}°
            </div>
          </StatCard>

          <StatCard label="Country">
            <div style={{ fontSize: 15, fontWeight: 700 }}>{peak.countryName}</div>
          </StatCard>

          {visited && (
            <StatCard label="Your status" accent>
              <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 14, fontWeight: 700, color: "var(--color-accent-text)" }}>
                <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2.2">
                  <path d="M4 10l4 4 8-8" />
                </svg>
                Visited
              </div>
            </StatCard>
          )}
        </div>

        <div style={{ flexGrow: 1 }}>
          <PeakMap name={peak.name} latitude={peak.latitude} longitude={peak.longitude} />

          {onBucketList && (
            <div style={{ marginTop: 24 }}>
              <CreateTripPanel peakId={peak.id} peakName={peak.name} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
