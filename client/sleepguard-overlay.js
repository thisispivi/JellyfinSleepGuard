(() => {
    const settings = {
        language: "auto",
        promptText: null,
        headerText: null,
        pauseWhenShown: true,
        continueButtonText: null,
        dismissButtonText: null,
    };

    const translations = {
        en: {
            promptText: "Are you still watching?",
            headerText: "SleepGuard",
            continueButtonText: "Continue watching",
            dismissButtonText: "Stay paused",
        },
        it: {
            promptText: "Stai ancora guardando?",
            headerText: "SleepGuard",
            continueButtonText: "Continua la riproduzione",
            dismissButtonText: "Resta in pausa",
        },
    };

    const overlayId = "sleepguard-fullscreen-overlay";

    const normalizeLanguage = (value) => {
        const language = String(value || "").toLowerCase().split("-")[0];
        return Object.prototype.hasOwnProperty.call(translations, language)
            ? language
            : "en";
    };

    const getLanguage = () => {
        if (settings.language && settings.language !== "auto") {
            return normalizeLanguage(settings.language);
        }

        const languages = navigator.languages || [navigator.language];
        return normalizeLanguage(languages[0]);
    };

    const getText = (key) => settings[key] || translations[getLanguage()][key];

    const findVideo = () => document.querySelector("video");

    const isPlaybackPage = () => {
        const video = findVideo();
        return Boolean(video && video.readyState > 0 && !video.ended);
    };

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
            "button[title='Play'],button[aria-label='Play'],button[is='paper-icon-button-light'][title='Play']",
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

        if (!isPlaybackPage()) {
            return;
        }

        if (settings.pauseWhenShown) {
            pausePlayback();
        }

        const overlay = document.createElement("div");
        overlay.id = overlayId;
        overlay.innerHTML = `
      <div class="sleepguard-panel">
        <div class="sleepguard-title">${getText("headerText")}</div>
        <div class="sleepguard-message">${getText("promptText")}</div>
        <div class="sleepguard-actions">
          <button type="button" class="sleepguard-continue">${getText("continueButtonText")}</button>
          <button type="button" class="sleepguard-dismiss">${getText("dismissButtonText")}</button>
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
        overlay
            .querySelector(".sleepguard-continue")
            .addEventListener("click", () => {
                removeOverlay();
                resumePlayback();
            });
        overlay
            .querySelector(".sleepguard-dismiss")
            .addEventListener("click", removeOverlay);
        document.body.appendChild(overlay);
    };

    const containsPrompt = (node) => {
        const text = node?.textContent || "";
        const knownPrompts = Object.values(translations).flatMap((translation) => [
            translation.promptText,
            translation.headerText,
        ]);
        if (settings.promptText) {
            knownPrompts.push(settings.promptText);
        }
        if (settings.headerText) {
            knownPrompts.push(settings.headerText);
        }

        return (
            knownPrompts.some((prompt) => text.includes(prompt))
        );
    };

    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (containsPrompt(node) && isPlaybackPage()) {
                    showOverlay();
                    return;
                }
            }
        }
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true,
    });

    window.SleepGuardOverlay = {
        show: showOverlay,
        hide: removeOverlay,
        settings,
    };
})();
