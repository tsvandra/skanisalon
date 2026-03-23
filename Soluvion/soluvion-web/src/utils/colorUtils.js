// src/utils/colorUtils.js

export const getCustomerColor = (customerId) => {
  if (!customerId) return 'hsl(0, 0%, 90%)';
  // Aranymetszés (Golden Ratio) szorzó a színek egyenletes szórásához
  const hue = (Number(customerId) * 137.508) % 360;
  return `hsl(${hue}, 70%, 85%)`; // Világos pasztell (Háttér)
};

export const getCustomerColorDarker = (customerId) => {
  if (!customerId) return 'hsl(0, 0%, 70%)';
  const hue = (Number(customerId) * 137.508) % 360;
  return `hsl(${hue}, 60%, 65%)`; // Sötétebb (Keretekhez)
};
