import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { SiteFooter } from "../site-footer";
import { getDictionary, hasLocale } from "../../../i18n/dictionaries";
import { locales } from "../../../i18n/config";

type LegalPageProps = {
  params: Promise<{ lang: string }>;
};

export async function generateMetadata({
  params,
}: LegalPageProps): Promise<Metadata> {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  const dictionary = await getDictionary(lang);

  return {
    ...dictionary.legal.metadata,
    alternates: {
      canonical: `/${lang}/legal`,
      languages: Object.fromEntries(
        locales.map((locale) => [locale, `/${locale}/legal`]),
      ),
    },
  };
}

export default async function LegalPage({ params }: LegalPageProps) {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  const { home, legal } = await getDictionary(lang);
  const homeHref = `/${lang}`;

  return (
    <div className="legalShell">
      <header className="legalHeader">
        <a className="brand" href={homeHref}>
          kinxter
        </a>
        <a className="legalBack" href={homeHref}>
          {legal.backHome}
        </a>
      </header>

      <main className="legalPage">
        <div className="legalLayout">
          <aside className="legalSidebar">
            <p>{legal.navigationTitle}</p>
            <nav aria-label={legal.navigationAriaLabel}>
              {legal.documents.map((document, index) => (
                <a href={`#${document.id}`} key={document.id}>
                  <span>{String(index + 1).padStart(2, "0")}</span>
                  {document.title}
                </a>
              ))}
            </nav>
          </aside>

          <div className="legalMain">
            <section className="legalIntro">
              <p className="sectionEyebrow">{legal.eyebrow}</p>
              <h1>{legal.title}</h1>
              <p>{legal.introduction}</p>
            </section>

            <div className="legalDocuments">
              {legal.documents.map((document) => (
                <section
                  id={document.id}
                  key={document.id}
                  className="legalDocument"
                >
                  <h2>{document.title}</h2>
                  <p>{document.description}</p>
                  <span>{legal.status}</span>
                </section>
              ))}
            </div>
          </div>
        </div>
      </main>

      <SiteFooter home={home} lang={lang} languagePath="/legal" />
    </div>
  );
}
