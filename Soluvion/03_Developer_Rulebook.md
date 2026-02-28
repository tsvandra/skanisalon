# 03. Fejlesztõi Játékszabályok (Developer Rulebook)

**Dátum:** 2026.02.25.
**Projekt:** Soluvion (Skani Salon)
**Típus:** Kódolási Szabályzat és Best Practices

Ez a dokumentum azokat a szigorú kódolási elveket és konvenciókat tartalmazza, amelyeket a rendszer stabilitásának és a "Clean Code" fenntartásának érdekében minden fejlesztés során be kell tartani.

---

## 1. Git Branching és Munkafolyamat

[cite_start]A kód stabilitásának megõrzése érdekében szigorú "Feature Branch" modellt követünk[cite: 58].

* [cite_start]**`main` ág (PRODUCTION):** > Kizárólag a stabil, éles verziót tartalmazza[cite: 59]. Közvetlenül ide pusholni **szigorúan tilos!**
* [cite_start]**`develop` ág (STAGING):** > A központi fejlesztõi ág[cite: 60]. A Netlify és a Railway ezt figyeli a tesztkörnyezet frissítéséhez.
* [cite_start]**`feature/...` ágak (LOCAL):** > Minden új funkciót vagy hibajavítást egy dedikált ágon kell elvégezni[cite: 60].
* [cite_start]**Névkonvenció:** > A feature ágak neve tartalmazza az Issue számát (pl. `feature/12-arlista-backend`)[cite: 61, 62].

---

## 2. Backend Kódolási Szabályok (.NET)

### 2.1. Controller Higiénia és DTO-k (Strict Mode)
* [cite_start]**Nincs inline DTO:** > Tilos DTO osztályokat, Enumokat vagy segédosztályokat a Controller fájlon belül definiálni[cite: 219]. [cite_start]Minden új adatstruktúrának azonnal létre kell hozni a saját fájlját a `Models/DTOs` mappában[cite: 194, 220].
* [cite_start]**Thin Controllers:** > Ha egy Controller meghaladja a 200 sort, vagy túl sok felelõssége van, a logikát **ki kell szervezni** egy Service osztályba[cite: 221].
* [cite_start]**DTO Kötelezettség Mentéskor:** > SOHA ne várjon egy Controller közvetlenül Adatbázis Entitást a paraméterekben (POST/PUT kéréseknél)[cite: 223]. [cite_start]Mindig DTO-t kell fogadni, és manuálisan leképezni az EF Core felé, elkerülve az adatbázis hibákat[cite: 224, 225].

### 2.2. API és Adatbázis Konvenciók
* [cite_start]**Tenant Isolation:** > Minden lekérdezést szûrni kell a Multi-tenancy miatt (pl. `.Where(x => x.CompanyId == currentCompanyId)`)[cite: 164]. [cite_start]Az azonosításhoz a JWT Tokenben lévõ `CompanyId` az irányadó[cite: 162].
* [cite_start]**Adatbázis Elnevezések:** > Kulcs-érték párokat tároló tábláknál a kulcs mezõ neve legyen konzisztensen `TranslationKey` (vagy `ConfigKey`)[cite: 222]. [cite_start]Kerüljük a generikus `Key` elnevezést az EF Core ütközések miatt[cite: 222].

---

## 3. Frontend Kódolási Szabályok (Vue 3)

### 3.1. Aszinkron Állapotkezelés és Race Condition Védelem
* [cite_start]**A `useAutoSaveQueue` kötelezõ:** > Olyan funkcióknál, amelyek gyors, egymás utáni API hívásokat generálnak (pl. Drag-and-Drop rendezés, Csúszkák húzása), kötelezõ a `useAutoSaveQueue` composable használata[cite: 196]. 
* [cite_start]**Párhuzamos hívások tilalma:** > Sose engedjünk párhuzamos API hívásokat ugyanarra az erõforrásra, ezzel elkerülve a "Race Condition" adatvesztést[cite: 197].
* [cite_start]**Input Kezelés:** > PrimeVue beviteli mezõknél a biztos mentés érdekében az `@update:modelValue` és `@blur` eseményeket együtt kell használni[cite: 172].

### 3.2. PrimeVue v4 Kompatibilitás
[cite_start]Új komponensek írásakor vagy refaktoráláskor **TILOS** a régi PrimeVue v3 elnevezéseket használni[cite: 214].
* [cite_start]**Kötelezõ használni:** `<Select>`, `<Tabs>`, `<AccordionPanel>`[cite: 215].
* [cite_start]**Tilos használni:** `<Dropdown>`, `<TabView>`, `<AccordionTab>`[cite: 215].

### 3.3. API Kommunikáció (Axios)
* [cite_start]**Fetch API tilalma:** > TILOS a natív `fetch` API használata manuális header építéssel[cite: 164].
* [cite_start]**Axios használata:** > Kizárólag a `src/services/api.js` Axios példány használható, mert az automatikusan kezeli a BaseURL-t és a JWT Bearer tokent[cite: 165].

### 3.4. Separation of Concerns (Admin UI)
* [cite_start]**Beállítások helye:** > A "Site Builder" jellegû funkciók (Logó feltöltés, Layout módosítás) helye a `SettingsView`-ban van[cite: 198].
* [cite_start]**Tiszta Layout:** > Kerüljük a szerkesztõ gombok elhelyezését közvetlenül a globális layout komponenseken (Header/Footer), hogy a felhasználói felület tiszta maradjon[cite: 199]. [cite_start]Használjuk a `provide/inject` mintát a beállítások érvényesítéséhez[cite: 200].

---

## 4. Mesterséges Intelligencia (AI) és Felhõ Szabályok

### 4.1. AI Integráció
* **API Kulcs Biztonság:** > Szigorúan TILOS API kulcsot (`sk-...`) a forráskódban vagy config fájlban commitolni. Helyi fejlesztés során az `OpenAI:ApiKey` tárolására KÖTELEZŐ a .NET `User Secrets` (dotnet user-secrets) használata. Ha véletlenül kikerül a Gitre, azonnal vissza kell vonni a kulcsot.
* **Vue-i18n Speciális Karakterek:** > Az AI által fordított vagy manuálisan beírt `@` (kukac) karaktereket (pl. e-mail címeknél) KÖTELEZŐ `{'@'}` formátumban escape-elni a JSON fájlokban és a Store betöltő logikájában, különben a Vue-i18n fatal errort dob és az alkalmazás összeomlik.
* [cite_start]**Prompt Logika (SaaS Rule):** > Tilos hard-coded üzleti típusokat (pl. "Fodrászat") írni a System Promptba[cite: 206]. [cite_start]Mindig a `CompanyType` változót kell behelyettesíteni[cite: 207].
* [cite_start]**Frontend Hívás:** > A Frontendnek KÖTELEZÕ küldenie a `context` paramétert ('service', 'gallery', stb.), különben a backend "general" módban fordít[cite: 209]. [cite_start]A fordítás mindig gombnyomásra történik, sosem a háttérben automatikusan[cite: 208].

### 4.2. Felhõ Tárolás (Cloud Storage)
* [cite_start]**Lokális tárolás tilalma:** > Felhasználó által feltöltött médiafájlokat TILOS lokálisan a `wwwroot`-ban tárolni[cite: 201].
* [cite_start]**Cloudinary használata:** > Minden médiafájlt a Cloudinary-ba kell feltölteni[cite: 202]. [cite_start]Az adatbázisban a `PublicId`-t is tárolni kell az `Url` mellett a törölhetõség érdekében[cite: 203].