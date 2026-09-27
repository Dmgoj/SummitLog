import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { getToken, setToken as persistToken } from "../api/client";
import { getProfile } from "../api/profileApi";
import type { Profile } from "../types";

interface AuthState {
  token: string | null;
  email: string | null;
  profile: Profile | null;
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean;
  displayName: string | null;
  login: (token: string, email: string) => void;
  logout: () => void;
  setProfile: (profile: Profile) => void;
}

const EMAIL_STORAGE_KEY = "summitlog.email";

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => ({
    token: getToken(),
    email: localStorage.getItem(EMAIL_STORAGE_KEY),
    profile: null,
  }));

  useEffect(() => {
    if (state.token && !state.profile) {
      getProfile()
        .then((profile) => setState((s) => ({ ...s, profile })))
        .catch(() => {});
    }
  }, [state.token, state.profile]);

  const login = useCallback((token: string, email: string) => {
    persistToken(token);
    localStorage.setItem(EMAIL_STORAGE_KEY, email);
    setState({ token, email, profile: null });
  }, []);

  const logout = useCallback(() => {
    persistToken(null);
    localStorage.removeItem(EMAIL_STORAGE_KEY);
    setState({ token: null, email: null, profile: null });
  }, []);

  const setProfile = useCallback((profile: Profile) => {
    setState((s) => ({ ...s, profile }));
  }, []);

  const displayName = useMemo(() => {
    const profile = state.profile;
    if (profile?.firstName || profile?.lastName) {
      return [profile.firstName, profile.lastName].filter(Boolean).join(" ");
    }
    return state.email;
  }, [state.profile, state.email]);

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, isAuthenticated: state.token !== null, displayName, login, logout, setProfile }),
    [state, displayName, login, logout, setProfile]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
