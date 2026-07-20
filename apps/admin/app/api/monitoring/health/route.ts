import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "../../auth/_lib/oauth";

export const dynamic = "force-dynamic";
export const revalidate = 0;

type MonitoringStatus = "ok" | "degraded" | "down";

type MonitoringOverview = {
  service?: string;
  status?: MonitoringStatus;
  checkedAt?: string;
  dependencies?: unknown[];
  metrics?: unknown;
  outbox?: unknown[];
};

const defaultApiBaseUrl = "http://localhost:8080";

export async function GET(request: NextRequest) {
  const startedAt = performance.now();
  const apiBaseUrl = getApiBaseUrl();
  const checkedAt = new Date().toISOString();
  const token = getAccessToken(request);

  try {
    const response = await fetch(`${apiBaseUrl}/api/v1/monitoring/overview`, {
      cache: "no-store",
      headers: {
        accept: "application/json",
        ...(token ? { authorization: `Bearer ${token}` } : {}),
      },
    });
    const latencyMs = Math.round(performance.now() - startedAt);
    const payload = await readJson(response);

    if (!response.ok) {
      return NextResponse.json(
        {
          status: "down",
          checkedAt,
          latencyMs,
          apiBaseUrl,
          overview: null,
          error: `API monitoring endpoint returned HTTP ${response.status}.`,
          payload,
        },
        { status: 503 },
      );
    }

    const overview = normalizeOverview(payload);

    return NextResponse.json(
      {
        status: overview.status,
        checkedAt,
        latencyMs,
        apiBaseUrl,
        overview,
      },
      { status: overview.status === "ok" ? 200 : 503 },
    );
  } catch (error) {
    return NextResponse.json(
      {
        status: "down",
        checkedAt,
        latencyMs: Math.round(performance.now() - startedAt),
        apiBaseUrl,
        overview: null,
        error: error instanceof Error ? error.message : "API is unavailable.",
      },
      { status: 503 },
    );
  }
}

function getApiBaseUrl() {
  return (
    process.env.ADMIN_API_BASE_URL ??
    process.env.KINXTER_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL ??
    defaultApiBaseUrl
  ).replace(/\/$/, "");
}

async function readJson(response: Response): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

function normalizeOverview(payload: unknown): Required<MonitoringOverview> {
  if (!payload || typeof payload !== "object") {
    return emptyOverview("down");
  }

  const overview = payload as MonitoringOverview;

  return {
    service: typeof overview.service === "string" ? overview.service : "Kinxter.Api",
    status: normalizeStatus(overview.status),
    checkedAt: typeof overview.checkedAt === "string" ? overview.checkedAt : new Date().toISOString(),
    dependencies: Array.isArray(overview.dependencies) ? overview.dependencies : [],
    metrics: overview.metrics ?? {},
    outbox: Array.isArray(overview.outbox) ? overview.outbox : [],
  };
}

function normalizeStatus(status: unknown): MonitoringStatus {
  return status === "ok" || status === "degraded" || status === "down"
    ? status
    : "down";
}

function emptyOverview(status: MonitoringStatus): Required<MonitoringOverview> {
  return {
    service: "Kinxter.Api",
    status,
    checkedAt: new Date().toISOString(),
    dependencies: [],
    metrics: {},
    outbox: [],
  };
}
