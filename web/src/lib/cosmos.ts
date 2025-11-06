import { CosmosClient, Database, Container } from '@azure/cosmos';

const endpoint = process.env.COSMOS_ENDPOINT || '';
const key = process.env.COSMOS_KEY || '';
const databaseId = 'SendToKindleDB';
const usersContainerId = 'Users';
const sessionsContainerId = 'Sessions';

let client: CosmosClient | null = null;
let database: Database | null = null;

function getClient(): CosmosClient {
  if (!client) {
    client = new CosmosClient({ endpoint, key });
  }
  return client;
}

async function getDatabase(): Promise<Database> {
  if (!database) {
    const client = getClient();
    const { database: db } = await client.databases.createIfNotExists({
      id: databaseId
    });
    database = db;
  }
  return database;
}

export async function getUsersContainer(): Promise<Container> {
  const db = await getDatabase();
  const { container } = await db.containers.createIfNotExists({
    id: usersContainerId,
    partitionKey: '/id'
  });
  return container;
}

export async function getSessionsContainer(): Promise<Container> {
  const db = await getDatabase();
  const { container } = await db.containers.createIfNotExists({
    id: sessionsContainerId,
    partitionKey: '/userId',
    defaultTtl: 86400 // 24 hours
  });
  return container;
}
