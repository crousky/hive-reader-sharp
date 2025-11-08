import type { APIRoute } from 'astro';
import { createSession } from '../../../lib/auth';
import { isLocalEnvironment, getOrCreateTestUser } from '../../../lib/test-user';
import { getUsersContainer } from '../../../lib/cosmos';

/**
 * Local development endpoint to automatically log in with test user
 * Only available when NODE_ENV=development or USE_TEST_USER=true
 */
export const GET: APIRoute = async ({ cookies, redirect }) => {
  if (!isLocalEnvironment()) {
    return new Response('Not available in production', { status: 403 });
  }

  try {
    // Get or create test user
    const testUser = await getOrCreateTestUser(getUsersContainer);

    // Validate that testUser was returned successfully
    if (!testUser) {
      console.error('getOrCreateTestUser returned undefined or null');
      throw new Error('Failed to get or create test user: returned undefined or null');
    }

    if (!testUser.id) {
      console.error('Test user missing id property:', testUser);
      throw new Error('Failed to get or create test user: missing id property');
    }

    // Create session for test user
    await createSession(testUser, cookies);

    return redirect('/dashboard');
  } catch (error) {
    console.error('Local login error:', error);
    return redirect('/?error=local_login_failed');
  }
};
