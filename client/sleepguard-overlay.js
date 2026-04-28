(() => {
    // --- Server-injected configuration (prepended by /SleepGuard/overlay.js) ---
    const serverConfig = window.__SLEEPGUARD_CONFIG__ || {};

    const settings = {
        language: serverConfig.language ?? "auto",
        promptText: serverConfig.promptMessage ?? null,
        headerText: serverConfig.promptHeader ?? null,
        pauseWhenShown: true,
        continueButtonText: null,
        dismissButtonText: null,
        accentColor: serverConfig.accentColor ?? "#00a4dc",
        backgroundOpacity: serverConfig.backgroundOpacity ?? 92,
        useBackdropImage: serverConfig.useBackdropImage ?? false,
        blurBackdrop: serverConfig.blurBackdrop ?? true,
        showContinueButton: serverConfig.showContinueButton ?? true,
        showDismissButton: serverConfig.showDismissButton ?? true,
        continueTextEn: serverConfig.continueTextEn ?? null,
        continueTextIt: serverConfig.continueTextIt ?? null,
        dismissTextEn: serverConfig.dismissTextEn ?? null,
        dismissTextIt: serverConfig.dismissTextIt ?? null,
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

    /**
     * Returns per-language button text with fallback chain:
     * server override → built-in translation → English translation.
     */
    const getButtonText = (type) => {
        const lang = getLanguage();
        const langKey = `${type}Text${lang.charAt(0).toUpperCase()}${lang.slice(1)}`;
        return settings[langKey] || translations[lang]?.[`${type}ButtonText`] || translations.en[`${type}ButtonText`];
    };

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

    /**
     * Reads the current Jellyfin backdrop image from the DOM.
     * Verified against Jellyfin Web 10.11.x DOM structure.
     */
    const getBackdropImageUrl = () => {
        const el = document.querySelector("#backdropContainer, .backdropContainer");
        if (!el) return null;
        const bg = getComputedStyle(el).backgroundImage;
        const match = bg.match(/url\(["']?([^"')]+)["']?\)/);
        return match ? match[1] : null;
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

        // --- Build button HTML conditionally ---
        const continueBtn = settings.showContinueButton
            ? `<button type="button" class="sleepguard-continue">${getButtonText("continue")}</button>`
            : "";
        const dismissBtn = settings.showDismissButton
            ? `<button type="button" class="sleepguard-dismiss">${getButtonText("dismiss")}</button>`
            : "";

        overlay.innerHTML = `
      <div class="sleepguard-panel">
        <div class="sleepguard-title">${getText("headerText")}</div>
        <div class="sleepguard-message">${getText("promptText")}</div>
        <div class="sleepguard-actions">
          ${continueBtn}
          ${dismissBtn}
        </div>
      </div>
    `;

        // --- Backdrop image support ---
        const backdropUrl = settings.useBackdropImage ? getBackdropImageUrl() : null;
        const hasBackdrop = Boolean(backdropUrl);
        const bgOpacity = (settings.backgroundOpacity / 100).toFixed(2);

        const style = document.createElement("style");
        style.textContent = `
      @keyframes sleepguard-fadein {
        from { opacity: 0; }
        to { opacity: 1; }
      }

      #${overlayId} {
        position: fixed;
        inset: 0;
        z-index: 2147483647;
        display: grid;
        place-items: center;
        color: #fff;
        font-family: inherit;
        animation: sleepguard-fadein 200ms ease-out;
        ${hasBackdrop
            ? `background: url("${backdropUrl}") center/cover no-repeat;`
            : `background: rgba(0, 0, 0, ${bgOpacity});`}
        ${hasBackdrop && settings.blurBackdrop ? `backdrop-filter: blur(8px);` : ""}
      }

      ${hasBackdrop ? `
      #${overlayId}::before {
        content: "";
        position: absolute;
        inset: 0;
        background: rgba(0, 0, 0, ${bgOpacity});
        z-index: 0;
      }
      ` : ""}

      #${overlayId} .sleepguard-panel {
        position: relative;
        z-index: 1;
        width: min(680px, calc(100vw - 32px));
        text-align: center;
        padding: 32px 24px;
        ${hasBackdrop ? `
        background: rgba(0, 0, 0, 0.72);
        border-radius: 12px;
        ` : ""}
      }

      #${overlayId} .sleepguard-title {
        font-size: 18px;
        opacity: 0.72;
        margin-bottom: 16px;
        ${hasBackdrop ? `text-shadow: 0 1px 4px rgba(0,0,0,.8);` : ""}
      }

      #${overlayId} .sleepguard-message {
        font-size: clamp(32px, 6vw, 64px);
        line-height: 1.05;
        font-weight: 700;
        margin-bottom: 32px;
        ${hasBackdrop ? `text-shadow: 0 1px 4px rgba(0,0,0,.8);` : ""}
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
        background: ${settings.accentColor};
        color: #fff;
      }

      #${overlayId} .sleepguard-dismiss {
        background: rgba(255, 255, 255, 0.14);
        color: #fff;
      }
    `;

        overlay.appendChild(style);

        // --- Keyboard handler ---
        const cleanup = () => {
            document.removeEventListener("keydown", handleKey);
        };

        const handleKey = (e) => {
            if (e.key === "Escape") {
                cleanup();
                removeOverlay();
            }
            if (e.key === "Enter") {
                cleanup();
                removeOverlay();
                resumePlayback();
            }
        };

        document.addEventListener("keydown", handleKey);

        // --- Button event listeners ---
        const continueEl = overlay.querySelector(".sleepguard-continue");
        if (continueEl) {
            continueEl.addEventListener("click", () => {
                cleanup();
                removeOverlay();
                resumePlayback();
            });
        }

        const dismissEl = overlay.querySelector(".sleepguard-dismiss");
        if (dismissEl) {
            dismissEl.addEventListener("click", () => {
                cleanup();
                removeOverlay();
            });
        }

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

    /**
     * Public SleepGuard overlay API.
     * @property {function(): void} show - Shows the overlay if a video is active.
     * @property {function(): void} hide - Removes the overlay without resuming.
     * @property {object} settings       - Live settings; mutate before calling show().
     */
    window.SleepGuardOverlay = {
        show: showOverlay,
        hide: removeOverlay,
        settings,
    };
})();
