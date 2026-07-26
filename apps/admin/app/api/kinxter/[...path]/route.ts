import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "../../auth/_lib/oauth";

export const dynamic = "force-dynamic";
type Context = { params: Promise<{ path: string[] }> };

async function proxy(request: NextRequest, context: Context) {
  const session = await getAccessToken(request);
  if (!session.accessToken) { const response = NextResponse.json({ error: "unauthorized" }, { status: 401 }); session.apply(response); return response; }
  const { path } = await context.params; const url = new URL(`${baseUrl()}/api/v1/${path.map(encodeURIComponent).join("/")}`);
  request.nextUrl.searchParams.forEach((value, key) => url.searchParams.append(key, value));
  const hasBody = !["GET", "HEAD"].includes(request.method);
  const upstream = await fetch(url, { method: request.method, cache: "no-store", headers: { accept: "application/json", authorization: `Bearer ${session.accessToken}`, ...(hasBody ? { "content-type": request.headers.get("content-type") ?? "application/json" } : {}) }, body: hasBody ? await request.arrayBuffer() : undefined });
  const response = new NextResponse(await upstream.arrayBuffer(), { status: upstream.status, headers: { "content-type": upstream.headers.get("content-type") ?? "application/json" } }); session.apply(response); return response;
}
export const GET = proxy; export const POST = proxy; export const PUT = proxy; export const PATCH = proxy; export const DELETE = proxy;
function baseUrl() { return (process.env.ADMIN_API_BASE_URL ?? process.env.KINXTER_API_BASE_URL ?? "http://localhost:8080").replace(/\/$/, ""); }
