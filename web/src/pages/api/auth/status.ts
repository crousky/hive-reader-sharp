import type { APIRoute } from 'astro';
import { getCurrentUser } from '../../../lib/auth';
import type { AuthResponse } from '../../../types';

export const GET: APIRoute = async ({ cookies }) => {
  const user = await getCurrentUser(cookies);

  const response: AuthResponse = {
    authenticated: !!user,
    email: user?.email,
    user: user || undefined
  };

  return new Response(JSON.stringify(response), {
    status: 200,
    headers: {
      'Content-Type': 'application/json'
    }
  });
};
