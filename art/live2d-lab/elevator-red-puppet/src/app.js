/* elevator-red-puppet ---------------------------------------------------------
   A 2.5D "fake Live2D" puppet. Every layer is a full-frame RGBA cut-out of the
   reference still, so a layer only needs a pivot + a transform; no atlas maths.
   World space == reference pixel space (546 x 292).
--------------------------------------------------------------------------- */
(function () {
  'use strict';

  var W = 546, H = 292;

  var cv = document.getElementById('stage');
  var ctx = cv.getContext('2d');
  var hud = document.getElementById('hud');

  /* ---------------------------------------------------------------- assets */
  var IMG = {};
  var pending = 0, failed = [];

  function load(name, uri, done) {
    var im = new Image();
    pending++;
    im.onload = function () { IMG[name] = im; if (--pending === 0) done(); };
    im.onerror = function () { failed.push(name); if (--pending === 0) done(); };
    im.src = uri;
  }

  /* ------------------------------------------------------------- rig setup */
  // pivots in world px, measured off the reference
  var RIG = {
    hair_back:  { pivot: [243, 46] },
    leg_stand:  { pivot: [259, 188] },
    body:       { pivot: [252, 196] },
    dress_hem:  { pivot: [268, 176] },
    leg_raised: { pivot: [236, 160] },
    arm:        { pivot: [274, 82] },
    head:       { pivot: [244, 82] },
    eyes:       { pivot: [244, 82] }
  };

  var EYE_L = { x: 236.0, y: 55.5, w: 12.0, h: 6.4, rot: -0.13 };
  var EYE_R = { x: 250.5, y: 53.5, w: 11.0, h: 6.0, rot: -0.13 };

  /* --------------------------------------------------------------- state */
  var t0 = performance.now();
  var time = 0, paused = false;
  var debug = 0;            // 0 off, 1 rig overlay, 2 rig + layer tint
  var showRef = false;
  var solo = -1;

  var mouse = { x: 0, y: 0, inside: false };
  var look = { x: 0, y: 0, vx: 0, vy: 0 };          // smoothed, -1..1
  var blink = { v: 0, next: 1.6, phase: 'idle', t: 0 };

  var LAYER_ORDER = [
    'hair_back', 'leg_stand', 'body', 'dress_hem', 'leg_raised', 'arm', 'head'
  ];

  /* ------------------------------------------------------------- helpers */
  function clamp(v, a, b) { return v < a ? a : (v > b ? b : v); }
  function lerp(a, b, k) { return a + (b - a) * k; }

  // cheap deterministic 1d value noise
  function noise(x) {
    var i = Math.floor(x), f = x - i;
    function h(n) { n = (n << 13) ^ n; return 1 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824; }
    var a = h(i), b = h(i + 1);
    var u = f * f * (3 - 2 * f);
    return a + (b - a) * u;
  }

  /* -------------------------------------------------- sample skin colours */
  var SKIN = '#e8cdc6', SKIN_HI = '#f3ded8', LASH = 'rgba(48,26,38,0.85)';
  function sampleSkin() {
    if (!IMG.head) return;
    var c = document.createElement('canvas');
    c.width = W; c.height = H;
    var g = c.getContext('2d');
    g.drawImage(IMG.head, 0, 0);
    function at(x, y) {
      try {
        var d = g.getImageData(x, y, 1, 1).data;
        if (d[3] < 200) return null;
        return 'rgb(' + d[0] + ',' + d[1] + ',' + d[2] + ')';
      } catch (e) { return null; }
    }
    SKIN = at(243, 47) || at(240, 65) || SKIN;      // forehead / cheek
    SKIN_HI = at(246, 64) || SKIN_HI;
  }

  /* ------------------------------------------------------------ pre-render */
  // scanline + grain plates, built once
  var scan = document.createElement('canvas');
  (function () {
    scan.width = 4; scan.height = 4;
    var g = scan.getContext('2d');
    g.fillStyle = 'rgba(0,0,0,0.16)';
    g.fillRect(0, 0, 4, 1);
    g.fillStyle = 'rgba(0,0,0,0.06)';
    g.fillRect(0, 2, 4, 1);
  })();

  var grains = [];
  (function () {
    for (var k = 0; k < 4; k++) {
      var c = document.createElement('canvas');
      c.width = 137; c.height = 73;
      var g = c.getContext('2d');
      var d = g.createImageData(c.width, c.height);
      for (var i = 0; i < d.data.length; i += 4) {
        var v = 128 + (Math.random() * 2 - 1) * 42;
        d.data[i] = d.data[i + 1] = d.data[i + 2] = v;
        d.data[i + 3] = 26;
      }
      g.putImageData(d, 0, 0);
      grains.push(c);
    }
  })();

  /* ------------------------------------------------------------ transforms */
  function xform(name, tx, ty, rot, sx, sy, skx) {
    var p = RIG[name].pivot;
    ctx.translate(p[0] + tx, p[1] + ty);
    ctx.rotate(rot || 0);
    if (skx) ctx.transform(1, 0, skx, 1, 0, 0);
    ctx.scale(sx === undefined ? 1 : sx, sy === undefined ? 1 : sy);
    ctx.translate(-p[0], -p[1]);
  }

  function drawLayer(name) {
    if (IMG[name]) ctx.drawImage(IMG[name], 0, 0);
  }

  /* --------------------------------------------------------------- motion */
  function pose(t) {
    var br = Math.sin(t * 1.05);                      // breath, ~0.6 Hz
    var br2 = Math.sin(t * 1.05 - 0.5);
    var sway = Math.sin(t * 0.52) * 0.7 + Math.sin(t * 0.31 + 1.2) * 0.3;
    var bounce = Math.sin(t * 2.02 + 0.4) * 0.75 + Math.sin(t * 3.3) * 0.25;
    var flut = Math.sin(t * 1.55) * 0.7 + Math.sin(t * 2.6 + 0.8) * 0.3;
    var drift = noise(t * 0.23) * 0.5;

    return {
      br: br, br2: br2, sway: sway, bounce: bounce, flut: flut, drift: drift,
      lx: look.x, ly: look.y
    };
  }

  function render(t) {
    var p = pose(t);
    var lx = p.lx, ly = p.ly;

    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, W, H);

    /* ---- backdrop (with a hair-thin camera drift so it never feels frozen) */
    var camx = p.drift * 0.8 + lx * 1.1;
    var camy = noise(t * 0.19 + 9) * 0.5 + ly * 0.6;
    ctx.save();
    ctx.translate(camx * -0.35, camy * -0.35);
    ctx.scale(1.006, 1.006);
    ctx.translate(-W * 0.003, -H * 0.003);
    if (IMG.bg) ctx.drawImage(IMG.bg, 0, 0);
    else { ctx.fillStyle = '#1a0c12'; ctx.fillRect(0, 0, W, H); }

    /* ---- contact shadow on the door behind her */
    var sh = ctx.createRadialGradient(250, 210, 6, 250, 210, 120);
    sh.addColorStop(0, 'rgba(20,4,10,0.55)');
    sh.addColorStop(1, 'rgba(20,4,10,0)');
    ctx.fillStyle = sh;
    ctx.fillRect(120, 90, 260, 202);
    ctx.restore();

    /* ---- puppet ------------------------------------------------------- */
    ctx.save();
    ctx.translate(camx * 0.55, camy * 0.4);

    var vis = function (i) { return solo < 0 || solo === i; };

    // hair: follows the head, but lags and overshoots
    if (vis(0)) {
      ctx.save();
      xform('hair_back',
        lx * 1.9 + p.sway * 0.55,
        ly * 0.9 + p.br * 0.35,
        (-lx * 0.030) + p.sway * 0.0085,
        1, 1 + p.br * 0.004);
      drawLayer('hair_back');
      ctx.restore();
    }

    // standing leg: almost still, just carries the breath
    if (vis(1)) {
      ctx.save();
      xform('leg_stand', lx * 0.35, 0, -lx * 0.004 + p.bounce * 0.0016, 1, 1);
      drawLayer('leg_stand');
      ctx.restore();
    }

    // torso: breath (chest lift + rib expand) + weight shift toward the cursor
    if (vis(2)) {
      ctx.save();
      xform('body',
        lx * 1.2,
        -p.br * 0.55 + ly * 0.5,
        -lx * 0.016 + p.sway * 0.0025,
        1 + p.br * 0.0045,
        1 + p.br * 0.0075);
      drawLayer('body');
      ctx.restore();
    }

    // dress slit: flutters late, shears sideways
    if (vis(3)) {
      ctx.save();
      xform('dress_hem',
        lx * 1.0 + p.flut * 0.5,
        ly * 0.4,
        p.flut * 0.017 - lx * 0.012,
        1, 1 + p.flut * 0.004,
        p.flut * 0.022);
      drawLayer('dress_hem');
      ctx.restore();
    }

    // raised leg: micro bounce around the hip
    if (vis(4)) {
      ctx.save();
      xform('leg_raised',
        lx * 1.1 - p.bounce * 0.25,
        ly * 0.55 + p.bounce * 0.55 - p.br * 0.25,
        p.bounce * 0.0105 - lx * 0.014,
        1, 1);
      drawLayer('leg_raised');
      ctx.restore();
    }

    // arm on the hip: rides the torso
    if (vis(5)) {
      ctx.save();
      xform('arm',
        lx * 1.35,
        -p.br * 0.5 + ly * 0.45,
        -lx * 0.020 + p.br2 * 0.0045,
        1, 1);
      drawLayer('arm');
      ctx.restore();
    }

    // head: the loudest mouse-look channel
    var headTx = lx * 3.0, headTy = ly * 2.1 - p.br * 0.75;
    var headRot = -lx * 0.052 + p.sway * 0.0045;
    if (vis(6)) {
      ctx.save();
      xform('head', headTx, headTy, headRot, 1, 1);
      drawLayer('head');
      ctx.restore();

      // eye band: head transform + a sub-pixel nudge = gaze tracking
      ctx.save();
      xform('head', headTx + lx * 1.25, headTy + ly * 0.95, headRot, 1, 1);
      drawLayer('eyes');
      ctx.restore();

      // lids
      if (blink.v > 0.01) {
        ctx.save();
        xform('head', headTx, headTy, headRot, 1, 1);
        lid(EYE_L, blink.v);
        lid(EYE_R, blink.v);
        ctx.restore();
      }
    }
    ctx.restore();

    /* ---- grade: red bounce light, flicker, vignette, scanlines, grain --- */
    var flick = 0.5 + 0.5 * noise(t * 3.1);
    var flick2 = 0.5 + 0.5 * noise(t * 11.0 + 4);

    ctx.save();
    ctx.globalCompositeOperation = 'screen';
    var gl = ctx.createRadialGradient(214, 92, 10, 214, 92, 210);
    gl.addColorStop(0, 'rgba(190,26,40,' + (0.14 + flick * 0.07 + flick2 * 0.02).toFixed(3) + ')');
    gl.addColorStop(1, 'rgba(120,10,30,0)');
    ctx.fillStyle = gl;
    ctx.fillRect(0, 0, W, H);
    ctx.restore();

    ctx.save();
    var vg = ctx.createRadialGradient(273, 146, 90, 273, 146, 330);
    vg.addColorStop(0, 'rgba(0,0,0,0)');
    vg.addColorStop(1, 'rgba(2,0,4,0.72)');
    ctx.fillStyle = vg;
    ctx.fillRect(0, 0, W, H);
    ctx.restore();

    ctx.save();
    ctx.globalAlpha = 0.55;
    ctx.fillStyle = ctx.createPattern(scan, 'repeat');
    ctx.fillRect(0, 0, W, H);
    ctx.restore();

    ctx.save();
    ctx.globalCompositeOperation = 'overlay';
    ctx.globalAlpha = 0.5;
    var gi = grains[Math.floor(t * 12) % grains.length];
    ctx.drawImage(gi, 0, 0, W, H);
    ctx.restore();

    if (showRef && IMG.reference) {
      ctx.save();
      ctx.globalAlpha = 0.5;
      ctx.drawImage(IMG.reference, 0, 0, W, H);
      ctx.restore();
    }

    if (debug) drawDebug(t, headTx, headTy, headRot);
  }

  function lid(e, k) {
    ctx.save();
    ctx.translate(e.x, e.y);
    ctx.rotate(e.rot);
    ctx.beginPath();
    ctx.ellipse(0, 0, e.w / 2 + 0.7, e.h / 2 + 1.0, 0, 0, Math.PI * 2);
    ctx.clip();
    var top = -e.h / 2 - 1.2;
    var hgt = (e.h + 2.4) * k;
    var g = ctx.createLinearGradient(0, top, 0, top + hgt);
    g.addColorStop(0, SKIN);
    g.addColorStop(1, SKIN_HI);
    ctx.fillStyle = g;
    ctx.fillRect(-e.w, top, e.w * 2, hgt);
    ctx.fillStyle = LASH;
    ctx.fillRect(-e.w, top + hgt - 0.55, e.w * 2, 0.75);
    ctx.restore();
  }

  function drawDebug(t, htx, hty, hrot) {
    ctx.save();
    ctx.lineWidth = 0.4;
    ctx.font = '5px monospace';
    for (var k in RIG) {
      var p = RIG[k].pivot;
      ctx.strokeStyle = 'rgba(0,255,190,0.9)';
      ctx.beginPath();
      ctx.moveTo(p[0] - 4, p[1]); ctx.lineTo(p[0] + 4, p[1]);
      ctx.moveTo(p[0], p[1] - 4); ctx.lineTo(p[0], p[1] + 4);
      ctx.stroke();
      ctx.fillStyle = 'rgba(0,255,190,0.9)';
      ctx.fillText(k, p[0] + 5, p[1] - 1);
    }
    // eye boxes, in head space
    ctx.save();
    xform('head', htx, hty, hrot, 1, 1);
    ctx.strokeStyle = 'rgba(255,235,60,0.95)';
    [EYE_L, EYE_R].forEach(function (e) {
      ctx.save();
      ctx.translate(e.x, e.y); ctx.rotate(e.rot);
      ctx.beginPath();
      ctx.ellipse(0, 0, e.w / 2, e.h / 2, 0, 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();
    });
    ctx.restore();
    ctx.restore();
  }

  /* ---------------------------------------------------------------- loop */
  var last = performance.now();
  function frame(now) {
    var dt = Math.min(0.05, (now - last) / 1000);
    last = now;
    if (!paused) time += dt;

    // smooth the cursor into a spring so the look never snaps
    var tx = mouse.inside ? mouse.x : 0;
    var ty = mouse.inside ? mouse.y : 0;
    look.vx = lerp(look.vx, (tx - look.x) * 9.0, 0.35);
    look.vy = lerp(look.vy, (ty - look.y) * 9.0, 0.35);
    look.x = clamp(look.x + look.vx * dt, -1.2, 1.2);
    look.y = clamp(look.y + look.vy * dt, -1.2, 1.2);

    // blink state machine
    if (!paused) {
      blink.t += dt;
      if (blink.phase === 'idle' && blink.t > blink.next) {
        blink.phase = 'close'; blink.t = 0;
      } else if (blink.phase === 'close') {
        blink.v = clamp(blink.t / 0.065, 0, 1);
        if (blink.v >= 1) { blink.phase = 'open'; blink.t = 0; }
      } else if (blink.phase === 'open') {
        blink.v = 1 - clamp(blink.t / 0.11, 0, 1);
        if (blink.v <= 0) {
          blink.phase = 'idle'; blink.t = 0; blink.v = 0;
          blink.next = 2.1 + Math.random() * 3.6;
          if (Math.random() < 0.18) blink.next = 0.22;   // occasional double blink
        }
      }
    }

    render(time);
    requestAnimationFrame(frame);
  }

  /* --------------------------------------------------------------- layout */
  function fit() {
    var dpr = Math.min(2.5, window.devicePixelRatio || 1);
    var box = cv.parentNode.getBoundingClientRect();
    var s = Math.min(box.width / W, box.height / H);
    cv.style.width = (W * s) + 'px';
    cv.style.height = (H * s) + 'px';
    cv.width = Math.round(W * s * dpr);
    cv.height = Math.round(H * s * dpr);
    ctx.imageSmoothingEnabled = true;
    ctx.imageSmoothingQuality = 'high';
    baseScale = s * dpr;
    ctx.setTransform(baseScale, 0, 0, baseScale, 0, 0);
  }
  var baseScale = 1;

  // every render starts from identity, so re-apply the fit scale there
  var _setTransform = ctx.setTransform.bind(ctx);
  ctx.setTransform = function (a, b, c, d, e, f) {
    if (a === 1 && b === 0 && c === 0 && d === 1 && e === 0 && f === 0) {
      _setTransform(baseScale, 0, 0, baseScale, 0, 0);
    } else _setTransform(a, b, c, d, e, f);
  };

  /* ---------------------------------------------------------------- input */
  function point(ev) {
    var r = cv.getBoundingClientRect();
    var cx = (ev.touches ? ev.touches[0].clientX : ev.clientX);
    var cy = (ev.touches ? ev.touches[0].clientY : ev.clientY);
    mouse.x = clamp(((cx - r.left) / r.width - 0.5) * 2, -1, 1);
    mouse.y = clamp(((cy - r.top) / r.height - 0.5) * 2, -1, 1);
    mouse.inside = true;
  }
  window.addEventListener('mousemove', point);
  window.addEventListener('touchmove', function (e) { point(e); e.preventDefault(); }, { passive: false });
  window.addEventListener('touchstart', point);
  document.addEventListener('mouseleave', function () { mouse.inside = false; });
  window.addEventListener('resize', fit);

  window.addEventListener('keydown', function (e) {
    var k = e.key.toLowerCase();
    if (k === ' ') { paused = !paused; e.preventDefault(); }
    else if (k === 'd') debug = (debug + 1) % 2;
    else if (k === 'r') showRef = !showRef;
    else if (k === 'b') { blink.phase = 'close'; blink.t = 0; }
    else if (k === 'h') hud.classList.toggle('off');
    else if (k >= '0' && k <= '7') solo = (k === '0') ? -1 : parseInt(k, 10) - 1;
    updateHud();
  });

  function updateHud() {
    var s = document.getElementById('state');
    if (!s) return;
    s.textContent = (paused ? 'paused' : 'live') +
      (solo >= 0 ? ' · solo: ' + LAYER_ORDER[solo] : '') +
      (debug ? ' · rig' : '') + (showRef ? ' · reference 50%' : '');
  }

  /* ----------------------------------------------------------------- boot */
  var names = ['bg', 'hair_back', 'head', 'eyes', 'body', 'dress_hem', 'arm', 'leg_raised', 'leg_stand', 'reference'];
  var A = window.__ASSETS__ || {};
  names.forEach(function (n) { if (A[n]) load(n, A[n], booted); });
  if (pending === 0) booted();

  function booted() {
    if (failed.length) {
      var w = document.getElementById('warn');
      if (w) { w.style.display = 'block'; w.textContent = 'missing layers: ' + failed.join(', '); }
    }
    sampleSkin();
    fit();
    updateHud();
    requestAnimationFrame(frame);
  }
})();
