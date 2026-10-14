# 03. Fejlesztõi Játékszabályok (Developer Rulebook)

**Dátum:** 2026.10.13.  
**Projekt:** Soluvion (átnevezés alatt: BeeMagic Salon)  
**Típus:** Kódolási Szabályzat és Best Practices

Ez a dokumentum azokat a szigorú kódolási elveket és konvenciókat tartalmazza, amelyeket a rendszer stabilitásának és a "Clean Code" fenntartásának érdekében minden fejlesztés során be kell tartani.

---

## 1. Git Branching és Munkafolyamat

A kód stabilitásának megőrzése érdekében szigorú "Feature Branch" modellt követünk.

* **SZIGORÚ TILALOM AZ AI SZÁMÁRA:** Soha ne futtass önállóan `git commit`, `git merge` vagy `git push` parancsot! Minden esetben várd meg a fejlesztő kifejezett utasítását a commitolásra, és a helyi tesztelés után a külön kifejezett engedélyét a merge-elésre!
* **`main` ág (PRODUCTION):** Kizárólag a stabil, éles verziót tartalmazza. Közvetlenül ide pusholni **szigorúan tilos!**
* **`develop` ág (STAGING):** A központi fejlesztői ág. A Netlify és a Railway ezt figyeli a tesztkörnyezet frissítéséhez.
* **`feature/...` ágak (LOCAL):** Minden új funkciót vagy hibajavítást egy dedikált ágon kell elvégezni, és ott kell hagyni tesztelésre, amíg a fejlesztő nem kéri a commitot / merge-et.
* **Névkonvenció:** A feature ágak neve tartalmazza a fázist vagy a funkciót (pl. `feature/phase-1-quick-wins`, `feature/phase-2-user-context-and-validation`, `feature/ai-termekfelismeres`).

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

### 2.3. Zéró Hardkódolt Nyelv & Gépi ErrorCode Szabvány
* **Tilos a hardkódolt szöveges hibaüzenet a backend válaszokban!** Sose adjunk vissza fix magyar vagy szlovák mondatokat (`NotFound("Cég nem található.")` vagy `BadRequest("...")`).
* **Strukturált ErrorCode kötelező:** Minden hibaesetben gépi azonosítót adjunk vissza JSON objektumban:
  ```csharp
  return NotFound(new { errorCode = "COMPANY_NOT_FOUND" });
  return BadRequest(new { errorCode = "CANNOT_DELETE_DEFAULT_LANGUAGE" });
  return Conflict(new { errorCode = "BOOKING_SLOT_TAKEN" });
  ```
* A kliensoldali Vue-i18n az `errorCode` alapján a helyi `src/locales/{lang}/errors.json` szótárból jeleníti meg a hibát a felhasználó nyelvén.

### 2.4. Felhasználói és Tenant Kontextus (`IUserContext` és `ITenantContext`)
* Tilos a kontrollerekben vagy szervizekben manuálisan Claim-eket keresgélni vagy parse-olni.
* Mindig a scoped `IUserContext` (`UserId`, `CompanyId`, `Email`, `Role`) és `ITenantContext` szolgáltatásokat használjuk dependency injection segítségével.

### 2.5. Szalonspecifikus Arculati Tartalom Védelme (Tenant Brand Preservation)
* A szalon egyedi bemutatkozó és marketing szövegeit (`home.introTitle`, `home.introText`, `home.subtitle`, `home.welcome`, `home.checkGallery`) a `UiTranslationOverrides` tábla őrzi.
* **Szigorú Tilos:** Tilos ezeket a rekordokat tömeges szótárfrissítéssel, reseteléssel vagy seedeléssel törölni vagy módosítani! A `ResetOverrides` és karbantartó eljárások kötelezően kivételt képeznek a `home.*` prefixű kulcsokra.

---

## 3. Frontend Kódolási Szabályok (Vue 3)

### 3.1. Kliensoldali Képtömörítés (SZIGORÚ SZABÁLY!)
* **Railway Egress Védelem:** Szigorúan tilos nyers, 5–15 MB-os mobilkamerás fotókat feltölteni a szerverre!
* Minden képet a feltöltés előtt kötelező átméretezni és optimalizálni a [`src/utils/imageCompressor.js`](file:///C:/Codes/Tomi/skanisalon/Soluvion/soluvion-web/src/utils/imageCompressor.js) segédprogrammal (`compressImage` vagy `compressImages`). Célméret: max. 1600–1800px, 0.8–0.85 minőség (~150–350 KB).

### 3.2. Autentikáció és Állapotkezelés (authStore)
* Be- és kijelentkezéskor a központi [`authStore.js`](file:///C:/Codes/Tomi/skanisalon/Soluvion/soluvion-web/src/stores/authStore.js) Pinia store-t használjuk (`authStore.setToken(...)`, `authStore.logout()`).
* **Tilos a `window.location.reload()` hack!** A reaktív store gondoskodik az azonnali UI frissülésről és a sima router navigációról.

### 3.3. Aszinkron Állapotkezelés és Race Condition Védelem
* **A `useAutoSaveQueue` kötelező:** Olyan funkcióknál, amelyek gyors, egymás utáni API hívásokat generálnak (pl. Drag-and-Drop rendezés, szövegmezők gépelése), kötelező a `useAutoSaveQueue` composable használata.
* **Párhuzamos hívások tilalma:** Sose engedjünk párhuzamos felülíró API hívásokat ugyanarra az elemre.

### 3.4. PrimeVue v4 Kompatibilitás és Téma
* Kötelező használni a v4 szabványokat (`<Select>`, `<Tabs>`, `<ToggleSwitch>`, stb.).
* A PrimeVue `darkModeSelector: '.dark'` konfigurációt használja, így a szalon sötét/világos stílusa mindig megegyezik a `companyStore` által beállított témával, függetlenül az OS beállításaitól.

### 3.5. API Hívások és Hálózati Réteg
* Szigorúan tilos közvetlen `axios` hívásokat írni a komponensekbe. Mindig a dedikált szerviz réteget használjuk (`inventoryApi`, `productApi`, `tenantApi`, stb.), vagy a központi `src/services/api.js` klienst.

### 3.6. Többnyelvűsítés (i18n) és Feliratok
* **Zéró Hardcode Nyelv a Kliensben:** Tilos keményen beégetett szöveget hagyni a template-ekben, szkriptekben, hibakezelőkben vagy konzol logokban (`console.error("Hiba...")` helyett angol vagy kódolt log).
* Minden UI felirat a `src/locales/{hu, sk, en}/` megfelelő JSON fájljaiba kerüljön `$t('kulcs')` hívással.
* **Kétlépcsős fordítás (te/t minta):** Dinamikus hibakódoknál és opcionális fordításoknál a `te()` segédfüggvénnyel ellenőrizzük a kulcs létezését a fordítás előtt:
  ```javascript
  const code = err.response?.data?.errorCode;
  if (code && te('errors.' + code)) {
    errorMsg.value = t('errors.' + code);
  } else {
    errorMsg.value = t('errors.generic');
  }
  ```
* **Tenant Nyelvi Szuverenitás:** Ha a tenant csupán egyetlen nyelvet aktivált, a fejlécből automatikusan elrejtjük a nyelvválasztó ikont.

---

## 4. Fejlesztési Ellenőrzőlista (Definition of Done)

Mielőtt egy feladatot befejezettnek tekintünk és a `develop` ágra merge-elünk, ellenőrizni kell az alábbi pontokat:

- [ ] **DTO és Validáció:** Minden új végpont saját DTO-t használ és DataAnnotations attribútumokkal van ellátva.
- [ ] **Multi-Tenancy:** Az adatbázis műveletek `CompanyId` szűrése az `ITenantContext` / `IUserContext` alapján történik.
- [ ] **Zéró Hardkódolt Hibaüzenet:** A backend `errorCode` azonosítót ad vissza, nem szöveges mondatot.
- [ ] **3 Nyelv Konzisztenica:** Az új UI szövegek mindhárom alapnyelv (`hu`, `sk`, `en`) szótárába bekerültek.
- [ ] **Képtömörítés:** Minden fájlfeltöltés előtt lefut a kliensoldali tömörítő (`imageCompressor.js`).
- [ ] **Tiszta Konzol:** Nincsenek ideiglenes debug `console.log` sorok vagy hardkódolt magyar hibaüzenetek.
- [ ] **Branching:** A kód tesztelt feature branch-ről kerül a `develop` ágra. Közvetlenül a `main`-re pusholni tilos!