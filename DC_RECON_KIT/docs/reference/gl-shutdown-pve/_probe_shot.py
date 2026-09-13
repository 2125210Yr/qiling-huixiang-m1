from pathlib import Path
from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
out = DEST / "_probe_now.png"

with sync_playwright() as p:
    browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    context = browser.contexts[0]
    page = context.pages[0] if context.pages else context.new_page()
    info = page.evaluate(
        """() => ({
  url: location.href,
  title: document.title,
  h1: (document.querySelector('h1')||{}).innerText || '',
  body: (document.body && document.body.innerText || '').slice(0, 400)
})"""
    )
    page.screenshot(path=str(out), full_page=False)
    print(info)
    print("shot", out, out.stat().st_size)
