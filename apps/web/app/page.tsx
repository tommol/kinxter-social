import { PanelHashNavigation } from "./panel-hash-navigation";

const loginHref = "/api/auth/login";

const categories = [
  "BDSM",
  "Shibari",
  "Fetysz",
  "Queer",
  "Edukacja",
  "Wydarzenia",
];

const safetyItems = [
  {
    number: "01",
    title: "Prywatność od początku",
    description:
      "Ty decydujesz, co pokazujesz i komu. Widoczność profilu pozostaje pod Twoją kontrolą.",
    icon: "lock",
  },
  {
    number: "02",
    title: "Zgoda i granice",
    description:
      "Jasne zasady kontaktu wspierają relacje oparte na komunikacji, szacunku i świadomej zgodzie.",
    icon: "handshake",
  },
  {
    number: "03",
    title: "Aktywna moderacja",
    description:
      "Zgłoszenia i blokowanie pomagają chronić bezpieczną przestrzeń dla każdej osoby 18+.",
    icon: "people",
  },
] as const;

const steps = [
  {
    number: "1",
    title: "Utwórz profil",
    description: "Udostępnij tylko tyle, ile chcesz.",
  },
  {
    number: "2",
    title: "Odkrywaj",
    description: "Znajduj ludzi i społeczności we własnym tempie.",
  },
  {
    number: "3",
    title: "Buduj relacje",
    description: "Rozmawiaj w przestrzeni opartej na zgodzie.",
  },
];

export default function Home() {
  return (
    <main className="landingShell">
      <PanelHashNavigation />

      <header className="siteHeader">
        <a className="brand" href="#start" aria-label="Kinxter — strona główna">
          kinxter
        </a>

        <nav className="mainNav" aria-label="Główna nawigacja">
          <a className="navLink navLinkDiscover" href="#start">
            Odkrywaj
          </a>
          <a className="navLink navLinkCommunities" href="#join">
            Społeczności
          </a>
          <a className="navLink navLinkSafety" href="#safety">
            Bezpieczeństwo
          </a>
        </nav>

        <div className="headerActions">
          <a className="loginLink" href={loginHref}>
            Zaloguj się
          </a>
          <a className="button buttonLight buttonCompact" href={loginHref}>
            Dołącz teraz
          </a>
        </div>
      </header>

      <div className="horizontalViewport">
        <section id="start" className="panel hero" aria-labelledby="hero-title">
          <div className="heroGlow" aria-hidden="true" />

          <div className="heroStage">
            <div className="heroCopy">
              <p className="heroEyebrow">Przestrzeń dla dorosłych 18+</p>
              <h1 id="hero-title">
                Znajdź ludzi, przy których możesz być sobą.
              </h1>
              <p className="heroSummary">
                Dyskretna przestrzeń do poznawania ludzi, odkrywania siebie i
                budowania społeczności bez oceniania.
              </p>
              <div className="heroActions">
                <a className="button buttonDark" href={loginHref}>
                  Dołącz dyskretnie
                </a>
                <a className="button buttonLight" href="#safety">
                  Jak dbamy o bezpieczeństwo
                </a>
              </div>
            </div>

            <CommunityPreview />
          </div>

          <div className="trustBar" aria-label="Najważniejsze zasady Kinxter">
            <div>
              <Icon name="lock" />
              <span>Prywatność od początku</span>
            </div>
            <div>
              <Icon name="handshake" />
              <span>Zgoda i granice</span>
            </div>
            <div>
              <Icon name="people" />
              <span>Moderowana społeczność</span>
            </div>
          </div>

          <PanelNavigation current="01" next="#join" nextLabel="Społeczności" />
        </section>

        <section id="join" className="panel joinPanel" aria-labelledby="join-title">
          <div className="joinGlow" aria-hidden="true" />

          <div className="panelContent joinContent">
            <div className="joinIntro">
              <p className="sectionEyebrow">Zacznij we własnym tempie</p>
              <h2 id="join-title">Od ciekawości do społeczności.</h2>
              <p>
                Nie musisz wiedzieć wszystkiego na początku. Możesz obserwować,
                poznawać ludzi i odkrywać Kinxter po swojemu.
              </p>
              <a className="button buttonOrange" href={loginHref}>
                Utwórz konto
              </a>
            </div>

            <div className="joinDetails">
              <div className="compactSteps">
                {steps.map((step) => (
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
                <p>Odkrywaj między innymi</p>
                <div className="communityCloud" aria-label="Przykładowe społeczności">
                  {categories.map((category) => (
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
            previous="#start"
            previousLabel="Odkrywaj"
            next="#safety"
            nextLabel="Bezpieczeństwo"
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
                <p className="sectionEyebrow">Bezpieczeństwo w centrum</p>
                <h2 id="safety-title">
                  Technologia powinna chronić Twoje granice.
                </h2>
              </div>
              <p>
                Prywatność, świadoma zgoda i skuteczna moderacja wpływają na
                każdą decyzję projektową w Kinxter.
              </p>
            </div>

            <div className="safetyGrid">
              {safetyItems.map((item) => (
                <article key={item.number} className="safetyCard">
                  <div className="cardTopline">
                    <span className="cardIcon">
                      <Icon name={item.icon} />
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
              <p>
                Bez presji. Bez oceniania. Z pełną kontrolą nad własną
                obecnością.
              </p>
            </div>
          </div>

          <PanelNavigation
            current="03"
            previous="#join"
            previousLabel="Społeczności"
            dark
          />
        </section>
      </div>

      <SiteFooter />
    </main>
  );
}

function CommunityPreview() {
  return (
    <article className="communityPreview" aria-label="Podgląd społeczności">
      <div className="previewHeader">
        <span className="previewIcon" aria-hidden="true">
          <Icon name="people" />
        </span>
        <div>
          <p>Odkrywaj społeczności</p>
          <span>Znajdź przestrzeń dla siebie</span>
        </div>
      </div>

      <div className="categoryChips">
        {categories.map((category) => (
          <span key={category}>{category}</span>
        ))}
      </div>

      <div className="previewTiles" aria-hidden="true">
        <span className="previewTile tileLeather" />
        <span className="previewTile tileRope" />
        <span className="previewTile tileLight" />
        <span className="previewTile tileLacquer" />
      </div>

      <div className="previewFooter">
        <div className="avatarStack" aria-label="Aktywni członkowie">
          <span>MK</span>
          <span>AS</span>
          <span>KL</span>
          <span>+128</span>
        </div>
        <span className="onlineLabel">
          <i aria-hidden="true" /> aktywni teraz
        </span>
      </div>
    </article>
  );
}

type PanelNavigationProps = {
  current: string;
  previous?: string;
  previousLabel?: string;
  next?: string;
  nextLabel?: string;
  dark?: boolean;
};

function PanelNavigation({
  current,
  previous,
  previousLabel,
  next,
  nextLabel,
  dark = false,
}: PanelNavigationProps) {
  return (
    <nav
      className={`panelNavigation${dark ? " panelNavigationDark" : ""}`}
      aria-label="Nawigacja między sekcjami"
    >
      <span>{current} / 03</span>
      <div>
        {previous ? (
          <a href={previous} aria-label={`Poprzednia sekcja: ${previousLabel}`}>
            ←
          </a>
        ) : null}
        {next ? (
          <a href={next} aria-label={`Następna sekcja: ${nextLabel}`}>
            →
          </a>
        ) : null}
      </div>
    </nav>
  );
}

function SiteFooter() {
  return (
    <footer className="siteFooter">
      <div className="footerTop">
        <div className="footerBrand">
          <a className="brand" href="#start">
            kinxter
          </a>
          <p>
            Prywatna platforma społecznościowa dla pełnoletnich osób ze
            społeczności kink i fetysz.
          </p>
          <span>18+ · Szacunek · Zgoda · Prywatność</span>
        </div>

        <nav className="footerColumn" aria-label="Kinxter">
          <h3>Kinxter</h3>
          <a href="#start">O platformie</a>
          <a href="#join">Społeczności</a>
          <a href="#safety">Bezpieczeństwo</a>
          <a href="mailto:hello@kinxter.com">Kontakt</a>
        </nav>

        <nav className="footerColumn" aria-label="Dokumenty prawne">
          <h3>Dokumenty</h3>
          <a href="/legal#privacy">Polityka prywatności</a>
          <a href="/legal#terms">Warunki korzystania</a>
          <a href="/legal#cookies">Polityka cookies</a>
          <a href="/legal#community">Zasady społeczności</a>
        </nav>

        <div className="footerColumn languageColumn">
          <h3>Język</h3>
          <a className="activeLanguage" href="/" aria-current="page">
            Polski
          </a>
          <span>
            English <small>wkrótce</small>
          </span>
          <span>
            Deutsch <small>wkrótce</small>
          </span>
        </div>
      </div>

      <div className="footerBottom">
        <span>© {new Date().getFullYear()} Kinxter</span>
        <span>Platforma wyłącznie dla osób pełnoletnich.</span>
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
