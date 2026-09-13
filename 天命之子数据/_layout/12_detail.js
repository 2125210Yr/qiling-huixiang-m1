/* Gallery .card → one bottom dialog. Does not replace filt / setView / applyHash. */
(function () {
  var EL_CLASS = { "火": "el-fire", "水": "el-water", "木": "el-wood", "光": "el-light", "暗": "el-dark" };
  var SKILLS = [
    ["auto", "普攻"],
    ["tap", "TS"],
    ["slide", "SS"],
    ["drive", "DS"],
    ["leader", "队长"]
  ];
  var openId = "";
  var lastCard = null;
  var map = {};
  var box, inner, closeBtn;

  function esc(s) {
    return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }

  function dash(s) {
    s = s == null ? "" : String(s).trim();
    return s ? s : "—";
  }

  function loadMap() {
    var node = document.getElementById("child-json");
    var list = [];
    if (node) {
      try {
        list = JSON.parse(node.textContent || "[]") || [];
      } catch (e) {
        list = [];
      }
    }
    map = {};
    if (Array.isArray(list)) {
      for (var i = 0; i < list.length; i++) {
        var row = list[i];
        if (row && row.id != null && row.id !== "") map[String(row.id)] = row;
      }
    } else if (list && typeof list === "object") {
      map = list;
    }
  }

  function fromCard(card) {
    var nm = card.querySelector(".nm");
    var img = card.querySelector(".face img");
    return {
      id: card.getAttribute("data-id") || "",
      name: nm ? nm.textContent.trim() : "",
      el: card.getAttribute("data-element") || "",
      role: card.getAttribute("data-role") || "",
      rar: card.getAttribute("data-rarity") || "",
      av: img ? img.getAttribute("src") || "" : ""
    };
  }

  function merge(card) {
    var id = card.getAttribute("data-id") || "";
    var base = fromCard(card);
    var extra = map[id];
    if (!extra) return base;
    var out = {};
    var k;
    for (k in base) if (Object.prototype.hasOwnProperty.call(base, k)) out[k] = base[k];
    for (k in extra) if (Object.prototype.hasOwnProperty.call(extra, k) && extra[k] !== "") out[k] = extra[k];
    return out;
  }

  function faceHtml(d) {
    var el = d.el || "";
    if (d.av) {
      return '<div class="cdet-face" data-el="' + esc(el) + '"><img src="' + esc(d.av) + '" alt="' + esc(d.name || "") + '" width="152" height="152"></div>';
    }
    return '<div class="cdet-face" data-el="' + esc(el) + '"><div class="cdet-ph">无</div></div>';
  }

  function badgesHtml(d) {
    var el = d.el || "";
    var cls = EL_CLASS[el] || "";
    return (
      '<div class="cdet-badges">' +
      '<span class="badge ' + cls + '">' + esc(dash(el)) + "</span>" +
      '<span class="badge">' + esc(dash(d.role)) + "</span>" +
      '<span class="badge">' + esc(dash(d.rar)) + "</span>" +
      "</div>"
    );
  }

  function statsHtml(d) {
    return (
      '<div class="cdet-stats">CP ' + esc(dash(d.cp)) +
      "　HP " + esc(dash(d.hp)) +
      "　攻" + esc(dash(d.atk)) +
      "　防" + esc(dash(d.def)) +
      "　敏" + esc(dash(d.agl)) +
      "　暴" + esc(dash(d.crt)) +
      "</div>"
    );
  }

  function skillsHtml(d) {
    var html = '<div class="cdet-sk">';
    for (var i = 0; i < SKILLS.length; i++) {
      var key = SKILLS[i][0];
      var lab = SKILLS[i][1];
      html +=
        '<div class="cdet-row"><span class="cdet-lab">' + lab + "</span>" +
        esc(dash(d[key])) + "</div>";
    }
    html += "</div>";
    return html;
  }

  function render(d) {
    var cv = d.cv ? '<div class="cdet-cv">CV ' + esc(d.cv) + "</div>" : "";
    inner.innerHTML =
      faceHtml(d) +
      '<div class="cdet-body">' +
      '<h2 class="cdet-name" id="cdet-name">' + esc(dash(d.name)) + "</h2>" +
      badgesHtml(d) +
      statsHtml(d) +
      skillsHtml(d) +
      cv +
      "</div>";
  }

  function clearOn() {
    var nodes = document.querySelectorAll("#gal .card.on");
    for (var i = 0; i < nodes.length; i++) {
      nodes[i].classList.remove("on");
      nodes[i].setAttribute("aria-expanded", "false");
      nodes[i].removeAttribute("aria-pressed");
    }
  }

  function closeDetail() {
    if (!box || !openId) {
      openId = "";
      return;
    }
    openId = "";
    clearOn();
    box.classList.remove("on");
    box.setAttribute("hidden", "");
    document.body.classList.remove("cdet-open");
    if (lastCard && document.contains(lastCard)) {
      try { lastCard.focus(); } catch (e) {}
    }
    lastCard = null;
  }

  function openDetail(card) {
    var id = card.getAttribute("data-id") || "";
    if (!id) return;
    var d = merge(card);
    lastCard = card;
    openId = id;
    clearOn();
    card.classList.add("on");
    card.setAttribute("tabindex", "-1");
    card.setAttribute("aria-expanded", "true");
    card.setAttribute("aria-pressed", "true");
    render(d);
    box.classList.add("on");
    box.removeAttribute("hidden");
    document.body.classList.add("cdet-open");
    if (closeBtn) {
      try { closeBtn.focus(); } catch (e) {}
    }
  }

  function onGalClick(ev) {
    var card = ev.target.closest("#gal .card");
    if (!card) return;
    var id = card.getAttribute("data-id") || "";
    if (openId && id === openId) {
      closeDetail();
      return;
    }
    openDetail(card);
  }

  function onGalKey(ev) {
    var card = ev.target.closest("#gal .card");
    if (!card) return;
    if (ev.key !== "Enter" && ev.key !== " ") return;
    ev.preventDefault();
    if (openId && (card.getAttribute("data-id") || "") === openId) closeDetail();
    else openDetail(card);
  }

  function isShown(el) {
    return !!(el && (el.offsetWidth || el.offsetHeight || el.getClientRects().length));
  }

  function onDocKey(ev) {
    if (!openId) return;
    if (ev.key === "Escape") {
      ev.preventDefault();
      closeDetail();
      return;
    }
    if (ev.key !== "Tab" || !box || box.hasAttribute("hidden")) return;
    var sel = box.querySelectorAll("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])");
    var list = [];
    for (var i = 0; i < sel.length; i++) {
      if (!sel[i].disabled && isShown(sel[i])) list.push(sel[i]);
    }
    if (!list.length) {
      ev.preventDefault();
      if (closeBtn) closeBtn.focus();
      return;
    }
    var first = list[0];
    var last = list[list.length - 1];
    if (ev.shiftKey && document.activeElement === first) {
      ev.preventDefault();
      last.focus();
    } else if (!ev.shiftKey && document.activeElement === last) {
      ev.preventDefault();
      first.focus();
    }
  }

  function wrap(name, after) {
    var fn = window[name];
    if (typeof fn !== "function") return;
    window[name] = function () {
      var r = fn.apply(this, arguments);
      after();
      return r;
    };
  }

  function afterView() {
    var gal = document.getElementById("gal");
    if (gal && gal.classList.contains("hidden")) closeDetail();
  }

  function afterFilt() {
    if (!openId) return;
    var card = document.querySelector("#gal .card.on");
    if (!card || card.classList.contains("hidden")) closeDetail();
  }

  function init() {
    var gal = document.getElementById("gal");
    box = document.getElementById("cdet");
    if (!gal || !box) return;
    inner = document.getElementById("cdet-inner") || box;
    closeBtn = document.getElementById("cdet-close");
    loadMap();
    if (!inner || inner === box) {
      inner = document.createElement("div");
      inner.id = "cdet-inner";
      inner.className = "cdet-inner";
      box.appendChild(inner);
    }
    if (!closeBtn) {
      closeBtn = document.createElement("button");
      closeBtn.type = "button";
      closeBtn.id = "cdet-close";
      closeBtn.className = "cdet-close";
      closeBtn.setAttribute("aria-label", "关闭详情");
      closeBtn.textContent = "×";
      box.insertBefore(closeBtn, box.firstChild);
    }
    var cards = gal.querySelectorAll(".card");
    for (var i = 0; i < cards.length; i++) {
      cards[i].setAttribute("aria-controls", "cdet");
      cards[i].setAttribute("aria-expanded", "false");
    }
    gal.addEventListener("click", onGalClick);
    gal.addEventListener("keydown", onGalKey);
    document.addEventListener("keydown", onDocKey);
    if (closeBtn) closeBtn.addEventListener("click", closeDetail);
    wrap("setView", afterView);
    wrap("filt", afterFilt);
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
  else init();
})();
