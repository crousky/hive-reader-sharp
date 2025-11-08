import { CosmosClient, Database, Container } from '@azure/cosmos';

// Detect if running locally
const isLocal = process.env.NODE_ENV === 'development' || process.env.USE_EMULATOR === 'true';

// Cosmos DB Emulator default settings
const EMULATOR_ENDPOINT = 'https://localhost:8081';
const EMULATOR_KEY = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==';

const endpoint = isLocal ? EMULATOR_ENDPOINT : (process.env.COSMOS_ENDPOINT || '');
const key = isLocal ? EMULATOR_KEY : (process.env.COSMOS_KEY || '');
const databaseId = 'SendToKindleDB';
const usersContainerId = 'Users';
const sessionsContainerId = 'Sessions';

let client: CosmosClient | null = null;
let database: Database | null = null;

function getClient(): CosmosClient {
  if (!client) {
    const options: any = { endpoint, key };

    // Disable SSL verification for local emulator
    if (isLocal) {
      process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
    }

    client = new CosmosClient(options);
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
