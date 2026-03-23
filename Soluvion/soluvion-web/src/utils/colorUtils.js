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
