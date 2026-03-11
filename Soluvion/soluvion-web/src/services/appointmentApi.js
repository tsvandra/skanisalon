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

  deleteAppointment(id) {
    return api.delete(`/api/Appointment/${id}`);
  }
};
