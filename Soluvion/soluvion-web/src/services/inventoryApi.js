import api from './api';

export default {
    getAllDocuments() {
        return api.get('/api/inventory');
    },
    getDocument(id) {
        return api.get(`/api/inventory/${id}`);
    },
    getProductHistory(productId) {
        return api.get(`/api/inventory/product/${productId}/history`);
    },
    updateDocumentNote(documentId, note) {
        return api.patch(`/api/inventory/${documentId}/note`, { note });
    },
    updateItemNote(itemId, note) {
        return api.patch(`/api/inventory/items/${itemId}/note`, { note });
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

