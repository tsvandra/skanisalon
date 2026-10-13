// src/locales/sk/index.js
// Slovenský jazykový balík zostavený z modulov (rovnako ako hu/index.js)
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

const sk = orderedPaths.reduce((acc, path) => deepMerge(acc, modules[path]), {});

export default sk;

