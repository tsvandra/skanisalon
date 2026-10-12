/**
 * Kliensoldali képtömörítő segédprogram a Soluvion alkalmazáshoz.
 * Segítségével a nagyméretű (10-40 MB-os mobilkamerás) fotók feltöltés előtt
 * automatikusan lekicsinyítésre és optimalizálásra kerülnek a böngészőben (HTML5 Canvas segítségével).
 * 
 * Előnyei:
 * - 95-98%-kal kevesebb hálózati forgalom (Railway Network Egress megtakarítás).
 * - Jelentősen gyorsabb feltöltési idő mobilhálózaton.
 * - Drasztikusan kevesebb memóriaterhelés a szerveren és a böngészőben.
 */

/**
 * Egyetlen képfájl átméretezése és tömörítése.
 * 
 * @param {File|Blob} file Az eredeti képfájl
 * @param {Object} options Beállítások (maxWidth, maxHeight, quality, mimeType, skipIfSmallerThanBytes)
 * @returns {Promise<File>} A tömörített új File objektum
 */
export async function compressImage(file, options = {}) {
  const {
    maxWidth = 1800,
    maxHeight = 1800,
    quality = 0.82,
    mimeType = 'image/jpeg',
    skipIfSmallerThanBytes = 350 * 1024 // 350 KB alatt, ha a dimenziók is rendben vannak, nem piszkáljuk
  } = options;

  if (!file || !(file instanceof Blob)) {
    return file;
  }

  // Ha nem kép vagy SVG/GIF (vektor/animált), ne piszkáljuk
  if (!file.type.startsWith('image/') || file.type === 'image/svg+xml' || file.type === 'image/gif') {
    return file;
  }

  return new Promise((resolve) => {
    const objectUrl = URL.createObjectURL(file);
    const img = new Image();

    img.onload = () => {
      URL.revokeObjectURL(objectUrl);

      let width = img.naturalWidth || img.width;
      let height = img.naturalHeight || img.height;

      // Ha már eleve kicsi a felbontás és a fájlméret is a határ alatt van
      if (width <= maxWidth && height <= maxHeight && file.size <= skipIfSmallerThanBytes) {
        return resolve(file);
      }

      // Képarány megtartása
      if (width > maxWidth || height > maxHeight) {
        if (width / height > maxWidth / maxHeight) {
          height = Math.round((height * maxWidth) / width);
          width = maxWidth;
        } else {
          width = Math.round((width * maxHeight) / height);
          height = maxHeight;
        }
      }

      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;

      const ctx = canvas.getContext('2d');
      if (!ctx) {
        return resolve(file);
      }

      // Magas minőségű simítás a tisztán olvasható szövegekért (OCR / vonalkód)
      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = 'high';

      // Ha JPEG-et készítünk, fehér háttér az esetleges átlátszó pixelek feketedése ellen
      if (mimeType === 'image/jpeg') {
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, width, height);
      }

      ctx.drawImage(img, 0, 0, width, height);

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            return resolve(file);
          }

          // Ha az eredmény valamiért nagyobb lenne, mint az eredeti, tartsuk meg az eredetit
          if (blob.size >= file.size) {
            return resolve(file);
          }

          const originalName = file.name || 'image.jpg';
          const baseName = originalName.substring(0, originalName.lastIndexOf('.')) || originalName;
          
          let extension = '.jpg';
          if (mimeType === 'image/png') extension = '.png';
          else if (mimeType === 'image/webp') extension = '.webp';

          const newFileName = `${baseName}${extension}`;

          const compressedFile = new File([blob], newFileName, {
            type: mimeType,
            lastModified: Date.now()
          });

          console.log(
            `[ImageCompressor] ${file.name}: ${(file.size / 1024).toFixed(0)} KB -> ${(compressedFile.size / 1024).toFixed(0)} KB (-${Math.round((1 - compressedFile.size / file.size) * 100)}%) | ${img.naturalWidth}x${img.naturalHeight} -> ${width}x${height}`
          );

          resolve(compressedFile);
        },
        mimeType,
        quality
      );
    };

    img.onerror = (err) => {
      URL.revokeObjectURL(objectUrl);
      console.warn('[ImageCompressor] Hiba a kép betöltésekor, eredeti fájl megtartása:', err);
      resolve(file);
    };

    img.src = objectUrl;
  });
}

/**
 * Több kép párhuzamos tömörítése.
 * 
 * @param {Array<File|Blob>} files Fájlok listája
 * @param {Object} options Beállítások
 * @returns {Promise<Array<File>>}
 */
export async function compressImages(files, options = {}) {
  if (!Array.isArray(files) || files.length === 0) return [];
  return Promise.all(files.map((file) => compressImage(file, options)));
}

