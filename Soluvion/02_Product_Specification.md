# 02. Termék Specifikáció és Üzleti Logika (Product Specification)

**Dátum:** 2026.02.25.
**Projekt:** Soluvion (Skani Salon)
**Típus:** Élõ Alkalmazás Dokumentáció (Living App Doc)

Ez a dokumentum az alkalmazás üzleti logikáját, a SaaS mûködést, a lokalizációs stratégiát és a fõbb modulok funkcionális leírását tartalmazza.

---

## 1. Cél és Vízió

[cite_start]A rendszer egy .NET backend alapú, Vue.js frontenddel rendelkezõ webalkalmazás[cite: 50]. [cite_start]A végsõ cél egy Multi-tenant (SaaS) rendszer kialakítása, ahol több különbözõ szalon (vagy más vállalkozás) is használhatja az alkalmazást saját adatbázis adatokkal, de egyetlen közös kódbázison[cite: 52].
[cite_start]Minden üzleti entitás (Service, Gallery, User, Company) kötelezõen rendelkezik egy `CompanyId` azonosítóval az adatok szeparálása érdekében[cite: 64].

---

## 2. SaaS és "White-Label" Mûködés

[cite_start]Az alkalmazás szigorúan a "Zéró Hardcode" elvet követi: soha nem drótozunk be fix cégnevet, logót vagy színeket a forráskódba[cite: 66].

* [cite_start]**Dinamikus Arculat (Style Injection):** A rendszer a látogató beérkezésekor vagy bejelentkezéskor lekéri a cég adatait az adatbázisból, és valós idõben felülírja a globális CSS változókat (pl. `--primary-color`, `--secondary-color`)[cite: 68, 70].
* **Média és Felhõ:** A vizuális elemek (Logó, Lábléc háttérkép) testreszabhatóak. [cite_start]A felhasználó által feltöltött médiafájlokat szigorúan tilos lokálisan a `wwwroot`-ban tárolni[cite: 201]. [cite_start]Minden ilyen fájlt (arculat és galéria) a Cloudinary skálázható felhõtárhelye kezeli[cite: 99, 202].

---

## 3. Hibrid Lokalizáció és AI Fordítás

[cite_start]SaaS környezetben nem hozhatunk létre fizikai `.json` fájlokat minden új nyelvhez, amit egy ügyfél bekapcsol[cite: 128]. Erre egy egyedi "Master Template" architektúrát használunk.

### 3.1. Nyelvkezelés (Runtime Generation)
* [cite_start]**Master Source:** Kizárólag a `locales/hu.json` létezik fizikai fájlként, ez a rendszer "DNS-e", ami az összes statikus kulcsot tartalmazza[cite: 129, 211].
* [cite_start]**Memory Cloning & Merge:** Nyelvváltáskor a Vue Store a memóriában leklónozza a Master fájlt, majd letölti a cég-specifikus felülírásokat az `UiTranslationOverrides` adatbázis táblából, és ráolvasztja a klónra [cite: 130-131].
* **Fallback Logika:** A dinamikus fallback nyelv sosem lehet beégetett `'hu'`, hanem mindig az aktuális cég beállítása: `company.value?.defaultLanguage || [cite_start]'hu'`[cite: 143, 227].

### 3.2. Intelligens AI Fordítás
[cite_start]A beépített OpenAI modul kontextus-érzékeny, és a System Promptokat dinamikusan rakja össze[cite: 122].
* [cite_start]Figyelembe veszi a cég típusát (`CompanyType` - pl. tudja, hogy a "vágás" hajat jelent egy fodrászatnál)[cite: 123].
* [cite_start]Figyelembe veszi az oldal kontextusát (Frontend küldi: 'service' tömörséget kér, 'gallery' kreatív leírást) [cite: 124-125, 209].
* [cite_start]A fordítás aszinkron háttérszálon fut, a Frontend pedig Progress Barral (Polling) követi a folyamatot[cite: 134, 218].

---

## 4. Fõ Modulok és Mûködés

### 4.1. Publikus Oldalak (Front Office)
A látogatói nézet egy modern SPA (Single Page Application).
* [cite_start]**Home & Contact:** Dinamikus bemutatkozás, SQL-bõl betöltött címek, telefonszámok, nyitvatartási idõ és beágyazott Google Maps modul [cite: 74-75]. [cite_start]A nyitvatartási táblázatok is HTML alapú, szerkeszthetõ adatbázis mezõkbõl jönnek[cite: 77].

### 4.2. Arculat és Cégbeállítások (Admin)
[cite_start]A "Site Builder" jellegû funkciók egy dedikált beállítások oldalon érhetõk el a véletlen módosítások elkerülése végett[cite: 119].
* [cite_start]PrimeVue `Tabs` felület a jobb átláthatóságért[cite: 114].
* [cite_start]Valós idejû csúszkák (slider) a Logó és a Lábléc magasságának állításához, azonnali vizuális visszajelzéssel[cite: 115, 120].
* **Többnyelvű Cégadatok:** A nyitvatartási információk JSONB formátumban, nyelvenként szeparálva (Dictionary) tárolódnak, biztosítva a tökéletes lokalizációt.
* **AI Varázspálca:** Egykattintásos OpenAI fordítási asszisztens a beállítási mezőkhöz, dedikált "Összes fordítása" csoportos funkcióval és dinamikus kontextus-felismeréssel.

### 4.3. Árlista és Szolgáltatások (Services)
* [cite_start]**Hierarchikus Adatkezelés:** Az adatok Kategória -> Szolgáltatás -> Variáns (Variant) struktúrában épülnek fel[cite: 94].
* [cite_start]**Rendezés:** A kategóriák és szolgáltatások vizuális sorrendje Drag-and-Drop módszerrel (`vuedraggable`) állítható be[cite: 92].
* [cite_start]**UI/UX:** A modul sötét témát (Dark Mode) használ, és támogat többnyelvû megjegyzés (Description) mezõket a szolgáltatások finomhangolásához [cite: 93-94].

### 4.4. Okos Galéria (Smart Gallery)
[cite_start]A képek fizikai tárolását a Cloudinary végzi[cite: 99].
* [cite_start]**Nested Drag-and-Drop:** A felhasználók szabadon sorrendezhetik a kategóriákat (függõleges lista) és a képeket a kategóriákon belül is [cite: 104-105]. Képek mozgathatóak kategóriák között is.
* [cite_start]**Kontextuális Kezelés:** Új kategória létrehozása ("Inline") azonnal megjelenik a tetején fókuszált mezõvel[cite: 106]. [cite_start]A képfeltöltés gombok közvetlenül a kategória fejlécekbe kerültek az egyértelmûségért[cite: 107].
* [cite_start]**Vizuális megjelenítés:** Egységesített, négyzetes képarány (thumbnail) és kattintásra nyíló Lightbox nagyítás[cite: 108].