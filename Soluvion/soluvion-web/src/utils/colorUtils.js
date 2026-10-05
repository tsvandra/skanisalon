// src/utils/colorUtils.js

export const getCustomerColor = (customerId) => {
  if (!customerId) return 'hsl(0, 0%, 60%)';
  const hue = (Number(customerId) * 137.508) % 360;
  // Sokkal élénkebb és sötétebb, hogy kontrasztos legyen a fehér naptáron (S: 85%, L: 55%)
  return `hsl(${hue}, 85%, 55%)`;
};

export const getCustomerColorDarker = (customerId) => {
  if (!customerId) return 'hsl(0, 0%, 40%)';
  const hue = (Number(customerId) * 137.508) % 360;
  // Még sötétebb a keretekhez (S: 85%, L: 35%)
  return `hsl(${hue}, 85%, 35%)`;
};

// "Nem jelent meg" (NoShow = 4) státusz felismerése (szám vagy szöveg)
export const isNoShowStatus = (status) =>
  status === 4 || status === '4' || (typeof status === 'string' && status.toLowerCase() === 'noshow');

// Fekete, jól elkülönül a vendégek élénk színeitől a fehér naptár cellákon
export const NO_SHOW_COLOR = '#000000';

// Naptár pötty színe: nem megjelent foglalásnál fekete, egyébként a vendég színe
export const getAppointmentDotColor = (app) =>
  isNoShowStatus(app?.status) ? NO_SHOW_COLOR : getCustomerColor(app?.customerId);
