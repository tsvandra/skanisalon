# 02. Termék Specifikáció és Üzleti Logika (Product Specification)

**Dátum:** 2026.10.13.  
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

A nemzetközi SaaS működéshez a rendszer fejlett, több rétegű fordítási és nyelvi modellt alkalmaz:

### 3.1. Gyári Rendszerszövegek (System UI - 3 Alapnyelv: HU, SK, EN)
* Minden statikus UI elem, menü, táblázatfejléc, gomb, toast üzenet, hibaüzenet és űrlap három teljes, natív, lektorált nyelvi csomagban érhető el a kódbázisban:
  - `src/locales/hu/` (Magyar)
  - `src/locales/sk/` (Szlovák)
  - `src/locales/en/` (Angol)
* **Előny:** Új szalon regisztrációjakor vagy nyelvváltáskor a felület **azonnal, 0 AI tokennel és 0 várakozási idővel** jelenik meg a kívánt nyelven.
* **Backend Hibakezelés (Gépi ErrorCode):** A backend végpontok nem küldenek hardkódolt magyar szövegeket. Hiba esetén gépi hibakódokat adnak vissza (`errorCode: "COMPANY_NOT_FOUND"`, `errorCode: "CANNOT_DELETE_DEFAULT_LANGUAGE"`), amit a kliensoldali Vue-i18n az `errors.json` szótárakból automatikusan fordít a felhasználó aktív nyelvére.

### 3.2. Tenant Nyelvi Szuverenitás és Beállítások
* **Szabad Nyelvválasztás:** Bár a rendszer mindhárom gyári nyelvet támogatja, minden szalon (tenant) önállóan dönti el a Cégbeállítások felületen, hogy ezek közül melyeket teszi elérhetővé a saját vendégei és munkatársai számára (ki- és bekapcsoló Toggle kapcsolók).
* **Alapértelmezett Nyelv (Default Language):** A szalon szabadon kiválaszthatja saját alapértelmezett nyelvét (pl. `sk` vagy `hu`). Az alapértelmezett nyelv soha nem törölhető és nem kapcsolható ki. Legalább egy nyelvnek mindig aktívnak kell lennie.
* **Intelligens Fejléc Nyelvválasztó:** Ha egy szalon csupán 1 aktív nyelvet tart bekapcsolva, a fejlécből automatikusan eltűnik a nyelvválasztó gomb, elkerülve a felesleges és zavaró vezérlőelemeket. Amint legalább 2 nyelv aktív, a választó azonnal megjelenik.
* **Gyári vs. Egyedi Nyelvek:** A 3 alapnyelv (HU, SK, EN) védett (nem törölhető, kizárólag inaktiválható). Ha a szalon egy 4. egyedi nyelvet vesz fel (pl. Német, Cseh), az bármikor törölhető is.

### 3.3. Rendszerszöveg vs. Szalonspecifikus Arculati Tartalom (Branding Protection)
* **Éles Határvonal:** 
  - A rendszer UI szövegei a Git repository verziókövetett JSON fájljaiban laknak (`src/locales/`).
  - A szalon egyedi bemutatkozó és arculati szövegei (`home.introTitle`, `home.introText`, `home.subtitle`, `home.welcome`, `home.checkGallery`) a szalon saját adatbázis rekordjaiban (`UiTranslationOverrides`) tárolódnak.
* **Arculatvédelmi Elv (Strict Brand Protection):** Rendszerfrissítés, UI fordítás-reszelés vagy szótár-reset során **szigorúan tilos** felülírni vagy törölni a szalon saját `home.*` marketing szövegeit! A backend `ResetOverrides` végpontja programozott szűrővel védi ezeket a rekordokat.

### 3.4. Dinamikus Szalontartalom és AI Fordítás
* A szalon egyedi szolgáltatásai, kategóriái, árlistája és leírásai JSONB szótárakként tárolódnak az adatbázisban (`{ "hu": "...", "sk": "..." }`).
* Új szolgáltatás rögzítésekor a beépített OpenAI modul dinamikus szakmai kontextussal, egyetlen gombnyomásra automatikusan lefordítja a hiányzó nyelvekre a szalon tartalmát.
* **Kereshető Fordításkezelő:** A Cégbeállítások Fordítások fülén valós idejű, intelligens kereső működik (kulcs, alapértelmezett szöveg, fordítás és egyedi felülírás alapján), amely automatikusan kinyitja az egyező kategóriákat és jelvénnyel (Badge) emeli ki az egyedileg módosított szövegeket.

---

## 4. Fő Modulok és Működés

### 4.1. Publikus Oldalak (Front Office)
* **Home:** Dinamikus borítókép, bemutatkozó kártya és közvetlen navigáció a galériához és szolgáltatásokhoz.
* **Services (Árlista):** Drag-and-drop rendezhető kategóriák, szolgáltatások és variánsok. A mentést az aszinkron `useAutoSaveQueue` védi.
* **Gallery (Okos Galéria):** Kategóriákba rendezett referenciaképek, teljes képernyős lightbox előnézettel, drag-and-drop sorrendezéssel.
* **Contact:** Címek, kattintható telefonszám, e-mail, interaktív nyitvatartási idősávok és kapcsolatfelvételi űrlap.

### 4.2. Intelligens Raktárkezelés és AI Képfelismerés (Inventory)
* **Raktárkészlet és Munkamenet-kiemelés:**
  - Termékek, árnyalatok/színkódok (`Shade`), EAN vonalkódok, beszerzési és eladási árak, valamint mértékegységek (`ml`, `g`, `db`) kezelése.
  - **Munkamenet Raktármozgás Kiemelő (`sessionStorage`):** A munkamenet során módosított termékek automatikusan a lista legelejére kerülnek az **„✓ Áttekintve, kész”** gomb megnyomásáig: a készletnövekedés **lila** (`✨ Friss bevét`, `(+X)`), a készletcsökkenés **sárgásbarna** (`📉 Friss levétel`, `(-X)`) jelölést kap, a munkamenet eleji nyitókészlethez (`originalStock`) viszonyított nettó számítással.
* **Többformátumú Szállítólevél (Dodací list) AI Feldolgozás:**
  - Mobilfotók (JPG, PNG, WebP) mellett natívan feldolgozza a **PDF**, **Excel** (`.xlsx`, `.xls`, `.csv`) és **Word** (`.docx`, `.doc`) dokumentumokat is.
  - Automatikusan kinyeri a fejlécadatokat, felismeri a dokumentumon szereplő több ároszlopot (legördülő menüvel választható oszloppal), szétválasztja a darabszámot (`db`) és a kiszerelést (`ml`/`g`), oda-vissza számolja az egységárat és az összárat, valamint szerkeszthetővé teszi a színkódokat (`Shade`).
* **Fényképes Termékfelismerő AI:** Flakonról vagy dobozról készült fotók alapján felismeri a márkát, terméknevet, árnyalatot, kiszerelést és vonalkódot, valamint automatikusan feltölti a termékképet a Cloudinary-be.
* **Anyagfelhasználás követés:** A szolgáltatás receptek alapján a naptári időpontok lezárásakor a rendszer automatikusan levonja a felhasznált anyagmennyiséget a raktárból.

### 4.3. Naptár és Időpontkezelés (Smart Booking)
* Dolgozói munkarendek, foglalások kezelése (napi, heti, havi nézet).
* **Naptár "MA" Navigációs UX:** A naptár gyorsgombja ("MA") reaktívan érzékeli az aktuális nézet dátumát: ha a felhasználó a mai napon/héten áll, a gomb inaktív és halványított (`opacity-40`), megakadályozva a felesleges újratöltéseket. Amint a felhasználó elnavigál az időben, a gomb felélénkül és azonnali visszatérést biztosít a mai napra.
* Folyamatban lévő fejlesztés (Master Roadmap Fázis 3): Munkarend-alapú szabad idősáv kalkuláció (`GetAvailableSlots`) és publikus vendégfoglaló felület rate limiting védelemmel.

### 4.4. Vezérlőpult (Dashboard)
* Raktári alacsony készlet figyelmeztetések, anyagfelhasználási összesítők.
* Folyamatban (Master Roadmap Fázis 4): Pénzügyi és forgalmi KPI statisztikák szerepkör-alapú jogosultságokkal.
* **Zero-Admin Profil Építés (Profile Modifiers):** A rendszer képes "tanulni" a napi szintű naptár-használatból. Ha a vendégnek lefoglalnak egy szolgáltatást, amihez be van állítva egy Módosító (pl. Női Hajvágás -> Hajhossz: Rövid), a backend transzparensen és automatikusan beírja (vagy frissíti) ezt az adatot a vendég JSONB profiljában a foglalás pillanatában. Így elkerülhető a dupla adminisztráció.