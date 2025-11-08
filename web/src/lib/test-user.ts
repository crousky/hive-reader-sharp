import type { User } from '../types';

// Test user for local development
export const TEST_USER: User = {
  id: 'test_user_local',
  email: 'testuser@example.com',
  name: 'Test User',
  kindleEmail: 'testuser@kindle.com',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString()
};

export function isLocalEnvironment(): boolean {
  return process.env.NODE_ENV === 'development' || process.env.USE_TEST_USER === 'true';
}

export async function getOrCreateTestUser(getUsersContainer: () => Promise<any>): Promise<User> {
  if (!isLocalEnvironment()) {
    throw new Error('Test user is only available in local development environment');
  }

  try {
    const container = await getUsersContainer();

    try {
      // Try to get existing test user
      const { resource } = await container.item(TEST_USER.id, TEST_USER.id).read();

      if (resource) {
        return resource as User;
      } else {
        // Resource is undefined, create new test user
        await container.items.create(TEST_USER);
        return TEST_USER;
      }
    } catch (error) {
      // Test user doesn't exist, create it
      await container.items.create(TEST_USER);
      return TEST_USER;
    }
  } catch (error) {
    console.error('Error accessing Cosmos DB, using fallback test user:', error);
    // Return test user even if DB operation fails (for offline development)
    return TEST_USER;
  }
}
