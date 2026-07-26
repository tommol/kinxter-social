import { NextRequest, NextResponse } from "next/server";
import * as oidc from "openid-client";

const stateCookie = "kinxter_admin_auth_state";
const verifierCookie = "kinxter_admin_auth_verifier";
const nonceCookie = "kinxter_admin_auth_nonce";
const accessTokenCookie = "kinxter_admin_access_token";
const refreshTokenCookie = "kinxter_admin_refresh_token";
const idTokenCookie = "kinxter_admin_id_token";
const refreshTokenLifetimeSeconds = 8 * 3600;

const allowedBackofficeRoles = new Set([
  "super_admin",
  "ops",
  "moderator",
  "support",
  "read_only",
  "admin",
]);

export type AccessTokenSession = {
  accessToken: string | null;
  apply(response: NextResponse): void;
};

let cachedConfiguration:
  | { key: string; value: Promise<oidc.Configuration> }
  | undefined;

export async function getAccessToken(
  request: NextRequest,
): Promise<AccessTokenSession> {
  const accessToken = request.cookies.get(accessTokenCookie)?.value;

  if (accessToken) {
    return { accessToken, apply: () => undefined };
  }

  const refreshToken = request.cookies.get(refreshTokenCookie)?.value;

  if (!refreshToken) {
    return { accessToken: null, apply: clearSessionCookies };
  }

  try {
    const configuration = await getConfiguration();
    const tokens = await oidc.refreshTokenGrant(configuration, refreshToken);

    return {
      accessToken: tokens.access_token,
      apply(response) {
        response.cookies.set(
          accessTokenCookie,
          tokens.access_token,
          getSessionCookieOptions(tokens.expires_in ?? 300),
        );

        if (tokens.refresh_token) {
          response.cookies.set(
            refreshTokenCookie,
            tokens.refresh_token,
            getSessionCookieOptions(refreshTokenLifetimeSeconds),
          );
        }

        if (tokens.id_token) {
          response.cookies.set(
            idTokenCookie,
            tokens.id_token,
            getSessionCookieOptions(tokens.expires_in ?? 300),
          );
        }
      },
    };
  } catch {
    return { accessToken: null, apply: clearSessionCookies };
  }
}

export async function startLogin(request: NextRequest, scopes: string[]) {
  const configuration = await getConfiguration();
  const state = oidc.randomState();
  const verifier = oidc.randomPKCECodeVerifier();
  const nonce = oidc.randomNonce();
  const challenge = await oidc.calculatePKCECodeChallenge(verifier);
  const authorizeUrl = oidc.buildAuthorizationUrl(configuration, {
    redirect_uri: getRedirectUri(request),
    response_type: "code",
    scope: scopes.join(" "),
    state,
    nonce,
    code_challenge: challenge,
    code_challenge_method: "S256",
  });
  const response = NextResponse.redirect(authorizeUrl);
  const cookieOptions = getTransientCookieOptions();

  response.cookies.set(stateCookie, state, cookieOptions);
  response.cookies.set(verifierCookie, verifier, cookieOptions);
  response.cookies.set(nonceCookie, nonce, cookieOptions);

  return response;
}

export async function completeLogin(request: NextRequest) {
  const expectedState = request.cookies.get(stateCookie)?.value;
  const verifier = request.cookies.get(verifierCookie)?.value;
  const expectedNonce = request.cookies.get(nonceCookie)?.value;

  if (!expectedState || !verifier || !expectedNonce) {
    return NextResponse.json({ error: "invalid_callback_session" }, { status: 400 });
  }

  try {
    const configuration = await getConfiguration();
    const tokens = await oidc.authorizationCodeGrant(
      configuration,
      new URL(request.url),
      {
        pkceCodeVerifier: verifier,
        expectedState,
        expectedNonce,
        idTokenExpected: true,
      },
    );
    const claims = tokens.claims();

    if (
      claims?.realm !== "backoffice" ||
      !hasAllowedBackofficeRole(claims?.role)
    ) {
      const response = NextResponse.json(
        { error: "backoffice_access_denied" },
        { status: 403 },
      );
      clearTransientCookies(response);
      clearSessionCookies(response);
      return response;
    }

    const response = NextResponse.redirect(new URL("/", request.nextUrl.origin));
    const sessionOptions = getSessionCookieOptions(tokens.expires_in ?? 3600);

    clearTransientCookies(response);
    response.cookies.set(accessTokenCookie, tokens.access_token, sessionOptions);

    if (tokens.refresh_token) {
      response.cookies.set(
        refreshTokenCookie,
        tokens.refresh_token,
        getSessionCookieOptions(refreshTokenLifetimeSeconds),
      );
    }

    if (tokens.id_token) {
      response.cookies.set(idTokenCookie, tokens.id_token, sessionOptions);
    }

    return response;
  } catch (error) {
    return NextResponse.json(
      {
        error: "oidc_callback_failed",
        error_description:
          error instanceof Error ? error.message : "The OIDC callback could not be validated.",
      },
      { status: 400 },
    );
  }
}

export async function logout(request: NextRequest) {
  const configuration = await getConfiguration();
  const idToken = request.cookies.get(idTokenCookie)?.value;
  const parameters: Record<string, string> = {
    post_logout_redirect_uri: request.nextUrl.origin,
  };

  if (idToken) {
    parameters.id_token_hint = idToken;
  }

  const response = NextResponse.redirect(
    oidc.buildEndSessionUrl(configuration, parameters),
  );

  clearTransientCookies(response);
  clearSessionCookies(response);

  return response;
}

function getConfiguration() {
  const issuer = getIssuer();
  const clientId = getClientId();
  const clientSecret = getClientSecret();
  const key = `${issuer}\u0000${clientId}\u0000${clientSecret}`;

  if (!cachedConfiguration || cachedConfiguration.key !== key) {
    const issuerUrl = new URL(issuer);
    const allowInsecureHttp =
      issuerUrl.protocol === "http:" &&
      (process.env.NODE_ENV !== "production" ||
        process.env.AUTH_ALLOW_INSECURE_HTTP === "true");

    cachedConfiguration = {
      key,
      value: oidc.discovery(
        issuerUrl,
        clientId,
        clientSecret,
        undefined,
        allowInsecureHttp
          ? { execute: [oidc.allowInsecureRequests] }
          : undefined,
      ),
    };
  }

  return cachedConfiguration.value;
}

function getIssuer() {
  const issuer = process.env.AUTH_ISSUER;

  if (!issuer) {
    throw new Error("AUTH_ISSUER must be configured.");
  }

  return issuer.replace(/\/$/, "");
}

function getClientId() {
  return process.env.AUTH_CLIENT_ID ?? "kinxter-admin";
}

function getClientSecret() {
  return process.env.AUTH_CLIENT_SECRET ?? "kinxter-admin-dev-secret";
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

function clearTransientCookies(response: NextResponse) {
  response.cookies.delete(stateCookie);
  response.cookies.delete(verifierCookie);
  response.cookies.delete(nonceCookie);
}

function clearSessionCookies(response: NextResponse) {
  response.cookies.delete(accessTokenCookie);
  response.cookies.delete(refreshTokenCookie);
  response.cookies.delete(idTokenCookie);
}

function hasAllowedBackofficeRole(value: unknown) {
  const roles = Array.isArray(value) ? value : typeof value === "string" ? [value] : [];

  return roles.some(
    (role) => typeof role === "string" && allowedBackofficeRoles.has(role),
  );
}

function useSecureCookies() {
  return process.env.AUTH_COOKIE_SECURE === "true";
}
