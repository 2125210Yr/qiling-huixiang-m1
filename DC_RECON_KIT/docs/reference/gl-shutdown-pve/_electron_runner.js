const { app } = require('electron');
const { spawnSync, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const dest = 'F:\\天命之子\\DC_RECON_KIT\\docs\\reference\\gl-shutdown-pve';
const logPath = path.join(dest, 'FETCH_LOG.txt');
const resultsPath = path.join(dest, '_fetch_results.txt');
const summaryPath = path.join(dest, '_fetch_summary.json');
const proxy = 'http://127.0.0.1:7890';

function append(file, text) {
  fs.appendFileSync(file, text + (text.endsWith('\n') ? '' : '\n'), 'utf8');
}

function findYtDlp() {
  try {
    const w = execSync('where yt-dlp', { encoding: 'utf8', shell: true, timeout: 15000 });
    const line = w.split(/\r?\n/).map((s) => s.trim()).filter(Boolean)[0];
    if (line) return line;
  } catch {}
  const candidates = [
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python312', 'Scripts', 'yt-dlp.exe'),
    path.join(process.env.LOCALAPPDATA || '', 'Programs', 'Python', 'Python311', 'Scripts', 'yt-dlp.exe'),
    path.join(process.env.APPDATA || '', 'Python', 'Python312', 'Scripts', 'yt-dlp.exe'),
    'yt-dlp.exe',
    'yt-dlp',
  ];
  for (const c of candidates) {
    try {
      if (c.endsWith('.exe') && fs.existsSync(c)) return c;
    } catch {}
  }
  return 'yt-dlp';
}

function runYt(ytdlp, args, outFile, errFile) {
  const r = spawnSync(ytdlp, args, {
    encoding: 'utf8',
    timeout: 600000,
    maxBuffer: 20 * 1024 * 1024,
    windowsHide: true,
    shell: false,
  });
  try { fs.writeFileSync(outFile, r.stdout || '', 'utf8'); } catch {}
  try { fs.writeFileSync(errFile, (r.stderr || '') + (r.error ? String(r.error) : ''), 'utf8'); } catch {}
  return {
    code: typeof r.status === 'number' ? r.status : -1,
    error: r.error ? String(r.error) : null,
    stderrTail: String(r.stderr || '').split(/\r?\n/).slice(-20).join('\n'),
    stdoutTail: String(r.stdout || '').split(/\r?\n/).slice(-10).join('\n'),
  };
}

function killYt() {
  try { execSync('taskkill /F /IM yt-dlp.exe', { stdio: 'ignore', timeout: 10000, shell: true }); } catch {}
}

app.whenReady().then(() => {
  const stamp = new Date().toISOString();
  append(logPath, '\n==== FETCH START (electron_runner) ' + stamp + ' ====');
  killYt();
  const ytdlp = findYtDlp();
  append(logPath, 'yt-dlp: ' + ytdlp);
  append(logPath, 'proxy: ' + proxy);
  append(logPath, 'node: ' + process.version);

  const items = [
    { id: 'aSbBuFD12HY', url: 'https://www.youtube.com/watch?v=aSbBuFD12HY', name: 'aSbBuFD12HY_GL_EN_Gameplay_Part1_202308' },
    { id: 'hgqXY5M9gFk', url: 'https://www.youtube.com/watch?v=hgqXY5M9gFk', name: 'hgqXY5M9gFk_GL_ND_Robin_Boss' },
    { id: 'IRDqNAhKKr4', url: 'https://www.youtube.com/watch?v=IRDqNAhKKr4', name: 'IRDqNAhKKr4_GL_EternalVow_NormalHard_480p' },
  ];

  const results = [];

  for (const it of items) {
    const outTpl = path.join(dest, it.name + '.%(ext)s');
    const mp4 = path.join(dest, it.name + '.mp4');
    const baseArgs = [
      '-f', 'bv*[height<=480]+ba/b[height<=480]/b',
      '--no-playlist',
      '--proxy', proxy,
      '--force-ipv4',
      '--legacy-server-connect',
      '--socket-timeout', '30',
      '-o', outTpl,
      it.url,
    ];

    const attempts = [
      { label: 'default', args: baseArgs },
      { label: 'android', args: baseArgs.concat(['--extractor-args', 'youtube:player_client=android']) },
      { label: 'tv_embedded', args: baseArgs.concat(['--extractor-args', 'youtube:player_client=tv_embedded']) },
    ];

    let last = { code: -1, stderrTail: '', stdoutTail: '' };
    let ok = false;

    for (const att of attempts) {
      if (ok) break;
      append(logPath, '\n--- ' + it.id + ' attempt ' + att.label + ' ---');
      last = runYt(
        ytdlp,
        att.args,
        path.join(dest, '_out_' + it.id + '_' + att.label + '.txt'),
        path.join(dest, '_err_' + it.id + '_' + att.label + '.txt')
      );
      append(logPath, 'exit=' + last.code);
      if (last.stderrTail) append(logPath, last.stderrTail);
      if (last.stdoutTail) append(logPath, last.stdoutTail);
      try {
        ok = fs.existsSync(mp4) && fs.statSync(mp4).size > 100000;
      } catch { ok = false; }
    }

    let size = 0;
    try { if (fs.existsSync(mp4)) size = fs.statSync(mp4).size; } catch {}
    const status = ok ? 'SUCCESS' : 'FAILURE';
    const line = status + ' id=' + it.id + ' exit=' + last.code + ' size=' + size + ' file=' + it.name + '.mp4';
    append(logPath, line);
    results.push({
      Id: it.id,
      Status: status,
      ExitCode: last.code,
      Size: size,
      LastError: last.stderrTail || last.error || '',
    });
  }

  append(logPath, '\n==== ROOT mp4 listing ====');
  try {
    for (const f of fs.readdirSync(dest)) {
      if (f.toLowerCase().endsWith('.mp4')) {
        const st = fs.statSync(path.join(dest, f));
        append(logPath, st.size + '\t' + f);
      }
    }
  } catch (e) {
    append(logPath, 'list_err ' + String(e));
  }

  fs.writeFileSync(summaryPath, JSON.stringify(results, null, 2), 'utf8');
  fs.writeFileSync(
    resultsPath,
    results.map((r) => Object.entries(r).map(([k, v]) => k + ' : ' + v).join('\n') + '\n').join('\n---\n'),
    'utf8'
  );
  append(logPath, '\n==== FETCH END ' + new Date().toISOString() + ' ====');
  fs.writeFileSync(path.join(dest, '_electron_runner_done.txt'), JSON.stringify(results, null, 2), 'utf8');
  app.quit();
}).catch((e) => {
  try {
    fs.writeFileSync(path.join(dest, '_electron_runner_done.txt'), 'FATAL ' + String(e), 'utf8');
  } catch {}
  app.exit(1);
});
