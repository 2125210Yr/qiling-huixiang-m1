/**
 * physics.js — 弹簧-阻尼次级运动，可直接丢进 canvas puppet。
 *
 * 无依赖，ES module。吃同目录的 physics_groups.json（Cubism physics3.json 格式），
 * 每帧读输入参数、写输出参数，其余渲染逻辑不关心。
 *
 *   import { createPhysicsRig } from './physics.js';
 *   const rig = createPhysicsRig(physicsJson);          // 一次
 *   rig.step(dt, params);                                // 每帧，params 是 { ParamXxx: number }
 *
 * params 是原地读写：rig 读 Input 的 Source.Id，写 Output 的 Destination.Id。
 *
 * 与 Cubism 内置物理的关系：这里不复刻 Cubism 的粒子解算，而是把 physics3.json 的
 * 每个 vertex 当成一个一维角度弹簧，用 Radius / Acceleration / Delay / Mobility 推出
 * 弹簧系数（见 springFromVertex）。同一份 JSON 在 Cubism 里和在这里跑，量级和相位关系
 * 一致，绝对数值不会逐帧相同。要像素级一致就用官方 SDK。
 */

const FIXED_DT = 1 / 120;
const MAX_STEPS = 8;

/** Radius→固有频率的换算常数。调大 = 整体更快更硬。360 下：刘海≈1.8Hz、发梢≈1.0Hz、脚链≈4.7Hz。 */
const GRAVITY_SCALE = 360;

/**
 * 满驱动（归一化输入打到 ±1）时的摆角 = Normalization.Angle.Maximum × 本常数，单位度。
 * physics3.json 的 Normalization.Angle 在 Cubism 里只用于输入归一化，不直接是摆角；
 * 这里把它当成"该组允许多大摆幅"的相对刻度，再由本常数换成实际角度。
 * 调它 = 整体摆幅总控；调单条 Output.Scale = 某个参数的读数强度。
 */
const SWING_GAIN = 3.4;

const DEG = 180 / Math.PI;

/**
 * Y 向输入（电梯垂向加速度、呼吸）对水平摆动的耦合系数。
 * 纯垂向激励对理想单摆不产生水平摆，但轿厢顿挫不是纯垂向、发束也不是垂直悬挂，
 * 所以这里给一个受控耦合，让 ParamElevatorAccel 能真的甩到发梢和下摆。0 = 只出 Y。
 */
const Y_TO_X_COUPLING = 0.45;

const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v);

/** 参数范围表。physics3.json 不含参数值域，归一化需要它。缺项时按 ID 猜，可用 paramRanges 覆盖。 */
const DEFAULT_RANGES = {
  ParamAngleX: [-30, 30],
  ParamAngleY: [-30, 30],
  ParamAngleZ: [-30, 30],
  ParamBodyAngleX: [-10, 10],
  ParamBodyAngleY: [-10, 10],
  ParamBodyAngleZ: [-10, 10],
  ParamBreath: [0, 1],
};

function rangeFor(id, overrides) {
  if (overrides && overrides[id]) return overrides[id];
  if (DEFAULT_RANGES[id]) return DEFAULT_RANGES[id];
  if (id.startsWith('ParamAngle')) return [-30, 30];
  if (id.startsWith('ParamBodyAngle')) return [-10, 10];
  return [-1, 1];
}

/**
 * 一维弹簧-阻尼。半隐式（symplectic）欧拉，在 120Hz 下对本 rig 的全部 ω 都稳定。
 * 显式欧拉在 zeta<0.2 的脚链那一组会发散，别换回去。
 */
export class Spring {
  constructor(freqHz = 2, zeta = 0.7, value = 0) {
    this.omega = freqHz * 2 * Math.PI;
    this.zeta = zeta;
    this.value = value;
    this.velocity = 0;
    this.target = value;
  }

  setFrequency(freqHz) {
    this.omega = freqHz * 2 * Math.PI;
  }

  step(dt) {
    const a = this.omega * this.omega * (this.target - this.value) - 2 * this.zeta * this.omega * this.velocity;
    this.velocity += a * dt;
    this.value += this.velocity * dt;
    return this.value;
  }

  /** 瞬时冲量。点胸 / 电梯启停这类事件用，不要直接改 value（会跳帧）。 */
  impulse(v) {
    this.velocity += v;
  }

  reset(value = 0) {
    this.value = value;
    this.target = value;
    this.velocity = 0;
  }
}

/**
 * 把 physics3.json 的一个 vertex 翻成弹簧系数。
 *
 *   Radius       → 摆长。越长越慢：ω ∝ sqrt(1/radius)
 *   Acceleration → 有效重力倍率，直接进 ω
 *   Delay        → 迟滞。越大越像重物：ζ ∝ delay
 *   Mobility     → 响应速度，进 ω。**不是**稳态增益：静态输入下低 mobility 的段最终
 *                  也要摆到位，只是慢。当增益用会让抬起腿、贴腿裙片这类低 mobility 组直接失声。
 */
function springFromVertex(vertex, gravityScale) {
  const radius = Math.max(vertex.Radius ?? 1, 0.05);
  const accel = vertex.Acceleration ?? 1;
  const delay = vertex.Delay ?? 1;
  const mobility = clamp(vertex.Mobility ?? 1, 0.01, 1);

  const omega = Math.sqrt((gravityScale * accel) / radius) * (0.5 + 0.5 * mobility);
  const zeta = clamp(delay * 0.55, 0.08, 1.6);

  const s = new Spring(1, zeta, 0);
  s.omega = omega;
  return s;
}

/** 一条物理链：输入归一化 → 逐段弹簧 → 输出写回。 */
class Chain {
  constructor(setting, opts) {
    this.id = setting.Id;
    this.name = opts.names[setting.Id] || setting.Id;
    this.inputs = setting.Input || [];
    this.outputs = setting.Output || [];

    const norm = setting.Normalization || {};
    this.normPos = norm.Position || { Minimum: -10, Default: 0, Maximum: 10 };
    this.normAngle = norm.Angle || { Minimum: -10, Default: 0, Maximum: 10 };

    this.ranges = opts.paramRanges;
    this.swingGain = opts.swingGain;

    // vertex 0 是固定根，不建弹簧。输出的 VertexIndex 从 1 开始，故此处补一个 null 对齐下标。
    // 每段两个自由度：sx = 水平摆（Angle / X 输出读它），sy = 垂向惯性（Y 输出读它）。
    const verts = setting.Vertices || [];
    this.springs = [null];
    for (let i = 1; i < verts.length; i++) {
      this.springs.push({
        sx: springFromVertex(verts[i], opts.gravityScale),
        sy: springFromVertex(verts[i], opts.gravityScale),
      });
    }
  }

  /**
   * 输入合成为两个 [-1,1] 驱动量。
   * X / Angle 型输入进水平通道；Y 型输入进垂向通道，并按 Y_TO_X_COUPLING 漏一部分到水平。
   */
  readInput(params) {
    let accX = 0;
    let sumX = 0;
    let accY = 0;
    let sumY = 0;

    for (const inp of this.inputs) {
      const id = inp.Source.Id;
      const [lo, hi] = rangeFor(id, this.ranges);
      const span = Math.max(Math.abs(lo), Math.abs(hi)) || 1;
      let n = clamp((params[id] ?? 0) / span, -1, 1);
      if (inp.Reflect) n = -n;
      const w = (inp.Weight ?? 0) / 100;

      if ((inp.Type || 'X') === 'Y') {
        accY += n * w;
        sumY += w;
      } else {
        accX += n * w;
        sumX += w;
      }
    }

    const denomX = sumX + Y_TO_X_COUPLING * sumY;
    return {
      x: denomX > 0 ? clamp((accX + Y_TO_X_COUPLING * accY) / denomX, -1, 1) : 0,
      y: sumY > 0 ? clamp(accY / sumY, -1, 1) : 0,
    };
  }

  step(dt, params) {
    const drive = this.readInput(params);
    const amp = (this.normAngle.Maximum || 10) * this.swingGain;

    // 根段目标由输入给；后续段追前一段的当前值，迟滞沿链逐级累积。
    let parentX = drive.x * amp;
    let parentY = drive.y * amp;
    for (let i = 1; i < this.springs.length; i++) {
      const { sx, sy } = this.springs[i];
      sx.target = parentX;
      sy.target = parentY;
      sx.step(dt);
      sy.step(dt);
      parentX = sx.value;
      parentY = sy.value;
    }
  }

  write(params) {
    for (const out of this.outputs) {
      const node = this.springs[out.VertexIndex];
      if (!node) continue;

      const id = out.Destination.Id;
      const [lo, hi] = rangeFor(id, this.ranges);
      const w = (out.Weight ?? 100) / 100;
      const scale = out.Scale ?? 1;

      // 跟 Cubism 一致：段角度取**弧度**再乘 Scale。用角度会大一个数量级、Scale 2-4 立刻打满。
      const isY = out.Type === 'Y';
      const rad = (isY ? node.sy.value : node.sx.value) / DEG;
      let v = isY ? Math.sin(rad) : out.Type === 'X' ? Math.sin(rad) : rad;
      if (out.Reflect) v = -v;

      params[id] = clamp(v * scale * w, lo, hi);
    }
  }

  reset() {
    for (const node of this.springs) {
      if (!node) continue;
      node.sx.reset(0);
      node.sy.reset(0);
    }
  }
}

/**
 * @param {object} physicsJson  physics_groups.json 的内容
 * @param {object} [options]
 * @param {object} [options.paramRanges]  { ParamXxx: [min, max] }，覆盖内置推断
 * @param {number} [options.gravityScale] 整体快慢，默认 360
 * @param {number} [options.swingGain]    整体摆幅，默认 3.4
 * @param {number} [options.fixedDt]      定步长，默认 1/120
 */
export function createPhysicsRig(physicsJson, options = {}) {
  const names = {};
  for (const e of physicsJson?.Meta?.PhysicsDictionary || []) names[e.Id] = e.Name;

  const opts = {
    names,
    paramRanges: options.paramRanges || null,
    gravityScale: options.gravityScale ?? GRAVITY_SCALE,
    swingGain: options.swingGain ?? SWING_GAIN,
  };
  const fixedDt = options.fixedDt ?? FIXED_DT;

  // PhysicsSettings 的顺序是有依赖的（thigh_raised 的输出是 dress_slit_panel / anklet_R 的输入），
  // 这里按 JSON 原序建链并按原序求解，不要排序。
  const chains = (physicsJson?.PhysicsSettings || []).map((s) => new Chain(s, opts));
  const byName = new Map(chains.map((c) => [c.name, c]));

  let accumulator = 0;

  return {
    chains,

    /** 按名字取链（physics_groups.json 的 PhysicsDictionary.Name，如 'hair_back'） */
    group(name) {
      return byName.get(name);
    },

    /**
     * @param {number} dt      秒。切标签页回来时可能是几秒，内部会截断。
     * @param {object} params  { ParamXxx: number }，原地读写。
     */
    step(dt, params) {
      if (!Number.isFinite(dt) || dt <= 0) dt = fixedDt;
      accumulator += Math.min(dt, fixedDt * MAX_STEPS);

      let steps = 0;
      while (accumulator >= fixedDt && steps < MAX_STEPS) {
        for (const c of chains) {
          c.step(fixedDt, params);
          c.write(params); // 立刻写回：下游链要读到本帧的上游输出
        }
        accumulator -= fixedDt;
        steps++;
      }
      if (steps === MAX_STEPS) accumulator = 0;
      return params;
    },

    /** 给某组打冲量，越靠末端越强。电梯顿挫 / 点击反馈用（别直接改参数值，会跳帧）。 */
    impulse(name, amount, axis = 'x') {
      const c = byName.get(name);
      if (!c) return;
      const last = c.springs.length - 1;
      for (let i = 1; i <= last; i++) {
        c.springs[i][axis === 'y' ? 'sy' : 'sx'].impulse(amount * (i / last));
      }
    },

    /** 全部回 rest。切换姿势时调用，避免第一帧突跳。 */
    reset() {
      accumulator = 0;
      for (const c of chains) c.reset();
    },
  };
}

export default createPhysicsRig;
