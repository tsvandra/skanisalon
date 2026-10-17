/**
 * Bitmap-to-SVG Vectorizer (Marching Squares + Ramer-Douglas-Peucker + Quadratic Curve Smoothing)
 * Converts a high-contrast or transparent-background AI icon image into a compact,
 * theme-colored <svg fill="currentColor" fill-rule="evenodd"> vector string (< 3800 chars).
 */

const GRID_SIZE = 96;
const MAX_SVG_LENGTH = 3800;

/**
 * Perpendicular distance from point P to segment AB
 */
function perpendicularDistance(p, a, b) {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  if (dx === 0 && dy === 0) {
    return Math.hypot(p.x - a.x, p.y - a.y);
  }
  const t = ((p.x - a.x) * dx + (p.y - a.y) * dy) / (dx * dx + dy * dy);
  const clampedT = Math.max(0, Math.min(1, t));
  const projX = a.x + clampedT * dx;
  const projY = a.y + clampedT * dy;
  return Math.hypot(p.x - projX, p.y - projY);
}

/**
 * Ramer-Douglas-Peucker polyline simplification
 */
function simplifyRDP(points, epsilon) {
  if (points.length <= 3) return points;
  let dmax = 0;
  let index = 0;
  const end = points.length - 1;

  for (let i = 1; i < end; i++) {
    const d = perpendicularDistance(points[i], points[0], points[end]);
    if (d > dmax) {
      index = i;
      dmax = d;
    }
  }

  if (dmax > epsilon) {
    const rec1 = simplifyRDP(points.slice(0, index + 1), epsilon);
    const rec2 = simplifyRDP(points.slice(index), epsilon);
    return rec1.slice(0, rec1.length - 1).concat(rec2);
  }
  return [points[0], points[end]];
}

/**
 * Smooths a closed polygon by averaging neighbors (removes staircase pixel artifacts)
 */
function smoothClosedPolygon(points, iterations = 2) {
  let pts = points;
  for (let iter = 0; iter < iterations; iter++) {
    const n = pts.length;
    if (n < 4) break;
    const next = [];
    for (let i = 0; i < n; i++) {
      const prev = pts[(i - 1 + n) % n];
      const curr = pts[i];
      const succ = pts[(i + 1) % n];
      next.push({
        x: prev.x * 0.25 + curr.x * 0.5 + succ.x * 0.25,
        y: prev.y * 0.25 + curr.y * 0.5 + succ.y * 0.25
      });
    }
    pts = next;
  }
  return pts;
}

/**
 * Signed area of a closed polygon (to filter tiny noise specks)
 */
function polygonArea(points) {
  let area = 0;
  const n = points.length;
  for (let i = 0; i < n; i++) {
    const j = (i + 1) % n;
    area += points[i].x * points[j].y - points[j].x * points[i].y;
  }
  return Math.abs(area) * 0.5;
}

/**
 * Extracts closed contour loops from a binary grid (size W x H) using directed cell-edge tracing
 */
function extractContours(grid, width, height) {
  // Build directed boundary unit edges between foreground (1) and background (0) pixels
  // Foreground pixel at (x, y) occupies [x, x+1] x [y, y+1]
  const outEdges = new Map();
  const keyOf = (x, y) => `${x},${y}`;

  const addEdge = (x1, y1, x2, y2) => {
    const k = keyOf(x1, y1);
    if (!outEdges.has(k)) outEdges.set(k, []);
    outEdges.get(k).push({ x: x2, y: y2, used: false });
  };

  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (grid[y * width + x] !== 1) continue;
      // Top edge: (x, y) -> (x+1, y) if pixel above is 0
      if (y === 0 || grid[(y - 1) * width + x] === 0) {
        addEdge(x, y, x + 1, y);
      }
      // Right edge: (x+1, y) -> (x+1, y+1) if pixel right is 0
      if (x === width - 1 || grid[y * width + (x + 1)] === 0) {
        addEdge(x + 1, y, x + 1, y + 1);
      }
      // Bottom edge: (x+1, y+1) -> (x, y+1) if pixel below is 0
      if (y === height - 1 || grid[(y + 1) * width + x] === 0) {
        addEdge(x + 1, y + 1, x, y + 1);
      }
      // Left edge: (x, y+1) -> (x, y) if pixel left is 0
      if (x === 0 || grid[y * width + (x - 1)] === 0) {
        addEdge(x, y + 1, x, y);
      }
    }
  }

  const contours = [];
  for (const [startKey, edges] of outEdges.entries()) {
    for (const firstEdge of edges) {
      if (firstEdge.used) continue;
      firstEdge.used = true;

      const [sx, sy] = startKey.split(',').map(Number);
      const loop = [{ x: sx, y: sy }];
      let curr = { x: firstEdge.x, y: firstEdge.y };

      let safety = width * height * 4;
      while ((curr.x !== sx || curr.y !== sy) && safety-- > 0) {
        loop.push(curr);
        const candidates = outEdges.get(keyOf(curr.x, curr.y));
        if (!candidates) break;
        const nextEdge = candidates.find(e => !e.used);
        if (!nextEdge) break;
        nextEdge.used = true;
        curr = { x: nextEdge.x, y: nextEdge.y };
      }

      if (loop.length >= 4) {
        contours.push(loop);
      }
    }
  }

  return contours;
}

/**
 * Formats simplified contours into a compact SVG path string
 */
function buildSvgFromContours(contours, epsilon) {
  const fmt = (n) => Math.round(n * 10) / 10;
  const subpaths = [];

  for (const rawLoop of contours) {
    const closed = [...rawLoop, rawLoop[0]];
    const simplified = simplifyRDP(closed, epsilon);
    if (simplified.length < 4) continue;

    const pts = simplified.slice(0, simplified.length - 1);
    let d = `M${fmt(pts[0].x)} ${fmt(pts[0].y)}`;
    for (let i = 1; i < pts.length; i++) {
      d += `L${fmt(pts[i].x)} ${fmt(pts[i].y)}`;
    }
    d += 'Z';
    subpaths.push(d);
  }

  if (subpaths.length === 0) return '';
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${GRID_SIZE} ${GRID_SIZE}" fill="currentColor" fill-rule="evenodd" stroke="none"><path d="${subpaths.join('')}"/></svg>`;
}

/**
 * Converts a base64 PNG/WebP dataUrl into a clean <svg> string under 3800 chars.
 * @param {string} dataUrl
 * @returns {Promise<string>}
 */
export async function vectorizeImageToSvg(dataUrl) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = 'anonymous';
    img.onload = () => {
      try {
        const canvas = document.createElement('canvas');
        canvas.width = GRID_SIZE;
        canvas.height = GRID_SIZE;
        const ctx = canvas.getContext('2d');

        // Leave a 4px padding margin so edge-touching icons close cleanly
        const pad = 4;
        ctx.clearRect(0, 0, GRID_SIZE, GRID_SIZE);
        ctx.drawImage(img, pad, pad, GRID_SIZE - pad * 2, GRID_SIZE - pad * 2);

        const imgData = ctx.getImageData(0, 0, GRID_SIZE, GRID_SIZE).data;
        const grid = new Uint8Array(GRID_SIZE * GRID_SIZE);

        // Check corner pixels inside the drawn region to see if background is transparent or solid
        const cornerIndices = [
          (pad * GRID_SIZE + pad) * 4,
          (pad * GRID_SIZE + (GRID_SIZE - pad - 1)) * 4,
          ((GRID_SIZE - pad - 1) * GRID_SIZE + pad) * 4,
          ((GRID_SIZE - pad - 1) * GRID_SIZE + (GRID_SIZE - pad - 1)) * 4
        ];

        let avgCornerAlpha = 0;
        let avgCornerLum = 0;
        for (const idx of cornerIndices) {
          avgCornerAlpha += imgData[idx + 3];
          avgCornerLum += 0.299 * imgData[idx] + 0.587 * imgData[idx + 1] + 0.114 * imgData[idx + 2];
        }
        avgCornerAlpha /= cornerIndices.length;
        avgCornerLum /= cornerIndices.length;

        const isTransparentBg = avgCornerAlpha < 128;

        for (let i = 0; i < GRID_SIZE * GRID_SIZE; i++) {
          const r = imgData[i * 4];
          const g = imgData[i * 4 + 1];
          const b = imgData[i * 4 + 2];
          const a = imgData[i * 4 + 3];

          if (isTransparentBg) {
            grid[i] = a > 96 ? 1 : 0;
          } else {
            if (a < 64) {
              grid[i] = 0;
            } else {
              const lum = 0.299 * r + 0.587 * g + 0.114 * b;
              // Foreground is whatever contrasts strongly with the corner background luminance
              grid[i] = Math.abs(lum - avgCornerLum) > 70 ? 1 : 0;
            }
          }
        }

        // Extract and smooth contours, keeping the largest meaningful shapes
        const rawContours = extractContours(grid, GRID_SIZE, GRID_SIZE)
          .map(c => ({ loop: smoothClosedPolygon(c, 2), area: polygonArea(c) }))
          .filter(c => c.area >= 4)
          .sort((a, b) => b.area - a.area)
          .slice(0, 24)
          .map(c => c.loop);

        if (rawContours.length === 0) {
          resolve('');
          return;
        }

        // Adaptively pick epsilon so the SVG fits comfortably inside the 4000-char DB column
        const epsilons = [0.65, 0.9, 1.2, 1.6, 2.1, 2.8, 3.6];
        let bestSvg = '';
        for (const eps of epsilons) {
          const candidate = buildSvgFromContours(rawContours, eps);
          bestSvg = candidate;
          if (candidate.length > 0 && candidate.length <= MAX_SVG_LENGTH) {
            break;
          }
        }

        resolve(bestSvg.length <= MAX_SVG_LENGTH ? bestSvg : bestSvg.slice(0, MAX_SVG_LENGTH));
      } catch (err) {
        reject(err);
      }
    };
    img.onerror = (e) => reject(e);
    img.src = dataUrl;
  });
}

