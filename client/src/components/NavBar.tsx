import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { useTheme } from "../theme/useTheme";
import { toAbsolutePictureUrl } from "../api/profileApi";
import { NotificationBell } from "./NotificationBell";

function initials(name: string | null): string {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join("");
}

export function NavBar() {
  const { isAuthenticated, displayName, profile, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const location = useLocation();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  function isActive(path: string) {
    return location.pathname === path;
  }

  return (
    <nav
      style={{
        display: "flex",
        alignItems: "center",
        gap: 40,
        padding: "0 56px",
        height: 88,
        borderBottom: "1px solid var(--color-border)",
        background: "var(--color-bg)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
        <svg width="30" height="30" viewBox="0 0 34 34" fill="none">
          <path d="M2 26L11 9L16 17L20 11L32 26H2Z" fill="var(--color-accent)" />
          <path d="M20 11L23 15.5L18.5 16.5L20 11Z" fill="var(--color-accent-text)" />
        </svg>
        <span style={{ fontFamily: "var(--font-display)", fontSize: 19, letterSpacing: 0.5, textTransform: "uppercase" }}>
          Summit<span style={{ color: "var(--color-accent)" }}>Log</span>
        </span>
      </div>

      <div style={{ display: "flex", alignItems: "center", gap: 32, fontSize: 14, fontWeight: 600, textTransform: "uppercase", letterSpacing: 0.3 }}>
        <Link
          to="/"
          className="link"
          style={{
            color: isActive("/") ? "var(--color-accent)" : "var(--color-text-muted)",
            borderBottom: isActive("/") ? "2px solid var(--color-accent)" : "2px solid transparent",
            paddingBottom: 4,
          }}
        >
          Search
        </Link>
        {isAuthenticated && (
          <Link
            to="/visited"
            className="link"
            style={{
              color: isActive("/visited") ? "var(--color-accent)" : "var(--color-text-muted)",
              borderBottom: isActive("/visited") ? "2px solid var(--color-accent)" : "2px solid transparent",
              paddingBottom: 4,
            }}
          >
            Visited Peaks
          </Link>
        )}
        {isAuthenticated && (
          <Link
            to="/bucket-list"
            className="link"
            style={{
              color: isActive("/bucket-list") ? "var(--color-accent)" : "var(--color-text-muted)",
              borderBottom: isActive("/bucket-list") ? "2px solid var(--color-accent)" : "2px solid transparent",
              paddingBottom: 4,
            }}
          >
            Bucket List
          </Link>
        )}
      </div>

      <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 16 }}>
        <button
          onClick={toggleTheme}
          aria-label={theme === "dark" ? "Switch to light mode" : "Switch to dark mode"}
          title={theme === "dark" ? "Switch to light mode" : "Switch to dark mode"}
          style={{
            width: 36,
            height: 36,
            borderRadius: "50%",
            border: "1px solid var(--color-border-strong)",
            background: "transparent",
            color: "var(--color-text-muted)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            cursor: "pointer",
          }}
        >
          {theme === "dark" ? (
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="5" />
              <path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" />
            </svg>
          ) : (
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M21 12.8A9 9 0 1 1 11.2 3 7 7 0 0 0 21 12.8Z" />
            </svg>
          )}
        </button>

        {isAuthenticated ? (
          <>
            <NotificationBell />
            <Link
              to="/profile"
              className="link"
              style={{ display: "flex", alignItems: "center", gap: 10, padding: "6px 14px 6px 6px", border: "1px solid var(--color-border)", borderRadius: 999 }}
            >
              {profile?.profilePictureUrl ? (
                <img
                  src={toAbsolutePictureUrl(profile.profilePictureUrl) ?? undefined}
                  alt=""
                  style={{ width: 28, height: 28, borderRadius: "50%", objectFit: "cover" }}
                />
              ) : (
                <span
                  style={{
                    width: 28,
                    height: 28,
                    borderRadius: "50%",
                    background: "var(--color-accent)",
                    color: "var(--color-accent-contrast)",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: 11,
                    fontWeight: 800,
                  }}
                >
                  {initials(displayName)}
                </span>
              )}
              <span style={{ fontSize: 13, fontWeight: 700 }}>{displayName}</span>
            </Link>
            <button onClick={handleLogout} className="btn btn-ghost">
              Log out
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="link" style={{ fontSize: 14, fontWeight: 600, color: "var(--color-text-muted)" }}>
              Log in
            </Link>
            <Link to="/register" className="btn btn-primary">
              Register
            </Link>
          </>
        )}
      </div>
    </nav>
  );
}
