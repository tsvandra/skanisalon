// src/locales/en/index.js
// English language bundle assembled from modules (matching hu/index.js)
const modules = import.meta.glob('./*.json', { eager: true, import: 'default' });

const isPlainObject = (value) => value !== null && typeof value === 'object' && !Array.isArray(value);

const deepMerge = (target, source) => {
  Object.keys(source).forEach((key) => {
    if (isPlainObject(source[key]) && isPlainObject(target[key])) {
      deepMerge(target[key], source[key]);
    } else {
      target[key] = isPlainObject(source[key]) ? deepMerge({}, source[key]) : source[key];
    }
  });
  return target;
};

const orderedPaths = Object.keys(modules).sort((a, b) => {
  if (a === './base.json') return -1;
  if (b === './base.json') return 1;
  return a.localeCompare(b);
});

const en = orderedPaths.reduce((acc, path) => deepMerge(acc, modules[path]), {});

export default en;

