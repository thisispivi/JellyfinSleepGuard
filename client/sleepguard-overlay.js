(() => {
  const settings = {
    promptText: "Are you still watching?",
    headerText: "SleepGuard",
    pauseWhenShown: true,
    continueButtonText: "Continue watching",
    dismissButtonText: "Stay paused"
  };

  const overlayId = "sleepguard-fullscreen-overlay";

  const findVideo = () => document.querySelector("video");

  const pausePlayback = () => {
    const video = findVideo();
    if (video && !video.paused) {
      video.pause();
    }
  };

  const resumePlayback = () => {
    const video = findVideo();
    if (video) {
      video.play().catch(() => {});
      return;
    }

    const playButton = document.querySelector(
      "button[title='Play'],button[aria-label='Play'],button[is='paper-icon-button-light'][title='Play']"
    );
    if (playButton) {
      playButton.click();
    }
  };

  const removeOverlay = () => {
    document.getElementById(overlayId)?.remove();
  };

  const showOverlay = () => {
    if (document.getElementById(overlayId)) {
      return;
    }

    if (settings.pauseWhenShown) {
      pausePlayback();
    }

    const overlay = document.createElement("div");
    overlay.id = overlayId;
    overlay.innerHTML = `
      <div class="sleepguard-panel">
        <div class="sleepguard-title">${settings.headerText}</div>
        <div class="sleepguard-message">${settings.promptText}</div>
        <div class="sleepguard-actions">
          <button type="button" class="sleepguard-continue">${settings.continueButtonText}</button>
          <button type="button" class="sleepguard-dismiss">${settings.dismissButtonText}</button>
        </div>
      </div>
    `;

    const style = document.createElement("style");
    style.textContent = `
      #${overlayId} {
        position: fixed;
        inset: 0;
        z-index: 2147483647;
        display: grid;
        place-items: center;
        background: rgba(0, 0, 0, 0.92);
        color: #fff;
        font-family: inherit;
      }

      #${overlayId} .sleepguard-panel {
        width: min(680px, calc(100vw - 32px));
        text-align: center;
        padding: 32px 24px;
      }

      #${overlayId} .sleepguard-title {
        font-size: 18px;
        opacity: 0.72;
        margin-bottom: 16px;
      }

      #${overlayId} .sleepguard-message {
        font-size: clamp(32px, 6vw, 64px);
        line-height: 1.05;
        font-weight: 700;
        margin-bottom: 32px;
      }

      #${overlayId} .sleepguard-actions {
        display: flex;
        justify-content: center;
        gap: 12px;
        flex-wrap: wrap;
      }

      #${overlayId} button {
        border: 0;
        border-radius: 6px;
        padding: 14px 20px;
        font-size: 16px;
        font-weight: 600;
        cursor: pointer;
      }

      #${overlayId} .sleepguard-continue {
        background: #00a4dc;
        color: #fff;
      }

      #${overlayId} .sleepguard-dismiss {
        background: rgba(255, 255, 255, 0.14);
        color: #fff;
      }
    `;

    overlay.appendChild(style);
    overlay.querySelector(".sleepguard-continue").addEventListener("click", () => {
      removeOverlay();
      resumePlayback();
    });
    overlay.querySelector(".sleepguard-dismiss").addEventListener("click", removeOverlay);
    document.body.appendChild(overlay);
  };

  const containsPrompt = node => {
    const text = node?.textContent || "";
    return text.includes(settings.promptText) || text.includes(settings.headerText);
  };

  const observer = new MutationObserver(mutations => {
    for (const mutation of mutations) {
      for (const node of mutation.addedNodes) {
        if (containsPrompt(node)) {
          showOverlay();
          return;
        }
      }
    }
  });

  observer.observe(document.body, {
    childList: true,
    subtree: true
  });

  window.SleepGuardOverlay = {
    show: showOverlay,
    hide: removeOverlay,
    settings
  };
})();
