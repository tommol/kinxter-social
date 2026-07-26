import { access, rm } from "node:fs/promises";
import net from "node:net";
import process from "node:process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, "..");

const applications = new Map([
  [
    "web",
    {
      cacheDirectory: resolve(repositoryRoot, "apps/web/.next"),
      developmentPort: 3000,
    },
  ],
  [
    "admin",
    {
      cacheDirectory: resolve(repositoryRoot, "apps/admin/.next"),
      developmentPort: 3001,
    },
  ],
]);

const forceCleanup = process.argv.includes("--force");
const requestedApplications = process.argv
  .slice(2)
  .filter((argument) => argument !== "--force");
const applicationNames =
  requestedApplications.length > 0
    ? [...new Set(requestedApplications)]
    : [...applications.keys()];

for (const applicationName of applicationNames) {
  if (!applications.has(applicationName)) {
    console.error(
      `Unknown Next.js application "${applicationName}". Expected: ${[
        ...applications.keys(),
      ].join(", ")}.`,
    );
    process.exitCode = 1;
  }
}

if (process.exitCode) {
  process.exit();
}

let developmentServerDetected = false;

for (const applicationName of applicationNames) {
  const application = applications.get(applicationName);
  const developmentLock = join(application.cacheDirectory, "dev/lock");

  if (
    !forceCleanup &&
    ((await pathExists(developmentLock)) ||
      (await isPortInUse(application.developmentPort)))
  ) {
    console.error(
      `Refusing to clean ${applicationName}: a development server may be running. Stop it first, or use --force only for a stale lock.`,
    );
    developmentServerDetected = true;
  }
}

if (developmentServerDetected) {
  process.exitCode = 1;
} else {
  for (const applicationName of applicationNames) {
    const application = applications.get(applicationName);

    await rm(application.cacheDirectory, { force: true, recursive: true });
    console.log(`Cleaned ${applicationName}: ${application.cacheDirectory}`);
  }
}

async function isPortInUse(port) {
  const connectionResults = await Promise.all([
    canConnect("127.0.0.1", port),
    canConnect("::1", port),
  ]);

  return connectionResults.some(Boolean);
}

function canConnect(host, port) {
  return new Promise((resolveConnection) => {
    const socket = net.createConnection({
      host,
      port,
    });

    socket.setTimeout(300);
    socket.once("connect", () => {
      socket.destroy();
      resolveConnection(true);
    });
    socket.once("error", () => {
      socket.destroy();
      resolveConnection(false);
    });
    socket.once("timeout", () => {
      socket.destroy();
      resolveConnection(false);
    });
  });
}

async function pathExists(path) {
  try {
    await access(path);
    return true;
  } catch {
    return false;
  }
}
