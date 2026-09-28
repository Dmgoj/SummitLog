import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { confirmEmail } from "../api/authApi";
import { ApiError } from "../api/client";

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");

    if (!userId || !token) {
      setStatus("error");
      setError("This confirmation link is missing required information.");
      return;
    }

    confirmEmail(userId, token)
      .then(() => setStatus("success"))
      .catch((err) => {
        setStatus("error");
        setError(err instanceof ApiError ? err.message : "Something went wrong.");
      });
  }, [searchParams]);

  return (
    <div style={{ maxWidth: 380, margin: "80px auto", padding: "0 24px", textAlign: "center" }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: 32, textTransform: "uppercase", letterSpacing: 0.3, marginBottom: 20 }}>
        Confirm email
      </h1>
      {status === "loading" && <p style={{ color: "var(--color-text-muted)" }}>Confirming your email...</p>}
      {status === "success" && (
        <>
          <p>Your email has been confirmed.</p>
          <Link to="/login" className="btn btn-primary" style={{ display: "inline-block", marginTop: 12, padding: "14px 24px" }}>
            Log in
          </Link>
        </>
      )}
      {status === "error" && <p style={{ color: "var(--color-danger)" }}>{error}</p>}
    </div>
  );
}
