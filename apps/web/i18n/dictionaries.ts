import "server-only";

import type polishDictionary from "./dictionaries/pl.json";
import { locales } from "./config";
import type { Locale } from "./config";

export type Dictionary = typeof polishDictionary;

const dictionaries: Record<Locale, () => Promise<Dictionary>> = {
  pl: () => import("./dictionaries/pl.json").then((module) => module.default),
  en: () => import("./dictionaries/en.json").then((module) => module.default),
};

export function hasLocale(locale: string): locale is Locale {
  return locales.some((supportedLocale) => supportedLocale === locale);
}

export function getDictionary(locale: Locale) {
  return dictionaries[locale]();
}
