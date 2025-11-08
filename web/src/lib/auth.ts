import type { AstroCookies } from 'astro';
import { nanoid } from 'nanoid';
import type { User, Session } from '../types';
import { getSessionsContainer, getUsersContainer } from './cosmos';

const SESSION_COOKIE_NAME = 'session_token';
const SESSION_DURATION = 24 * 60 * 60 * 1000; // 24 hours

export async function createSession(user: User, cookies: AstroCookies): Promise<void> {
  const sessionToken = nanoid(32);
  const expiresAt = Date.now() + SESSION_DURATION;

  const session: Session & { id: string } = {
    id: sessionToken,
    userId: user.id,
    email: user.email,
    expiresAt
  };

  const container = await getSessionsContainer();
  await container.items.create(session);

  cookies.set(SESSION_COOKIE_NAME, sessionToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    maxAge: SESSION_DURATION / 1000,
    path: '/'
  });
}

export async function getSession(cookies: AstroCookies): Promise<Session | null> {
  const sessionToken = cookies.get(SESSION_COOKIE_NAME)?.value;
  if (!sessionToken) {
    return null;
  }

  try {
    const container = await getSessionsContainer();
    const { resource } = await container.item(sessionToken, sessionToken).read();

    if (!resource || resource.expiresAt < Date.now()) {
      return null;
    }

    return resource as Session;
  } catch (error) {
    return null;
  }
}

export async function deleteSession(cookies: AstroCookies): Promise<void> {
  const sessionToken = cookies.get(SESSION_COOKIE_NAME)?.value;
  if (sessionToken) {
    try {
      const container = await getSessionsContainer();
      await container.item(sessionToken, sessionToken).delete();
    } catch (error) {
      // Ignore errors when deleting session
    }
  }

  cookies.delete(SESSION_COOKIE_NAME, { path: '/' });
}

export async function getCurrentUser(cookies: AstroCookies): Promise<User | null> {
  const session = await getSession(cookies);
  if (!session) {
    return null;
  }

  try {
    const container = await getUsersContainer();
    const { resource } = await container.item(session.userId, session.userId).read();
    return resource as User || null;
  } catch (error) {
    return null;
  }
}
