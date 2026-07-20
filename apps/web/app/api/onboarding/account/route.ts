import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "../../auth/_lib/oauth";

export const dynamic = "force-dynamic";
export const revalidate = 0;

export async function POST(request: NextRequest) {
  const token = getAccessToken(request);

  if (!token) {
    return NextResponse.json({ authenticated: false }, { status: 401 });
  }

  const response = await fetch(`${getApiBaseUrl()}/api/v1/onboarding/account`, {
    method: "POST",
    cache: "no-store",
    headers: {
      accept: "application/json",
      authorization: `Bearer ${token}`,
      "content-type": "application/json",
    },
    body: await request.text(),
  });
  const payload = await response.json().catch(() => null);

  return NextResponse.json(payload, { status: response.status });
}

function getApiBaseUrl() {
  return (
    process.env.KINXTER_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL ??
    "http://localhost:8080"
  ).replace(/\/$/, "");
}
