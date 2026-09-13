/* web-puppet - a layered-PNG puppet engine (no Cubism runtime, no build step).
 *
 * Draws a stack of canvas-aligned PNGs, each transformed about its own pivot by
 * a named bone. Bones are driven by mouse-follow (eye/head/body), idle breath,
 * hair/cloth springs, blink, and a held rest pose.
 *
 * Models register themselves via DCPuppet.register({...}); see models/*.model.js
 * for the config shape. A folder of PNGs + pivots.json can also be dropped in
 * at runtime through the folder picker.
 */
(function () {
  'use strict';

  var REG = [];
  var DCPuppet = window.DCPuppet = { registry: REG, register: function (m) { REG.push(m); } };

  /* ------------------------------------------------------------------ math */

  // Affine as [a,b,c,d,e,f]:  x' = a*x + c*y + e ,  y' = b*x + d*y + f
  var ID = [1, 0, 0, 1, 0, 0];

  function mul(m, n) { // apply n first, then m
    return [
      m[0] * n[0] + m[2] * n[1],
      m[1] * n[0] + m[3] * n[1],
      m[0] * n[2] + m[2] * n[3],
      m[1] * n[2] + m[3] * n[3],
      m[0] * n[4] + m[2] * n[5] + m[4],
      m[1] * n[4] + m[3] * n[5] + m[5]
    ];
  }

  /* Local transform: rotate+scale about (px,py), then translate by (tx,ty). */
  function local(px, py, rot, sx, sy, tx, ty) {
    var c = Math.cos(rot || 0), s = Math.sin(rot || 0);
    sx = sx == null ? 1 : sx;
    sy = sy == null ? 1 : sy;
    var a = c * sx, b = s * sx, cc = -s * sy, d = c * sy;
    return [a, b, cc, d,
      px + (tx || 0) - (a * px + cc * py),
      py + (ty || 0) - (b * px + d * py)];
  }

  function blend(m, k) { // ease a transform back toward identity
    return [
      1 + (m[0] - 1) * k, m[1] * k, m[2] * k, 1 + (m[3] - 1) * k,
      m[4] * k, m[5] * k
    ];
  }

  function Spring() { this.x = 0; this.v = 0; }
  Spring.prototype.step = function (target, freq, zeta, dt) {
    var w = freq * Math.PI * 2;
    this.v += (-2 * zeta * w * this.v - w * w * (this.x - target)) * dt;
    this.x += this.v * dt;
    return this.x;
  };

  function clamp(x, lo, hi) { return x < lo ? lo : x > hi ? hi : x; }

  /* --------------------------------------------------------- role defaults */

  // Draw order, back to front. Lower sorts earlier.
  var Z = {
    bg: 0, hairBack: 10, hairTip: 12, hairSide: 14,
    base: 20, torso: 22, chest: 24, cloth: 26, clothPanel: 28,
    legStand: 30, legRaiseThigh: 34, legRaiseCalf: 36, foot: 38,
    arm: 42, hand: 44, weapon: 46,
    neck: 50, head: 54, eyes: 56, blink: 58, hairFront: 62, horns: 64,
    'static': 25
  };

  // Fallback pivots in normalized top-left space when a model has none.
  var PIV = {
    root: [0.50, 0.96], torso: [0.50, 0.58], head: [0.50, 0.26],
    hairBack: [0.46, 0.22], hairSide: [0.50, 0.24], hairFront: [0.50, 0.18],
    cloth: [0.50, 0.55], legRaiseThigh: [0.46, 0.60], legRaiseCalf: [0.40, 0.72],
    foot: [0.40, 0.84], legStand: [0.52, 0.60], hand: [0.44, 0.55],
    arm: [0.52, 0.36], eyes: [0.50, 0.22], chest: [0.50, 0.40]
  };

  var GUESS = [
    [/blink|eyes?_?closed|closed_?eyes?/, 'blink'],
    [/bg|background|plate/, 'bg'],
    [/(hair_?back|back_?hair)/, 'hairBack'],
    [/(hair_?tip|tip_?hair)/, 'hairTip'],
    [/(hair_?side|side_?hair)/, 'hairSide'],
    [/(hair_?front|front_?hair|bang)/, 'hairFront'],
    [/horn/, 'horns'],
    [/(iris|eyewhite|pupil|eye)/, 'eyes'],
    [/(face|head)/, 'head'],
    [/neck/, 'neck'],
    [/(chest|bust|breast)/, 'chest'],
    [/(slit|panel)/, 'clothPanel'],
    [/(dress|skirt|cloth|hem|robe|coat)/, 'cloth'],
    [/thigh/, 'legRaiseThigh'],
    [/calf|shin/, 'legRaiseCalf'],
    [/(foot|shoe|boot|toe)/, 'foot'],
    [/leg.*stand|stand.*leg/, 'legStand'],
    [/leg/, 'legRaiseThigh'],
    [/hand/, 'hand'],
    [/arm/, 'arm'],
    [/(sword|blade|weapon|bow|gun|staff|spear)/, 'weapon'],
    [/(body|torso|base)/, 'base']
  ];

  // Files that are references / debug output, never puppet layers.
  var SKIP = /(composite|compare|diff|_dbg|^mask_|_probe|_raw$|raw$|bbox|report|sheet)/;

  function guessRole(name) {
    var n = name.toLowerCase().replace(/\.png$/, '').replace(/^\d+[_-]/, '');
    for (var i = 0; i < GUESS.length; i++) if (GUESS[i][0].test(n)) return GUESS[i][1];
    return 'static';
  }

  /* ------------------------------------------------------------- the rig */

  function Puppet(model, canvas, status) {
    this.model = model;
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.status = status || function () { };
    this.W = (model.canvas && model.canvas[0]) || 1024;
    this.H = (model.canvas && model.canvas[1]) || 1536;
    canvas.width = this.W;
    canvas.height = this.H;

    this.layers = [];
    this.missing = [];
    this.look = { x: 0, y: 0, tx: 0, ty: 0 };
    this.sHairA = new Spring(); this.sHairB = new Spring(); this.sHairC = new Spring();
    this.sHem = new Spring(); this.sLeg = new Spring(); this.sLegB = new Spring();
    this.gust = 0;
    this.blink = 0;
    this.blinkUntil = 0;
    this.nextBlink = 1600;
    this.t0 = performance.now();
    this.last = this.t0;
    this.fps = 0;

    this.opt = {
      paused: false, follow: 1, breath: 1, sway: 1, bend: 1,
      restPose: true, showPivots: false, autoBlink: true
    };
  }

  Puppet.prototype.pivot = function (spec, role) {
    var p = spec;
    if (typeof p === 'string') p = this.model.pivots && this.model.pivots[p];
    if (!p && role) p = this.model.pivots && this.model.pivots[role];
    if (!p) p = PIV[role] || PIV.torso;
    var v = p[1];
    if (this.model.pivotOrigin === 'bottom-left') v = 1 - v;
    return [p[0] * this.W, v * this.H];
  };

  Puppet.prototype.load = function (done) {
    var self = this;
    var defs = this.model.layers.slice();
    // stable back-to-front order
    defs.forEach(function (d, i) { d._i = i; });
    defs.sort(function (a, b) {
      var za = a.z != null ? a.z : (Z[a.role] != null ? Z[a.role] : 25);
      var zb = b.z != null ? b.z : (Z[b.role] != null ? Z[b.role] : 25);
      return za - zb || a._i - b._i;
    });

    var pending = defs.length, any = false;
    if (!pending) { done(); return; }

    defs.forEach(function (d) {
      var L = {
        def: d, role: d.role, img: new Image(), ok: false, on: true,
        name: d.name || (d.file || '').split('/').pop()
      };
      self.layers.push(L);
      L.img.onload = function () { L.ok = true; any = true; finish(); };
      L.img.onerror = function () {
        if (d.fallback && !L._fb) {
          L._fb = true;
          L.name = d.fallback;
          L.img.src = d.url || encodeURI((self.model.dir || '') + d.fallback);
          return;
        }
        self.missing.push(L.name);
        finish();
      };
      L.img.src = d.url || encodeURI((self.model.dir || '') + d.file);
    });

    function finish() {
      if (--pending > 0) {
        self.status('loading ' + (defs.length - pending) + '/' + defs.length);
        return;
      }
      self.ready = any;
      // Canvas must match the actual PNG, not a stale hardcoded size.
      var probe = null;
      for (var i = 0; i < self.layers.length; i++) {
        if (self.layers[i].ok && self.layers[i].role === 'base') { probe = self.layers[i]; break; }
      }
      if (!probe) {
        for (var j = 0; j < self.layers.length; j++) {
          if (self.layers[j].ok) { probe = self.layers[j]; break; }
        }
      }
      if (probe && probe.img.naturalWidth && probe.img.naturalHeight) {
        self.W = probe.img.naturalWidth;
        self.H = probe.img.naturalHeight;
        self.canvas.width = self.W;
        self.canvas.height = self.H;
        self.model.canvas = [self.W, self.H];
      }
      done();
    }
  };

  /* Build every bone for this frame. */
  Puppet.prototype.bones = function (s) {
    var W = this.W, H = this.H, o = this.opt, m = this.model;
    var rest = (o.restPose && m.restPose) || {};
    var B = {};
    var pv = this.pivot.bind(this);

    var pRoot = pv(m.pivots && m.pivots.root ? 'root' : null, 'root');
    B.root = local(pRoot[0], pRoot[1],
      s.lookX * 0.016 * o.follow,
      1, 1,
      s.lookX * 5 * o.follow, s.bob + s.lookY * 2 * o.follow);

    var pTor = pv(null, 'torso');
    B.torso = mul(B.root, local(pTor[0], pTor[1], 0,
      1 + s.breath * 0.004 * o.breath, 1 + s.breath * 0.009 * o.breath, 0, 0));
    B.chest = mul(B.root, local(pv(null, 'chest')[0], pv(null, 'chest')[1], 0,
      1 + s.breath * 0.012 * o.breath, 1 + s.breath * 0.018 * o.breath, 0, 0));
    B.base = B.torso;
    B['static'] = B.torso;

    var pHead = pv(null, 'head');
    B.head = mul(B.torso, local(pHead[0], pHead[1],
      (s.lookX * 0.15 * o.follow) + s.idle * 0.35,
      1, 1,
      s.lookX * W * 0.012 * o.follow,
      s.lookY * H * 0.006 * o.follow - s.breath * 1.2 * o.breath));
    B.neck = mul(B.torso, blend(mul([1, 0, 0, 1, -B.torso[4], -B.torso[5]], B.head), 0.35));
    B.horns = B.head;

    var pEye = pv(null, 'eyes');
    B.eyes = mul(B.head, local(pEye[0], pEye[1], 0, 1, 1,
      s.lookX * W * 0.006 * o.follow, s.lookY * H * 0.003 * o.follow));

    var pHB = pv(null, 'hairBack');
    B.hairBack = mul(B.torso, local(pHB[0], pHB[1], s.hairA * 0.055 * o.sway, 1, 1, 0, s.hairY * 3));
    B.hairTip = mul(B.torso, local(pHB[0], pHB[1], s.hairA * 0.085 * o.sway, 1, 1, 0, s.hairY * 5));
    var pHS = pv(null, 'hairSide');
    B.hairSide = mul(B.torso, local(pHS[0], pHS[1], s.hairB * 0.05 * o.sway, 1, 1, 0, s.hairY * 2));
    var pHF = pv(null, 'hairFront');
    B.hairFront = mul(B.head, local(pHF[0], pHF[1], s.hairC * 0.045 * o.sway, 1, 1, 0, 0));

    var pCl = pv(null, 'cloth');
    B.cloth = mul(B.torso, local(pCl[0], pCl[1], s.hem * 0.035 * o.sway, 1, 1, 0, 0));
    B.clothPanel = mul(B.torso, local(pCl[0], pCl[1], s.hem * 0.055 * o.sway, 1, 1, 0, 0));

    var deg = Math.PI / 180;
    var pTh = pv(null, 'legRaiseThigh');
    B.legRaiseThigh = mul(B.root, local(pTh[0], pTh[1],
      (rest.legRaiseThigh || 0) * deg + s.leg * 0.018, 1, 1, 0, 0));
    var pCa = pv(null, 'legRaiseCalf');
    B.legRaiseCalf = mul(B.legRaiseThigh, local(pCa[0], pCa[1],
      (rest.legRaiseCalf || 0) * deg + s.legLag * 0.030, 1, 1, 0, 0));
    var pFt = pv(null, 'foot');
    B.foot = mul(B.legRaiseCalf, local(pFt[0], pFt[1],
      (rest.foot || 0) * deg + s.legLag * 0.045, 1, 1, 0, 0));
    var pLs = pv(null, 'legStand');
    B.legStand = mul(B.root, local(pLs[0], pLs[1],
      (rest.legStand || 0) * deg - s.leg * 0.005, 1, 1, 0, 0));

    var pArm = pv(null, 'arm');
    B.arm = mul(B.torso, local(pArm[0], pArm[1],
      (rest.arm || 0) * deg - s.lookX * 0.022 * o.follow, 1, 1, 0, 0));
    var pHand = pv(null, 'hand');
    B.hand = mul(B.arm, local(pHand[0], pHand[1], -s.lookX * 0.012 * o.follow, 1, 1, 0, 0));
    B.weapon = mul(B.hand, local(pHand[0], pHand[1], s.hairB * 0.012 * o.sway, 1, 1, 0, 0));

    B.bg = ID;
    B.blink = B.head;
    return B;
  };

  var STRIPS = 16;

  Puppet.prototype.drawLayer = function (L, mat, alpha) {
    var ctx = this.ctx, W = this.W, H = this.H;
    ctx.setTransform(mat[0], mat[1], mat[2], mat[3], mat[4], mat[5]);
    ctx.globalAlpha = alpha == null ? 1 : alpha;

    var bendAmt = (L.def.bend || 0) * this.opt.bend * this.opt.sway;
    if (!bendAmt) {
      ctx.drawImage(L.img, 0, 0, W, H);
      return;
    }
    // Cheap non-rigid sway: shift horizontal strips more the further they sit
    // from the pivot, so long hair / a hem curves instead of pivoting rigidly.
    var py = this.pivot(L.def.pivot, L.role)[1];
    var sh = H / STRIPS;
    var drive = bendAmt * (L.bendDrive || 0);
    for (var i = 0; i < STRIPS; i++) {
      var y = i * sh;
      var k = (y + sh * 0.5 - py) / H;
      var dx = drive * k * Math.abs(k) * W * 0.10;
      ctx.drawImage(L.img, 0, y, W, sh + 1, dx, y, W, sh + 1);
    }
  };

  Puppet.prototype.draw = function (s) {
    var ctx = this.ctx, W = this.W, H = this.H;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.globalAlpha = 1;
    ctx.clearRect(0, 0, W, H);
    if (this.model.backdrop) {
      ctx.fillStyle = this.model.backdrop;
      ctx.fillRect(0, 0, W, H);
    }
    if (!this.ready) return;

    var B = this.bones(s);
    var drives = {
      hairBack: s.hairA, hairTip: s.hairA * 1.5, hairSide: s.hairB,
      hairFront: s.hairC, cloth: s.hem, clothPanel: s.hem * 1.4
    };

    for (var i = 0; i < this.layers.length; i++) {
      var L = this.layers[i];
      if (!L.ok || !L.on) continue;
      L.bendDrive = drives[L.role] || 0;
      var mat = B[L.role] || B.torso;
      if (L.role === 'blink') {
        if (s.blink > 0.001) this.drawLayer(L, B[L.def.follow || 'base'] || B.base, s.blink);
        continue;
      }
      this.drawLayer(L, mat, L.def.alpha);
    }

    // Procedural eyelids, for rigs with no closed-eye plate.
    var lids = this.model.lids;
    if (lids && s.blink > 0.02) {
      var m = B.head;
      ctx.setTransform(m[0], m[1], m[2], m[3], m[4], m[5]);
      ctx.globalAlpha = 1;
      for (var e = 0; e < lids.eyes.length; e++) {
        var ex = lids.eyes[e][0] * W;
        var ey = lids.eyes[e][1] * H;
        var rx = lids.rx * W, ry = lids.ry * H * s.blink;
        ctx.fillStyle = lids.color || '#e6c6b0';
        ctx.beginPath();
        ctx.ellipse(ex, ey, rx, ry, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = lids.lash || 'rgba(60,34,40,0.85)';
        ctx.fillRect(ex - rx, ey + ry - Math.max(1, ry * 0.22), rx * 2, Math.max(1, ry * 0.22));
      }
    }

    if (this.opt.showPivots) this.debugPivots(B);
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.globalAlpha = 1;
  };

  Puppet.prototype.debugPivots = function (B) {
    var ctx = this.ctx;
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.globalAlpha = 1;
    ctx.font = (this.W / 64) + 'px monospace';
    for (var i = 0; i < this.layers.length; i++) {
      var L = this.layers[i];
      if (!L.ok || !L.on || L.role === 'bg') continue;
      var p = this.pivot(L.def.pivot, L.role);
      var m = B[L.role] || B.torso;
      var x = m[0] * p[0] + m[2] * p[1] + m[4];
      var y = m[1] * p[0] + m[3] * p[1] + m[5];
      ctx.strokeStyle = '#7cf6ff';
      ctx.lineWidth = Math.max(1, this.W / 400);
      ctx.beginPath();
      ctx.arc(x, y, this.W / 90, 0, Math.PI * 2);
      ctx.moveTo(x - this.W / 60, y); ctx.lineTo(x + this.W / 60, y);
      ctx.moveTo(x, y - this.W / 60); ctx.lineTo(x, y + this.W / 60);
      ctx.stroke();
      ctx.fillStyle = '#7cf6ff';
      ctx.fillText(L.role, x + this.W / 70, y - this.W / 120);
    }
  };

  Puppet.prototype.tick = function (now) {
    var dt = Math.min(0.034, (now - this.last) / 1000);
    this.last = now;
    this.fps += ((1 / Math.max(dt, 1e-4)) - this.fps) * 0.08;
    var t = (now - this.t0) / 1000;
    var o = this.opt;

    var k = Math.min(1, dt * 9);
    this.look.x += (this.look.tx - this.look.x) * k;
    this.look.y += (this.look.ty - this.look.y) * k;

    if (!o.paused) {
      var wind = Math.sin(t * 0.62) * 0.42 + Math.sin(t * 1.9) * 0.12 + this.gust;
      this.sHairA.step(wind, 1.05, 0.30, dt);
      this.sHairB.step(-wind * 0.8, 1.25, 0.32, dt);
      this.sHairC.step(wind * 0.55, 1.55, 0.34, dt);
      this.sHem.step(-wind * 0.9, 1.30, 0.28, dt);
      this.sLeg.step(Math.sin(t * 1.35) * 0.30 + this.gust * 0.10, 1.6, 0.34, dt);
      this.sLegB.step(this.sLeg.x, 2.1, 0.30, dt);
      this.gust *= Math.pow(0.90, dt * 60);

      if (o.autoBlink && now > this.nextBlink) {
        this.blinkUntil = now + 105;
        this.nextBlink = now + 2600 + Math.random() * 3400;
      }
      var target = now < this.blinkUntil ? 1 : 0;
      this.blink += (target - this.blink) * Math.min(1, dt * (target ? 26 : 13));
      this.tSim = (this.tSim || 0) + dt;
    }

    var ts = this.tSim || 0;
    this.draw({
      lookX: clamp(this.look.x, -1, 1),
      lookY: clamp(this.look.y, -1, 1),
      breath: 0.5 + 0.5 * Math.sin(ts * 2.0),
      bob: Math.sin(ts * 1.0) * this.H * 0.0022 * o.breath,
      idle: Math.sin(ts * 0.45) * 0.05,
      hairA: this.sHairA.x, hairB: this.sHairB.x, hairC: this.sHairC.x,
      hairY: Math.sin(ts * 1.1) * 2,
      hem: this.sHem.x,
      leg: this.sLeg.x, legLag: this.sLegB.x,
      blink: this.blink
    });
  };

  Puppet.prototype.start = function () {
    var self = this;
    function frame(now) { self.raf = requestAnimationFrame(frame); self.tick(now); }
    this.raf = requestAnimationFrame(frame);
  };
  Puppet.prototype.stop = function () { if (this.raf) cancelAnimationFrame(this.raf); };

  /* ------------------------------------------------------- folder loading */

  /* Build a model from an arbitrary folder of PNGs (+ pivots.json). */
  function modelFromFiles(files, label) {
    var pngs = [], pivots = null, explicit = null, pending = [];
    for (var i = 0; i < files.length; i++) {
      var f = files[i];
      var base = f.name.split('/').pop();
      if (/^(pivots|landmarks)\.json$/i.test(base)) pending.push(['pivots', f]);
      else if (/^(model|puppet)\.json$/i.test(base)) pending.push(['model', f]);
      else if (/\.png$/i.test(base) && !SKIP.test(base.toLowerCase())) pngs.push(f);
    }

    return new Promise(function (resolve, reject) {
      Promise.all(pending.map(function (p) {
        return p[1].text().then(function (txt) {
          try { return [p[0], JSON.parse(txt)]; } catch (e) { return [p[0], null]; }
        });
      })).then(function (parsed) {
        parsed.forEach(function (p) {
          if (p[0] === 'pivots') pivots = p[1];
          if (p[0] === 'model') explicit = p[1];
        });
        if (!pngs.length) { reject(new Error('no usable PNGs in that folder')); return; }

        var seen = {}, layers = [], skipped = [];
        pngs.sort(function (a, b) { return a.name.localeCompare(b.name); });
        pngs.forEach(function (f) {
          var base = f.name.split('/').pop();
          var role = guessRole(base);
          // one layer per role, except roles that legitimately repeat
          var multi = /^(static|foot|hand|arm|horns|eyes|clothPanel)$/.test(role);
          if (!multi && seen[role]) { skipped.push(base); return; }
          seen[role] = true;
          layers.push({
            file: base, name: base, role: role,
            url: URL.createObjectURL(f),
            bend: /hair|cloth|dress|skirt|hem/.test(role.toLowerCase()) ? 1 : 0
          });
        });

        var probe = new Image();
        probe.onload = function () {
          var m = {
            id: 'dropped', name: label || 'Dropped folder',
            canvas: [probe.naturalWidth, probe.naturalHeight],
            pivotOrigin: (pivots && pivots.pivotOrigin) || 'top-left',
            pivots: pivots || {}, layers: layers, skipped: skipped,
            backdrop: '#101018'
          };
          if (explicit) for (var kk in explicit) if (kk !== 'layers') m[kk] = explicit[kk];
          resolve(m);
        };
        probe.onerror = function () { reject(new Error('could not decode the PNGs')); };
        probe.src = layers[0].url;
      });
    });
  }

  /* Fetch a folder over http: needs pivots.json + an explicit file list. */
  function modelFromDir(dir, files) {
    if (dir.slice(-1) !== '/') dir += '/';
    return fetch(encodeURI(dir + 'pivots.json')).then(function (r) {
      return r.ok ? r.json() : {};
    }).catch(function () { return {}; }).then(function (pivots) {
      var layers = files.map(function (f) {
        var role = guessRole(f);
        return {
          file: f, name: f, role: role,
          bend: /hair|cloth/.test(role.toLowerCase()) ? 1 : 0
        };
      });
      return new Promise(function (resolve, reject) {
        var probe = new Image();
        probe.onload = function () {
          resolve({
            id: 'dir', name: dir, dir: dir,
            canvas: [probe.naturalWidth, probe.naturalHeight],
            pivotOrigin: pivots.pivotOrigin || 'top-left',
            pivots: pivots, layers: layers, backdrop: '#101018'
          });
        };
        probe.onerror = function () { reject(new Error('cannot read ' + dir)); };
        probe.src = encodeURI(dir + files[0]);
      });
    });
  }

  DCPuppet.Puppet = Puppet;
  DCPuppet.modelFromFiles = modelFromFiles;
  DCPuppet.modelFromDir = modelFromDir;
  DCPuppet.guessRole = guessRole;
})();
