import type { APIRoute } from 'astro';
import { getCurrentUser } from '../../../lib/auth';
import { getUsersContainer } from '../../../lib/cosmos';

export const POST: APIRoute = async ({ request, cookies }) => {
  const user = await getCurrentUser(cookies);

  if (!user) {
    return new Response(JSON.stringify({ error: 'Unauthorized' }), {
      status: 401,
      headers: { 'Content-Type': 'application/json' }
    });
  }

  try {
    const { kindleEmail } = await request.json();

    if (!kindleEmail || !kindleEmail.includes('@')) {
      return new Response(JSON.stringify({ error: 'Invalid Kindle email' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    // Update user in database
    const container = await getUsersContainer();
    user.kindleEmail = kindleEmail;
    user.updatedAt = new Date().toISOString();

    await container.item(user.id, user.id).replace(user);

    return new Response(JSON.stringify({ success: true, user }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });
  } catch (error) {
    return new Response(JSON.stringify({ error: 'Failed to update Kindle email' }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' }
    });
  }
};
