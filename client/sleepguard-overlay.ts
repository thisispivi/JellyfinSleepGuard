// ---------------------------------------------------------------------------
// SleepGuard overlay — TypeScript source
// Compiled to dist/sleepguard-overlay.js and embedded in the plugin DLL.
// Loaded by the JavaScript Injector script entry; the server prepends
// `window.__SLEEPGUARD_CONFIG__ = {...};` before this IIFE.
// ---------------------------------------------------------------------------

interface SleepGuardServerConfig {
    language?: string;
    promptMessage?: string;
    promptHeader?: string;
}

interface SleepGuardSettings {
    language: string;
    promptText: string | null;
    headerText: string | null;
    pauseWhenShown: boolean;
}

interface SleepGuardOverlayApi {
    show: () => void;
    hide: () => void;
    settings: SleepGuardSettings;
}

interface Window {
    __SLEEPGUARD_CONFIG__?: SleepGuardServerConfig;
    SleepGuardOverlay?: SleepGuardOverlayApi;
}

(() => {
    const serverConfig: SleepGuardServerConfig = window.__SLEEPGUARD_CONFIG__ ?? {};

    const settings: SleepGuardSettings = {
        language:       serverConfig.language      ?? "auto",
        promptText:     serverConfig.promptMessage ?? null,
        headerText:     serverConfig.promptHeader  ?? null,
        pauseWhenShown: true,
    };

    type SupportedLanguage = "en" | "it";

    interface TranslationSet {
        promptText: string;
        headerText: string;
        continueButtonText: string;
        dismissButtonText: string;
    }

    const translations: Record<SupportedLanguage, TranslationSet> = {
        en: {
            promptText:         "Are you still watching?",
            headerText:         "SleepGuard",
            continueButtonText: "Continue watching",
            dismissButtonText:  "Stay paused",
        },
        it: {
            promptText:         "Stai ancora guardando?",
            headerText:         "SleepGuard",
            continueButtonText: "Continua la riproduzione",
            dismissButtonText:  "Resta in pausa",
        },
    };

    const overlayId = "sleepguard-fullscreen-overlay";
    let observer!: MutationObserver;

    const normalizeLanguage = (value: string): SupportedLanguage => {
        const language = String(value || "").toLowerCase().split("-")[0];
        return (language in translations) ? (language as SupportedLanguage) : "en";
    };

    const getLanguage = (): SupportedLanguage => {
        if (settings.language && settings.language !== "auto") {
            return normalizeLanguage(settings.language);
        }

        const languages = navigator.languages || [navigator.language];
        return normalizeLanguage(languages[0] ?? "");
    };

    const getText = (key: "headerText" | "promptText"): string =>
        settings[key] ?? translations[getLanguage()][key];

    const getButtonText = (type: "continue" | "dismiss"): string => {
        const translation = translations[getLanguage()] ?? translations.en;
        return type === "continue" ? translation.continueButtonText : translation.dismissButtonText;
    };

    const findVideo = (): HTMLVideoElement | null =>
        document.querySelector("video");

    const isPlaybackPage = (): boolean => {
        const video = findVideo();
        return Boolean(video && video.readyState > 0 && !video.ended);
    };

    const pausePlayback = (): void => {
        const video = findVideo();
        if (video && !video.paused) video.pause();
    };

    const resumePlayback = (): void => {
        const video = findVideo();
        if (video) {
            video.play().catch((err: unknown) => {
                if (
                    err instanceof DOMException &&
                    (err.name === "AbortError" || err.name === "NotAllowedError")
                ) {
                    return;
                }

                console.warn("[SleepGuard] video.play() failed:", err);
            });
            return;
        }

        const playButton =
            document.querySelector<HTMLButtonElement>('button[data-action="play"]') ??
            document.querySelector<HTMLButtonElement>('button[aria-label="Play"]') ??
            document.querySelector<HTMLButtonElement>('button[title="Play"]');
        if (playButton) playButton.click();
    };

    const removeOverlay = (): void => {
        document.getElementById(overlayId)?.remove();
        observer.observe(document.body, { childList: true, subtree: true });
    };

    const showOverlay = (): void => {
        observer.disconnect();

        if (document.getElementById(overlayId)) return;
        if (!isPlaybackPage()) return;
        if (settings.pauseWhenShown) pausePlayback();

        const overlay = document.createElement("div");
        overlay.id = overlayId;

        const panel = document.createElement("div");
        panel.className = "sleepguard-panel";

        const titleEl = document.createElement("div");
        titleEl.className = "sleepguard-title";
        titleEl.textContent = getText("headerText");

        const messageEl = document.createElement("div");
        messageEl.className = "sleepguard-message";
        messageEl.textContent = getText("promptText");

        const actionsEl = document.createElement("div");
        actionsEl.className = "sleepguard-actions";

        const continueButton = document.createElement("button");
        continueButton.type = "button";
        continueButton.className = "sleepguard-continue";
        continueButton.textContent = getButtonText("continue");
        actionsEl.appendChild(continueButton);

        const dismissButton = document.createElement("button");
        dismissButton.type = "button";
        dismissButton.className = "sleepguard-dismiss";
        dismissButton.textContent = getButtonText("dismiss");
        actionsEl.appendChild(dismissButton);

        panel.appendChild(titleEl);
        panel.appendChild(messageEl);
        panel.appendChild(actionsEl);
        overlay.appendChild(panel);

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
        background: rgba(0, 0, 0, 0.92);
      }

      #${overlayId} .sleepguard-panel {
        position: relative;
        z-index: 1;
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

        function cleanup(): void {
            document.removeEventListener("keydown", handleKey);
        }

        function handleKey(e: KeyboardEvent): void {
            if (e.key === "Escape") {
                cleanup();
                removeOverlay();
            }
            if (e.key === "Enter") {
                cleanup();
                removeOverlay();
                resumePlayback();
            }
        }

        document.addEventListener("keydown", handleKey);

        continueButton.addEventListener("click", () => {
            cleanup();
            removeOverlay();
            resumePlayback();
        });

        dismissButton.addEventListener("click", () => {
            cleanup();
            removeOverlay();
        });

        document.body.appendChild(overlay);
    };

    const containsPrompt = (node: Node | null): boolean => {
        const text = node?.textContent ?? "";
        const knownPrompts = Object.values(translations).flatMap((t) => [
            t.promptText,
            t.headerText,
        ]);
        if (settings.promptText) knownPrompts.push(settings.promptText);
        if (settings.headerText) knownPrompts.push(settings.headerText);
        return knownPrompts.some((prompt) => text.includes(prompt));
    };

    observer = new MutationObserver((mutations: MutationRecord[]) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (containsPrompt(node) && isPlaybackPage()) {
                    showOverlay();
                    return;
                }
            }
        }
    });

    observer.observe(document.body, { childList: true, subtree: true });

    window.SleepGuardOverlay = {
        show: showOverlay,
        hide: removeOverlay,
        settings,
    };
})();
