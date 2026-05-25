// This file is embedded and served by /Yuorself/theme.js
// Source is derived from dist/jellyfinexpressive.user.js in this repo.
(() => {
  const BAR_CLASS = "jfx-squigglebar";

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

  scan();
  const mo = new MutationObserver(() => scan());
  mo.observe(document.documentElement, { subtree: true, childList: true });
})();

