import { create } from 'zustand';
import {
  createSession,
  validateSession,
  loginResultTypeToMessage,
  ApiError,
  CreateSessionResult,
} from '../services/apiClient';

// Device ID - generated once per install and persisted in localStorage
function getDeviceId(): string {
  let id = localStorage.getItem('octgn_device_id');
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem('octgn_device_id', id);
  }
  return id;
}

export interface AuthState {
  // User info
  isLoggedIn: boolean;
  username: string | null;
  userId: string | null;
  sessionKey: string | null;

  // UI state
  isLoading: boolean;
  error: string | null;

  // Credential cache (in-memory only, not persisted here)
  savedUsername: string | null;
  savedPassword: string | null;

  // Actions
  login: (username: string, password: string, rememberMe: boolean) => Promise<boolean>;
  logout: () => Promise<void>;
  loadSavedCredentials: () => Promise<void>;
  validateStoredSession: () => Promise<boolean>;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  isLoggedIn: false,
  username: null,
  userId: null,
  sessionKey: null,
  isLoading: false,
  error: null,
  savedUsername: null,
  savedPassword: null,

  login: async (username: string, password: string, rememberMe: boolean): Promise<boolean> => {
    set({ isLoading: true, error: null });
    try {
      const deviceId = getDeviceId();
      const result: CreateSessionResult = await createSession(username, password, deviceId);

      if (result.Result.Type !== 'Ok') {
        set({
          isLoading: false,
          error: loginResultTypeToMessage(result.Result.Type),
        });
        return false;
      }

      // Store session info in localStorage (non-sensitive)
      localStorage.setItem('octgn_session_key', result.SessionKey);
      localStorage.setItem('octgn_user_id', result.UserId);
      localStorage.setItem('octgn_username', result.Result.Username);

      // Save encrypted credentials if "remember me" is checked (via Electron safeStorage)
      if (rememberMe && window.electronAPI?.saveCredentials) {
        await window.electronAPI.saveCredentials(result.Result.Username, password);
      } else if (!rememberMe && window.electronAPI?.clearCredentials) {
        await window.electronAPI.clearCredentials();
      }

      set({
        isLoggedIn: true,
        username: result.Result.Username,
        userId: result.UserId,
        sessionKey: result.SessionKey,
        isLoading: false,
        error: null,
        savedUsername: rememberMe ? result.Result.Username : null,
        savedPassword: rememberMe ? password : null,
      });

      return true;
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Unable to connect to the OCTGN servers. Please check your internet connection.';
      set({ isLoading: false, error: message });
      return false;
    }
  },

  logout: async () => {
    localStorage.removeItem('octgn_session_key');
    localStorage.removeItem('octgn_user_id');
    localStorage.removeItem('octgn_username');
    // Keep saved credentials so user doesn't have to re-enter them
    set({
      isLoggedIn: false,
      username: null,
      userId: null,
      sessionKey: null,
      error: null,
    });
  },

  loadSavedCredentials: async () => {
    // Load saved username from localStorage (always available)
    const savedUsername = localStorage.getItem('octgn_username');

    // Load encrypted password from OS keychain (Electron only)
    let savedPassword: string | null = null;
    if (window.electronAPI?.loadCredentials) {
      const result = await window.electronAPI.loadCredentials();
      if (result?.success && result.password) {
        savedPassword = result.password;
      }
    }

    set({ savedUsername, savedPassword });
  },

  validateStoredSession: async (): Promise<boolean> => {
    const userId = localStorage.getItem('octgn_user_id');
    const sessionKey = localStorage.getItem('octgn_session_key');
    const username = localStorage.getItem('octgn_username');

    if (!userId || !sessionKey) return false;

    try {
      const deviceId = getDeviceId();
      const valid = await validateSession(userId, deviceId, sessionKey);
      if (valid) {
        set({ isLoggedIn: true, userId, sessionKey, username });
      }
      return valid;
    } catch {
      return false;
    }
  },

  clearError: () => set({ error: null }),
}));
