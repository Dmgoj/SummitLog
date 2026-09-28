import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { login as loginRequest, resendConfirmation } from "../api/authApi";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/useAuth";

export function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [suspended, setSuspended] = useState(false);
  const [resendMessage, setResendMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuspended(false);
    setResendMessage(null);
    setSubmitting(true);
    try {
      const result = await loginRequest(email, password);
      login(result.token, result.email);
      navigate("/");
    } catch (err) {
      if (err instanceof ApiError && err.status === 401 && err.message.toLowerCase().includes("suspended")) {
        setSuspended(true);
        setError(err.message);
      } else {
        setError(err instanceof ApiError ? "Invalid email or password." : "Something went wrong.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  async function handleResend() {
    setResendMessage(null);
    try {
      const result = await resendConfirmation(email);
      setResendMessage(result);
    } catch (err) {
      setResendMessage(err instanceof ApiError ? err.message : "Something went wrong.");
    }
  }

  return (
    <div style={{ maxWidth: 380, margin: "80px auto", padding: "0 24px" }}>
      <h1 style={{ fontFamily: "var(--font-display)", fontSize: 32, textTransform: "uppercase", letterSpacing: 0.3, marginBottom: 28 }}>
        Log in
      </h1>
      <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }}>
        <input
          className="input"
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <input
          className="input"
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error && <p style={{ color: "var(--color-danger)", margin: 0, fontSize: 14 }}>{error}</p>}
        <button type="submit" disabled={submitting} className="btn btn-primary" style={{ padding: "14px 0", fontSize: 14 }}>
          {submitting ? "Logging in..." : "Log in"}
        </button>
      </form>

      {suspended && (
        <div style={{ marginTop: 16 }}>
          <button onClick={handleResend} className="btn btn-ghost" style={{ fontSize: 13 }}>
            Resend confirmation email
          </button>
          {resendMessage && <p style={{ fontSize: 13, color: "var(--color-text-muted)", marginTop: 8 }}>{resendMessage}</p>}
        </div>
      )}

      <p style={{ marginTop: 20 }}>
        <Link to="/forgot-password" className="link" style={{ fontSize: 13 }}>
          Forgot your password?
        </Link>
      </p>
    </div>
  );
}
