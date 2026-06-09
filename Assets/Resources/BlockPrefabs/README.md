# Block Prefabs Folder

Folder này chứa các prefab có thể đặt vào level.

## Cách tạo Block Prefab

1. **Tạo GameObject trong scene** với model/mesh bạn muốn
2. **Đặt pivot ở đáy** (bottom center) của object:
   - Trong 3D software: Export với pivot ở bottom
   - Hoặc trong Unity: Tạo empty parent, đặt mesh làm child với offset phù hợp
3. **Add BoxCollider** để raycast có thể detect
4. **Kéo thả vào folder này** để tạo prefab
5. **Đặt tên file** theo format: `Block_TenVatThe.prefab`

## Yêu cầu cho Prefab

- ✅ **Pivot ở đáy giữa** (bottom center) để placement chính xác
- ✅ **Có Collider** để raycast và delete hoạt động
- ✅ **Kích thước**: Tốt nhất là bội số của grid cell (1x1x1, 2x1x1, ...)
- ✅ **Layer**: Đặt ở Default layer

## Prefab Size Metadata

Nếu prefab có kích thước đặc biệt (không phải 1x1x1), thêm component `BlockData` và set:
- `gridSize`: Kích thước trong grid units (ví dụ: 2x1x3)

## Ví dụ

```
BlockPrefabs/
  ├── Block_Wall.prefab          (1x1x1 tường đơn)
  ├── Block_Floor.prefab         (1x1x1 nền)
  ├── Block_Door.prefab          (1x2x1 cửa cao)
  ├── Block_Platform.prefab      (2x1x2 sàn lớn)
  └── Block_Stairs.prefab        (2x1x2 cầu thang)
```

## Lưu ý

- Tên file sẽ hiển thị trong UI để chọn
- Prefab sẽ được load bằng `Resources.LoadAll<GameObject>("BlockPrefabs")`
- Chỉ prefab trong folder này mới được load vào game
