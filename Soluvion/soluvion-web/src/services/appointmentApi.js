import api from './api'; // A meglévő axios példányod

export default {
  getAppointments(startDate, endDate, employeeId = null) {
    let url = `/api/Appointment?start=${startDate.toISOString()}&end=${endDate.toISOString()}`;
    if (employeeId) {
      url += `&employeeId=${employeeId}`;
    }
    return api.get(url);
  },

  createAppointment(appointmentData) {
    return api.post('/api/Appointment', appointmentData);
  },

  updateAppointment(id, appointmentData) {
    return api.put(`/api/Appointment/${id}`, appointmentData);
  },

  getPendingAppointments() {
    return api.get('/api/Appointment/pending');
  },

  updateAppointmentStatus(id, statusData) {
    return api.patch(`/api/Appointment/${id}/status`, statusData);
  },

  deleteAppointment(id) {
    return api.delete(`/api/Appointment/${id}`);
  },

  parseChatBooking(formData) {
    return api.post('/api/Appointment/parse-chat-booking', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  updatePendingFromChat(id, formData) {
    return api.post(`/api/Appointment/${id}/update-from-chat`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  getChatInbox() {
    return api.get('/api/ChatInbox');
  },

  updateChatInboxConfig(configData) {
    return api.put('/api/ChatInbox/config', configData);
  },

  markChatConversationRead(conversationId) {
    return api.post(`/api/ChatInbox/conversations/${conversationId}/read`);
  },

  sendChatReply(conversationId, replyData) {
    return api.post(`/api/ChatInbox/conversations/${conversationId}/reply`, replyData);
  },

  simulateIncomingChat(payload) {
    return api.post('/api/ChatInbox/simulate-incoming', payload);
  }
};

