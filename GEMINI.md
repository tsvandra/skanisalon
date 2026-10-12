# GEMINI.md - Soluvion / BeeMagic SaaS Rendszerkontextus & AI Szabályzat

> **Utolsó frissítés:** 2026. október 12.  
> **Projekt:** Soluvion (átnevezés alatt: BeeMagic Salon)  
> **Státusz:** MVP Stabilizálva, Staging és Production környezetek élnek.  
> **Elsődleges dokumentáció:** `Soluvion/DevDoc/` mappa (`01_Architecture_and_DevOps.md`, `02_Product_Specification.md`, `03_Developer_Rulebook.md`, `04_Changelog.md`, `05_Master_Roadmap.md`).

---

## 1. A Rendszer Célja és Architektúrája

A **Soluvion / BeeMagic Salon** egy modern, felhőalapú **Multi-Tenant SaaS platform**, amelyet prémium fodrászatok, kozmetikák és szépségszalonok számára fejlesztünk.

### 1.1. Backend (.NET 10 Web API)
* **Keretrendszer:** .NET 10 (C# 14).
* **Adatbázis & ORM:** PostgreSQL 16+ Entity Framework Core-ral (Code-First megközelítés).
* **Auto-Migráció:** Az alkalmazás induláskor automatikusan lefuttatja a hiányzó migrációkat (`db.Database.Migrate()`).
* **Multi-Tenancy:** Minden entitás rendelkezik `CompanyId`-val. A kérésekből a `TenantResolutionMiddleware` állítja elő az `ITenantContext`-et.
* **Autentikáció:** Stateless JWT Bearer token (HMAC-SHA512). A lejárati idő konfigurálható (`AppSettings:TokenExpirationDays`, alapértelmezett: 14 nap).
* **Képtárolás:** CloudinaryDotNet (`IImageService`).
* **Mesterséges Intelligencia:** 
  - Google Gemini 2.0 Flash (`Gemini:ApiKey`)
  - OpenAI Vision / Chat (`OpenAI:ApiKey`, modell: `gpt-5.6-luna` / `gpt-4o-mini`)
  - Funkciók: Szállítólevél (Dodací list) OCR beolvasás, termékfelismerés fotóról, dinamikus többnyelvűsítés.
* **Hosting:** Railway (konténeres Docker környezet).

### 1.2. Frontend (Vue 3 SPA)
* **Keretrendszer:** Vue 3 (Composition API, `<script setup>` szintaxis).
* **Build Tool:** Vite.
* **UI Könyvtár:** PrimeVue v4 (Aura preset, `darkModeSelector: '.dark'`) és PrimeIcons.
* **CSS:** Tailwind CSS v4.
* **Állapotkezelés:** Pinia store-ok (`authStore.js`, `companyStore.js`, `translationStore.js`, `appointmentStore.js`).
* **Lokalizáció:** Vue-i18n 11. 3 alapnyelv támogatott: Magyar (`hu`), Szlovák (`sk`), Angol (`en`).
* **Hálózat:** Axios interceptorokkal (`src/services/api.js`).
* **Hosting:** Netlify (külön production és branch-deploy staging környezettel).

---

## 2. Kritikus Fejlesztési Szabályok (Rules for AI)

### 2.1. Képek és Hálózati Forgalom (Railway Egress Védelem)
* **SZIGORÚ SZABÁLY:** Soha ne tölts fel nagyfelbontású nyers képet közvetlenül a szerverre!
* Minden képet (termékfotó, szállítólevél oldal, galéria, logó) a feltöltés előtt kötelező kliensoldalon átméretezni és tömöríteni a `src/utils/imageCompressor.js` segédprogrammal (`compressImage` vagy `compressImages`).
* Célméret: max. 1600–1800px, minőség: 0.8–0.85, tipikus fájlméret: 150–350 KB (a telefonos 10-15 MB helyett).

### 2.2. Autentikáció és Állapotkezelés
* Be- és kijelentkezéskor a központi `authStore.js` használandó (`authStore.setToken(...)`, `authStore.logout()`).
* **Tilos a `window.location.reload()` hack!** A reaktív store és a Vue Router gondoskodik a zökkenőmentes navigációról.

### 2.3. Dinamikus SaaS Téma & Kontraszt
* A rendszer "White-Label" megjelenést biztosít: a tenant színeit a `companyStore.applyTheme()` injektálja a `:root`-ba.
* A PrimeVue `darkModeSelector: '.dark'` beállítással van ellátva, ami szinkronban van a `document.documentElement` `.dark` vagy `.light` osztályával (függetlenül az OS natív preferenciájától).
* Új komponens készítésekor mindig a Tailwind szemantikus színváltozóit használd: `bg-background`, `bg-surface`, `text-text`, `text-text-muted`, `border-text/10`, `text-primary`. Kerüld a hardkódolt fix fekete/fehér háttereket!

### 2.4. Git és Telepítési Munkafolyamat (CI/CD)
* **Közvetlenül a `main` ágra fejleszteni szigorúan tilos!**
* Mindig témás feature branch-en dolgozunk (`feature/...`, `fix/...`), amit tesztelés után a `develop` ágra merge-elünk.
* A `develop` ágra érkező push automatikusan frissíti a **Staging** környezetet (Railway Staging DB/API + Netlify develop preview).
* Az élesítés kizárólag a `develop` ág `main`-re történő merge-elésével történik (Production release).

### 2.5. Többnyelvűsítés (i18n)
* Tilos keményen beégetett szöveget hagyni a template-ekben és szkriptekben!
* Minden új felirat a `src/locales/` megfelelő JSON fájljaiba kerüljön, a template-ben `$t('kulcs')` hívással.
* A 3 hivatalos alapnyelv: `hu`, `sk`, `en`.

---

## 3. Kulcsfontosságú Könyvtárstruktúra

```
Soluvion/
├── Soluvion.API/              # ASP.NET Core 10 Web API
│   ├── Controllers/          # REST végpontok (Inventory, Products, Service, Auth, stb.)
│   ├── Data/                 # AppDbContext, Entitás konfigurációk, DbSeeder
│   ├── DTOs/                 # Bemeneti és kimeneti adatmodellek validációval
│   ├── Interfaces/           # Szolgáltatás interfészek
│   ├── Middleware/           # TenantResolution, GlobalExceptionHandler
│   ├── Migrations/           # EF Core migrációs fájlok
│   └── Services/             # Üzleti logika (Auth, Inventory, ProductAi, Image, stb.)
├── Soluvion.Domain/           # Adatbázis modellek (Company, Product, User, Appointment...)
├── soluvion-web/              # Vue 3 SPA frontend
│   ├── src/
│   │   ├── assets/           # main.css (Tailwind v4 theme és layer beállítások)
│   │   ├── components/       # Újrafelhasználható UI komponensek (admin, booking...)
│   │   ├── composables/      # useAutoSaveQueue, useDragAndDrop, useTranslation...
│   │   ├── locales/          # hu/, sk/, en/ i18n nyelvi csomagok
│   │   ├── router/           # Vue Router útvonalak és nyelvi slugok
│   │   ├── services/         # Axios API hívások (inventoryApi, productApi...)
│   │   ├── stores/           # Pinia store-ok (authStore, companyStore...)
│   │   ├── utils/            # imageCompressor, localizedText, jwt...
│   │   └── views/            # Oldalak (HomeView, InventoryView, DashboardView...)
├── DevDoc/                    # Hivatalos architektúra és roadmap dokumentumok
└── soluvion-db-backup/        # Automatizált napi PostgreSQL pg_dump mentés Backblaze B2-re
```

---

## 4. Aktuális Ütemterv (Master Roadmap)

A projekt a **05. Master Fejlesztési Ütemterv** szerint halad:
1. **Fázis 1: Quick Wins & Stabilitás** *(Home téma kontraszt, authStore, useAutoSaveQueue, GEMINI.md, Szabálykönyvek)* - **FOLYAMATBAN**
2. **Fázis 2: Multi-Tenancy, Biztonság, Validáció és a 3 Alapnyelv (HU, SK, EN)**
3. **Fázis 3: A Termék Szíve: Komplett Foglalási Rendszer (Booking Engine)**
4. **Fázis 4: Szalonvezetés és Napi Munkaeszközök (KPI Dashboard, Raktár Seed, PWA)**
5. **Fázis 5: SaaS Skálázás, Jogosultságok és Monetizáció (Feature Gating, RBAC, Superadmin)**
6. **Fázis 6: Go-to-Market, Minőség és Értékesítés (BeeMagic Marketing, Tesztek, Brand)**

