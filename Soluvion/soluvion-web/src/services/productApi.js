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
    aiScan(images, primaryImageIndex = null) {
        const formData = new FormData();
        images.forEach(img => {
            formData.append('images', img);
        });
        if (primaryImageIndex !== null && primaryImageIndex !== undefined) {
            formData.append('primaryImageIndex', primaryImageIndex);
        }
        return api.post('/api/products/ai-scan', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    },
    uploadImage(file) {
        const formData = new FormData();
        formData.append('file', file);
        return api.post('/api/products/upload-image', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });
    }
};

