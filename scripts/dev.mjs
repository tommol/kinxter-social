import { spawn } from "node:child_process";
import net from "node:net";
import process from "node:process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(scriptDirectory, "..");
const packageManager = process.platform === "win32" ? "pnpm.cmd" : "pnpm";
const skipInfrastructure = process.argv.includes("--skip-infra");
const unknownArguments = process.argv
  .slice(2)
  .filter((argument) => argument !== "--skip-infra");

if (unknownArguments.length > 0) {
  console.error(`Unknown argument(s): ${unknownArguments.join(", ")}`);
  console.error("Usage: pnpm dev [--skip-infra]");
  process.exit(1);
}

const colors = {
  auth: "\u001b[36m",
  api: "\u001b[32m",
  web: "\u001b[35m",
  admin: "\u001b[33m",
  reset: "\u001b[0m",
};

const sharedDotnetEnvironment = {
  ASPNETCORE_ENVIRONMENT: "Development",
  Database__ApplyMigrationsOnStartup: "true",
  ModuleEvents__Transport: "Nats",
  ModuleEvents__Nats__Url: "nats://localhost:4222",
};
const publicAuthIssuer =
  process.env.AUTH_ISSUER ??
  "http://localhost:8081/realms/kinxter";
const backofficeAuthIssuer =
  process.env.ADMIN_AUTH_ISSUER ??
  "http://localhost:8081/realms/kinxter-admin";

const services = [
  {
    name: "auth",
    script: "auth:watch",
    environment: {
      ...sharedDotnetEnvironment,
      Auth__Realms__0__Issuer: publicAuthIssuer,
      Auth__Realms__0__PathBase: getPathBase(publicAuthIssuer),
      Auth__Realms__1__Issuer: backofficeAuthIssuer,
      Auth__Realms__1__PathBase: getPathBase(backofficeAuthIssuer),
      AuthAdmin__Enabled: process.env.AuthAdmin__Enabled ?? "true",
      AuthAdmin__Bootstrap__Username:
        process.env.AuthAdmin__Bootstrap__Username ?? "admin",
      AuthAdmin__Bootstrap__Password:
        process.env.AuthAdmin__Bootstrap__Password ??
        "kinxter-control-dev-password",
      ModuleEvents__Nats__ConsumerEnabled: "false",
      ModuleEvents__Nats__ConsumerName: "kinxter-auth",
      Email__Host: "localhost",
      Email__Port: "1025",
      Email__UseTls: "false",
    },
  },
  {
    name: "api",
    script: "api:watch",
    environment: {
      ...sharedDotnetEnvironment,
      Auth__PublicIssuer: publicAuthIssuer,
      Auth__BackofficeIssuer: backofficeAuthIssuer,
      ModuleEvents__Nats__ConsumerEnabled: "true",
      ModuleEvents__Nats__ConsumerName: "kinxter-api",
    },
  },
  {
    name: "web",
    script: "web:dev",
    environment: {
      AUTH_ISSUER: publicAuthIssuer,
      AUTH_CLIENT_ID: process.env.AUTH_CLIENT_ID ?? "kinxter-web",
      AUTH_CLIENT_SECRET:
        process.env.AUTH_CLIENT_SECRET ?? "kinxter-web-dev-secret",
      AUTH_SECRET:
        process.env.AUTH_SECRET ?? "kinxter-web-dev-auth-secret",
      KINXTER_API_BASE_URL:
        process.env.KINXTER_API_BASE_URL ?? "http://localhost:8080",
      NEXT_PUBLIC_API_BASE_URL:
        process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080",
    },
  },
  {
    name: "admin",
    script: "admin:dev",
    environment: {
      AUTH_ISSUER: backofficeAuthIssuer,
      AUTH_CLIENT_ID:
        process.env.ADMIN_AUTH_CLIENT_ID ?? "kinxter-admin",
      AUTH_CLIENT_SECRET:
        process.env.ADMIN_AUTH_CLIENT_SECRET ?? "kinxter-admin-dev-secret",
      AUTH_SECRET:
        process.env.ADMIN_AUTH_SECRET ?? "kinxter-admin-dev-auth-secret",
      ADMIN_API_BASE_URL:
        process.env.ADMIN_API_BASE_URL ?? "http://localhost:8080",
    },
  },
];

try {
  if (!skipInfrastructure) {
    console.log("Starting PostgreSQL and NATS...");
    await runCommand(packageManager, ["run", "dev:infra"]);
    await Promise.all([
      waitForPort("PostgreSQL", 15432),
      waitForPort("NATS", 4222),
      waitForPort("Mailpit SMTP", 1025),
      waitForPort("MinIO", 9000),
    ]);
  } else {
    console.log("Skipping infrastructure startup (--skip-infra).");
  }
} catch (error) {
  console.error(`Unable to start local infrastructure: ${error.message}`);
  console.error(
    "Start PostgreSQL and NATS manually, then retry with: pnpm dev --skip-infra",
  );
  process.exit(1);
}

console.log(
  [
    "Starting local development:",
    "  web  http://localhost:3000",
    "  admin http://localhost:3001",
    "  API  http://localhost:8080",
    `  auth ${publicAuthIssuer}`,
    "  auth control http://localhost:8081/control",
    "",
    "Press Ctrl+C to stop the applications.",
  ].join("\n"),
);

const children = new Map();
const processGroups = new Set();
let shuttingDown = false;
let finalExitCode = 0;
let forceStopTimer;
let forceStopCompleted = false;

for (const service of services) {
  const child = spawn(packageManager, ["run", service.script], {
    cwd: repositoryRoot,
    detached: process.platform !== "win32",
    env: {
      ...process.env,
      ...service.environment,
    },
    stdio: ["ignore", "pipe", "pipe"],
  });

  children.set(service.name, child);
  if (process.platform !== "win32" && child.pid !== undefined) {
    processGroups.add(child.pid);
  }
  pipeWithPrefix(child.stdout, process.stdout, service.name);
  pipeWithPrefix(child.stderr, process.stderr, service.name);

  child.once("error", (error) => {
    console.error(`[${service.name}] Failed to start: ${error.message}`);
    shutdown(1);
  });

  child.once("close", (exitCode, signal) => {
    children.delete(service.name);

    if (!shuttingDown) {
      const reason =
        signal === null
          ? `exit code ${exitCode ?? 1}`
          : `signal ${signal}`;
      console.error(`[${service.name}] Stopped unexpectedly (${reason}).`);
      shutdown(exitCode === 0 ? 1 : (exitCode ?? 1));
    }

    finishWhenStopped();
  });
}

process.on("SIGINT", () => shutdown(0));
process.on("SIGTERM", () => shutdown(0));

function runCommand(command, args) {
  return new Promise((resolveCommand, rejectCommand) => {
    const child = spawn(command, args, {
      cwd: repositoryRoot,
      env: process.env,
      stdio: "inherit",
    });

    child.once("error", (error) => {
      rejectCommand(
        new Error(
          `Unable to run "${command} ${args.join(" ")}": ${error.message}`,
        ),
      );
    });

    child.once("exit", (exitCode, signal) => {
      if (exitCode === 0) {
        resolveCommand();
        return;
      }

      const reason =
        signal === null ? `exit code ${exitCode ?? 1}` : `signal ${signal}`;
      rejectCommand(
        new Error(`"${command} ${args.join(" ")}" failed (${reason}).`),
      );
    });
  });
}

async function waitForPort(name, port) {
  const timeoutMs = 60_000;
  const startedAt = Date.now();

  while (Date.now() - startedAt < timeoutMs) {
    if (await canConnect(port)) {
      console.log(`${name} is ready on port ${port}.`);
      return;
    }

    await new Promise((resolveDelay) => setTimeout(resolveDelay, 250));
  }

  throw new Error(`${name} did not become ready on port ${port}.`);
}

function canConnect(port) {
  return new Promise((resolveConnection) => {
    const socket = net.createConnection({
      host: "127.0.0.1",
      port,
    });

    socket.setTimeout(500);
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

function getPathBase(issuer) {
  const pathBase = new URL(issuer).pathname.replace(/\/$/, "");

  return pathBase || "/";
}

function pipeWithPrefix(source, destination, name) {
  let bufferedText = "";
  const useColors = destination.isTTY;
  const prefix = useColors
    ? `${colors[name]}[${name.padEnd(4)}]${colors.reset} `
    : `[${name.padEnd(4)}] `;

  source.setEncoding("utf8");
  source.on("data", (chunk) => {
    bufferedText += chunk;
    const lines = bufferedText.split(/\r?\n/);
    bufferedText = lines.pop() ?? "";

    for (const line of lines) {
      destination.write(`${prefix}${line}\n`);
    }
  });
  source.on("end", () => {
    if (bufferedText.length > 0) {
      destination.write(`${prefix}${bufferedText}\n`);
    }
  });
}

function shutdown(exitCode) {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;
  finalExitCode = exitCode;
  console.log("\nStopping local applications...");

  for (const child of children.values()) {
    stopProcessTree(child, "SIGINT");
  }

  forceStopTimer = setTimeout(() => {
    if (process.platform === "win32") {
      for (const child of children.values()) {
        stopProcessTree(child, "SIGKILL");
      }
    } else {
      for (const processGroup of processGroups) {
        stopProcessGroup(processGroup, "SIGKILL");
      }
    }

    forceStopCompleted = true;
    finishWhenStopped();
  }, 2_000);
}

function stopProcessTree(child, signal) {
  if (child.exitCode !== null || child.signalCode !== null) {
    return;
  }

  try {
    if (process.platform === "win32") {
      child.kill(signal);
    } else {
      process.kill(-child.pid, signal);
    }
  } catch (error) {
    if (error.code !== "ESRCH") {
      console.error(`Unable to stop process ${child.pid}: ${error.message}`);
    }
  }
}

function stopProcessGroup(processGroup, signal) {
  try {
    process.kill(-processGroup, signal);
  } catch (error) {
    if (error.code !== "ESRCH") {
      console.error(
        `Unable to stop process group ${processGroup}: ${error.message}`,
      );
    }
  }
}

function finishWhenStopped() {
  if (!shuttingDown || !forceStopCompleted || children.size > 0) {
    return;
  }

  clearTimeout(forceStopTimer);
  if (!skipInfrastructure) {
    console.log(
      "Applications stopped. PostgreSQL and NATS are still running; stop them with: pnpm containers:down",
    );
  }
  process.exit(finalExitCode);
}
