import { useState } from "react";
import { Link } from "react-router-dom";
import type { PeakSummary } from "../types";
import { VisitedToggleButton } from "./VisitedToggleButton";
import { BucketListToggleButton } from "./BucketListToggleButton";

interface Props {
  peak: PeakSummary;
  visited: boolean;
  onBucketList: boolean;
  index?: number;
}

export function PeakCard({ peak, visited, onBucketList, index }: Props) {
  const [onList, setOnList] = useState(onBucketList);

  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 20,
        padding: "18px 12px",
        borderTop: "1px solid var(--color-border)",
      }}
    >
      {index !== undefined && (
        <div className="mono" style={{ fontSize: 12, color: "var(--color-text-dim)", width: 24 }}>
          {String(index + 1).padStart(2, "0")}
        </div>
      )}

      <div
        style={{
          width: 44,
          height: 44,
          borderRadius: 8,
          background: "var(--color-surface)",
          border: "1px solid var(--color-border)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flexShrink: 0,
        }}
      >
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="var(--color-text-muted)" strokeWidth="1.6">
          <path d="M3 20L9 8l4 6 3-4 5 10H3Z" />
        </svg>
      </div>

      <div style={{ flexGrow: 1, minWidth: 0 }}>
        <Link to={`/peaks/${peak.id}`} className="link" style={{ fontWeight: 700, fontSize: 17 }}>
          {peak.name}
        </Link>
        <div style={{ fontSize: 13, color: "var(--color-text-muted)", marginTop: 2 }}>{peak.countryCode}</div>
      </div>

      <div className="mono" style={{ textAlign: "right", width: 90, flexShrink: 0 }}>
        {peak.elevationMeters ? (
          <span style={{ fontSize: 18, fontWeight: 700, color: "var(--color-accent-text)" }}>
            {peak.elevationMeters.toLocaleString()}
            <span style={{ fontSize: 12, color: "var(--color-text-faint)" }}> m</span>
          </span>
        ) : (
          <span style={{ fontSize: 13, color: "var(--color-text-faint)" }}>—</span>
        )}
      </div>

      <div style={{ width: 140, flexShrink: 0, display: "flex", justifyContent: "flex-end" }}>
        <BucketListToggleButton
          key={`bucket-${peak.id}-${onList}`}
          peakId={peak.id}
          initialOnList={onList}
          onChange={setOnList}
        />
      </div>
      <div style={{ width: 130, flexShrink: 0, display: "flex", justifyContent: "flex-end" }}>
        <VisitedToggleButton
          key={`visited-${peak.id}`}
          peakId={peak.id}
          initialVisited={visited}
          onChange={(isVisited) => {
            if (isVisited) {
              setOnList(false);
            }
          }}
        />
      </div>
    </div>
  );
}
