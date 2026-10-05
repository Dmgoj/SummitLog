import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotifications, markNotificationRead } from "../api/notificationsApi";
import type { AppNotification } from "../types";

const POLL_INTERVAL_MS = 60000;

export function NotificationBell() {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function load() {
      getNotifications()
        .then(setNotifications)
        .catch(() => {});
    }
    load();
    const interval = setInterval(load, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  async function handleSelect(notification: AppNotification) {
    setOpen(false);
    if (!notification.isRead) {
      setNotifications((prev) => prev.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n)));
      markNotificationRead(notification.id).catch(() => {});
    }
    if (notification.tripId) {
      navigate(`/trips/${notification.tripId}`);
    }
  }

  return (
    <div ref={containerRef} style={{ position: "relative" }}>
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label="Notifications"
        title="Notifications"
        style={{
          position: "relative",
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
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {unreadCount > 0 && (
          <span
            style={{
              position: "absolute",
              top: -2,
              right: -2,
              minWidth: 16,
              height: 16,
              borderRadius: 8,
              background: "var(--color-danger)",
              color: "#fff",
              fontSize: 10,
              fontWeight: 700,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              padding: "0 3px",
            }}
          >
            {unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          className="card"
          style={{
            position: "absolute",
            right: 0,
            top: 44,
            width: 320,
            maxHeight: 400,
            overflowY: "auto",
            padding: 8,
            zIndex: 20,
          }}
        >
          {notifications.length === 0 ? (
            <p style={{ color: "var(--color-text-faint)", fontSize: 13, padding: 12, margin: 0 }}>No notifications yet.</p>
          ) : (
            notifications.map((n) => (
              <button
                key={n.id}
                onClick={() => handleSelect(n)}
                style={{
                  display: "block",
                  width: "100%",
                  textAlign: "left",
                  padding: 12,
                  borderRadius: 8,
                  border: "none",
                  background: n.isRead ? "transparent" : "var(--color-accent-soft)",
                  cursor: "pointer",
                  fontSize: 13,
                  color: "var(--color-text)",
                }}
              >
                <div>{n.message}</div>
                <div style={{ color: "var(--color-text-faint)", fontSize: 11, marginTop: 4 }}>
                  {new Date(n.createdAt).toLocaleString()}
                </div>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  );
}
