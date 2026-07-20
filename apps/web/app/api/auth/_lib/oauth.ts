import { NextRequest, NextResponse } from "next/server";

const stateCookie = "kinxter_auth_state";
const verifierCookie = "kinxter_auth_verifier";
const accessTokenCookie = "kinxter_access_token";
const refreshTokenCookie = "kinxter_refresh_token";
const idTokenCookie = "kinxter_id_token";

type TokenResponse = {
  access_token?: string;
  refresh_token?: string;
  id_token?: string;
  expires_in?: number;
  error?: string;
  error_description?: string;
};

export function getAccessToken(request: NextRequest) {
  return request.cookies.get(accessTokenCookie)?.value ?? null;
}

export async function startLogin(request: NextRequest, scopes: string[]) {
  const state = randomBase64Url(32);
  const verifier = randomBase64Url(48);
  const challenge = await codeChallenge(verifier);
  const redirectUri = getRedirectUri(request);
  const authorizeUrl = new URL(`${getIssuer()}/connect/authorize`);

  authorizeUrl.searchParams.set("client_id", getClientId());
  authorizeUrl.searchParams.set("redirect_uri", redirectUri);
  authorizeUrl.searchParams.set("response_type", "code");
  authorizeUrl.searchParams.set("scope", scopes.join(" "));
  authorizeUrl.searchParams.set("state", state);
  authorizeUrl.searchParams.set("code_challenge", challenge);
  authorizeUrl.searchParams.set("code_challenge_method", "S256");

  const response = NextResponse.redirect(authorizeUrl);
  const cookieOptions = getTransientCookieOptions();

  response.cookies.set(stateCookie, state, cookieOptions);
  response.cookies.set(verifierCookie, verifier, cookieOptions);

  return response;
}

export async function completeLogin(request: NextRequest) {
  const url = request.nextUrl;
  const error = url.searchParams.get("error");

  if (error) {
    return NextResponse.json(
      { error, error_description: url.searchParams.get("error_description") },
      { status: 400 },
    );
  }

  const code = url.searchParams.get("code");
  const state = url.searchParams.get("state");
  const expectedState = request.cookies.get(stateCookie)?.value;
  const verifier = request.cookies.get(verifierCookie)?.value;

  if (!code || !state || !expectedState || !verifier || state !== expectedState) {
    return NextResponse.json({ error: "invalid_callback" }, { status: 400 });
  }

  const body = new URLSearchParams({
    grant_type: "authorization_code",
    client_id: getClientId(),
    client_secret: getClientSecret(),
    redirect_uri: getRedirectUri(request),
    code,
    code_verifier: verifier,
  });
  const tokenResponse = await fetch(`${getIssuer()}/connect/token`, {
    method: "POST",
    headers: {
      "content-type": "application/x-www-form-urlencoded",
      accept: "application/json",
    },
    body,
    cache: "no-store",
  });
  const tokenPayload = (await tokenResponse.json()) as TokenResponse;

  if (!tokenResponse.ok || !tokenPayload.access_token) {
    return NextResponse.json(
      {
        error: tokenPayload.error ?? "token_exchange_failed",
        error_description: tokenPayload.error_description,
      },
      { status: 400 },
    );
  }

  const response = NextResponse.redirect(new URL("/", request.nextUrl.origin));
  const sessionOptions = getSessionCookieOptions(tokenPayload.expires_in ?? 3600);

  response.cookies.delete(stateCookie);
  response.cookies.delete(verifierCookie);
  response.cookies.set(accessTokenCookie, tokenPayload.access_token, sessionOptions);

  if (tokenPayload.refresh_token) {
    response.cookies.set(refreshTokenCookie, tokenPayload.refresh_token, getSessionCookieOptions(30 * 24 * 3600));
  }

  if (tokenPayload.id_token) {
    response.cookies.set(idTokenCookie, tokenPayload.id_token, sessionOptions);
  }

  return response;
}

export function logout(request: NextRequest) {
  const logoutUrl = new URL(`${getIssuer()}/connect/logout`);

  logoutUrl.searchParams.set("post_logout_redirect_uri", request.nextUrl.origin);

  const response = NextResponse.redirect(logoutUrl);

  response.cookies.delete(accessTokenCookie);
  response.cookies.delete(refreshTokenCookie);
  response.cookies.delete(idTokenCookie);

  return response;
}

function getIssuer() {
  const issuer = process.env.AUTH_ISSUER;

  if (!issuer) {
    throw new Error("AUTH_ISSUER must be configured.");
  }

  return issuer.replace(/\/$/, "");
}

function getClientId() {
  return process.env.AUTH_CLIENT_ID ?? "kinxter-web";
}

function getClientSecret() {
  return process.env.AUTH_CLIENT_SECRET ?? "kinxter-web-dev-secret";
}

function getRedirectUri(request: NextRequest) {
  return new URL("/api/auth/callback/kinxter", request.nextUrl.origin).toString();
}

function getTransientCookieOptions() {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: useSecureCookies(),
    path: "/",
    maxAge: 600,
  };
}

function getSessionCookieOptions(maxAge: number) {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    secure: useSecureCookies(),
    path: "/",
    maxAge,
  };
}

function useSecureCookies() {
  return process.env.AUTH_COOKIE_SECURE === "true";
}

function randomBase64Url(bytes: number) {
  const array = new Uint8Array(bytes);

  crypto.getRandomValues(array);

  return Buffer.from(array).toString("base64url");
}

async function codeChallenge(verifier: string) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));

  return Buffer.from(digest).toString("base64url");
}
