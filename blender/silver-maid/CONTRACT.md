# silver-maid 无头 Blender 合同

Blender: `F:\Blender\blender-5.2.0-windows-x64\blender.exe`
Mode: `blender.exe --background --python <script.py>`
Engine: Blender 5.2, EEVEE_NEXT, Z-up, meters.

Reference still: `ref/still.jpg` (银白高马尾、黑高领女仆裙、金扣、耳机、漆盒、坐金属高脚凳、一脚悬空掉鞋、3/4 朝向镜头)。
Style lock: `ref/style-lock.jpg` (DC 8 头身、发块比人大、肉光、纯黑底)。

## 世界

- Origin 在凳面中心。凳面 Z=0.72。
- 角色面向 -Y，略向 +X 转 18°（3/4）。
- 坐骨在 origin 上方 0.02。头顶（不含发）约 Z=1.48。发梢可到 Z=1.95 与地面附近。
- 单位：米。成人时装比例，不是 Q 版。

## 模块接口

每个 `parts/*.py` 必须：

```python
def build(bpy, root):
    """Create objects, parent to `root` (Empty named MaidRoot). Return dict name->object."""
```

禁止 `bpy.ops.wm.read_homefile`。不要删 MaidRoot。材质名 `maid_*`。物体名 `maid_*`。

## 分件

| 文件 | 内容 |
|---|---|
| `parts/body.py` | 头、颈、胸、腰、臀、上臂前臂手、大腿小腿脚。细分+光滑。时装沙漏。皮肤 SSS。 |
| `parts/hair.py` | 银白高马尾根 + 两大发块（左大右小），Bezier 倒角曲线，梢带钩。发量大于躯干。 |
| `parts/dress.py` | 高领盖胸黑皮上装、腰金扣带、蓬蓬裙多层荷叶、丝袜。 |
| `parts/accessories.py` | 黑金耳机、金漆蒸笼盒、金属线框高脚凳、金步枪饰扣、黑高跟（右脚穿上，左脚掉在 Z=0.02）。 |
| `parts/look.py` | 纯黑世界、暖左上主光、弱 Rim、相机匹配 still 构图、EEVEE 渲染。 |

## 输出

`out/maid.blend` `out/front.png` `out/threeq.png` `out/side.png`
分辨率 832×1248，透明关、纯黑底。
