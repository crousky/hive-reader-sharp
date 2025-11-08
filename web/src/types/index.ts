export interface User {
  id: string;
  email: string;
  name: string;
  kindleEmail?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Session {
  userId: string;
  email: string;
  expiresAt: number;
}

export interface AuthResponse {
  authenticated: boolean;
  email?: string;
  user?: User;
}
