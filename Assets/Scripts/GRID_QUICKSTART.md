# Grid-Based Level Maker - Quick Start Guide

## Hệ thống Grid mới

Level Maker giờ sử dụng **hệ thống grid-based** - map được chia thành các cells có kích thước cố định, objects snap vào grid và chiếm các slots.

## Bắt đầu trong 5 phút

### Bước 1: Tạo Grid Manager
1. Mở **Tools > Level Maker**
2. Trong tab **Place**, click **"Create Grid Manager"**
3. Cấu hình Grid:
   - **Cell Size**: 1.0 (mỗi cell = 1m)
   - **Grid Dimensions**: 20x5x20 (20x20 cells, 5 layers cao)

### Bước 2: Hiểu Grid System

#### Grid Cells
- Map được chia thành lưới 3D
- Mỗi cell có kích thước **Cell Size** (mặc định 1m)
- Objects **bắt buộc snap** vào grid cells

#### Grid Size
- Objects được định nghĩa bởi số cells chiếm: **1x1x1**, **2x1x1**, **2x2x2**, etc.
- Ví dụ:
  - **1x1x1**: Khối đơn (1m x 1m x 1m)
  - **2x1x1**: Tường ngang (2m x 1m x 1m)
  - **3x3x1**: Sàn lớn (3m x 3m x 1m)

### Bước 3: Đặt Objects

#### Placement Mode
1. Chọn mode **Place**
2. Chọn **Primitive Type** (Cube, Sphere, etc.)
3. Set **Grid Size** (ví dụ: 2x1x1 cho tường)
4. Di chuột trong Scene view:
   - Preview hiển thị vị trí và kích thước
   - **Xanh lá**: Vị trí hợp lệ
   - **Đỏ**: Bị chồng lấp
5. **Left Click** để đặt

#### Grid Overlay Info
Scene view hiển thị:
- **Grid Position**: Tọa độ grid (0,0,0), (5,0,3), etc.
- **Grid Size**: Kích thước object (cells)
- **Status**: Valid hoặc Occupied
- **Cell Size**: Kích thước mỗi cell

### Bước 4: Resize Objects

#### Trong Resize Mode
1. Chọn mode **Resize/Move**
2. Select object trong scene
3. Trong Inspector, thay đổi **Grid Size**
4. Hoặc dùng **Grid Size Presets**: 1x1x1, 2x1x1, 2x2x2, etc.

#### Di chuyển Objects
- Trong Resize mode, kéo **Position Handle** (arrow gizmo)
- Object tự động snap vào grid cells gần nhất
- Không thể di chuyển nếu vị trí mới bị chiếm

### Bước 5: Grid Visualization

#### Trong Scene View
- **Lưới trắng**: Grid cells
- **Wireframe đỏ**: Occupied cells
- **Wireframe xanh**: Selected object bounds

#### Grid Manager Settings
Select GridManager object:
- **Show Grid**: Bật/tắt hiển thị grid
- **Grid Color**: Màu lưới
- **Occupied Color**: Màu cells đã chiếm

## Ví dụ: Tạo một căn phòng

### 1. Tạo nền (Floor)
```
Grid Size: 10x1x10
Position: (0, 0, 0)
Result: Sàn 10m x 10m
```

### 2. Tạo tường
```
Tường trước: 10x3x1 tại (0, 0, 5)
Tường sau:   10x3x1 tại (0, 0, -5)
Tường trái:  1x3x10 tại (-5, 0, 0)
Tường phải:  1x3x10 tại (5, 0, 0)
```

### 3. Tạo cửa (Boolean Subtract)
```
1. Đặt cube 2x3x1 vào tường
2. Boolean Mode > Subtract
3. Select tường và cube
4. Perform Operation
```

### 4. Thêm đồ nội thất
```
Bàn: 2x1x1
Ghế: 1x1x1
Tủ: 1x2x1
```

## Grid System Features

### ✅ Snap tự động
- Tất cả objects snap vào grid
- Không thể đặt giữa cells
- Alignment hoàn hảo

### ✅ Collision Detection
- Không thể đặt object chồng lên nhau
- Preview đỏ khi vị trí không hợp lệ
- Resize bị chặn nếu overlap

### ✅ Grid Units
- Resize theo grid units (1x, 2x, 3x)
- Không có kích thước lẻ
- Dễ tính toán và quản lý

### ✅ Occupied Tracking
- GridManager track cells đã chiếm
- Hiển thị occupied cells
- Rebuild grid từ scene

## Inspector Quick Actions

### BlockData Inspector
Khi select một block:
- **Grid Position**: Vị trí trong grid
- **Grid Size**: Kích thước (cells)
- **Reset Size**: Khôi phục size ban đầu
- **Duplicate**: Clone vào cell trống kế bên
- **Delete from Grid**: Xóa và free cells
- **Grid Size Presets**: Các size thông dụng

### GridManager Inspector
Select GridManager:
- **Clear All Occupied Cells**: Reset grid data
- **Rebuild Grid from Scene**: Tái xây dựng từ objects hiện có

## Tips & Tricks

### Tip 1: Planning với Grid
Lên kế hoạch level theo grid units:
```
Phòng nhỏ: 5x3x5
Phòng lớn: 10x3x10
Hành lang: 15x3x2
```

### Tip 2: Modular Design
Tạo các pieces tái sử dụng:
```
Tường đơn: 1x3x1
Tường đôi: 2x3x1
Góc: 1x3x1 + 1x3x1
```

### Tip 3: Layers
Sử dụng Y coordinate cho layers:
```
Y=0: Nền tầng
Y=1-3: Tường
Y=4: Trần
Y=5: Tầng 2
```

### Tip 4: Presets
Tạo prefabs cho các sizes thông dụng

### Tip 5: Grid Dimensions
Đặt Grid Dimensions phù hợp với level:
```
Level nhỏ: 10x5x10
Level trung: 20x5x20
Level lớn: 50x10x50
```

## Common Issues

### Issue: Không thể đặt object
**Cause**: Cells đã bị chiếm
**Solution**: Di chuyển tới vị trí khác hoặc xóa objects cũ

### Issue: Preview không hiện
**Cause**: Không có GridManager
**Solution**: Click "Create Grid Manager"

### Issue: Grid không hiển thị
**Cause**: Show Grid = false
**Solution**: Select GridManager > Enable "Show Grid"

### Issue: Object size sai
**Cause**: Grid Size không khớp với mong muốn
**Solution**: Adjust Grid Size trong Inspector hoặc dùng Presets

### Issue: Occupied cells sai
**Cause**: Grid data bị corrupt
**Solution**: GridManager Inspector > "Rebuild Grid from Scene"

## Advanced Usage

### Custom Grid Sizes
Có thể đặt bất kỳ grid size nào:
```
1x1x1 = Cube nhỏ
5x1x2 = Platform
1x5x1 = Cột cao
3x3x3 = Khối lớn
```

### Multi-Layer Building
Xây dựng theo tầng:
```
Layer 0: Nền
Layer 1-3: Tường tầng 1
Layer 4: Sàn tầng 2
Layer 5-7: Tường tầng 2
```

### Grid-Based Prefabs
Tạo prefabs với BlockData configured:
1. Tạo và configure object
2. Save as Prefab
3. Drag & drop vào scene
4. Vẫn snap to grid

## Keyboard Shortcuts

```
Left Click - Place object
G - Toggle Grid visibility (khi select GridManager)
Ctrl+Z - Undo
Ctrl+D - Duplicate selected
Delete - Delete selected
F - Focus on selected
```

## Performance

### Grid Size Impact
- **Small grid** (10x5x10): ~500 cells, nhanh
- **Medium grid** (20x10x20): ~4000 cells, tốt
- **Large grid** (50x20x50): ~50000 cells, có thể chậm

### Optimization Tips
1. Dùng grid dimensions vừa đủ
2. Combine static objects khi xong
3. Disable "Show Grid" khi không cần
4. Use occlusion culling

---

## Next Steps
1. Thử tạo một phòng đơn giản
2. Experiment với các grid sizes khác nhau
3. Tạo modular pieces
4. Export level để backup

**Happy Grid-Based Level Making!** 🎮📐

---

*Grid System - Version 2.0*
