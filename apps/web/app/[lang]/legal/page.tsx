import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { getDictionary, hasLocale } from "../../../i18n/dictionaries";

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

  return dictionary.legal.metadata;
}

export default async function LegalPage({ params }: LegalPageProps) {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  const { legal } = await getDictionary(lang);
  const homeHref = `/${lang}`;

  return (
    <main className="legalPage">
      <header className="legalHeader">
        <a className="brand" href={homeHref}>
          kinxter
        </a>
        <a className="legalBack" href={homeHref}>
          {legal.backHome}
        </a>
      </header>

      <section className="legalIntro">
        <p className="sectionEyebrow">{legal.eyebrow}</p>
        <h1>{legal.title}</h1>
        <p>{legal.introduction}</p>
      </section>

      <div className="legalDocuments">
        {legal.documents.map((document) => (
          <section id={document.id} key={document.id} className="legalDocument">
            <h2>{document.title}</h2>
            <p>{document.description}</p>
            <span>{legal.status}</span>
          </section>
        ))}
      </div>
    </main>
  );
}
