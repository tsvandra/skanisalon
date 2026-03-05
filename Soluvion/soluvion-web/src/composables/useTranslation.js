import { ref } from 'vue';
import api from '@/services/api';

export function useTranslation() {
  // A töltés állapotát is a composable kezeli!
  const translatingField = ref(null);

  /**
   * Közös AI fordító logika
   * @param {Object} options.obj - Az objektum, amit fordítunk (pl. service, group, image)
   * @param {String} options.fieldName - A mező neve, amiben a nyelvi szótár van (pl. 'name', 'description')
   * @param {String} options.targetLang - A célnyelv kódja (pl. 'en', 'de')
   * @param {String} options.defaultLang - Az alapértelmezett nyelv (amiből fordítunk)
   * @param {String} options.loadingKey - Egyedi azonosító a spinnerhez (pl. '12-name-en')
   * @param {Function} options.onSuccess - (Opcionális) Callback függvény, ami lefut sikeres fordítás után
   */
  const translateField = async ({ obj, fieldName, targetLang, defaultLang, loadingKey, onSuccess }) => {
    if (targetLang === defaultLang) {
      alert(`A(z) '${defaultLang}' az alapértelmezett nyelv, erről fordítunk a többire! Válts nyelvet fentről.`);
      return;
    }

    const sourceText = obj[fieldName][defaultLang] || obj[fieldName][targetLang];
    if (!sourceText || sourceText.trim() === '') return;

    translatingField.value = loadingKey;

    try {
      const response = await api.post('/api/Translation', { text: sourceText, targetLanguage: targetLang });
      if (response.data && response.data.translatedText) {
        // Frissítjük a modellt
        obj[fieldName][targetLang] = response.data.translatedText;

        // Ha van megadva sikeres lefutás utáni akció (pl. mentés, textarea méretezés), hívjuk meg
        if (onSuccess) {
          await onSuccess(obj);
        }
      }
    } catch (err) {
      console.error("Fordítási hiba:", err);
      alert("Nem sikerült a fordítás.");
    } finally {
      translatingField.value = null;
    }
  };

  return {
    translatingField,
    translateField
  };
}
