const { spawn, execFile } = require('child_process');
const fs = require('fs');
const path = require('path');

module.exports = async function (page) {
  const dest = 'F:\\天命之子\\DC_RECON_KIT\\docs\\reference\\gl-shutdown-pve';
  const py = 'C:\\Users\\Administrator\\AppData\\Local\\Programs\\Python\\Python313\\pythonw.exe';
  const script = path.join(dest, '_detach_fetch.py');
  const marker = path.join(dest, '_pw_require_marker.txt');
  try {
    fs.writeFileSync(marker, 'require_works ' + new Date().toISOString(), 'utf8');
  } catch (e) {
    return { markerErr: String(e) };
  }
  try {
    const child = spawn(py, [script], {
      detached: true,
      stdio: 'ignore',
      windowsHide: true,
      cwd: dest,
    });
    child.unref();
    return { ok: true, pid: child.pid };
  } catch (e) {
    return { spawnErr: String(e && e.stack || e) };
  }
};
