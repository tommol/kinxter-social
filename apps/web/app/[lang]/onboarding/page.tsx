import { notFound } from "next/navigation";
import { hasLocale } from "../../../i18n/dictionaries";
import { OnboardingWizard } from "./wizard";

export default async function OnboardingPage({ params }: { params: Promise<{ lang: string }> }) {
  const { lang } = await params;
  if (!hasLocale(lang)) notFound();
  return <OnboardingWizard lang={lang as "pl" | "en"} />;
}
