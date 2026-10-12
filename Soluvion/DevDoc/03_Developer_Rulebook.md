# 03. Fejlesztõi Játékszabályok (Developer Rulebook)

**Dátum:** 2026.10.12.  
**Projekt:** Soluvion (átnevezés alatt: BeeMagic Salon)  
**Típus:** Kódolási Szabályzat és Best Practices

Ez a dokumentum azokat a szigorú kódolási elveket és konvenciókat tartalmazza, amelyeket a rendszer stabilitásának és a "Clean Code" fenntartásának érdekében minden fejlesztés során be kell tartani.

---

## 1. Git Branching és Munkafolyamat

A kód stabilitásának megőrzése érdekében szigorú "Feature Branch" modellt követünk.

* **`main` ág (PRODUCTION):** Kizárólag a stabil, éles verziót tartalmazza. Közvetlenül ide pusholni **szigorúan tilos!**
* **`develop` ág (STAGING):** A központi fejlesztői ág. A Netlify és a Railway ezt figyeli a tesztkörnyezet frissítéséhez.
* **`feature/...` ágak (LOCAL):** Minden új funkciót vagy hibajavítást egy dedikált ágon kell elvégezni.
* **Névkonvenció:** A feature ágak neve tartalmazza a fázist vagy a funkciót (pl. `feature/phase-1-quick-wins`, `feature/ai-termekfelismeres`).

---

## 2. Backend Kódolási Szabályok (.NET 10)

### 2.1. Controller Higiénia és DTO-k (Strict Mode)
* **Nincs inline DTO:** Tilos DTO osztályokat vagy segédosztályokat a Controller fájlon belül definiálni. Minden új adatstruktúrának saját fájlja van a `DTOs` mappában.
* **Thin Controllers:** A kontroller csak a kérés fogadásáért, a validáció ellenőrzéséért és a HTTP válaszkód visszaadásáért felel. Az üzleti logikát kötelező kiszervezni a megfelelő Service osztályba.
* **DTO Kötelezettség:** SOHA ne várjon egy Controller közvetlenül Adatbázis Entitást a paraméterekben (POST/PUT kéréseknél). Mindig DTO-t kell fogadni.
* **Validáció:** A bemeneti adatok ellenőrzéséhez a .NET natív `System.ComponentModel.DataAnnotations` attribútumait (`[Required]`, `[Range]`, `[MaxLength]`) használjuk.

### 2.2. API és Adatbázis Konvenciók
* **Tenant Isolation:** Minden lekérdezést szűrni kell a Multi-tenancy miatt (`.Where(x => x.CompanyId == currentCompanyId)`).
* **Konfigurálható Értékek:** Tilos a kódban napokat, kulcsokat vagy URL-eket égetni. Pl. a JWT lejárati időt az `AppSettings:TokenExpirationDays` kulcsból olvassuk.

---

## 3. Frontend Kódolási Szabályok (Vue 3)

### 3.1. Kliensoldali Képtömörítés (SZIGORÚ SZABÁLY!)
* **Railway Egress Védelem:** Szigorúan tilos nyers, 5–15 MB-os mobilkamerás fotókat feltölteni a szerverre!
* Minden képet a feltöltés előtt kötelező átméretezni és optimalizálni a [`src/utils/imageCompressor.js`](file:///C:/Codes/Tomi/skanisalon/Soluvion/soluvion-web/src/utils/imageCompressor.js) segédprogrammal (`compressImage` vagy `compressImages`). Célméret: max. 1600–1800px, 0.8–0.85 minőség (~200–350 KB).

### 3.2. Autentikáció és Állapotkezelés (authStore)
* Be- és kijelentkezéskor a központi [`authStore.js`](file:///C:/Codes/Tomi/skanisalon/Soluvion/soluvion-web/src/stores/authStore.js) Pinia store-t használjuk (`authStore.setToken(...)`, `authStore.logout()`).
* **Tilos a `window.location.reload()` hack!** A reaktív store gondoskodik az azonnali UI frissülésről és a sima router navigációról.

### 3.3. Aszinkron Állapotkezelés és Race Condition Védelem
* **A `useAutoSaveQueue` kötelező:** Olyan funkcióknál, amelyek gyors, egymás utáni API hívásokat generálnak (pl. Drag-and-Drop rendezés, szövegmezők gépelése), kötelező a `useAutoSaveQueue` composable használata.
* **Párhuzamos hívások tilalma:** Sose engedjünk párhuzamos felülíró API hívásokat ugyanarra az elemre.

### 3.4. PrimeVue v4 Kompatibilitás és Téma
* Kötelező használni a v4 szabványokat (`<Select>`, `<Tabs>`, stb.).
* A PrimeVue `darkModeSelector: '.dark'` konfigurációt használja, így a szalon sötét/világos stílusa mindig megegyezik a `companyStore` által beállított témával, függetlenül az OS beállításaitól.

### 3.5. API Hívások és Hálózati Réteg
* Szigorúan tilos közvetlen `axios` hívásokat írni a komponensekbe. Mindig a dedikált szerviz réteget használjuk (`inventoryApi`, `productApi`, stb.), vagy a központi `src/services/api.js` klienst.

### 3.6. Többnyelvűsítés (i18n)
* Tilos keményen beégetett szöveget hagyni a template-ekben és szkriptekben!
* Minden UI felirat a `src/locales/` megfelelő JSON fájljaiba kerüljön, a template-ben `$t('kulcs')` hívással.