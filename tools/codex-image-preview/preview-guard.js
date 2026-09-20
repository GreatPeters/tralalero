// Local, removable safeguard for Codex 26.908.40401 image clicks in VS Code.
// Uses the already displayed image; never changes thread state or reads files.
(() => {
  "use strict";
  const marker = "codex-local-image-preview-guard";
  if (document.documentElement.hasAttribute(marker)) return;
  document.documentElement.setAttribute(marker, "1");
  const triggerSelector = '[data-testid="generated-image-preview"],'
    + '[data-markdown-image-preview-trigger="true"]';
  let preview = null;

  function openPreview(source, trigger) {
    const src = source.currentSrc || source.getAttribute("src");
    if (!src) return false;
    const priorFocus = document.activeElement;
    const dialog = document.createElement("dialog");
    dialog.id = marker;
    dialog.setAttribute("aria-label", "이미지 확대");
    dialog.style.cssText = "position:fixed;inset:0;margin:auto;width:94vw;height:90vh;"
      + "max-width:94vw;max-height:90vh;padding:16px;box-sizing:border-box;"
      + "border:1px solid #65758a;border-radius:8px;background:#18212d;color:#fff;";
    const layout = document.createElement("div");
    layout.style.cssText = "display:flex;flex-direction:column;height:100%;gap:12px";
    const toolbar = document.createElement("div");
    toolbar.style.cssText = "display:flex;align-items:center;gap:12px;flex-shrink:0";
    const title = document.createElement("span");
    title.textContent = source.alt || "이미지 확대";
    title.style.cssText = "flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap";
    const close = document.createElement("button");
    close.type = "button";
    close.textContent = "닫기 (Esc)";
    close.style.cssText = "padding:8px 14px;background:#30435b;color:white;"
      + "border:1px solid #8292a5;border-radius:4px;cursor:pointer";
    const viewport = document.createElement("div");
    viewport.style.cssText = "flex:1;min-height:0;overflow:auto;display:grid;place-items:center";
    const image = document.createElement("img");
    image.alt = source.alt || "이미지";
    image.referrerPolicy = source.referrerPolicy || "no-referrer";
    image.style.cssText = "display:block;width:100%;height:100%;min-width:0;min-height:0;"
      + "max-width:100%;max-height:100%;object-fit:contain;cursor:zoom-in";
    const status = document.createElement("div");
    status.setAttribute("role", "status");
    status.textContent = "이미지를 누르면 원본 크기로 전환합니다. 닫으면 대화로 돌아갑니다.";
    status.style.cssText = "font-size:12px;color:#c4cedb;flex-shrink:0";
    let zoomed = false;
    image.addEventListener("click", () => {
      zoomed = !zoomed;
      image.style.width = zoomed ? `${image.naturalWidth}px` : "100%";
      image.style.height = zoomed ? `${image.naturalHeight}px` : "100%";
      image.style.maxWidth = zoomed ? "none" : "100%";
      image.style.maxHeight = zoomed ? "none" : "100%";
      image.style.cursor = zoomed ? "zoom-out" : "zoom-in";
      viewport.style.placeItems = zoomed ? "start" : "center";
    });
    image.addEventListener("error", () => {
      status.textContent = "이미지를 불러오지 못했습니다. 닫기 또는 Esc로 대화에 돌아갈 수 있습니다.";
    });
    const dismiss = () => { if (dialog.open) dialog.close(); };
    close.addEventListener("click", dismiss);
    dialog.addEventListener("click", event => { if (event.target === dialog) dismiss(); });
    dialog.addEventListener("close", () => {
      dialog.remove();
      if (preview === dialog) preview = null;
      const target = priorFocus?.isConnected ? priorFocus : trigger;
      if (target?.isConnected) target.focus({ preventScroll: true });
    }, { once: true });
    toolbar.append(title, close);
    viewport.append(image);
    layout.append(toolbar, viewport, status);
    dialog.append(layout);
    document.body.append(dialog);
    try {
      dialog.showModal();
      preview = dialog;
      image.src = src;
      close.focus({ preventScroll: true });
      return true;
    } catch {
      dialog.remove();
      return false;
    }
  }

  window.addEventListener("click", event => {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey
      || event.metaKey || event.shiftKey || event.altKey || preview) return;
    if (!(event.target instanceof Element)) return;
    const trigger = event.target.closest(triggerSelector);
    if (!trigger || trigger.matches(":disabled,[aria-disabled='true']")) return;
    const source = trigger.querySelector("img");
    if (!source || !openPreview(source, trigger)) return;
    // Stop only after the replacement viewer exists. Do not activate the
    // original editor-panel route, which can hide the entire conversation.
    event.preventDefault();
    event.stopImmediatePropagation();
  }, true);
})();
