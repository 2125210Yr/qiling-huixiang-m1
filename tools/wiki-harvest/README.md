# GameKee harvest

Reference dump for Destiny Child mechanics. Original names stay under `docs/reference/gamekee/`.

```
python -u tools/wiki-harvest/harvest.py
python -u tools/wiki-harvest/harvest_site.py --alias destinychild --workers 10
python -u tools/wiki-harvest/harvest_site.py --alias dcj --workers 10
python tools/wiki-harvest/inventory_three_wikis.py
python tools/wiki-harvest/flatten_tree.py
python tools/wiki-harvest/parse_child.py
python tools/wiki-harvest/parse_aux.py
```

`harvest.py` = 国际服 `dc`（Game-Alias: dc）。韩服 `destinychild`、日服 `dcj` 走 `harvest_site.py`。
