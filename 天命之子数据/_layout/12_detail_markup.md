# 12 天子详情 — 粘贴用标记

Gallery 默认。点 `#gal .card` 打开一条底部 dialog。不要改 `filt()` / `setView()` / `#gal` `#tbl` `#r5` 的现有脚本。

## 插入位置

`</main>` 之前（`#gal` 与 `#tbl` 之后均可）。`#child-json` 与面板同级。

`<head>` 的 `<style>` 末尾粘贴 `12_detail.css`。现有 `</script>` 之后粘贴 `12_detail.js`（或并入同一脚本块末尾，不要覆盖 `setView` / `filt` / `applyHash`）。

## 面板 HTML

```html
<aside id="cdet" class="child-detail" role="dialog" aria-modal="true" aria-labelledby="cdet-name" hidden>
  <button type="button" class="cdet-close" id="cdet-close" aria-label="关闭详情">×</button>
  <div class="cdet-inner" id="cdet-inner"></div>
</aside>
<script type="application/json" id="child-json">[]</script>
```

JS 写入 `#cdet-inner`。关闭钮固定在面板上，打开后聚焦它。只有这一块 dialog，换卡时替换内容，不叠第二层。

打开后 `#cdet-inner` 形状（JS 生成，勿手写技能正文）：

```html
<div class="cdet-face" data-el="水"><img src="icons/C5f_000.png" alt="黑暗塞勒涅" width="152" height="152"></div>
<div class="cdet-body">
  <h2 class="cdet-name" id="cdet-name">黑暗塞勒涅</h2>
  <div class="cdet-badges">
    <span class="badge el-water">水</span>
    <span class="badge">辅助型</span>
    <span class="badge">5星</span>
  </div>
  <div class="cdet-stats">CP —　HP —　攻—　防—　敏—　暴—</div>
  <div class="cdet-sk">
    <div class="cdet-row"><span class="cdet-lab">普攻</span>…CSV auto_text…</div>
    <div class="cdet-row"><span class="cdet-lab">TS</span>…CSV tap_text…</div>
    <div class="cdet-row"><span class="cdet-lab">SS</span>…CSV slide_text…</div>
    <div class="cdet-row"><span class="cdet-lab">DS</span>…CSV drive_text…</div>
    <div class="cdet-row"><span class="cdet-lab">队长</span>…CSV leader_text…</div>
  </div>
  <div class="cdet-cv">CV 本泉莉奈</div>
</div>
```

无 `cv` 则不渲染 `.cdet-cv`。空数值 / 空技能显示 `—`。头像空则 `.cdet-ph`「无」。

## JSON（`#child-json`）

与 `inject_visual_catalog.py` 相同：数组，`id` 主键。卡片已有 `data-id`，**不要**再写 `hay`（检索仍用卡片 `data-hay`）。不要写 ignited / wiki / source / `*_init`（HP 缺省时用 `hp` 否则 `hp_init`）。

字段（空字符串可省略以缩小体积）：

| key | CSV |
|-----|-----|
| id | id |
| name | name |
| el | element |
| role | role |
| rar | rarity |
| av | avatar_file |
| cp | cp |
| hp | hp 或 hp_init |
| atk | atk |
| def | def_ |
| agl | agl |
| crt | crt |
| auto | auto_text |
| tap | tap_text |
| slide | slide_text |
| drive | drive_text |
| leader | leader_text |
| cv | cv |

生成（父脚本注入真实 CSV，禁止编造技能）：

```python
payload = []
for r in rows:
    rec = {
        "id": r.get("id") or r.get("idx") or "",
        "name": r.get("name") or "",
        "el": r.get("element") or "",
        "role": r.get("role") or "",
        "rar": r.get("rarity") or "",
        "av": (r.get("avatar_file") or "").replace("\\", "/"),
        "cp": r.get("cp") or "",
        "hp": r.get("hp") or r.get("hp_init") or "",
        "atk": r.get("atk") or "",
        "def": r.get("def_") or "",
        "agl": r.get("agl") or "",
        "crt": r.get("crt") or "",
        "auto": r.get("auto_text") or "",
        "tap": r.get("tap_text") or "",
        "slide": r.get("slide_text") or "",
        "drive": r.get("drive_text") or "",
        "leader": r.get("leader_text") or "",
        "cv": r.get("cv") or "",
    }
    payload.append({k: v for k, v in rec.items() if v})
blob = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
# <script type="application/json" id="child-json">{blob}</script>
```

`id` 必须保留。JS 也接受 `{ "M5-000": { ... } }` 对象表。

## 示例一行（汇总表.csv M5-000，原文照抄）

```json
[{"id":"M5-000","name":"黑暗塞勒涅","el":"水","role":"辅助型","rar":"5星","av":"icons/C5f_000.png","auto":"对目标造成102（690）自动攻击伤害","tap":"对1名水属性敌人优先造成399（2575）伤害1次完全充能：1名盟友（随机）60（90.4）%概率","slide":"对2名水属性敌人优先造成725（4036）伤害1次skill充能加速：16秒内，3名盟友（低攻击力）+50（93.5）%忍耐：20秒内3次，3名盟友（低攻击力）100%概率弱化减益无效：16秒内，3名木属性盟友（PVP）70（90.1）%概率","drive":"对3名敌人（随机）造成1976（7039）伤害1次忍耐：20秒内4次，4名盟友（低攻击力）100%概率 弱化减益无效：4名盟友（低攻击力）100%概率","leader":"skill充能加速：永远套用所有木属性盟友+20%skill充能加速：永远套用所有木属性盟友（PVP）+20%","cv":"本泉莉奈"}]
```

M5-000 的 CP/HP/攻/防/敏/暴在 CSV 为空，面板显示 `—`。有值的例子（M5-001，技能列为空）：

```json
{"id":"M5-001","name":"忍者琪隆","el":"木","role":"辅助型","rar":"5星","av":"icons/C5f_001.png","cp":"2267","hp":"2275","atk":"1054","def":"848","agl":"788","crt":"907","cv":"本泉莉奈"}
```
