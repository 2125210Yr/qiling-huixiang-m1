import subprocess
import os
import sys
import time

DEST = r'F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve'
YT = r'C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe'
PROXY = 'http://127.0.0.1:7890'
LOG = os.path.join(DEST, 'FETCH_LOG.txt')
DONE = os.path.join(DEST, '_py_detach_done.txt')

CREATE_NO_WINDOW = 0x08000000
DETACHED_PROCESS = 0x00000008
CREATE_NEW_PROCESS_GROUP = 0x00000200
FLAGS = DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP | CREATE_NO_WINDOW

items = [
    ('aSbBuFD12HY', 'https://www.youtube.com/watch?v=aSbBuFD12HY', 'aSbBuFD12HY_GL_EN_Gameplay_Part1_202308'),
    ('hgqXY5M9gFk', 'https://www.youtube.com/watch?v=hgqXY5M9gFk', 'hgqXY5M9gFk_GL_ND_Robin_Boss'),
    ('IRDqNAhKKr4', 'https://www.youtube.com/watch?v=IRDqNAhKKr4', 'IRDqNAhKKr4_GL_EternalVow_NormalHard_480p'),
]

def append(msg):
    with open(LOG, 'a', encoding='utf-8') as f:
        f.write(msg + '\n')

def run_one(vid, url, name):
    out_tpl = os.path.join(DEST, name + '.%(ext)s')
    mp4 = os.path.join(DEST, name + '.mp4')
    attempts = [
        ('default', []),
        ('android', ['--extractor-args', 'youtube:player_client=android']),
        ('tv_embedded', ['--extractor-args', 'youtube:player_client=tv_embedded']),
    ]
    last_code = -1
    last_err = ''
    for label, extra in attempts:
        if os.path.exists(mp4) and os.path.getsize(mp4) > 100000:
            break
        err_path = os.path.join(DEST, f'_err_{vid}_{label}.txt')
        out_path = os.path.join(DEST, f'_out_{vid}_{label}.txt')
        args = [
            YT,
            '-f', 'bv*[height<=480]+ba/b[height<=480]/b',
            '--no-playlist',
            '--proxy', PROXY,
            '--force-ipv4',
            '--legacy-server-connect',
            '--socket-timeout', '30',
            '-o', out_tpl,
            *extra,
            url,
        ]
        append(f'--- {vid} attempt {label} ---')
        with open(out_path, 'w', encoding='utf-8', errors='replace') as fo, \
             open(err_path, 'w', encoding='utf-8', errors='replace') as fe:
            p = subprocess.run(
                args,
                stdout=fo,
                stderr=fe,
                timeout=600,
                creationflags=CREATE_NO_WINDOW,
            )
            last_code = p.returncode
        append(f'exit={last_code}')
        try:
            with open(err_path, 'r', encoding='utf-8', errors='replace') as fe:
                lines = fe.read().splitlines()
                last_err = '\n'.join(lines[-20:])
                if last_err:
                    append(last_err)
        except Exception:
            pass
    size = os.path.getsize(mp4) if os.path.exists(mp4) else 0
    status = 'SUCCESS' if size > 100000 else 'FAILURE'
    append(f'{status} id={vid} exit={last_code} size={size} file={name}.mp4')
    return {
        'Id': vid,
        'Status': status,
        'ExitCode': last_code,
        'Size': size,
        'LastError': last_err.replace('\n', ' | ')[:2000],
    }

def main():
    stamp = time.strftime('%Y-%m-%d %H:%M:%S')
    append(f'\n==== FETCH START (python_detach) {stamp} ====')
    append(f'yt-dlp: {YT}')
    append(f'proxy: {PROXY}')
    # kill hung
    try:
        subprocess.run(['taskkill', '/F', '/IM', 'yt-dlp.exe'], capture_output=True, creationflags=CREATE_NO_WINDOW)
    except Exception:
        pass
    results = [run_one(*it) for it in items]
    append('\n==== ROOT mp4 listing ====')
    for fn in os.listdir(DEST):
        if fn.lower().endswith('.mp4'):
            append(f'{os.path.getsize(os.path.join(DEST, fn))}\t{fn}')
    import json
    with open(os.path.join(DEST, '_fetch_summary.json'), 'w', encoding='utf-8') as f:
        json.dump(results, f, ensure_ascii=False, indent=2)
    with open(os.path.join(DEST, '_fetch_results.txt'), 'w', encoding='utf-8') as f:
        for r in results:
            for k, v in r.items():
                f.write(f'{k} : {v}\n')
            f.write('\n---\n')
    append(f'\n==== FETCH END {time.strftime("%Y-%m-%d %H:%M:%S")} ====')
    with open(DONE, 'w', encoding='utf-8') as f:
        json.dump(results, f, ensure_ascii=False, indent=2)

if __name__ == '__main__':
    # If launched as detached spawner:
    if len(sys.argv) > 1 and sys.argv[1] == '--spawn-self':
        log = open(os.path.join(DEST, '_py_spawn_log.txt'), 'w', encoding='utf-8')
        log.write('spawning\n')
        log.flush()
        subprocess.Popen(
            [sys.executable, __file__],
            creationflags=DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP | CREATE_NO_WINDOW,
            close_fds=True,
            cwd=DEST,
            stdout=open(os.path.join(DEST, '_py_stdout.txt'), 'w'),
            stderr=open(os.path.join(DEST, '_py_stderr.txt'), 'w'),
        )
        log.write('spawned\n')
        log.close()
        sys.exit(0)
    main()
