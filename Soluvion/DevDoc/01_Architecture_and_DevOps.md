# 01. Architektúra és DevOps (Architecture & DevOps)

**Dátum:** 2026.10.12.  
**Projekt:** Soluvion (átnevezés alatt: BeeMagic Salon)  
**Státusz:** MVP Stabilizálva, Staging infrastruktúra kiépítve, AI modulok és kliensoldali optimalizáció integrálva.

Ez a dokumentum a rendszer technikai alapjait, a felhős infrastruktúrát, a telepítési folyamatokat (CI/CD) és a biztonsági protokollokat írja le.

---

## 1. Technológiai Stack (Tech Stack)

A rendszer egy modern, szétválasztott architektúrára (Backend API + SPA Frontend) épül.

### 1.1. Backend Ecosystem
* **Keretrendszer:** .NET 10 (ASP.NET Core Web API).
* **Nyelv:** C# 14.
* **Adatelérés (ORM):** Entity Framework Core (Code-First megközelítés).
* **Külső integrációk (NuGet):**
    * `Npgsql.EntityFrameworkCore.PostgreSQL`: Adatbázis meghajtó.
    * `BCrypt.Net-Next`: Kriptográfiai jelszókezelés.
    * `CloudinaryDotNet`: Felhő alapú képtároláshoz és manipulációhoz.
    * Hivatalos `OpenAI` .NET kliens és `Google Gemini` REST integráció a dinamikus lokalizációhoz, termékfelismeréshez és szállítólevél (Dodací list) OCR beolvasáshoz.

### 1.2. Frontend Ecosystem
* **Keretrendszer:** Vue 3 (Composition API, `<script setup>` szintaxis).
* **Build Tool:** Vite.
* **UI Komponensek:** PrimeVue (v4 szabványok, Aura preset `darkModeSelector: '.dark'` opcióval) és PrimeIcons.
* **Állapotkezelés & Routing:** Pinia store-ok (`authStore`, `companyStore`, `translationStore`, `appointmentStore`), Vue Router.
* **Lokalizáció:** Vue-i18n 11 (3 hivatalos alapnyelv: HU, SK, EN).
* **API Kliens:** Axios (Környezeti változókból vezérelt BaseURL-lel és JWT token interceptorral).
* **Extra könyvtárak:** `vuedraggable` (Drag & Drop funkciókhoz), `jwt-decode`.
* **API Kommunikáció:** Dedikált hálózati réteg (`src/services/*Api.js`) az Axios köré építve, elválasztva az állapotkezeléstől (Pinia) a jobb tesztelhetőség és karbantarthatóság érdekében.
* **Kliensoldali Képtömörítés:** Saját fejlesztésű [`imageCompressor.js`](file:///C:/Codes/Tomi/skanisalon/Soluvion/soluvion-web/src/utils/imageCompressor.js) Canvas-alapú modul (1600-1800px optimalizálás, védve a Railway Network Egress keretet).
* **CSS Keretrendszer:** Tailwind CSS v4. Globális CSS változókra (`var(--primary-color)`, stb.) építve, amelyeket a Vue `companyStore` komponense valós időben, a backendről érkező cégadatok alapján injektál a DOM gyökerébe (`:root`), biztosítva az azonnali SaaS "White-Label" megjelenést.

---

## 2. Infrastruktúra és Hosting

A rendszer felhőalapú megoldásokat használ, különválasztva az éles (Production) és a teszt (Staging) környezeteket.

* **Backend Hosting:** Railway felhő. Külön 'production' és 'staging' dockerizált környezet.
* **Frontend Hosting:** Netlify (Static site hosting SPA konfigurációval).
* **Környezeti Változók:** A Netlify UI-n a "Different value for each deploy context" beállítás biztosítja, hogy a megfelelő `VITE_API_URL` mutasson a PROD, illetve a STAGING backendre.

---

## 3. CI/CD Pipeline (Élesítés és Tesztelés)

Szigorúan tilos új fejlesztést vagy kísérletezést egyenesen a `main` ágra tolni. A telepítési folyamat a Git ágak (branches) alapján automatizált.

* **Éles Környezet (Production):**
    * **Trigger:** Git Push a `main` ágra.
    * **Folyamat:** A Railway frissíti a fő konténert, a Netlify pedig frissíti a fő domaint (pl. `skanisalon.sk`).
* **Auto-Migráció:** Az alkalmazás induláskor (`Program.cs`) automatikusan ellenőrzi és lefuttatja a hiányzó Entity Framework migrációkat (`db.Database.Migrate()`), így a sémafrissítések manuális beavatkozás nélkül, biztonságosan megtörténnek az éles környezetben is.
* **Teszt Környezet (Staging):**
    * **Trigger:** Git Push a `develop` ágra.
    * **Folyamat:** A Netlify automatikusan egy dedikált teszt linket generál (Branch Deploy: `develop--[app-neve].netlify.app`). Ez a verzió a Railway elkülönített Staging adatbázisával és API-jával kommunikál.

---

## 4. Adatbázis és Biztonsági Mentés (Database & Backup)

A rendszer relációs adatbázist használ. A Railway beépített mentéseinek fizetős korlátai miatt egyedi mentési stratégiát alkalmazunk.

* **Motor:** PostgreSQL 16+.
* **Adatbázis Kapcsolat:** A külvilág felé TCP Proxy-n keresztül kommunikál. DBeaver esetén kötelező a JDBC URL formátum és a kikényszerített SSL: `jdbc:postgresql://[HOST]:[PORT]/railway?sslmode=require`.
* **Biztonsági Mentés (Backup):** Automatizált napi mentések futnak a **Backblaze B2 Cloud Storage** tárhelyre, tömörített `.sql.gz` formátumban (pg_dump).
* **Klónozás és Helyreállítás (PROD -> STAGING):**
    1. A Railway felhős adatbázisára lokális parancssorból (CLI) ráereszteni a `dotnet ef database update` parancsot tilos a szigorú TCP proxy szabályok miatt.
    2. A Backblaze B2-ből letöltött `.sql.gz` fájlt ki kell csomagolni.
    3. A fájlból a terminál-specifikus `\restrict` és `\unrestrict` sorokat törölni kell.
    4. A klónozást DBeaver-ben a "Tools -> Execute Script" (psql háttérmotor) funkcióval kell futtatni, mert a sima SQL Editor nem tudja kezelni a PostgreSQL `COPY` parancsait.

---

## 5. Biztonság és Autentikáció (Security Stack)

A rendszer nem tárol nyílt szöveges jelszavakat és szigorú hálózati házirendeket alkalmaz.

* **Jelszó Hashing:** A rendszer a **BCrypt** algoritmust használja egyedi sózással (salt).
* **Autentikáció:** Stateless JWT Bearer Token alapokon (HMAC-SHA512). A lejárati idő konfigurálható (`AppSettings:TokenExpirationDays`). A frontend a reaktív Pinia `authStore`-ral kezeli a munkamenetet villanásmentes navigációval.
* **Hálózati Biztonság (CORS & HTTPS):** A CORS házirend szigorúan szabályozott, csak a Netlify domain és a Localhost engedélyezett a kérésekhez. A kommunikáció kizárólag kötelező HTTPS titkosított csatornán történik.
* **Központi Hibakezelés (Global Exception Handling):** A backend egy dedikált Middleware segítségével kapja el a felmerülő kivételeket, szabványos JSON válaszokat generálva.