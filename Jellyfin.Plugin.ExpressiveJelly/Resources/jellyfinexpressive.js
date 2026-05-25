// This file is embedded and served by /ExpressiveJelly/theme.js
// Source is derived from dist/jellyfinexpressive.user.js in this repo.
(() => {
  const BAR_CLASS = "jfx-squigglebar";
  const DYN_ATTR = "data-jfx-dynamic";
  let booted = false;
  let dynamicEnabled = false;
  let root = null;
  let baseAccent = "#7ddcff";
  let baseAccent2 = "#c4b5ff";
  let baseAccent3 = "#ff6bd6";
  let lastArtworkUrl = null;
  let pending = null;
  let dynamicDebounceHandle = null;

  function getDynamicThemingEnabled() {
    if (window.__ExpressiveJelly && typeof window.__ExpressiveJelly.dynamicThemingEnabled === "boolean") {
      return window.__ExpressiveJelly.dynamicThemingEnabled;
    }
    try {
      const script = Array.from(document.scripts).find((s) =>
        typeof s.src === "string" && s.src.includes("/ExpressiveJelly/theme.js")
      );
      if (!script) return true;
      const u = new URL(script.src, location.href);
      const dyn = u.searchParams.get("dyn");
      if (dyn === null) return true;
      return dyn !== "0";
    } catch {
      return true;
    }
  }

  function ensureSvgFilterHost(root) {
    if (root.querySelector("#jfx-squiggle-filters")) return;
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("id", "jfx-squiggle-filters");
    svg.setAttribute("width", "0");
    svg.setAttribute("height", "0");
    svg.style.position = "absolute";
    svg.style.left = "-9999px";
    svg.style.top = "-9999px";
    svg.innerHTML = `
      <defs>
        <filter id="jfxSquiggle" x="-20%" y="-200%" width="140%" height="500%">
          <feTurbulence type="fractalNoise" baseFrequency="0.02 0.8" numOctaves="2" seed="2" result="noise">
            <animate attributeName="baseFrequency" dur="1.2s" values="0.02 0.6;0.03 0.9;0.02 0.6" repeatCount="indefinite"/>
            <animate attributeName="seed" dur="2.8s" values="1;8;1" repeatCount="indefinite"/>
          </feTurbulence>
          <feDisplacementMap in="SourceGraphic" in2="noise" scale="14" xChannelSelector="R" yChannelSelector="G"/>
        </filter>
      </defs>
    `;
    root.appendChild(svg);
  }

  function findOverlayHostForVideo(videoEl) {
    let el = videoEl;
    for (let i = 0; i < 8 && el; i++) {
      const style = getComputedStyle(el);
      if (style.position !== "static") return el;
      el = el.parentElement;
    }
    return videoEl.parentElement || document.body;
  }

  function ensureBar(host) {
    let bar = host.querySelector(`:scope > .${BAR_CLASS}`);
    if (bar) return bar;

    const hostStyle = getComputedStyle(host);
    if (hostStyle.position === "static") host.style.position = "relative";

    bar = document.createElement("div");
    bar.className = BAR_CLASS;
    bar.innerHTML = `
      <div class="jfx-track">
        <div class="jfx-fill"></div>
      </div>
    `;
    bar.dataset.visible = "false";
    bar.dataset.mode = "normal";
    host.appendChild(bar);
    return bar;
  }

  function setBarProgress(bar, pct) {
    const clamped = Math.max(0, Math.min(100, pct));
    bar.style.setProperty("--jfx-progress", `${clamped}%`);
  }

  function attachToVideo(videoEl) {
    if (videoEl.__jfxAttached) return;
    videoEl.__jfxAttached = true;

    ensureSvgFilterHost(document.body);

    const host = findOverlayHostForVideo(videoEl);
    const bar = ensureBar(host);

    const show = (mode) => {
      bar.dataset.visible = "true";
      bar.dataset.mode = mode;
    };
    const hide = () => {
      bar.dataset.visible = "false";
      bar.dataset.mode = "normal";
    };

    const updateProgress = () => {
      const d = videoEl.duration;
      const t = videoEl.currentTime;
      if (!Number.isFinite(d) || d <= 0) return;
      setBarProgress(bar, (t / d) * 100);
    };

    const onWaiting = () => show("buffering");
    const onStalled = () => show("buffering");
    const onSeeking = () => show("buffering");
    const onPlaying = () => show("normal");
    const onCanPlay = () => show("normal");
    const onTimeUpdate = () => updateProgress();
    const onProgress = () => updateProgress();
    const onEnded = () => hide();
    const onPause = () => show("normal");

    videoEl.addEventListener("waiting", onWaiting);
    videoEl.addEventListener("stalled", onStalled);
    videoEl.addEventListener("seeking", onSeeking);
    videoEl.addEventListener("playing", onPlaying);
    videoEl.addEventListener("canplay", onCanPlay);
    videoEl.addEventListener("canplaythrough", onCanPlay);
    videoEl.addEventListener("timeupdate", onTimeUpdate);
    videoEl.addEventListener("progress", onProgress);
    videoEl.addEventListener("pause", onPause);
    videoEl.addEventListener("ended", onEnded);

    updateProgress();
    show("normal");
  }

  function scan() {
    document.querySelectorAll("video").forEach(attachToVideo);
  }

  function clamp01(v) {
    return Math.max(0, Math.min(1, v));
  }

  function rgbToHsl(r, g, b) {
    r /= 255;
    g /= 255;
    b /= 255;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    const d = max - min;
    let h = 0;
    let s = 0;
    const l = (max + min) / 2;
    if (d !== 0) {
      s = d / (1 - Math.abs(2 * l - 1));
      switch (max) {
        case r:
          h = ((g - b) / d) % 6;
          break;
        case g:
          h = (b - r) / d + 2;
          break;
        default:
          h = (r - g) / d + 4;
          break;
      }
      h *= 60;
      if (h < 0) h += 360;
    }
    return [h, s, l];
  }

  function hslToRgb(h, s, l) {
    h = ((h % 360) + 360) % 360;
    s = clamp01(s);
    l = clamp01(l);
    const c = (1 - Math.abs(2 * l - 1)) * s;
    const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
    const m = l - c / 2;
    let r1 = 0, g1 = 0, b1 = 0;
    if (h < 60) [r1, g1, b1] = [c, x, 0];
    else if (h < 120) [r1, g1, b1] = [x, c, 0];
    else if (h < 180) [r1, g1, b1] = [0, c, x];
    else if (h < 240) [r1, g1, b1] = [0, x, c];
    else if (h < 300) [r1, g1, b1] = [x, 0, c];
    else [r1, g1, b1] = [c, 0, x];
    return [
      Math.round((r1 + m) * 255),
      Math.round((g1 + m) * 255),
      Math.round((b1 + m) * 255),
    ];
  }

  function toHex2(n) {
    return n.toString(16).padStart(2, "0");
  }

  function rgbToHex(r, g, b) {
    return `#${toHex2(r)}${toHex2(g)}${toHex2(b)}`;
  }

  function extractUrl(cssBackgroundImage) {
    if (!cssBackgroundImage || cssBackgroundImage === "none") return null;
    const m = cssBackgroundImage.match(/url\((['"]?)(.*?)\1\)/i);
    if (!m) return null;
    return m[2];
  }

  function pickArtworkUrl() {
    const candidates = [
      ".backdropImage",
      ".detailPagePrimaryContainer .backdropImage",
      ".backgroundContainer .backdropImage",
      ".itemBackdrop",
      "[data-backdrop]",
    ];

    for (const sel of candidates) {
      const el = document.querySelector(sel);
      if (!el) continue;
      const bg = getComputedStyle(el).backgroundImage;
      const url = extractUrl(bg);
      if (url) return url;
    }

    // Fallback: look for any element with a Jellyfin Items image.
    const els = Array.from(document.querySelectorAll("*")).slice(0, 500);
    for (const el of els) {
      const bg = getComputedStyle(el).backgroundImage;
      if (!bg || bg === "none") continue;
      const url = extractUrl(bg);
      if (url && url.includes("/Items/")) return url;
    }

    return null;
  }

  function applyAccents(accent, accent2, accent3) {
    if (!root) return;
    root.style.setProperty("--jfx-accent", accent);
    root.style.setProperty("--jfx-accent2", accent2);
    root.style.setProperty("--jfx-accent3", accent3);
  }

  function resetAccents() {
    applyAccents(baseAccent, baseAccent2, baseAccent3);
  }

  async function computeAccentsFromImage(url) {
    const img = new Image();
    img.decoding = "async";
    img.crossOrigin = "anonymous";
    img.src = url;

    await new Promise((resolve, reject) => {
      img.onload = resolve;
      img.onerror = reject;
    });

    const w = 48;
    const h = 48;
    const canvas = document.createElement("canvas");
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext("2d", { willReadFrequently: true });
    if (!ctx) throw new Error("no canvas context");

    ctx.drawImage(img, 0, 0, w, h);
    const data = ctx.getImageData(0, 0, w, h).data;

    let bestScore = -1;
    let best = [125, 220, 255];

    // Sample pixels; prefer saturated mid-luminance colors.
    for (let i = 0; i < data.length; i += 16) {
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      const a = data[i + 3];
      if (a < 200) continue;
      const [hue, sat, lum] = rgbToHsl(r, g, b);
      if (lum < 0.12 || lum > 0.92) continue;
      const score = sat * (1 - Math.abs(lum - 0.55)) * 1.2;
      if (score > bestScore) {
        bestScore = score;
        best = [r, g, b];
      }
    }

    const [hHue, hSat, hLum] = rgbToHsl(best[0], best[1], best[2]);
    const primary = rgbToHex(best[0], best[1], best[2]);

    const accent2Rgb = hslToRgb(hHue + 28, Math.min(1, hSat * 0.85 + 0.05), clamp01(hLum + 0.06));
    const accent3Rgb = hslToRgb(hHue - 34, Math.min(1, hSat * 0.95 + 0.08), clamp01(hLum + 0.02));
    const secondary = rgbToHex(accent2Rgb[0], accent2Rgb[1], accent2Rgb[2]);
    const tertiary = rgbToHex(accent3Rgb[0], accent3Rgb[1], accent3Rgb[2]);

    return { primary, secondary, tertiary };
  }

  async function updateDynamicTheme() {
    if (!dynamicEnabled) return;
    const url = pickArtworkUrl();
    if (!url) {
      resetAccents();
      lastArtworkUrl = null;
      return;
    }

    if (url === lastArtworkUrl) return;
    lastArtworkUrl = url;

    if (pending) pending.abort();
    const ac = new AbortController();
    pending = ac;

    try {
      const { primary, secondary, tertiary } = await computeAccentsFromImage(url);
      if (ac.signal.aborted) return;
      applyAccents(primary, secondary, tertiary);
    } catch {
      if (ac.signal.aborted) return;
      resetAccents();
    }
  }

  function start() {
    if (booted) return;
    if (!document.documentElement || !document.body) return;
    booted = true;

    scan();
    const mo = new MutationObserver(() => scan());
    mo.observe(document.documentElement, { subtree: true, childList: true });

    dynamicEnabled = getDynamicThemingEnabled();
    if (!dynamicEnabled) return;

    root = document.documentElement;
    if (!root.hasAttribute(DYN_ATTR)) root.setAttribute(DYN_ATTR, "on");

    baseAccent = getComputedStyle(root).getPropertyValue("--jfx-accent").trim() || "#7ddcff";
    baseAccent2 = getComputedStyle(root).getPropertyValue("--jfx-accent2").trim() || "#c4b5ff";
    baseAccent3 = getComputedStyle(root).getPropertyValue("--jfx-accent3").trim() || "#ff6bd6";

    updateDynamicTheme();
    const mo2 = new MutationObserver(() => {
      if (dynamicDebounceHandle) clearTimeout(dynamicDebounceHandle);
      dynamicDebounceHandle = setTimeout(updateDynamicTheme, 250);
    });
    mo2.observe(document.documentElement, { subtree: true, childList: true, attributes: true, attributeFilter: ["style", "class"] });
    window.addEventListener("hashchange", updateDynamicTheme, { passive: true });
    window.addEventListener("popstate", updateDynamicTheme, { passive: true });
  }

  function scheduleStart() {
    const run = () => {
      window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => {
          start();
        });
      });
    };

    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", run, { once: true });
      return;
    }

    run();
  }

  scheduleStart();
})();
