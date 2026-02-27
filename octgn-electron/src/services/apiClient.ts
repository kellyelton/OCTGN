const API_BASE = 'https://www.octgn.net';

export type LoginResultType =
  | 'Ok'
  | 'UnknownError'
  | 'EmailUnverified'
  | 'UnknownUsername'
  | 'PasswordWrong'
  | 'NotSubscribed'
  | 'NoEmailAssociated';

export interface LoginResult {
  Type: LoginResultType;
  Username: string;
}

export interface CreateSessionResult {
  Result: LoginResult;
  SessionKey: string;
  UserId: string;
}

export interface CreateSessionRequest {
  Username: string;
  Password: string;
  DeviceId: string;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly resultType?: LoginResultType
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function loginResultTypeToMessage(type: LoginResultType): string {
  switch (type) {
    case 'Ok': return '';
    case 'UnknownUsername': return 'Unknown username or email address.';
    case 'PasswordWrong': return 'Incorrect password.';
    case 'EmailUnverified': return 'Your email address has not been verified. Please check your email.';
    case 'NotSubscribed': return 'An active subscription is required to play online.';
    case 'NoEmailAssociated': return 'No email address is associated with this account.';
    default: return 'An unknown error occurred. Please try again.';
  }
}

export async function createSession(
  username: string,
  password: string,
  deviceId: string
): Promise<CreateSessionResult> {
  const body: CreateSessionRequest = { Username: username, Password: password, DeviceId: deviceId };

  const resp = await fetch(`${API_BASE}/api/sessions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(body),
  });

  if (!resp.ok) {
    throw new ApiError(`Login request failed with status ${resp.status}`, resp.status);
  }

  const result: CreateSessionResult = await resp.json();
  return result;
}

export async function validateSession(
  userId: string,
  deviceId: string,
  sessionId: string
): Promise<boolean> {
  const resp = await fetch(
    `${API_BASE}/api/users/${userId}/devices/${deviceId}/session/validate`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(sessionId),
    }
  );

  if (resp.status === 404) return false;
  if (!resp.ok) throw new ApiError(`Session validation failed with status ${resp.status}`, resp.status);

  const result = await resp.json();
  return result === 'ok';
}
