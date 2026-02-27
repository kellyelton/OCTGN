import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';

export default function LoginPage() {
  const navigate = useNavigate();
  const { login, loadSavedCredentials, isLoading, error, clearError, savedUsername, savedPassword } =
    useAuthStore();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const usernameRef = useRef<HTMLInputElement>(null);

  // Load saved credentials on mount
  useEffect(() => {
    loadSavedCredentials().then(() => {
      const { savedUsername, savedPassword } = useAuthStore.getState();
      if (savedUsername) {
        setUsername(savedUsername);
        setRememberMe(true);
      }
      if (savedPassword) {
        setPassword(savedPassword);
      }
    });
  }, [loadSavedCredentials]);

  // Focus the appropriate field after loading credentials
  useEffect(() => {
    if (savedUsername && !savedPassword) {
      // focus password field — handled by autofocus below
    } else if (!savedUsername) {
      usernameRef.current?.focus();
    }
  }, [savedUsername, savedPassword]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password) return;

    const success = await login(username.trim(), password, rememberMe);
    if (success) {
      navigate('/');
    }
  };

  const handleUsernameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setUsername(e.target.value);
    if (error) clearError();
  };

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setPassword(e.target.value);
    if (error) clearError();
  };

  return (
    <div
      className="h-full flex items-center justify-center bg-octgn-dark"
      data-testid="login-page"
    >
      <div className="w-full max-w-md px-4">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-gradient-to-br from-octgn-highlight to-octgn-blue shadow-glow mb-4">
            <span className="text-5xl">🃏</span>
          </div>
          <h1 className="text-4xl font-bold text-gradient mb-2">Sign In</h1>
          <p className="text-gray-400">Sign in to access online play and your account</p>
        </div>

        {/* Sign In Form */}
        <form
          onSubmit={handleSubmit}
          className="panel space-y-5"
          data-testid="login-form"
        >
          {/* Error message */}
          {error && (
            <div
              className="flex items-start space-x-3 p-3 rounded-lg bg-red-500/10 border border-red-500/30"
              data-testid="login-error"
              role="alert"
            >
              <span className="text-red-400 text-lg flex-shrink-0">⚠</span>
              <p className="text-red-300 text-sm">{error}</p>
            </div>
          )}

          {/* Username / Email */}
          <div>
            <label
              htmlFor="login-username"
              className="block text-sm font-medium text-gray-300 mb-1.5"
            >
              E-mail or Username
            </label>
            <input
              id="login-username"
              ref={usernameRef}
              type="text"
              value={username}
              onChange={handleUsernameChange}
              className="input w-full"
              placeholder="Enter your e-mail or username"
              autoComplete="username"
              disabled={isLoading}
              data-testid="login-username"
              required
            />
          </div>

          {/* Password */}
          <div>
            <label
              htmlFor="login-password"
              className="block text-sm font-medium text-gray-300 mb-1.5"
            >
              Password
            </label>
            <div className="relative">
              <input
                id="login-password"
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={handlePasswordChange}
                className="input w-full pr-10"
                placeholder="Enter your password"
                autoComplete="current-password"
                autoFocus={!!savedUsername && !savedPassword}
                disabled={isLoading}
                data-testid="login-password"
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="absolute inset-y-0 right-0 px-3 flex items-center text-gray-400 hover:text-gray-200 transition-colors"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                tabIndex={-1}
                data-testid="toggle-password-visibility"
              >
                {showPassword ? '🙈' : '👁'}
              </button>
            </div>
          </div>

          {/* Remember Me */}
          <div className="flex items-center justify-between">
            <label className="flex items-center space-x-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="w-4 h-4 rounded"
                disabled={isLoading}
                data-testid="login-remember-me"
              />
              <span className="text-sm text-gray-300">Remember me</span>
            </label>
            <a
              href="https://www.octgn.net/Account/ForgotPassword"
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-octgn-highlight hover:underline"
              data-testid="forgot-password-link"
            >
              Forgot password?
            </a>
          </div>

          {/* Submit */}
          <button
            type="submit"
            className="btn btn-primary w-full py-3 text-base font-semibold"
            disabled={isLoading || !username.trim() || !password}
            data-testid="login-submit"
          >
            {isLoading ? (
              <span className="flex items-center justify-center space-x-2">
                <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                <span>Signing in…</span>
              </span>
            ) : (
              'Sign In'
            )}
          </button>

          {/* Register link */}
          <p className="text-center text-sm text-gray-400">
            Don't have an account?{' '}
            <a
              href="https://www.octgn.net/Account/Register"
              target="_blank"
              rel="noopener noreferrer"
              className="text-octgn-highlight hover:underline"
              data-testid="register-link"
            >
              Register for free
            </a>
          </p>
        </form>

        {/* Skip / offline play */}
        <div className="mt-4 text-center">
          <button
            onClick={() => navigate('/')}
            className="text-sm text-gray-500 hover:text-gray-300 transition-colors"
            data-testid="skip-login"
          >
            Continue without signing in (offline only)
          </button>
        </div>
      </div>
    </div>
  );
}
