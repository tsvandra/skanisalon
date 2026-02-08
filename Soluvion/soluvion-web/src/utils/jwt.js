export const getPayloadFromToken = () => {
  const token = localStorage.getItem('salon_token');
  if (!token) return null;

  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error("Token dekódolási hiba:", e);
    return null;
  }
};

// Külön segédfüggvény, ha csak a CompanyId kell (kényelmi funkció)
export const getCompanyIdFromToken = () => {
  const payload = getPayloadFromToken();
  // Kezeljük a kisbetűs/nagybetűs eltérést is
  return payload?.CompanyId || payload?.companyId || null;
};


export const parseJwt = (token) => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error("JWT Parse Error:", e);
    return null;
  }
};
