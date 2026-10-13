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
    },
    scanDeliveryNote(files) {
        const formData = new FormData();
        files.forEach(f => {
            formData.append('images', f);
        });
        return api.post('/api/inventory/scan-delivery-note', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    },
    importDeliveryNote(data) {
        return api.post('/api/inventory/import-delivery-note', data);
    }
};

