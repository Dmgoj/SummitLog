import { useEffect, useState } from "react";
import { searchPeaks, getCountries, type SortBy, type SortDir } from "../api/peaksApi";
import { getVisitedPeaks } from "../api/visitedPeaksApi";
import { getBucketList } from "../api/bucketListApi";
import { PeakCard } from "../components/PeakCard";
import { useAuth } from "../auth/useAuth";
import type { CountryOption, PeakSummary } from "../types";

const ELEVATION_BUCKETS = [
  { label: "Any elevation", min: undefined, max: undefined },
  { label: "< 1000m", min: undefined, max: 1000 },
  { label: "1000 - 2000m", min: 1000, max: 2000 },
  { label: "2000 - 3000m", min: 2000, max: 3000 },
  { label: "3000 - 4000m", min: 3000, max: 4000 },
  { label: "4000 - 5000m", min: 4000, max: 5000 },
  { label: "5000 - 6000m", min: 5000, max: 6000 },
  { label: "6000m+", min: 6000, max: undefined },
] as const;

export function SearchPage() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<PeakSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [visitedIds, setVisitedIds] = useState<Set<number>>(new Set());
  const [bucketListIds, setBucketListIds] = useState<Set<number>>(new Set());
  const [countries, setCountries] = useState<CountryOption[]>([]);
  const [countryFilter, setCountryFilter] = useState("");
  const [elevationBucketIndex, setElevationBucketIndex] = useState(0);
  const [sortBy, setSortBy] = useState<SortBy>("name");
  const [sortDir, setSortDir] = useState<SortDir>("asc");
  const { isAuthenticated } = useAuth();
  const pageSize = 20;

  function toggleSort(column: SortBy) {
    if (sortBy === column) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(column);
      setSortDir("asc");
    }
    setPage(1);
  }

  function sortArrow(column: SortBy) {
    if (sortBy !== column) {
      return "";
    }
    return sortDir === "asc" ? " ▲" : " ▼";
  }

  useEffect(() => {
    getCountries().then(setCountries);
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      getVisitedPeaks().then((visited) => setVisitedIds(new Set(visited.map((v) => v.peakId))));
      getBucketList().then((bucketList) => setBucketListIds(new Set(bucketList.map((b) => b.peakId))));
    } else {
      setVisitedIds(new Set());
      setBucketListIds(new Set());
    }
  }, [isAuthenticated]);

  const canSearch = query.trim().length === 0 || query.trim().length >= 2;

  useEffect(() => {
    if (!canSearch) {
      setResults([]);
      setTotalCount(0);
      return;
    }

    const bucket = ELEVATION_BUCKETS[elevationBucketIndex];

    const timeout = setTimeout(() => {
      setLoading(true);
      searchPeaks(query.trim(), page, pageSize, {
        country: countryFilter || undefined,
        minElevation: bucket.min,
        maxElevation: bucket.max,
        sortBy,
        sortDir,
      })
        .then((res) => {
          setResults(res.items);
          setTotalCount(res.totalCount);
        })
        .finally(() => setLoading(false));
    }, 300);

    return () => clearTimeout(timeout);
  }, [query, page, countryFilter, elevationBucketIndex, canSearch, sortBy, sortDir]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: "56px 40px 64px" }}>
      <div style={{ fontFamily: "var(--font-mono)", fontSize: 12, letterSpacing: 3, textTransform: "uppercase", color: "var(--color-accent)", marginBottom: 14 }}>
        Search the catalogue
      </div>
      <h1
        style={{
          margin: "0 0 32px",
          fontFamily: "var(--font-display)",
          fontSize: 48,
          lineHeight: 1,
          letterSpacing: 0.5,
          textTransform: "uppercase",
        }}
      >
        Find your next summit
      </h1>

      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 16,
          background: "var(--color-surface-alt)",
          border: "2px solid var(--color-accent)",
          borderRadius: 10,
          padding: "4px 6px 4px 20px",
        }}
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" strokeWidth="2.4">
          <circle cx="11" cy="11" r="7" />
          <path d="M21 21l-4.3-4.3" />
        </svg>
        <input
          type="text"
          placeholder="Search by name (e.g. Everest), or leave blank and use the filters below"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setPage(1);
          }}
          style={{
            flexGrow: 1,
            background: "transparent",
            border: "none",
            outline: "none",
            fontFamily: "var(--font-body)",
            fontSize: 16,
            fontWeight: 600,
            color: "var(--color-text)",
            padding: "14px 0",
          }}
        />
      </div>

      <div style={{ display: "flex", alignItems: "center", gap: 14, marginTop: 18 }}>
        <span style={{ fontFamily: "var(--font-mono)", fontSize: 11, letterSpacing: 2, textTransform: "uppercase", color: "var(--color-text-faint)" }}>
          Filter
        </span>
        <select
          className="select"
          value={countryFilter}
          onChange={(e) => {
            setCountryFilter(e.target.value);
            setPage(1);
          }}
          style={{ borderRadius: 999 }}
        >
          <option value="">All countries</option>
          {countries.map((c) => (
            <option key={c.code} value={c.code}>
              {c.name}
            </option>
          ))}
        </select>
        <select
          className="select"
          value={elevationBucketIndex}
          onChange={(e) => {
            setElevationBucketIndex(Number(e.target.value));
            setPage(1);
          }}
          style={{
            borderRadius: 999,
            borderColor: elevationBucketIndex !== 0 ? "var(--color-accent)" : undefined,
            color: elevationBucketIndex !== 0 ? "var(--color-accent-text)" : undefined,
            background: elevationBucketIndex !== 0 ? "var(--color-accent-soft)" : undefined,
          }}
        >
          {ELEVATION_BUCKETS.map((bucket, index) => (
            <option key={bucket.label} value={index}>
              {bucket.label}
            </option>
          ))}
        </select>
      </div>

      {loading && <p style={{ color: "var(--color-text-muted)" }}>Searching...</p>}
      {!loading && canSearch && results.length === 0 && <p style={{ color: "var(--color-text-muted)" }}>No peaks found.</p>}

      {results.length > 0 && (
        <div style={{ display: "flex", alignItems: "center", gap: 20, marginTop: 32, fontSize: 13, padding: "0 12px" }}>
          <div style={{ width: 24 }} />
          <div style={{ width: 44 }} />
          <div style={{ flexGrow: 1, minWidth: 0 }}>
            <button
              onClick={() => toggleSort("name")}
              className="link"
              style={{
                background: "none",
                border: "none",
                padding: 0,
                cursor: "pointer",
                fontFamily: "inherit",
                fontWeight: 700,
                color: sortBy === "name" ? "var(--color-accent)" : "var(--color-text-faint)",
              }}
            >
              Sort by name{sortArrow("name")}
            </button>
          </div>
          <div style={{ textAlign: "right", minWidth: 90 }}>
            <button
              onClick={() => toggleSort("elevation")}
              className="link"
              style={{
                background: "none",
                border: "none",
                padding: 0,
                cursor: "pointer",
                fontFamily: "inherit",
                fontWeight: 700,
                color: sortBy === "elevation" ? "var(--color-accent)" : "var(--color-text-faint)",
              }}
            >
              Elevation{sortArrow("elevation")}
            </button>
          </div>
        </div>
      )}

      <div style={{ marginTop: 8 }}>
        {results.map((peak, index) => (
          <PeakCard
            key={peak.id}
            index={index}
            peak={peak}
            visited={visitedIds.has(peak.id)}
            onBucketList={bucketListIds.has(peak.id)}
          />
        ))}
      </div>

      {totalCount > pageSize && (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", gap: 18, marginTop: 40 }}>
          <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="btn btn-ghost mono">
            ← Prev
          </button>
          <span className="mono" style={{ fontSize: 13, color: "var(--color-text-muted)" }}>
            Page {page} of {totalPages}
          </span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className="btn btn-ghost mono">
            Next →
          </button>
        </div>
      )}
    </div>
  );
}
