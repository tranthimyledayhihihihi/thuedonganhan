# Các URL hình ảnh thay thế cho Lễ phục tốt nghiệp

## Từ Pexels
1. `https://images.pexels.com/photos/267885/pexels-photo-267885.jpeg?auto=compress&cs=tinysrgb&w=800`
2. `https://images.pexels.com/photos/1205651/pexels-photo-1205651.jpeg?auto=compress&cs=tinysrgb&w=800`
3. `https://images.pexels.com/photos/1205033/pexels-photo-1205033.jpeg?auto=compress&cs=tinysrgb&w=800`

## Từ Pixabay
1. `https://cdn.pixabay.com/photo/2016/03/09/09/22/workplace-1245776_960_720.jpg`
2. `https://cdn.pixabay.com/photo/2017/05/11/11/15/workplace-2303851_960_720.jpg`

## Từ Placeholder Services
1. `https://via.placeholder.com/800x600/667eea/ffffff?text=Graduation+Gown`
2. `https://placehold.co/800x600/667eea/white?text=Le+Phuc+Tot+Nghiep`
3. `https://dummyimage.com/800x600/667eea/fff&text=Graduation`

## Từ Lorem Picsum (Random)
1. `https://picsum.photos/800/600?random=graduation`
2. `https://picsum.photos/seed/graduation/800/600`

## Cách sử dụng:

Thay thế URL trong file Index.cshtml:
```html
<img src="[URL_TỪ_DANH_SÁCH_TRÊN]" alt="Lễ phục tốt nghiệp" ... />
```

## Hoặc tải về local:

1. Mở một trong các URL trên trong browser
2. Click chuột phải > Save Image As
3. Lưu vào: `WEB/wwwroot/images/products/graduation-gown.jpg`
4. Sử dụng: `<img src="/images/products/graduation-gown.jpg" ... />`
