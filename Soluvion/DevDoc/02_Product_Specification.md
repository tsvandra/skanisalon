# 02. Termék Specifikáció és Üzleti Logika (Product Specification)

**Dátum:** 2026.10.12.  
**Projekt:** Soluvion (átnevezés alatt: BeeMagic Salon)  
**Típus:** Élő Alkalmazás Dokumentáció (Living App Doc)

Ez a dokumentum az alkalmazás üzleti logikáját, a SaaS működést, a lokalizációs stratégiát és a főbb modulok funkcionális leírását tartalmazza.

---

## 1. Cél és Vízió

A rendszer egy .NET backend alapú, Vue.js frontenddel rendelkező webalkalmazás. A végső cél egy Multi-tenant (SaaS) rendszer kialakítása, ahol több különböző szalon (vagy más vállalkozás) is használhatja az alkalmazást saját adatbázis adatokkal, de egyetlen közös kódbázison.
Minden üzleti entitás (`Service`, `Gallery`, `User`, `Company`, `Product`, `InventoryDocument`, `Appointment`) kötelezően rendelkezik egy `CompanyId` azonosítóval az adatok szeparálása érdekében.

---

## 2. SaaS és "White-Label" Működés

Az alkalmazás szigorúan a "Zéró Hardcode" elvet követi: soha nem drótozunk be fix cégnevet, logót vagy színeket a forráskódba.

* **Dinamikus Arculat (Style Injection):** A rendszer a látogató beérkezésekor vagy bejelentkezéskor lekéri a cég adatait az adatbázisból, és valós időben felülírja a globális CSS változókat (pl. `--primary-color`, `--secondary-color`).
* **Dinamikus Kontraszt & Sötét Mód:** A `companyStore.applyTheme()` intelligensen W3C luminancia alapján detektálja a sötét vagy világos hátteret, és beállítja a `dark` / `light` osztályt a gyökérelemre. A PrimeVue `darkModeSelector: '.dark'` biztosítja a tökéletes olvashatóságot operációs rendszertől függetlenül.
* **Média és Felhő:** A vizuális elemek (Logó, Favicon, Hero borítókép, Lábléc) testreszabhatóak. Minden felhasználói médiafájlt a Cloudinary skálázható felhőtárhelye kezel. A feltöltés előtt minden képet a kliensoldali `imageCompressor.js` optimalizál (150–350 KB méretre).

---

## 3. Lokalizáció és Többnyelvűség (i18n Stratégia)

A nemzetközi SaaS működéshez a rendszer két külön szinten kezeli a fordításokat:

### 3.1. Gyári Rendszerszövegek (System UI - 3 Alapnyelv: HU, SK, EN)
* Minden statikus UI elem, menü, táblázatfejléc, gomb, toast üzenet és űrlap három teljes, lektorált nyelvi csomagban érhető el:
  - `src/locales/hu/` (Magyar)
  - `src/locales/sk/` (Szlovák)
  - `src/locales/en/` (Angol)
* **Előny:** Új szalon bekapcsolásakor vagy nyelvváltáskor a felület **azonnal, 0 AI tokennel és 0 várakozási idővel** megjelenik a kívánt nyelven.

### 3.2. Dinamikus Szalontartalom és AI Fordítás
* A szalon egyedi szolgáltatásai, árlistája és leírásai JSONB szótárakként tárolódnak (`{ "hu": "...", "sk": "..." }`).
* Ha a szalon új szolgáltatást vesz fel (pl. magyarul), a beépített OpenAI modul dinamikus kontextussal gombnyomásra automatikusan lefordítja a hiányzó nyelvekre.

---

## 4. Fő Modulok és Működés

### 4.1. Publikus Oldalak (Front Office)
* **Home:** Dinamikus borítókép, bemutatkozó kártya és közvetlen navigáció a galériához és szolgáltatásokhoz.
* **Services (Árlista):** Drag-and-drop rendezhető kategóriák, szolgáltatások és variánsok. A mentést az aszinkron `useAutoSaveQueue` védi.
* **Gallery (Okos Galéria):** Kategóriákba rendezett referenciaképek, teljes képernyős lightbox előnézettel, drag-and-drop sorrendezéssel.
* **Contact:** Címek, kattintható telefonszám, e-mail, interaktív nyitvatartási idősávok és kapcsolatfelvételi űrlap.

### 4.2. Intelligens Raktárkezelés és AI Képfelismerés (Inventory)
* **Raktárkészlet:** Termékek, árnyalatok/színkódok (`Shade`), EAN vonalkódok, beszerzési és eladási árak, valamint mértékegységek (`ml`, `g`, `db`) kezelése.
* **Szállítólevél (Dodací list) AI beolvasás:** Mobilfotókból a Google Gemini / OpenAI Vision automatikusan felismeri a fejlécadatokat (szállító, sorszám, dátum) és a tételsorokat, majd fuzzy algoritmussal intelligensen párosítja a meglévő raktártermékekkel, egykattintásos bevételezést biztosítva.
* **Fényképes Termékfelismerő AI:** Flakonról vagy dobozról készült fotók alapján felismeri a márkát, terméknevet, árnyalatot, kiszerelést és vonalkódot, valamint automatikusan feltölti a termékképet a Cloudinary-be.
* **Anyagfelhasználás követés:** A szolgáltatás receptek alapján a naptári időpontok lezárásakor a rendszer automatikusan levonja a felhasznált anyagmennyiséget a raktárból.

### 4.3. Naptár és Időpontkezelés (Smart Booking)
* Dolgozói munkarendek, foglalások kezelése (napi, heti, havi nézet).
* Folyamatban lévő fejlesztés (Master Roadmap Fázis 3): Munkarend-alapú szabad idősáv kalkuláció (`GetAvailableSlots`) és publikus vendégfoglaló felület rate limiting védelemmel.

### 4.4. Vezérlőpult (Dashboard)
* Raktári alacsony készlet figyelmeztetések, anyagfelhasználási összesítők.
* Folyamatban (Master Roadmap Fázis 4): Pénzügyi és forgalmi KPI statisztikák szerepkör-alapú jogosultságokkal.