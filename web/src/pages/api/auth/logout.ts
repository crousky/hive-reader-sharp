import type { APIRoute } from 'astro';
import { deleteSession } from '../../../lib/auth';

export const POST: APIRoute = async ({ cookies, redirect }) => {
  await deleteSession(cookies);
  return redirect('/');
};
