import type { APIRoute } from 'astro';
import { nanoid } from 'nanoid';
import { getGoogleAuthUrl } from '../../../lib/google-auth';

export const GET: APIRoute = async ({ cookies, redirect }) => {
  // Generate state for CSRF protection
  const state = nanoid(32);

  // Store state in cookie
  cookies.set('oauth_state', state, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    maxAge: 600, // 10 minutes
    path: '/'
  });

  // Redirect to Google OAuth
  const authUrl = getGoogleAuthUrl(state);
  return redirect(authUrl);
};
