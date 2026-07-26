import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "../../auth/_lib/oauth";

export const dynamic = "force-dynamic";
export const revalidate = 0;

type RouteContext = { params: Promise<{ path: string[] }> };

async function proxy(request: NextRequest, context: RouteContext) {
  const token = getAccessToken(request);
  if (!token) return NextResponse.json({ authenticated: false }, { status: 401 });

  const { path } = await context.params;
  const upstream = new URL(`${getApiBaseUrl()}/api/v1/${path.map(encodeURIComponent).join("/")}`);
  request.nextUrl.searchParams.forEach((value, key) => upstream.searchParams.append(key, value));
  const hasBody = !["GET", "HEAD"].includes(request.method);
  const response = await fetch(upstream, {
    method: request.method,
    cache: "no-store",
    headers: {
      accept: "application/json",
      authorization: `Bearer ${token}`,
      ...(hasBody ? { "content-type": request.headers.get("content-type") ?? "application/json" } : {}),
    },
    body: hasBody ? await request.arrayBuffer() : undefined,
  });
  const body = await response.arrayBuffer();
  return new NextResponse(body, {
    status: response.status,
    headers: { "content-type": response.headers.get("content-type") ?? "application/json" },
  });
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;

function getApiBaseUrl() {
  return (process.env.KINXTER_API_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080").replace(/\/$/, "");
}
