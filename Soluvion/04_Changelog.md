# 04. Fejlesztési Napló (Changelog & Release Notes)

**Dátum:** 2026.02.25.
**Projekt:** Soluvion (Skani Salon)
**Típus:** Verziótörténet és Funkció Napló

Ez a dokumentum kronológiai sorrendben (a legújabbtól visszafelé) rögzíti a befejezett funkciókat, infrastrukturális változásokat és a kritikus hibajavításokat.

---

## [2026-02-28] Feature 14: Cégbeállítások Lokalizációja és AI Varázspálca Integráció
**Státusz:** Kész, Élesítve
* **Adatbázis & Backend:** A cég nyitvatartási adatai (`OpeningHoursTitle`, `OpeningHoursDescription`, `OpeningTimeSlots`, `OpeningExtraInfo`) `jsonb` típusú többnyelvű szótárrá (Dictionary) lettek alakítva az Entity Frameworkben.
* **Auto-Migráció:** A `Program.cs` felkészítve a hiányzó adatbázis migrációk automatikus futtatására induláskor, megkönnyítve a Railway deploymentet.
* **AI Integráció:** "Varázspálca" gombok és "Összes Mező Fordítása AI-val" funkció hozzáadva a `SettingsView.vue` Nyitvatartás füléhez.
* **OpenAI Frissítés:** Az AI szolgáltatás átállítva a modern és gyors `gpt-5.2` modellre. A `context` és `CompanyType` paraméterek dinamikus, szabályos átadása a frontendtől az AI promptig.
* **UI/UX & Hibajavítások:** * 401 Unauthorized hiba esetén automatikus kijelentkeztetés és átirányítás a login oldalra (`api.js` interceptor).
  * A kiválasztott nyelv tartós mentése `localStorage`-be a megbízható nyelvváltás és navigáció érdekében (`translationStore.js`, `App.vue`).
  * Biztonsági javítás a Vue-i18n `@` (kukac) szimbólum okozta tokenizációs összeomlásának elkerülésére (dinamikus escape-elés `{'@'}`).

## [2026-02-25] Infrastruktúra: Staging Környezet és CI/CD Pipeline
[cite_start]**Státusz:** Kész, Stabilizálva [cite: 149]
* [cite_start]**Backend (Railway):** Dedikált 'staging' környezet létrehozása új PostgreSQL adatbázissal és .NET Web API konténerrel[cite: 150]. [cite_start]CORS Policy frissítve a teszt domainekhez[cite: 151].
* [cite_start]**Frontend (Netlify):** 'Branch Deploys' aktiválva a `develop` ágra[cite: 151]. [cite_start]Deploy Context környezeti változók beállítva a dinamikus API hívásokhoz[cite: 152].
* [cite_start]**Adatbázis:** Sikeres adatbázis klónozás a Backblaze B2 napi mentésbõl a Staging adatbázisba (DBeaver JDBC proxy és `COPY` parancsok feldolgozásával)[cite: 154, 155].

## [2026-02-23] SaaS Lokalizáció UX és DTO Stabilizálás
**Státusz:** Kész
* [cite_start]**Dinamikus Alapnyelv:** A komponensek (Services, Gallery) már a `company.defaultLanguage` beállítást használják a beégetett 'hu' helyett[cite: 142, 143].
* [cite_start]**UX Finomhangolás:** Többnyelvû megjegyzés dobozoknál dinamikus placeholder (az alapnyelv szövege jelenik meg, ha a célnyelv üres)[cite: 145]. [cite_start]Textareák automatikus méretezése Vue életciklus eseményekhez kötve[cite: 146].
* **EF Core Fix:** 400 Bad Request hibák kiküszöbölése. [cite_start]A végpontok kizárólag DTO-kat fogadnak, manuális leképezéssel elkerülve az EF Core collection tracking hibáit[cite: 147, 148].

## [2026-02-18] DTO Konszolidáció és Stabilizálás
**Státusz:** Kész
* [cite_start]**DTO Struktúra:** A Controller-beli inline definíciók megszüntetése, minden DTO a `Models/DTOs` mappába került (pl. `TranslationDto.cs`)[cite: 136, 137].
* [cite_start]**Kulcs Elnevezések:** A Fordításoknál az adatbázis és modell szinten a `TranslationKey` lett a mérvadó az egyszerû `Key` helyett[cite: 138, 139].
* [cite_start]**JSONB Implementáció:** `EnableDynamicJson()` és explicit konverterek beépítése a `Service`, `GalleryImage` és `ServiceVariant` entitásokhoz[cite: 141].

## [2026-02-16] Hibrid SaaS Lokalizáció
**Státusz:** Kész
* **Nyelvkezelés:** Master Source (`hu.json`) alapú mûködés. [cite_start]Runtime klónozás és adatbázisból érkezõ felülírások (Overrides) beolvasztása [cite: 129-131].
* [cite_start]**Fordítási Folyamat:** Admin nyelv hozzáadásakor a Backend Fire-and-Forget háttérszálon elindítja a fordítást, amit a Frontend Polling-gal (Progress Bar) figyel [cite: 132-134].
* [cite_start]**Tech Debt:** PrimeVue v4-es komponensek (`Tabs`, `Select`, `AccordionPanel`) kötelezõ bevezetése a deprecated elemek helyett[cite: 135].

## [2026-02-12] Intelligens AI Fordítás
**Státusz:** Kész
* **SaaS Logika:** OpenAI integráció dinamikus System Message injektálással. [cite_start]Fix promptok helyett a `CompanyType` és a Frontend által küldött `context` ('service', 'gallery') alapján fordít [cite: 122-125].

## [2026-02-10] Feature 14: Arculat Kezelés & Beállítások Refaktor
**Státusz:** Kész
* [cite_start]**Dinamikus Arculat:** Logó, Lábléc, Színek és Magasságok testreszabása [cite: 110-111]. [cite_start]Cloudinary feltöltõ végpontok (`/upload/logo`, `/upload/footer`) bevezetése[cite: 112].
* **UI/UX:** PrimeVue `Tabs` bevezetése a Beállításokban. [cite_start]Valós idejû csúszkák (sliders) az azonnali vizuális frissítéshez[cite: 114, 115, 120].

## [2026-02-09] Feature 13: Smart Gallery & Cloudinary
**Státusz:** Kész
* [cite_start]**Cloud Storage:** Cloudinary integráció, helyi fájltárolás kivezetése[cite: 99].
* [cite_start]**UI/UX:** Nested Drag-and-Drop implementálása a Kategóriák és Képek rendezéséhez[cite: 104, 105]. [cite_start]Inline kategória létrehozás és kontextuális képfeltöltés a fejlécekben[cite: 106, 107].
* [cite_start]**Queueing:** `useAutoSaveQueue.js` composable létrehozása a Race Condition elkerülésére aszinkron mentéseknél[cite: 109].

## [2026-02-09] Feature 12: ServicesView Refactoring & Drag-and-Drop
**Státusz:** Kész
* [cite_start]**Kategória Kezelés:** Hierarchikus adatkezelés és `vuedraggable` implementálása [cite: 92-94].
* [cite_start]**UI:** Dark Mode bevezetése, vendég nézet arany kiemelésekkel[cite: 93, 98].
* [cite_start]**Fix:** Race Condition hiba javítva Promise Chain Request Queue-val[cite: 96, 97].

## [MVP Alapok] Kezdeti Fejlesztések
* [cite_start]**Autentikáció:** JWT token alapú beléptetés és BCrypt jelszó titkosítás[cite: 178].
* [cite_start]**Publikus Oldal:** Dinamikus SPA routing, SQL-bõl töltött céginformációkkal (Home és Contact)[cite: 73, 74].
* [cite_start]**Alap Beállítások:** Color Picker a dinamikus CSS változókhoz, nyitvatartás és közösségi média linkek kezelése[cite: 76, 77].