// src/composables/useLocalizedRoute.js
// Név alapú, nyelvfüggő navigáció: to('customers') -> { name, params: { lang, slug } }
// A locale.value olvasása miatt a template-ben használt linkek nyelvváltáskor automatikusan frissülnek.
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { localizedLocation } from '@/router/routeSlugs';

export function useLocalizedRoute() {
  const router = useRouter();
  const { locale } = useI18n();

  // Route location objektum (router-link :to, router.push) az aktuális nyelven
  const to = (name, extra = {}) => localizedLocation(name, locale.value, extra);

  const go = (name, extra = {}) => router.push(to(name, extra));
  const replace = (name, extra = {}) => router.replace(to(name, extra));

  return { to, go, replace };
}

