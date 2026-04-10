# 01. Smart Booking Engine (Okos Idõpontfoglaló)

**Dátum:** 2026.03.17.
**Projekt:** Soluvion (Skani Salon)
**Modul:** Public Booking (Vendég Foglaló Folyamat)
**Státusz:** MVP Stabilizálva (Prototípus)

Ez a dokumentum a Soluvion rendszer publikus idõpontfoglaló moduljának mûködését, technikai felépítését és a jövõbeli fejlesztési terveket foglalja össze.

---

## 1. Elképzelés (Vision / Concept)

A modul célja, hogy a vállalkozások (szalonok, szerelõmûhelyek, orvosi rendelõk) ügyfelei gyorsan, regisztráció nélkül tudjanak idõpontot foglalni egy modern, letisztult felületen. 

**Üzleti és SaaS Követelmények:**
* **Zéró Hardcode & Szolgáltatás-függetlenség:** A foglalási folyamat nem tartalmazhat beégetett iparág-specifikus kérdéseket (pl. "Milyen hosszú a haja?"). Minden ilyen adatot dinamikusan kell kezelni.
* **Okos Idõkalkuláció:** A rendszernek tudnia kell kezelni a szolgáltatás-variációkat, és dinamikusan kiszámolni a foglalás teljes idejét és árát.
* **Ütközésvédelem (Conflict Prevention):** A foglaló motornak (Smart Booking Engine) szigorúan blokkolnia kell a dupla foglalásokat, figyelembe véve a munkatársak beosztását és a meglévõ foglalásokat.
* **Fantom Profilok (Guest Booking):** A vendégeknek nem kell bejelentkezniük. A rendszer az e-mail cím alapján automatikusan azonosítja õket, és a háttérben frissíti a vendégprofilt (`CompanyCustomer`).
* **Ütközésvédelem (Conflict Prevention) és Rákönyvelés (Double-Booking):** A foglaló motornak alapértelmezetten blokkolnia kell az ütköző (párhuzamos) időpontokat. Ugyanakkor az Adminisztrációs felületen a szalon menedzsmentje számára (HTTP 409 Conflict hibára építve) lehetőséget kell biztosítani egy figyelmeztető ablak után a kényszerített mentésre (`force: true`), így lehetővé téve a párhuzamos munkavégzést.

---

## 2. Megvalósított Rész (Implemented State)

A modul egy teljesen mûködõképes, 3-lépéses (Stepper) SPA (Single Page Application) folyamaton keresztül viszi végig a vendéget, újra-töltés nélkül.

### 2.1. Backend Architektúra
* **Adatbázis Entitások:** * `CompanyCustomer`: Tárolja a vendégeket. A `UserId` mezõ nullable (opcionális), hogy bejelentkezés nélküli vendégeket is tudjunk fogadni. Egyedi jellemzõk a `jsonb Attributes` mezõben tárolódnak.
  * `Appointment` és `AppointmentItem`: Tárolják a foglalás alapadatait, státuszát és a kiválasztott szolgáltatás-variánsokat.
* **API Végpont:** `PublicBookingController` (Token nélküli elérés).
* **Üzleti Logika:** A `CreateGuestBooking` metódus tranzakciókezeléssel (BeginTransaction) dolgozik:
  1. Megkeresi vagy létrehozza a vendéget (E-mail alapján a C# memóriában szûrve a JSONB fordítási hibák elkerülése végett).
  2. Meghívja az ütközésvizsgáló szervizt.
  3. Kiosztja az idõpontot a kiválasztott dolgozónak (ha a bejövõ `EmployeeId = 0`, a rendszer automatikusan az elsõ elérhetõ aktív munkatársat rendeli hozzá).

### 2.2. Frontend Architektúra (Vue 3 + Tailwind v4)
* **Single Responsibility Principle (SRP):** A foglaló felület szét lett darabolva az alábbi struktúrára:
  * `src/views/BookingView.vue` (Karmester: Állapotkezelés, API hívás, Sikeres képernyõ megjelenítése)
  * `src/components/booking/StepServices.vue` (1. Lépés: Szolgáltatások dinamikus listázása és kiválasztása)
  * `src/components/booking/StepDetails.vue` (2. Lépés: Alapadatok és dinamikus `jsonb` mezõk bekérése)
  * `src/components/booking/StepDateTime.vue` (3. Lépés: Dolgozó és Dátum/Idõ választása)
* **API Réteg:** A hálózati kommunikáció kiszervezve a `src/services/bookingApi.js` fájlba.
* **Lokalizáció (i18n):** Az összes szöveg a `hu.json`-bõl érkezik (`$t('booking...')`), kompatibilis az AI Varázspálcával.
* **UX:** Kifinomult, reszponzív UI, ahol a hibák (pl. 500-as Backend hiba) olvasható formában (Toast/Message) jelennek meg, sikeres foglaláskor pedig az ûrlapot egy animált visszaigazoló képernyõ váltja fel.

---

## 3. Teendõk és Technikai Adósság (To-Dos / Tech Debt)

Bár az MVP stabilan mûködik, a végleges (Production Ready) állapothoz az alábbi fejlesztések szükségesek:

- [ ] **Dinamikus Szabad Idõpontok (Time Slot Picker):** A jelenlegi natív HTML `<input type="datetime-local">` mezõ UX szempontból gyenge. A backendnek kell egy új végpont, ami a kiválasztott napra visszaadja a *ténylegesen szabad* idõsávokat (pl. 09:00, 09:30, 10:00), figyelembe véve a szolgáltatás kalkulált hosszát.
- [ ] **Munkatársak Betöltése:** A 3. lépésben a legördülõ menü jelenleg csak a "Bárki" (0) opciót tartalmazza. API-n keresztül le kell kérni az adott céghez (Tenant) tartozó aktív dolgozókat (`CompanyEmployee`).
- [ ] **Dinamikus Ûrlap Generálás (Settings):** A 2. lépésben (StepDetails) a jövõben a cégtulajdonos az Admin beállításoknál határozhatja meg, hogy milyen extra mezõket kér be (pl. Rendszám, Hajhossz). Ezeket v-for ciklussal kell legenerálni a Frontend `attributes` JSONB objektumába.
- [ ] **Értesítési Rendszer (Notifications):** Sikeres foglalás esetén e-mail kiküldése a Vendégnek (visszaigazolás) és az Ownernek (új foglalás értesítõ) egy háttérszolgáltatáson (pl. SendGrid) keresztül.