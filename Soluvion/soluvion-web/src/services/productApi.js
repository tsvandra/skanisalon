import api from './api';

export default {
    getAllProducts() {
        return api.get('/api/products');
    },
    getProduct(id) {
        return api.get(`/api/products/${id}`);
    },
    createProduct(data) {
        return api.post('/api/products', data);
    },
    updateProduct(id, data) {
        return api.put(`/api/products/${id}`, data);
    },
    deleteProduct(id) {
        return api.delete(`/api/products/${id}`);
    }
};

