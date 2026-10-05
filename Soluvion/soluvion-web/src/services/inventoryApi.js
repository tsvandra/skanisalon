import api from './api';

export default {
    getAllDocuments() {
        return api.get('/api/inventory');
    },
    getDocument(id) {
        return api.get(`/api/inventory/${id}`);
    },
    createDocument(data) {
        return api.post('/api/inventory', data);
    },
    reverseAppointmentClosing(appointmentId) {
        return api.post(`/api/inventory/appointment/${appointmentId}/reverse`);
    }
};

