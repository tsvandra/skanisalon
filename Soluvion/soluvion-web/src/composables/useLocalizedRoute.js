// src/composables/useLocalizedRoute.js
// Név alapú, nyelvfüggő navigáció: to('customers') -> { name, params: { lang, slug } }
// A locale.value olvasása miatt a template-ben használt linkek nyelvváltáskor automatikusan frissülnek.
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { localizedLocation } from '@/router/routeSlugs';

export function useLocalizedRoute() {
  const router = useRouter();
  const route = useRoute();
  const { locale } = useI18n();

  // Route location objektum (router-link :to, router.push) az aktuális nyelven
  const to = (name, extra = {}) => {
    const isDevOrTestHost = typeof window !== 'undefined' && (
      window.location.hostname === 'localhost' ||
      window.location.hostname === '127.0.0.1' ||
      window.location.hostname.endsWith('.netlify.app')
    );

    const activeForceTenant = isDevOrTestHost
      ? (route?.query?.forceTenant || (typeof window !== 'undefined' ? sessionStorage.getItem('soluvion_force_tenant') : null))
      : null;

    const mergedQuery = { ...(extra.query || {}) };
    if (activeForceTenant && !mergedQuery.forceTenant) {
      mergedQuery.forceTenant = activeForceTenant;
    }

    return localizedLocation(name, locale.value, {
      ...extra,
      query: Object.keys(mergedQuery).length > 0 ? mergedQuery : undefined
    });
  };

  const go = (name, extra = {}) => router.push(to(name, extra));
  const replace = (name, extra = {}) => router.replace(to(name, extra));

  return { to, go, replace };
}

