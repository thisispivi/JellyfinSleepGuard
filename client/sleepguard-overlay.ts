// ---------------------------------------------------------------------------
// SleepGuard overlay — TypeScript source
// Compiled to dist/sleepguard-overlay.js and embedded in the plugin DLL.
// Loaded by Jellyfin-JavaScript-Injector; the server prepends
// `window.__SLEEPGUARD_CONFIG__ = {...};` before this IIFE.
// ---------------------------------------------------------------------------

// --- Global type declarations (script-mode: no import/export) ---

interface SleepGuardServerConfig {
    language?: string;
    promptMessage?: string;
    promptHeader?: string;
    accentColor?: string;
    backgroundOpacity?: number;
    useBackdropImage?: boolean;
    blurBackdrop?: boolean;
    showContinueButton?: boolean;
    showDismissButton?: boolean;
    continueTextEn?: string | null;
    continueTextIt?: string | null;
    dismissTextEn?: string | null;
    dismissTextIt?: string | null;
    developerMode?: boolean;
}

interface SleepGuardSettings {
    language: string;
    /** Server override for the prompt body text; null uses the built-in translation. */
    promptText: string | null;
    /** Server override for the prompt header text; null uses the built-in translation. */
    headerText: string | null;
    pauseWhenShown: boolean;
    continueButtonText: null;
    dismissButtonText: null;
    accentColor: string;
    backgroundOpacity: number;
    useBackdropImage: boolean;
    blurBackdrop: boolean;
    showContinueButton: boolean;
    showDismissButton: boolean;
    continueTextEn: string | null;
    continueTextIt: string | null;
    dismissTextEn: string | null;
    dismissTextIt: string | null;
    developerMode: boolean;
}

/**
 * Public SleepGuard overlay API exposed on `window.SleepGuardOverlay`.
 * @property show  - Shows the overlay if a video is active.
 * @property hide  - Removes the overlay without resuming playback.
 * @property settings - Live settings object; mutate before calling show().
 */
interface SleepGuardOverlayApi {
    show: () => void;
    hide: () => void;
    settings: SleepGuardSettings;
}

// Augment the global Window interface (script-mode global declaration)
interface Window {
    __SLEEPGUARD_CONFIG__?: SleepGuardServerConfig;
    SleepGuardOverlay?: SleepGuardOverlayApi;
}

// ---------------------------------------------------------------------------
// IIFE — all runtime logic is scoped here to avoid polluting globals
// ---------------------------------------------------------------------------

(() => {
    // --- Server-injected configuration (prepended by /SleepGuard/overlay.js) ---
    const serverConfig: SleepGuardServerConfig = window.__SLEEPGUARD_CONFIG__ ?? {};

    const settings: SleepGuardSettings = {
        language:           serverConfig.language          ?? "auto",
        promptText:         serverConfig.promptMessage     ?? null,
        headerText:         serverConfig.promptHeader      ?? null,
        pauseWhenShown:     true,
        continueButtonText: null,
        dismissButtonText:  null,
        accentColor:        serverConfig.accentColor       ?? "#00a4dc",
        backgroundOpacity:  serverConfig.backgroundOpacity ?? 92,
        useBackdropImage:   serverConfig.useBackdropImage  ?? false,
        blurBackdrop:       serverConfig.blurBackdrop      ?? true,
        showContinueButton: serverConfig.showContinueButton ?? true,
        showDismissButton:  serverConfig.showDismissButton  ?? true,
        continueTextEn:     serverConfig.continueTextEn    ?? null,
        continueTextIt:     serverConfig.continueTextIt    ?? null,
        dismissTextEn:      serverConfig.dismissTextEn     ?? null,
        dismissTextIt:      serverConfig.dismissTextIt     ?? null,
        developerMode:      serverConfig.developerMode     ?? false,
    };

    // --- Translations ---

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

    // Forward-declare observer so showOverlay / removeOverlay can reference it
    // via closure before the MutationObserver is constructed below.
    let observer!: MutationObserver;

    // --- Language helpers ---

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

    type TextKey = "headerText" | "promptText";

    /**
     * Returns the prompt text for the given key.
     * Falls back from server override → built-in translation.
     */
    const getText = (key: TextKey): string =>
        settings[key] ?? translations[getLanguage()][key];

    type ButtonType = "continue" | "dismiss";

    /**
     * Returns per-language button text with fallback chain:
     * server override (language-specific) → built-in translation → English translation.
     */
    const getButtonText = (type: ButtonType): string => {
        const lang = getLanguage();
        const override =
            type === "continue"
                ? (lang === "it" ? settings.continueTextIt : settings.continueTextEn)
                : (lang === "it" ? settings.dismissTextIt  : settings.dismissTextEn);
        if (override) return override;

        const t = translations[lang] ?? translations.en;
        return type === "continue" ? t.continueButtonText : t.dismissButtonText;
    };

    // --- Playback helpers ---

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
            // FIX: surface unexpected errors instead of swallowing them silently
            video.play().catch((err: unknown) => {
                if (
                    err instanceof DOMException &&
                    (err.name === "AbortError" || err.name === "NotAllowedError")
                ) {
                    return; // expected browser autoplay-policy interruption
                }
                console.warn("[SleepGuard] video.play() failed:", err);
            });
            return;
        }

        // FIX: removed Polymer 1.x selector `button[is='paper-icon-button-light']`
        const playButton =
            document.querySelector<HTMLButtonElement>('button[data-action="play"]') ??
            document.querySelector<HTMLButtonElement>('button[aria-label="Play"]') ??
            document.querySelector<HTMLButtonElement>('button[title="Play"]');
        if (playButton) playButton.click();
    };

    /**
     * Reads the current Jellyfin backdrop image from the DOM.
     * Verified against Jellyfin Web 10.11.x DOM structure.
     */
    const getBackdropImageUrl = (): string | null => {
        const el = document.querySelector("#backdropContainer, .backdropContainer");
        if (!el) return null;
        const bg = getComputedStyle(el).backgroundImage;
        const match = bg.match(/url\(["']?([^"')]+)["']?\)/);
        return match?.[1] ?? null;
    };

    // --- Overlay lifecycle ---

    /** Removes the overlay. Resumes the MutationObserver to watch for the next prompt. */
    const removeOverlay = (): void => {
        document.getElementById(overlayId)?.remove();
        // FIX: resume watching for server-sent prompt toasts after overlay is dismissed
        observer.observe(document.body, { childList: true, subtree: true });
    };

    /** Shows the overlay if a video is active and the overlay is not already displayed. */
    const showOverlay = (): void => {
        // FIX: disconnect the observer while the overlay is visible to prevent
        // the overlay from re-triggering itself via its own DOM mutations.
        observer.disconnect();

        if (document.getElementById(overlayId)) return;
        if (!isPlaybackPage()) return;
        if (settings.pauseWhenShown) pausePlayback();

        const overlay = document.createElement("div");
        overlay.id = overlayId;

        // --- Build overlay DOM (FIX: was innerHTML template literal — XSS risk) ---
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

        if (settings.showContinueButton) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "sleepguard-continue";
            btn.textContent = getButtonText("continue");
            actionsEl.appendChild(btn);
        }

        if (settings.showDismissButton) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "sleepguard-dismiss";
            btn.textContent = getButtonText("dismiss");
            actionsEl.appendChild(btn);
        }

        panel.appendChild(titleEl);
        panel.appendChild(messageEl);
        panel.appendChild(actionsEl);
        overlay.appendChild(panel);

        // --- Styles (safe: contains only computed CSS values, no user text) ---
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

        // --- Keyboard handler (hoisted via function declarations) ---
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

        // --- Button event listeners ---
        const continueEl = overlay.querySelector<HTMLButtonElement>(".sleepguard-continue");
        if (continueEl) {
            continueEl.addEventListener("click", () => {
                cleanup();
                removeOverlay();
                resumePlayback();
            });
        }

        const dismissEl = overlay.querySelector<HTMLButtonElement>(".sleepguard-dismiss");
        if (dismissEl) {
            dismissEl.addEventListener("click", () => {
                cleanup();
                removeOverlay();
            });
        }

        document.body.appendChild(overlay);
    };

    // --- Prompt detection ---

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

    // Observe DOM mutations to detect when the server sends a prompt toast.
    // The observer is disconnected in showOverlay() and reconnected in removeOverlay()
    // so it is never active while the overlay itself is in the DOM.
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

    // --- Public API ---
    window.SleepGuardOverlay = {
        show:     showOverlay,
        hide:     removeOverlay,
        settings,
    };

    // --- Developer keyboard shortcut (only registered when developerMode = true) ---
    if (settings.developerMode) {
        document.addEventListener("keydown", (e: KeyboardEvent) => {
            if (e.ctrlKey && e.shiftKey && e.altKey && e.key === "S") {
                e.preventDefault();
                showOverlay();
            }
        });
    }
})();
