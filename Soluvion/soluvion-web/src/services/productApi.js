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
    },
    aiScan(images) {
        const formData = new FormData();
        images.forEach(img => {
            formData.append('images', img);
        });
        return api.post('/api/products/ai-scan', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    }
};

