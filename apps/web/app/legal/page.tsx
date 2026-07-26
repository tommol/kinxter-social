const documents = [
  {
    id: "privacy",
    title: "Polityka prywatności",
    description:
      "Dokument opisze zakres przetwarzanych danych, podstawy prawne, okresy przechowywania oraz prawa użytkowników.",
  },
  {
    id: "terms",
    title: "Warunki korzystania",
    description:
      "Dokument określi zasady korzystania z Kinxter, wymagania wiekowe, odpowiedzialność użytkowników i reguły konta.",
  },
  {
    id: "cookies",
    title: "Polityka cookies",
    description:
      "Dokument wyjaśni, które pliki cookies są niezbędne oraz w jaki sposób będą obsługiwane opcjonalne zgody.",
  },
  {
    id: "community",
    title: "Zasady społeczności",
    description:
      "Dokument opisze standardy świadomej zgody, prywatności, moderacji, zgłoszeń oraz treści niedozwolonych.",
  },
];

export default function LegalPage() {
  return (
    <main className="legalPage">
      <header className="legalHeader">
        <a className="brand" href="/">
          kinxter
        </a>
        <a className="legalBack" href="/">
          Powrót na stronę główną
        </a>
      </header>

      <section className="legalIntro">
        <p className="sectionEyebrow">Centrum dokumentów</p>
        <h1>Dokumenty i zasady Kinxter</h1>
        <p>
          Ta strona przygotowuje strukturę pod finalne dokumenty. Treści prawne
          wymagają jeszcze uzupełnienia i zatwierdzenia przed uruchomieniem
          platformy.
        </p>
      </section>

      <div className="legalDocuments">
        {documents.map((document) => (
          <section id={document.id} key={document.id} className="legalDocument">
            <h2>{document.title}</h2>
            <p>{document.description}</p>
            <span>Dokument w przygotowaniu</span>
          </section>
        ))}
      </div>
    </main>
  );
}
