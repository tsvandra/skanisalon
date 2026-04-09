// src/services/companyAttributesApi.js (vagy add hozzá a meglévőhöz)
import api from './api';

const API_URL = '/api/company-attributes';

export default {
  getAttributes() {
    return api.get(API_URL);
  },
  createAttribute(data) {
    return api.post(API_URL, data);
  },
  updateAttribute(id, data) {
    return api.put(`${API_URL}/${id}`, data);
  },
  deleteAttribute(id) {
    return api.delete(`${API_URL}/${id}`);
  }
};
