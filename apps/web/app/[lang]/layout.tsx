import { notFound } from "next/navigation";
import type { ReactNode } from "react";

import "../globals.css";
import { hasLocale } from "../../i18n/dictionaries";
import { locales } from "../../i18n/config";

type LocaleLayoutProps = {
  children: ReactNode;
  params: Promise<{ lang: string }>;
};

export function generateStaticParams() {
  return locales.map((lang) => ({ lang }));
}

export default async function RootLayout({
  children,
  params,
}: LocaleLayoutProps) {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  return (
    <html lang={lang}>
      <body>{children}</body>
    </html>
  );
}
