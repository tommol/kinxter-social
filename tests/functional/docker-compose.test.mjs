import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import { after, before, test } from "node:test";

const testDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(testDirectory, "../..");
const composeFile = join(repositoryRoot, "deploy/containers/docker-compose.yml");
const projectName = `kinxterfunctional${process.pid}`;
const composeCandidates = [
  { command: "docker", args: ["compose"], label: "docker compose" },
  { command: "docker-compose", args: [], label: "docker-compose" },
  { command: "podman", args: ["compose"], label: "podman compose" },
];

let apiBaseUrl;
let webBaseUrl;
let composeEnvironment;
let composeInvocation;

before(
  async () => {
    const apiPort = process.env.FUNCTIONAL_API_HTTP_PORT ?? "18080";
    const webPort = process.env.FUNCTIONAL_WEB_HTTP_PORT ?? "13000";
    const postgresPort = process.env.FUNCTIONAL_POSTGRES_PORT ?? "25432";
    const natsClientPort = process.env.FUNCTIONAL_NATS_CLIENT_PORT ?? "14222";
    const natsMonitorPort = process.env.FUNCTIONAL_NATS_MONITOR_PORT ?? "18222";

    apiBaseUrl = `http://localhost:${apiPort}`;
    webBaseUrl = `http://localhost:${webPort}`;
    composeEnvironment = {
      API_HTTP_PORT: String(apiPort),
      WEB_HTTP_PORT: String(webPort),
      POSTGRES_PORT: String(postgresPort),
      NATS_CLIENT_PORT: String(natsClientPort),
      NATS_MONITOR_PORT: String(natsMonitorPort),
      NEXT_PUBLIC_API_BASE_URL: apiBaseUrl,
      WEB_PUBLIC_ORIGIN: webBaseUrl,
    };

    await runDockerCompose(["up", "--build", "--detach"]);
    await waitForJson(`${apiBaseUrl}/health`, {
      timeoutMs: 120_000,
      validate: (payload) => payload?.status === "ok",
    });
    await waitForHttpOk(webBaseUrl, { timeoutMs: 120_000 });
  },
  { timeout: 300_000 },
);

after(
  async () => {
    if (!composeEnvironment) {
      return;
    }

    await runDockerCompose(["down", "--volumes", "--remove-orphans"], {
      allowFailure: true,
    });
  },
  { timeout: 120_000 },
);

test("API health endpoint returns the expected service status", async () => {
  const response = await fetch(`${apiBaseUrl}/health`);

  assert.equal(response.status, 200);
  assert.deepEqual(await response.json(), {
    status: "ok",
    service: "Kinxter.Api",
  });
});

test("API exposes an OpenAPI document for REST endpoints", async () => {
  const response = await fetch(`${apiBaseUrl}/openapi/v1.json`);
  const document = await response.json();

  assert.equal(response.status, 200);
  assert.match(document.openapi, /^3\./);
  assert.ok(document.paths["/health"]);
  assert.ok(document.paths["/api/v1/accounts/register"]?.post);
});

test("API serves the Scalar reference UI", async () => {
  const response = await fetch(`${apiBaseUrl}/scalar/v1`);
  const html = await response.text();

  assert.equal(response.status, 200);
  assert.match(html, /Kinxter API Reference|Scalar/);
  assert.match(html, /openapi\/v1\.json/);
});

test("web application serves the Kinxter workspace page", async () => {
  const response = await fetch(webBaseUrl);
  const html = await response.text();

  assert.equal(response.status, 200);
  assert.match(html, /Kinxter Web/);
  assert.match(html, /Web client workspace/);
});

test("web container is built with the functional API base URL", async () => {
  const response = await fetch(webBaseUrl);
  const html = stripHtmlComments(await response.text());

  assert.equal(response.status, 200);
  assert.match(html, new RegExp(escapeRegExp(`${apiBaseUrl}/health`)));
});

async function runDockerCompose(args, options = {}) {
  composeInvocation ??= await findComposeInvocation();
  const composeArgs = [
    ...composeInvocation.args,
    "--project-name",
    projectName,
    "--file",
    composeFile,
    ...args,
  ];
  const child = spawn(
    composeInvocation.command,
    composeArgs,
    {
      cwd: repositoryRoot,
      env: {
        ...process.env,
        ...composeEnvironment,
      },
      stdio: ["ignore", "pipe", "pipe"],
    },
  );

  let stdout = "";
  let stderr = "";
  child.stdout.setEncoding("utf8");
  child.stderr.setEncoding("utf8");
  child.stdout.on("data", (chunk) => {
    stdout += chunk;
  });
  child.stderr.on("data", (chunk) => {
    stderr += chunk;
  });

  const result = await new Promise((resolveResult) => {
    child.once("close", (exitCode) => {
      resolveResult({ exitCode });
    });
    child.once("error", (error) => {
      resolveResult({ error });
    });
  });

  if ("error" in result) {
    if (options.allowFailure) {
      return { stdout, stderr };
    }

    throw new Error(
      [
        `Unable to start ${composeInvocation.label} ${args.join(" ")}.`,
        "Make sure Docker or Podman Compose is installed and running before executing pnpm test:functional.",
        result.error.message,
      ]
        .filter(Boolean)
        .join("\n"),
    );
  }

  const { exitCode } = result;
  if (exitCode === 0 || options.allowFailure) {
    return { stdout, stderr };
  }

  throw new Error(
    [
      `${composeInvocation.label} ${args.join(" ")} failed with exit code ${exitCode}.`,
      "Make sure Docker or Podman Compose is installed and running before executing pnpm test:functional.",
      stdout.trim(),
      stderr.trim(),
    ]
      .filter(Boolean)
      .join("\n"),
  );
}

async function findComposeInvocation() {
  const errors = [];

  for (const candidate of composeCandidates) {
    const result = await runProcess(candidate.command, [...candidate.args, "version"]);

    if (result.exitCode === 0) {
      return candidate;
    }

    errors.push(
      `${candidate.label}: ${result.error?.message ?? result.stderr.trim() ?? `exit ${result.exitCode}`}`,
    );
  }

  throw new Error(
    [
      "Unable to find a working Docker Compose command.",
      "Tried: docker compose, docker-compose, podman compose.",
      ...errors,
    ].join("\n"),
  );
}

async function runProcess(command, args) {
  const child = spawn(command, args, {
    cwd: repositoryRoot,
    env: process.env,
    stdio: ["ignore", "pipe", "pipe"],
  });

  let stdout = "";
  let stderr = "";
  child.stdout.setEncoding("utf8");
  child.stderr.setEncoding("utf8");
  child.stdout.on("data", (chunk) => {
    stdout += chunk;
  });
  child.stderr.on("data", (chunk) => {
    stderr += chunk;
  });

  const result = await new Promise((resolveResult) => {
    child.once("close", (exitCode) => {
      resolveResult({ exitCode, stdout, stderr });
    });
    child.once("error", (error) => {
      resolveResult({ exitCode: null, stdout, stderr, error });
    });
  });

  return result;
}

async function waitForHttpOk(url, options) {
  await waitFor(url, {
    ...options,
    validate: async (response) => response.ok,
  });
}

async function waitForJson(url, options) {
  await waitFor(url, {
    ...options,
    validate: async (response) => {
      if (!response.ok) {
        return false;
      }

      return options.validate(await response.json());
    },
  });
}

async function waitFor(url, options) {
  const deadline = Date.now() + options.timeoutMs;
  let lastError;

  while (Date.now() < deadline) {
    try {
      const response = await fetch(url, { cache: "no-store" });
      if (await options.validate(response)) {
        return;
      }

      lastError = new Error(`${url} returned HTTP ${response.status}.`);
    } catch (error) {
      lastError = error;
    }

    await delay(1_000);
  }

  throw new Error(`Timed out waiting for ${url}: ${lastError?.message ?? "no response"}`);
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function stripHtmlComments(value) {
  return value.replace(/<!--.*?-->/g, "");
}
