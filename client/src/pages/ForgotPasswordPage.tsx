import { useState, type FormEvent } from "react";
import { forgotPassword } from "../api/authApi";
import { ApiError } from "../api/client";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const result = await forgotPassword(email);
      setMessage(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 380, margin: "80px auto", padding: "0 24px" }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: 32, textTransform: "uppercase", letterSpacing: 0.3, marginBottom: 28 }}>
        Forgot password
      </h1>
      {message ? (
        <p>{message}</p>
      ) : (
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <input
            className="input"
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          {error && <p style={{ color: "var(--color-danger)", margin: 0, fontSize: 14 }}>{error}</p>}
          <button type="submit" disabled={submitting} className="btn btn-primary" style={{ padding: "14px 0", fontSize: 14 }}>
            {submitting ? "Sending..." : "Send reset link"}
          </button>
        </form>
      )}
    </div>
  );
}
