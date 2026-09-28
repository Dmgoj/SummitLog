import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { resetPassword } from "../api/authApi";
import { ApiError } from "../api/client";

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const navigate = useNavigate();

  const userId = searchParams.get("userId");
  const token = searchParams.get("token");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!userId || !token) {
      setError("This reset link is missing required information.");
      return;
    }

    setSubmitting(true);
    try {
      await resetPassword(userId, token, password);
      setSuccess(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 380, margin: "80px auto", padding: "0 24px" }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: 32, textTransform: "uppercase", letterSpacing: 0.3, marginBottom: 28 }}>
        Reset password
      </h1>
      {success ? (
        <>
          <p>Your password has been reset.</p>
          <button onClick={() => navigate("/login")} className="btn btn-primary" style={{ padding: "14px 24px" }}>
            Log in
          </button>
        </>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <input
            className="input"
            type="password"
            placeholder="New password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
          />
          {error && <p style={{ color: "var(--color-danger)", margin: 0, fontSize: 14 }}>{error}</p>}
          <button type="submit" disabled={submitting} className="btn btn-primary" style={{ padding: "14px 0", fontSize: 14 }}>
            {submitting ? "Resetting..." : "Reset password"}
          </button>
        </form>
      )}
    </div>
  );
}
