import type { Dictionary } from "../../i18n/dictionaries";
import type { Locale } from "../../i18n/config";

type SiteFooterProps = {
  home: Dictionary["home"];
  lang: Locale;
  languagePath?: "" | "/legal";
};

export function SiteFooter({
  home,
  lang,
  languagePath = "",
}: SiteFooterProps) {
  const landingPrefix = languagePath ? `/${lang}` : "";
  const landingSectionHref = (section: string) =>
    `${landingPrefix}#${section}`;

  return (
    <footer
      className={`siteFooter${languagePath ? " siteFooterLegal" : ""}`}
    >
      <div className="footerTop">
        <div className="footerBrand">
          <a className="brand" href={landingSectionHref("start")}>
            kinxter
          </a>
          <p>{home.footer.description}</p>
          <span>{home.footer.values}</span>
        </div>

        <nav className="footerColumn" aria-label={home.footer.kinxterAriaLabel}>
          <h3>{home.footer.kinxterTitle}</h3>
          <a href={landingSectionHref("start")}>{home.footer.about}</a>
          <a href={landingSectionHref("join")}>{home.footer.communities}</a>
          <a href={landingSectionHref("safety")}>{home.footer.safety}</a>
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
            className={lang === "pl" ? "activeLanguage" : undefined}
            href={`/pl${languagePath}`}
            hrefLang="pl"
            aria-current={lang === "pl" ? "page" : undefined}
          >
            {home.footer.polish}
          </a>
          <a
            className={lang === "en" ? "activeLanguage" : undefined}
            href={`/en${languagePath}`}
            hrefLang="en"
            aria-current={lang === "en" ? "page" : undefined}
          >
            {home.footer.english}
          </a>
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
