# 三个 GameKee Destiny Child wiki

| 站点 | 别名 | game_id | 词条 | 本地 |
|---|---|---:|---:|---|
| [国际服](https://www.gamekee.com/dc/) | `dc` | 202 | 1227 | `pages/` |
| [韩服](https://www.gamekee.com/destinychild/) | `destinychild` | 1069 | 1006 | `destinychild/pages/` |
| [日服](https://www.gamekee.com/dcj/) | `dcj` | 1164 | 742 | `dcj/pages/` |

合计 **2975** 篇正文，harvest 失败 0。

重抓：

```
python -u tools/wiki-harvest/harvest_site.py --alias destinychild --workers 10
python -u tools/wiki-harvest/harvest_site.py --alias dcj --workers 10
python tools/wiki-harvest/inventory_three_wikis.py
```

国际服原脚本仍是 `harvest.py`（树 id 20278，与 game_id 202 等价）。
