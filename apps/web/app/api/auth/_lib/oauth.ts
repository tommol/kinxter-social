import { NextRequest, NextResponse } from "next/server";
import * as oidc from "openid-client";

const stateCookie = "kinxter_auth_state";
const verifierCookie = "kinxter_auth_verifier";
const nonceCookie = "kinxter_auth_nonce";
const accessTokenCookie = "kinxter_access_token";
const refreshTokenCookie = "kinxter_refresh_token";
const idTokenCookie = "kinxter_id_token";
const localeCookie = "kinxter_auth_locale";
const supportedLocales = new Set(["pl", "en"]);

let cachedConfiguration:
  | { key: string; value: Promise<oidc.Configuration> }
  | undefined;

export function getAccessToken(request: NextRequest) {
  return request.cookies.get(accessTokenCookie)?.value ?? null;
}

export async function startLogin(request: NextRequest, scopes: string[]) {
  const configuration = await getConfiguration();
  const state = oidc.randomState();
  const verifier = oidc.randomPKCECodeVerifier();
  const nonce = oidc.randomNonce();
  const challenge = await oidc.calculatePKCECodeChallenge(verifier);
  const locale = getRequestedLocale(request);
  const registrationRequested =
    request.nextUrl.searchParams.get("screen") === "register";
  const authorizeUrl = oidc.buildAuthorizationUrl(configuration, {
    redirect_uri: getRedirectUri(request),
    response_type: "code",
    scope: scopes.join(" "),
    state,
    nonce,
    code_challenge: challenge,
    code_challenge_method: "S256",
    ...(locale ? { ui_locales: locale } : {}),
    ...(registrationRequested ? { screen_hint: "signup" } : {}),
  });
  const response = NextResponse.redirect(authorizeUrl);
  const cookieOptions = getTransientCookieOptions();

  response.cookies.set(stateCookie, state, cookieOptions);
  response.cookies.set(verifierCookie, verifier, cookieOptions);
  response.cookies.set(nonceCookie, nonce, cookieOptions);
  if (locale) {
    response.cookies.set(localeCookie, locale, cookieOptions);
  }

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
    const locale = normalizeLocale(request.cookies.get(localeCookie)?.value);
    const response = NextResponse.redirect(
      new URL(locale ? `/${locale}/onboarding` : "/en/onboarding", request.nextUrl.origin),
    );
    const sessionOptions = getSessionCookieOptions(tokens.expires_in ?? 3600);

    clearTransientCookies(response);
    response.cookies.set(accessTokenCookie, tokens.access_token, sessionOptions);

    if (tokens.refresh_token) {
      response.cookies.set(
        refreshTokenCookie,
        tokens.refresh_token,
        getSessionCookieOptions(30 * 24 * 3600),
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
  response.cookies.delete(accessTokenCookie);
  response.cookies.delete(refreshTokenCookie);
  response.cookies.delete(idTokenCookie);

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

function clearTransientCookies(response: NextResponse) {
  response.cookies.delete(stateCookie);
  response.cookies.delete(verifierCookie);
  response.cookies.delete(nonceCookie);
  response.cookies.delete(localeCookie);
}

function getRequestedLocale(request: NextRequest) {
  return normalizeLocale(request.nextUrl.searchParams.get("lang"));
}

function normalizeLocale(locale: string | null | undefined) {
  if (!locale) {
    return null;
  }

  const normalized = locale.trim().toLowerCase().split("-")[0];

  return supportedLocales.has(normalized) ? normalized : null;
}

function useSecureCookies() {
  return process.env.AUTH_COOKIE_SECURE === "true";
}
