import { NextRequest, NextResponse } from "next/server";
import {
  AccessTokenSession,
  getAccessToken,
} from "../../auth/_lib/oauth";

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
  const session = await getAccessToken(request);

  try {
    const response = await fetch(`${apiBaseUrl}/api/v1/monitoring/overview`, {
      cache: "no-store",
      headers: {
        accept: "application/json",
        ...(session.accessToken
          ? { authorization: `Bearer ${session.accessToken}` }
          : {}),
      },
    });
    const latencyMs = Math.round(performance.now() - startedAt);
    const payload = await readJson(response);

    if (!response.ok) {
      return jsonWithSession(
        {
          status: "down",
          checkedAt,
          latencyMs,
          apiBaseUrl,
          overview: null,
          error: `API monitoring endpoint returned HTTP ${response.status}.`,
          payload,
        },
        503,
        session,
      );
    }

    const overview = normalizeOverview(payload);

    return jsonWithSession(
      {
        status: overview.status,
        checkedAt,
        latencyMs,
        apiBaseUrl,
        overview,
      },
      overview.status === "ok" ? 200 : 503,
      session,
    );
  } catch (error) {
    return jsonWithSession(
      {
        status: "down",
        checkedAt,
        latencyMs: Math.round(performance.now() - startedAt),
        apiBaseUrl,
        overview: null,
        error: error instanceof Error ? error.message : "API is unavailable.",
      },
      503,
      session,
    );
  }
}

function jsonWithSession(
  body: unknown,
  status: number,
  session: AccessTokenSession,
) {
  const response = NextResponse.json(body, { status });
  session.apply(response);
  return response;
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
