// src/locales/hu/index.js
// A magyar mester nyelvi fájl (Master Template) modulokból áll össze:
// minden src/locales/hu/*.json fájl egy-egy rész, a végeredmény mélyen összefésült objektum.
// A base.json az alap, a többi ábécé sorrendben fedi át (azonos kulcs esetén a későbbi nyer).
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

const hu = orderedPaths.reduce((acc, path) => deepMerge(acc, modules[path]), {});

export default hu;

