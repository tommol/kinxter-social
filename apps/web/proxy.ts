import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

import { defaultLocale, locales } from "./i18n/config";
import type { Locale } from "./i18n/config";

function pathnameHasLocale(pathname: string) {
  return locales.some(
    (locale) => pathname === `/${locale}` || pathname.startsWith(`/${locale}/`),
  );
}

function getPreferredLocale(request: NextRequest): Locale {
  const acceptLanguage = request.headers.get("accept-language");

  if (!acceptLanguage) {
    return defaultLocale;
  }

  const requestedLanguages = acceptLanguage
    .split(",")
    .map((entry) => {
      const [tag, qualityValue] = entry.trim().toLowerCase().split(";q=");
      const quality = qualityValue ? Number.parseFloat(qualityValue) : 1;

      return { tag, quality: Number.isNaN(quality) ? 0 : quality };
    })
    .sort((left, right) => right.quality - left.quality);

  for (const { tag } of requestedLanguages) {
    const locale = locales.find(
      (supportedLocale) =>
        tag === supportedLocale ||
        tag.startsWith(`${supportedLocale}-`) ||
        supportedLocale.startsWith(`${tag}-`),
    );

    if (locale) {
      return locale;
    }
  }

  return defaultLocale;
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (pathnameHasLocale(pathname)) {
    return NextResponse.next();
  }

  const locale = getPreferredLocale(request);
  const redirectUrl = request.nextUrl.clone();
  redirectUrl.pathname = `/${locale}${pathname === "/" ? "" : pathname}`;

  return NextResponse.redirect(redirectUrl);
}

export const config = {
  matcher: ["/((?!api|_next|.*\\..*).*)"],
};
