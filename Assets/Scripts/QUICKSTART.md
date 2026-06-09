# Quick Start Guide - Level Maker Tool

## Bắt đầu nhanh trong 5 phút

### Bước 1: Mở Tool
```
Unity Menu > Tools > Level Maker
```

### Bước 2: Tạo Level Container
1. Trong Level Maker window, click **"Create Level Container"**
2. Một GameObject "LevelContainer" sẽ được tạo trong Hierarchy

### Bước 3: Bắt đầu đặt Blocks
1. Chọn mode **Place**
2. Chọn **Cube** làm primitive type
3. Set Default Size = (2, 2, 2)
4. Enable **Snap to Grid**
5. Set Grid Size = 0.5

### Bước 4: Tạo nền (Ground)
1. Chọn **Plane** primitive
2. Set Default Size = (10, 1, 10)
3. Click vào scene để đặt nền
4. Đổi tên thành "Ground"

### Bước 5: Đặt các khối trên nền
1. Chọn lại **Cube** primitive
2. Di chuột lên surface của Ground
3. Click để đặt các cubes
4. Thử **Shift + Click** để align theo normal

### Bước 6: Resize Blocks
1. Chuyển sang mode **Resize**
2. Select một cube
3. Kéo handles để thay đổi size

### Bước 7: Thử Boolean Operations
1. Đặt 2 cubes chồng lên nhau
2. Chuyển sang mode **Boolean**
3. Chọn operation **Subtract**
4. Select cube đầu tiên > **Add to Boolean List**
5. Select cube thứ hai > **Add to Boolean List**
6. Click **Perform Boolean Operation**

### Bước 8: Export Level
1. Mở **Tools > Level Exporter**
2. Select "LevelContainer" trong Hierarchy
3. Kéo vào field **Level Container**
4. Nhập Level Name: "MyFirstLevel"
5. Click **Export Level**

## Ví dụ: Tạo một căn phòng đơn giản

### 1. Tạo nền
- Primitive: Plane
- Size: (10, 1, 10)

### 2. Tạo 4 bức tường
- Primitive: Cube
- Size: (10, 3, 0.2) cho tường trước/sau
- Size: (0.2, 3, 10) cho tường trái/phải

### 3. Tạo cửa sổ bằng Boolean Subtract
- Đặt một cube nhỏ vào tường
- Subtract cube đó từ tường

### 4. Tạo trần
- Primitive: Plane
- Size: (10, 1, 10)
- Position: Y = 3

### 5. Thêm nội thất
- Đặt các cubes nhỏ làm bàn, ghế
- Resize để tạo hình dạng phù hợp

## Tips & Tricks

### Tip 1: Snap Precision
Với Grid Size = 0.5, các objects sẽ align chính xác

### Tip 2: Group Objects
Select nhiều objects > Click **"Group Selected"** để organize

### Tip 3: Duplicate nhanh
Select object > Inspector > **Duplicate** button

### Tip 4: Align to Ground
Nếu object bị float, chọn nó và click **Align to Ground**

### Tip 5: Save thường xuyên
Export level thường xuyên để backup công việc

## Keyboard Workflow
```
1. Left Click - Đặt object
2. Shift + Left Click - Đặt với surface alignment
3. Ctrl + Z - Undo
4. Ctrl + D - Duplicate (Unity default)
5. F - Focus on selected
```

## Common Issues

### Issue: Preview không hiện
**Solution**: Đảm bảo bạn đang ở mode Place và di chuột trong Scene view

### Issue: Objects không snap
**Solution**: Enable "Snap to Grid" trong Placement Settings

### Issue: Boolean không hoạt động
**Solution**: Đảm bảo cả 2 objects đều có MeshFilter component

### Issue: Cannot place on surface
**Solution**: Object cần có Collider để raycast detect được

## Next Steps
- Thử nghiệm với các primitive types khác
- Tạo các structures phức tạp hơn
- Export và share levels với team
- Customize material cho blocks

---
Happy Level Making! 🎮🏗️
