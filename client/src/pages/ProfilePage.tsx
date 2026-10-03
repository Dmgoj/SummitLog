import { useEffect, useState, type FormEvent } from "react";
import { getProfile, updateProfile, uploadProfilePicture, removeProfilePicture, toAbsolutePictureUrl } from "../api/profileApi";
import { useAuth } from "../auth/useAuth";
import { ApiError } from "../api/client";

export function ProfilePage() {
  const { setProfile } = useAuth();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [pictureUrl, setPictureUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    getProfile()
      .then((profile) => {
        setFirstName(profile.firstName ?? "");
        setLastName(profile.lastName ?? "");
        setPictureUrl(profile.profilePictureUrl);
      })
      .finally(() => setLoading(false));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    setSaving(true);
    try {
      const profile = await updateProfile(firstName.trim(), lastName.trim());
      setProfile(profile);
      setSaved(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not save profile.");
    } finally {
      setSaving(false);
    }
  }

  async function handlePictureChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setError(null);
    setUploading(true);
    try {
      const profile = await uploadProfilePicture(file);
      setPictureUrl(profile.profilePictureUrl);
      setProfile(profile);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not upload picture.");
    } finally {
      setUploading(false);
      e.target.value = "";
    }
  }

  async function handleRemovePicture() {
    setError(null);
    setRemoving(true);
    try {
      const profile = await removeProfilePicture();
      setPictureUrl(profile.profilePictureUrl);
      setProfile(profile);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not remove picture.");
    } finally {
      setRemoving(false);
    }
  }

  if (loading) {
    return <p style={{ padding: "56px 40px", color: "var(--color-text-muted)" }}>Loading...</p>;
  }

  return (
    <div style={{ maxWidth: 460, margin: "0 auto", padding: "56px 24px 64px" }}>
      <div className="mono" style={{ fontSize: 12, letterSpacing: 3, textTransform: "uppercase", color: "var(--color-accent)", marginBottom: 14 }}>
        Your account
      </div>
      <h1 style={{ margin: "0 0 32px", fontFamily: "var(--font-display)", fontSize: 40, letterSpacing: 0.3, textTransform: "uppercase" }}>
        My Profile
      </h1>

      <div style={{ display: "flex", alignItems: "center", gap: 20, marginBottom: 32 }}>
        {pictureUrl ? (
          <img
            src={toAbsolutePictureUrl(pictureUrl) ?? undefined}
            alt="Profile"
            style={{ width: 84, height: 84, borderRadius: "50%", objectFit: "cover", border: "1px solid var(--color-border)" }}
          />
        ) : (
          <div
            style={{
              width: 84,
              height: 84,
              borderRadius: "50%",
              background: "var(--color-surface)",
              border: "1px solid var(--color-border)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "var(--color-text-faint)",
              fontSize: 12,
            }}
          >
            No photo
          </div>
        )}
        <div>
          <input
            type="file"
            accept="image/png,image/jpeg,image/gif,image/webp"
            onChange={handlePictureChange}
            disabled={uploading || removing}
            style={{ color: "var(--color-text-muted)", fontSize: 13 }}
          />
          {uploading && <p style={{ color: "var(--color-text-faint)", fontSize: 13 }}>Uploading...</p>}
          {pictureUrl && !uploading && (
            <button
              type="button"
              onClick={handleRemovePicture}
              disabled={removing}
              className="btn"
              style={{ marginTop: 8, padding: "4px 10px", fontSize: 12 }}
            >
              {removing ? "Removing..." : "Remove photo"}
            </button>
          )}
        </div>
      </div>

      <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        <label style={{ display: "flex", flexDirection: "column", gap: 6, fontSize: 12, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)" }} className="mono">
          First name
          <input
            className="input"
            type="text"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
        </label>
        <label style={{ display: "flex", flexDirection: "column", gap: 6, fontSize: 12, letterSpacing: 1, textTransform: "uppercase", color: "var(--color-text-faint)" }} className="mono">
          Last name
          <input
            className="input"
            type="text"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
        </label>
        {error && <p style={{ color: "var(--color-danger)", margin: 0, fontSize: 14 }}>{error}</p>}
        {saved && <p style={{ color: "var(--color-accent-text)", margin: 0, fontSize: 14 }}>Saved.</p>}
        <button type="submit" disabled={saving} className="btn btn-primary" style={{ padding: "14px 0", fontSize: 14 }}>
          {saving ? "Saving..." : "Save"}
        </button>
      </form>
    </div>
  );
}
