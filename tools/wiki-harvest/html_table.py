# -*- coding: utf-8 -*-
"""HTML table extractor for GameKee mould tables."""
from __future__ import annotations

import re
from html.parser import HTMLParser


class TableParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.rows = []
        self._row = []
        self._cell = None
        self._td = 0
        self._tr = 0

    def handle_starttag(self, tag, attrs):
        if tag == "tr":
            self._tr += 1
            self._row = []
        if tag in ("td", "th"):
            self._td += 1
            self._cell = []

    def handle_endtag(self, tag):
        if tag in ("td", "th") and self._td:
            self._td -= 1
            text = re.sub(r"\s+", " ", "".join(self._cell or [])).strip()
            self._row.append(text)
            self._cell = None
        if tag == "tr" and self._tr:
            self._tr -= 1
            if any(self._row):
                self.rows.append(self._row)

    def handle_data(self, data):
        if self._td and self._cell is not None:
            self._cell.append(data)


def extract_rows(html: str):
    if not html:
        return []
    p = TableParser()
    try:
        p.feed(html)
    except Exception:
        return []
    return p.rows
