import type { APIRoute } from 'astro';
import { nanoid } from 'nanoid';
import { exchangeCodeForToken, getGoogleUserInfo } from '../../../lib/google-auth';
import { getUsersContainer } from '../../../lib/cosmos';
import { createSession } from '../../../lib/auth';
import type { User } from '../../../types';

export const GET: APIRoute = async ({ url, cookies, redirect }) => {
  const code = url.searchParams.get('code');
  const state = url.searchParams.get('state');
  const storedState = cookies.get('oauth_state')?.value;

  // Verify state
  if (!code || !state || state !== storedState) {
    return redirect('/?error=invalid_state');
  }

  // Clear state cookie
  cookies.delete('oauth_state', { path: '/' });

  try {
    // Exchange code for token
    const tokenResponse = await exchangeCodeForToken(code);

    // Get user info
    const userInfo = await getGoogleUserInfo(tokenResponse.access_token);

    // Create or update user in database
    const container = await getUsersContainer();
    const userId = `google_${userInfo.id}`;

    let user: User;

    try {
      // Try to get existing user
      const { resource } = await container.item(userId, userId).read();
      user = resource as User;

      // Update user info
      user.name = userInfo.name;
      user.email = userInfo.email;
      user.updatedAt = new Date().toISOString();

      await container.item(userId, userId).replace(user);
    } catch (error) {
      // User doesn't exist, create new one
      user = {
        id: userId,
        email: userInfo.email,
        name: userInfo.name,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      };

      await container.items.create(user);
    }

    // Create session
    await createSession(user, cookies);

    return redirect('/dashboard');
  } catch (error) {
    console.error('OAuth callback error:', error);
    return redirect('/?error=authentication_failed');
  }
};
