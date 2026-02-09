import { ref } from 'vue';

// Egy Map-ben tároljuk a promise láncokat ID alapján
const saveQueues = new Map();

/**
 * Általános sorbaállító logika (Queue) automata mentéshez.
 * Megakadályozza, hogy a gyors egymásutáni kérések (pl. gépelés, drag) felülírják egymást.
 */
export function useAutoSaveQueue() {

  /**
   * Hozzáad egy mentési feladatot a sorhoz.
   * @param {string|number} itemId - Az elem egyedi azonosítója (amihez a sor tartozik)
   * @param {Function} taskFunction - Az aszinkron függvény, ami a tényleges API hívást végzi
   */
  const addToQueue = async (itemId, taskFunction) => {
    // Ha nincs még sor ehhez az elemhez, inicializáljuk
    if (!saveQueues.has(itemId)) {
      saveQueues.set(itemId, Promise.resolve());
    }

    const currentQueue = saveQueues.get(itemId);

    // Hozzáfüzzük az új feladatot a lánc végére
    const newPromise = currentQueue.then(async () => {
      try {
        await taskFunction();
      } catch (err) {
        console.error(`Hiba a sorbaállított mentésnél (ID: ${itemId}):`, err);
        throw err;
      }
    });

    // Frissítjük a sor végét
    saveQueues.set(itemId, newPromise);

    return newPromise;
  };

  return {
    addToQueue
  };
}
