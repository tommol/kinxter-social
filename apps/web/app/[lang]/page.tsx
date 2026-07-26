import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { PanelHashNavigation } from "../panel-hash-navigation";
import { getDictionary, hasLocale } from "../../i18n/dictionaries";
import type { Dictionary } from "../../i18n/dictionaries";

const loginHref = "/api/auth/login";

type HomePageProps = {
  params: Promise<{ lang: string }>;
};

type HomeDictionary = Dictionary["home"];

export async function generateMetadata({
  params,
}: HomePageProps): Promise<Metadata> {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  const dictionary = await getDictionary(lang);

  return dictionary.home.metadata;
}

export default async function Home({ params }: HomePageProps) {
  const { lang } = await params;

  if (!hasLocale(lang)) {
    notFound();
  }

  const { home } = await getDictionary(lang);
  const safetyIcons: IconName[] = ["lock", "handshake", "people"];

  return (
    <main className="landingShell">
      <PanelHashNavigation />

      <header className="siteHeader">
        <a className="brand" href="#start" aria-label={home.header.brandAriaLabel}>
          kinxter
        </a>

        <nav className="mainNav" aria-label={home.header.navigationAriaLabel}>
          <a className="navLink navLinkDiscover" href="#start">
            {home.header.discover}
          </a>
          <a className="navLink navLinkCommunities" href="#join">
            {home.header.communities}
          </a>
          <a className="navLink navLinkSafety" href="#safety">
            {home.header.safety}
          </a>
        </nav>

        <div className="headerActions">
          <a className="loginLink" href={loginHref}>
            {home.header.login}
          </a>
          <a className="button buttonLight buttonCompact" href={loginHref}>
            {home.header.join}
          </a>
        </div>
      </header>

      <div className="horizontalViewport">
        <section id="start" className="panel hero" aria-labelledby="hero-title">
          <div className="heroGlow" aria-hidden="true" />

          <div className="heroStage">
            <div className="heroCopy">
              <p className="heroEyebrow">{home.hero.eyebrow}</p>
              <h1 id="hero-title">{home.hero.title}</h1>
              <p className="heroSummary">{home.hero.summary}</p>
              <div className="heroActions">
                <a className="button buttonDark" href={loginHref}>
                  {home.hero.join}
                </a>
                <a className="button buttonLight" href="#safety">
                  {home.hero.safety}
                </a>
              </div>
            </div>
          </div>

          <div className="trustBar" aria-label={home.trust.ariaLabel}>
            <div>
              <Icon name="lock" />
              <span>{home.trust.privacy}</span>
            </div>
            <div>
              <Icon name="handshake" />
              <span>{home.trust.consent}</span>
            </div>
            <div>
              <Icon name="people" />
              <span>{home.trust.moderation}</span>
            </div>
          </div>

          <PanelNavigation
            current="01"
            labels={home.panelNavigation}
            next="#join"
            nextLabel={home.header.communities}
          />
        </section>

        <section id="join" className="panel joinPanel" aria-labelledby="join-title">
          <div className="joinGlow" aria-hidden="true" />

          <div className="panelContent joinContent">
            <div className="joinIntro">
              <p className="sectionEyebrow">{home.join.eyebrow}</p>
              <h2 id="join-title">{home.join.title}</h2>
              <p>{home.join.summary}</p>
              <a className="button buttonOrange" href={loginHref}>
                {home.join.createAccount}
              </a>
            </div>

            <div className="joinDetails">
              <div className="compactSteps">
                {home.join.steps.map((step) => (
                  <article className="compactStep" key={step.number}>
                    <span>{step.number}</span>
                    <div>
                      <h3>{step.title}</h3>
                      <p>{step.description}</p>
                    </div>
                  </article>
                ))}
              </div>

              <div className="categoryArea">
                <p>{home.join.categoryIntro}</p>
                <div
                  className="communityCloud"
                  aria-label={home.join.categoriesAriaLabel}
                >
                  {home.join.categories.map((category) => (
                    <span className="cloudItem" key={category}>
                      <i aria-hidden="true" />
                      {category}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          <PanelNavigation
            current="02"
            labels={home.panelNavigation}
            previous="#start"
            previousLabel={home.header.discover}
            next="#safety"
            nextLabel={home.header.safety}
          />
        </section>

        <section
          id="safety"
          className="panel safetyPanel"
          aria-labelledby="safety-title"
        >
          <div className="panelContent safetyContent">
            <div className="sectionHeading safetyHeading">
              <div>
                <p className="sectionEyebrow">{home.safety.eyebrow}</p>
                <h2 id="safety-title">{home.safety.title}</h2>
              </div>
              <p>{home.safety.summary}</p>
            </div>

            <div className="safetyGrid">
              {home.safety.items.map((item, index) => (
                <article key={item.number} className="safetyCard">
                  <div className="cardTopline">
                    <span className="cardIcon">
                      <Icon name={safetyIcons[index]} />
                    </span>
                    <span>{item.number}</span>
                  </div>
                  <h3>{item.title}</h3>
                  <p>{item.description}</p>
                </article>
              ))}
            </div>

            <div className="safetyStatement">
              <span>18+</span>
              <p>{home.safety.statement}</p>
            </div>
          </div>

          <PanelNavigation
            current="03"
            labels={home.panelNavigation}
            previous="#join"
            previousLabel={home.header.communities}
            dark
          />
        </section>
      </div>

      <SiteFooter home={home} lang={lang} />
    </main>
  );
}

type PanelNavigationProps = {
  current: string;
  labels: HomeDictionary["panelNavigation"];
  previous?: string;
  previousLabel?: string;
  next?: string;
  nextLabel?: string;
  dark?: boolean;
};

function PanelNavigation({
  current,
  labels,
  previous,
  previousLabel,
  next,
  nextLabel,
  dark = false,
}: PanelNavigationProps) {
  return (
    <nav
      className={`panelNavigation${dark ? " panelNavigationDark" : ""}`}
      aria-label={labels.ariaLabel}
    >
      <span>{current} / 03</span>
      <div>
        {previous ? (
          <a href={previous} aria-label={`${labels.previous}: ${previousLabel}`}>
            ←
          </a>
        ) : null}
        {next ? (
          <a href={next} aria-label={`${labels.next}: ${nextLabel}`}>
            →
          </a>
        ) : null}
      </div>
    </nav>
  );
}

function SiteFooter({
  home,
  lang,
}: {
  home: HomeDictionary;
  lang: string;
}) {
  return (
    <footer className="siteFooter">
      <div className="footerTop">
        <div className="footerBrand">
          <a className="brand" href="#start">
            kinxter
          </a>
          <p>{home.footer.description}</p>
          <span>{home.footer.values}</span>
        </div>

        <nav className="footerColumn" aria-label={home.footer.kinxterAriaLabel}>
          <h3>{home.footer.kinxterTitle}</h3>
          <a href="#start">{home.footer.about}</a>
          <a href="#join">{home.footer.communities}</a>
          <a href="#safety">{home.footer.safety}</a>
          <a href="mailto:hello@kinxter.com">{home.footer.contact}</a>
        </nav>

        <nav className="footerColumn" aria-label={home.footer.documentsAriaLabel}>
          <h3>{home.footer.documentsTitle}</h3>
          <a href={`/${lang}/legal#privacy`}>{home.footer.privacy}</a>
          <a href={`/${lang}/legal#terms`}>{home.footer.terms}</a>
          <a href={`/${lang}/legal#cookies`}>{home.footer.cookies}</a>
          <a href={`/${lang}/legal#community`}>{home.footer.communityRules}</a>
        </nav>

        <div className="footerColumn languageColumn">
          <h3>{home.footer.language}</h3>
          <a
            className="activeLanguage"
            href={`/${lang}`}
            hrefLang={lang}
            aria-current="page"
          >
            {home.footer.polish}
          </a>
          <span>
            {home.footer.english} <small>{home.footer.soon}</small>
          </span>
          <span>
            {home.footer.german} <small>{home.footer.soon}</small>
          </span>
        </div>
      </div>

      <div className="footerBottom">
        <span>© {new Date().getFullYear()} Kinxter</span>
        <span>{home.footer.adultsOnly}</span>
      </div>
    </footer>
  );
}

type IconName = "lock" | "handshake" | "people";

function Icon({ name }: { name: IconName }) {
  if (name === "lock") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M7 10V7a5 5 0 0 1 10 0v3" />
        <rect x="5" y="10" width="14" height="11" rx="3" />
        <path d="M12 14v3" />
      </svg>
    );
  }

  if (name === "handshake") {
    return (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="m8.5 12.5 3 3a2.1 2.1 0 0 0 3 0l4.2-4.2" />
        <path d="m15.7 8.3-2-2a2.4 2.4 0 0 0-3.4 0L6 10.6" />
        <path d="m3 8 3.5-3.5 4 4L7 12zM21 8l-3.5-3.5-3 3 3.5 4.5z" />
        <path d="m7 15 1 1a2 2 0 0 0 2.8 0" />
      </svg>
    );
  }

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <circle cx="9" cy="8" r="3" />
      <path d="M3.5 19v-1.5A4.5 4.5 0 0 1 8 13h2a4.5 4.5 0 0 1 4.5 4.5V19" />
      <circle cx="17.5" cy="9" r="2.5" />
      <path d="M15 14h2.5a3.5 3.5 0 0 1 3.5 3.5V19" />
    </svg>
  );
}
