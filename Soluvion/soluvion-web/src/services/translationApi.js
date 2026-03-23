// src/services/translationApi.js
import api from './api';

export const translationApi = {
  getLanguages(companyId) {
    return api.get(`/api/Translation/languages/${companyId}`);
  },

  addLanguage(data) {
    return api.post('/api/Translation/add-language', data);
  },

  publishLanguage(companyId, languageCode) {
    return api.post('/api/Translation/publish', { companyId, languageCode });
  },

  deleteLanguage(companyId, langCode) {
    return api.delete(`/api/Translation/language/${companyId}/${langCode}`);
  },

  getOverrides(companyId, langCode) {
    return api.get(`/api/Translation/overrides/${companyId}/${langCode}`);
  },

  translateSingleText(text, targetLanguage, context = 'ui') {
    return api.post('/api/Translation', {
      text: text,
      targetLanguage: targetLanguage,
      context: context
    });
  }
};
