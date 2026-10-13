import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import ServicesView from '../views/ServicesView.vue'
import GalleryView from '../views/GalleryView.vue'
import ContactView from '../views/ContactView.vue'
import LoginView from '../views/LoginView.vue'
import SettingsView from '../views/SettingsView.vue'
import { useCompanyStore } from '@/stores/companyStore'
import { useTranslationStore } from '@/stores/translationStore'
import i18n from '@/i18n'
import {
  DEFAULT_LANG,
  LANG_PATTERN,
  slugFor,
  slugVariants,
  legacyPath,
  localizedLocation,
  onSlugsChanged,
} from './routeSlugs'

import { useAuthStore } from '@/stores/authStore'

// Az URL szerkezete: /:lang/:slug  (pl. /hu/ugyfelek, /sk/zakaznici, /en/customers)
// A slug a nyelvi fájlok "routes.<név>" kulcsából jön (lásd routeSlugs.js).
const pages = [
  { name: 'services', component: ServicesView },
  { name: 'gallery', component: GalleryView },
  { name: 'contact', component: ContactView },
  { name: 'booking', component: () => import('../views/BookingView.vue') },
  { name: 'login', component: LoginView },
  { name: 'settings', component: SettingsView, meta: { requiresAuth: true } },
  { name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { requiresAuth: true } },
  { name: 'orders', component: () => import('../components/admin/calendar/CalendarGrid.vue'), meta: { requiresAuth: true } },
  { name: 'customers', component: () => import('../views/CustomersView.vue'), meta: { requiresAuth: true } },
  { name: 'inventory', component: () => import('../views/Inventory/InventoryView.vue'), meta: { requiresAuth: true } },
]

// Az alapértelmezett nyelv: mentett választás -> cég alapnyelve -> hu
const getPreferredLang = () => {
  const companyDefault = useCompanyStore().company?.defaultLanguage;
  return localStorage.getItem('user-locale') || companyDefault || DEFAULT_LANG;
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: (to) => localizedLocation('home', getPreferredLang(), { query: to.query, hash: to.hash }) },
    { path: `/:lang(${LANG_PATTERN})`, name: 'home', component: HomeView },

    // Régi, nyelvi elotag nélküli (magyar) linkek továbbra is muködnek: /ugyfelek -> /<nyelv>/<slug>
    ...pages.map((page) => ({
      path: legacyPath(page.name),
      name: `legacy-${page.name}`,
      redirect: (to) => localizedLocation(page.name, getPreferredLang(), { query: to.query, hash: to.hash }),
    })),
  ],
})

// A route-ok (újra)regisztrálása az aktuálisan betöltött nyelvek slug-jaival.
// Azonos névvel újra hozzáadott route felülírja a régit, így ez bármikor hívható.
const registerPages = () => {
  pages.forEach(({ name, ...record }) => {
    router.addRoute({
      ...record,
      name,
      path: `/:lang(${LANG_PATTERN})/:slug(${slugVariants(name).join('|')})`,
    });
  });
}

registerPages();
onSlugsChanged(registerPages);

// Catch-all: ismeretlen URL -> nyitóoldal a preferált nyelven
router.addRoute({
  path: '/:pathMatch(.*)*',
  name: 'not-found',
  redirect: () => localizedLocation('home', getPreferredLang()),
});

router.beforeEach(async (to) => {
  const lang = to.params.lang;
  if (!lang) return true;

  const companyStore = useCompanyStore();
  const translationStore = useTranslationStore();
  const company = companyStore.company;

  if (company && !translationStore.activeCompanyId) {
    translationStore.initCompany(company.id, company.defaultLanguage);
  }

  // Ha már ismerjük a cég nyelveit, az ismeretlen nyelvkódot a preferált nyelvre irányítjuk
  const knownLanguages = translationStore.languages;
  if (knownLanguages.length > 0 && !knownLanguages.some((l) => l.languageCode === lang)) {
    const fallback = company?.defaultLanguage || DEFAULT_LANG;
    return localizedLocation(to.name, fallback, { query: to.query, hash: to.hash, replace: true });
  }

  // Az URL a nyelv forrása: ha eltér az aktuálistól, betöltjük a nyelvet
  if (lang !== i18n.global.locale.value) {
    await translationStore.setLanguage(lang);
  }

  // A slug legyen az adott nyelvhez tartozó (pl. /sk/customers -> /sk/zakaznici)
  if (to.params.slug !== undefined) {
    const expected = slugFor(to.name, lang);
    if (to.params.slug !== expected) {
      return localizedLocation(to.name, lang, { query: to.query, hash: to.hash, replace: true });
    }
  }

  // Admin útvonalak védelme (Autentikáció & Tenant egyezőség ellenőrzése)
  if (to.meta?.requiresAuth) {
    const authStore = useAuthStore();
    authStore.initAuth();

    const currentCompanyId = companyStore.company?.id;
    const isTenantMismatch = currentCompanyId && authStore.companyId && authStore.companyId !== currentCompanyId;

    if (!authStore.isAuthenticated || isTenantMismatch) {
      if (isTenantMismatch) {
        console.warn(`⚠️ Eltérő szalon admin jog észlelve. Átirányítás a bejelentkezéshez.`);
        authStore.logout();
      }
      return localizedLocation('login', lang, {
        query: {
          ...to.query,
          error: isTenantMismatch ? 'tenant_mismatch' : undefined,
          redirect: to.fullPath
        },
        hash: to.hash,
        replace: true,
      });
    }
  }

  return true;
})

export default router


