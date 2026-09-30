// ---------------------------------------------------------------------------
// SleepGuard overlay - TypeScript source
// Compiled to dist/sleepguard-overlay.js and embedded in the plugin DLL.
// Loaded by the JavaScript Injector script entry; the server prepends
// `window.__SLEEPGUARD_CONFIG__ = {...};` before this IIFE.
// ---------------------------------------------------------------------------

type OverlayBackgroundMode = "NowPlayingArtwork" | "Solid" | "CustomUrl";
type OverlayArtworkPreference = "SeriesBackdrop" | "ItemBackdrop" | "PrimaryImage";

interface SleepGuardServerConfig {
    language?: string;
    promptMessage?: string;
    promptHeader?: string;
    overlayBackgroundMode?: string;
    overlayArtworkPreference?: string;
    overlayBackgroundColor?: string;
    overlayTextColor?: string;
    overlayPrimaryButtonColor?: string;
    overlayPrimaryButtonTextColor?: string;
    overlaySecondaryButtonColor?: string;
    overlaySecondaryButtonTextColor?: string;
    overlayBackgroundDimPercent?: number;
    overlayArtworkBlurPixels?: number;
    overlayPanelOpacityPercent?: number;
    overlayCustomBackgroundUrl?: string;
}

interface SleepGuardSettings {
    language: string;
    promptText: string | null;
    headerText: string | null;
    backgroundMode: OverlayBackgroundMode;
    artworkPreference: OverlayArtworkPreference;
    backgroundColor: string;
    textColor: string;
    primaryButtonColor: string;
    primaryButtonTextColor: string;
    secondaryButtonColor: string;
    secondaryButtonTextColor: string;
    backgroundDimPercent: number;
    artworkBlurPixels: number;
    panelOpacityPercent: number;
    customBackgroundUrl: string;
}

interface SleepGuardOverlayApi {
    show: () => void;
    hide: () => void;
    settings: SleepGuardSettings;
}

interface JellyfinImageTags {
    Primary?: string;
    Thumb?: string;
    [key: string]: string | undefined;
}

interface JellyfinItem {
    Id?: string;
    Type?: string;
    SeriesId?: string;
    ImageTags?: JellyfinImageTags;
    BackdropImageTags?: string[];
    ParentBackdropItemId?: string;
    ParentBackdropImageTags?: string[];
}

interface JellyfinSession {
    DeviceId?: string;
    UserId?: string;
    NowPlayingItem?: JellyfinItem;
}

interface JellyfinApiClient {
    getCurrentUserId?: () => string | null;
    deviceId?: () => string | null;
    getSessions?: (options?: Record<string, unknown>) => Promise<JellyfinSession[]>;
    getItem?: (userId: string | null, itemId: string) => Promise<JellyfinItem>;
    getImageUrl?: (itemId: string, options: Record<string, unknown>) => string;
}

interface Window {
    __SLEEPGUARD_CONFIG__?: SleepGuardServerConfig;
    SleepGuardOverlay?: SleepGuardOverlayApi;
    ApiClient?: JellyfinApiClient;
}

(() => {
    const serverConfig: SleepGuardServerConfig = window.__SLEEPGUARD_CONFIG__ ?? {};

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

    const isHexColor = (value: string | undefined): boolean =>
        /^#[0-9a-fA-F]{6}$/.test(String(value || ""));

    const normalizeHexColor = (value: string | undefined, fallback: string): string =>
        isHexColor(value) ? String(value).toUpperCase() : fallback;

    const clampNumber = (value: number | undefined, fallback: number, min: number, max: number): number => {
        const numberValue = Number(value);
        if (!Number.isFinite(numberValue)) return fallback;
        return Math.min(max, Math.max(min, Math.round(numberValue)));
    };

    const normalizeBackgroundMode = (value: string | undefined): OverlayBackgroundMode => {
        if (value === "Solid" || value === "CustomUrl" || value === "NowPlayingArtwork") return value;
        return "NowPlayingArtwork";
    };

    const normalizeArtworkPreference = (value: string | undefined): OverlayArtworkPreference => {
        if (value === "ItemBackdrop" || value === "PrimaryImage" || value === "SeriesBackdrop") return value;
        return "SeriesBackdrop";
    };

    const normalizeCustomUrl = (value: string | undefined): string => {
        const url = String(value || "").trim();
        if (!url) return "";
        if (url.startsWith("/") && !url.startsWith("//") && !url.includes("\\")) return url;
        try {
            const parsed = new URL(url);
            return parsed.protocol === "http:" || parsed.protocol === "https:" ? parsed.href : "";
        } catch {
            return "";
        }
    };

    const settings: SleepGuardSettings = {
        language:                 serverConfig.language                         ?? "auto",
        promptText:               serverConfig.promptMessage                    ?? null,
        headerText:               serverConfig.promptHeader                     ?? null,
        backgroundMode:           normalizeBackgroundMode(serverConfig.overlayBackgroundMode),
        artworkPreference:        normalizeArtworkPreference(serverConfig.overlayArtworkPreference),
        backgroundColor:          normalizeHexColor(serverConfig.overlayBackgroundColor, "#05080D"),
        textColor:                normalizeHexColor(serverConfig.overlayTextColor, "#FFFFFF"),
        primaryButtonColor:       normalizeHexColor(serverConfig.overlayPrimaryButtonColor, "#00A4DC"),
        primaryButtonTextColor:   normalizeHexColor(serverConfig.overlayPrimaryButtonTextColor, "#FFFFFF"),
        secondaryButtonColor:     normalizeHexColor(serverConfig.overlaySecondaryButtonColor, "#2B3038"),
        secondaryButtonTextColor: normalizeHexColor(serverConfig.overlaySecondaryButtonTextColor, "#FFFFFF"),
        backgroundDimPercent:     clampNumber(serverConfig.overlayBackgroundDimPercent, 62, 0, 95),
        artworkBlurPixels:        clampNumber(serverConfig.overlayArtworkBlurPixels, 10, 0, 40),
        panelOpacityPercent:      clampNumber(serverConfig.overlayPanelOpacityPercent, 72, 0, 100),
        customBackgroundUrl:      normalizeCustomUrl(serverConfig.overlayCustomBackgroundUrl),
    };

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

    const getApiClient = (): JellyfinApiClient | null =>
        window.ApiClient && typeof window.ApiClient === "object" ? window.ApiClient : null;

    const getActiveSession = async (apiClient: JellyfinApiClient): Promise<JellyfinSession | null> => {
        if (typeof apiClient.getSessions !== "function") return null;

        const currentDeviceId = typeof apiClient.deviceId === "function" ? apiClient.deviceId() : null;
        const currentUserId = typeof apiClient.getCurrentUserId === "function" ? apiClient.getCurrentUserId() : null;
        const options: Record<string, unknown> = { ActiveWithinSeconds: 30 };
        if (currentUserId) options.ControllableByUserId = currentUserId;

        const sessions = await apiClient.getSessions(options);
        return (
            sessions.find(session => Boolean(currentDeviceId && session.DeviceId === currentDeviceId && session.NowPlayingItem)) ??
            sessions.find(session => Boolean(session.NowPlayingItem)) ??
            null
        );
    };

    const getSeriesItem = async (apiClient: JellyfinApiClient, item: JellyfinItem, userId: string | null): Promise<JellyfinItem | null> => {
        if (!item.SeriesId || typeof apiClient.getItem !== "function") return null;

        try {
            return await apiClient.getItem(userId, item.SeriesId);
        } catch {
            return null;
        }
    };

    const getBackdropUrl = (apiClient: JellyfinApiClient, item: JellyfinItem | null): string | null => {
        if (!item || typeof apiClient.getImageUrl !== "function") return null;

        if (item.Id && item.BackdropImageTags && item.BackdropImageTags.length > 0) {
            return apiClient.getImageUrl(item.Id, {
                type: "Backdrop",
                index: 0,
                tag: item.BackdropImageTags[0],
                fillWidth: 1920,
                quality: 90,
            });
        }

        if (item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length > 0) {
            return apiClient.getImageUrl(item.ParentBackdropItemId, {
                type: "Backdrop",
                index: 0,
                tag: item.ParentBackdropImageTags[0],
                fillWidth: 1920,
                quality: 90,
            });
        }

        return null;
    };

    const getPrimaryUrl = (apiClient: JellyfinApiClient, item: JellyfinItem | null): string | null => {
        if (!item?.Id || !item.ImageTags?.Primary || typeof apiClient.getImageUrl !== "function") return null;

        return apiClient.getImageUrl(item.Id, {
            type: "Primary",
            tag: item.ImageTags.Primary,
            fillHeight: 1080,
            quality: 90,
        });
    };

    const chooseArtworkUrl = (
        apiClient: JellyfinApiClient,
        item: JellyfinItem,
        seriesItem: JellyfinItem | null
    ): string | null => {
        const candidates =
            settings.artworkPreference === "ItemBackdrop"
                ? [getBackdropUrl(apiClient, item), getBackdropUrl(apiClient, seriesItem), getPrimaryUrl(apiClient, item)]
                : settings.artworkPreference === "PrimaryImage"
                    ? [getPrimaryUrl(apiClient, item), getBackdropUrl(apiClient, item), getBackdropUrl(apiClient, seriesItem)]
                    : [getBackdropUrl(apiClient, seriesItem), getBackdropUrl(apiClient, item), getPrimaryUrl(apiClient, item)];

        return candidates.find(Boolean) ?? null;
    };

    const resolveNowPlayingArtworkUrl = async (): Promise<string | null> => {
        const apiClient = getApiClient();
        if (!apiClient) return null;

        try {
            const session = await getActiveSession(apiClient);
            const item = session?.NowPlayingItem;
            if (!item) return null;

            const userId =
                session.UserId ??
                (typeof apiClient.getCurrentUserId === "function" ? apiClient.getCurrentUserId() : null);
            const seriesItem = await getSeriesItem(apiClient, item, userId);
            return chooseArtworkUrl(apiClient, item, seriesItem);
        } catch {
            return null;
        }
    };

    const setOverlayImage = (overlay: HTMLElement, imageUrl: string): void => {
        const artwork = overlay.querySelector<HTMLElement>(".sleepguard-artwork");
        if (!artwork) return;

        artwork.style.backgroundImage = `url("${imageUrl.replace(/"/g, "%22")}")`;
        overlay.classList.add("sleepguard-has-image");
    };

    const applyBackground = async (overlay: HTMLElement): Promise<void> => {
        if (settings.backgroundMode === "Solid") return;

        const imageUrl =
            settings.backgroundMode === "CustomUrl"
                ? settings.customBackgroundUrl
                : await resolveNowPlayingArtworkUrl();

        if (imageUrl && document.getElementById(overlayId) === overlay) {
            setOverlayImage(overlay, imageUrl);
        }
    };

    const applyAppearanceVariables = (overlay: HTMLElement): void => {
        overlay.style.setProperty("--sg-background-color", settings.backgroundColor);
        overlay.style.setProperty("--sg-text-color", settings.textColor);
        overlay.style.setProperty("--sg-primary-button-color", settings.primaryButtonColor);
        overlay.style.setProperty("--sg-primary-button-text-color", settings.primaryButtonTextColor);
        overlay.style.setProperty("--sg-secondary-button-color", settings.secondaryButtonColor);
        overlay.style.setProperty("--sg-secondary-button-text-color", settings.secondaryButtonTextColor);
        overlay.style.setProperty("--sg-dim-opacity", String(settings.backgroundDimPercent / 100));
        overlay.style.setProperty("--sg-artwork-blur", `${settings.artworkBlurPixels}px`);
        overlay.style.setProperty("--sg-panel-opacity", String(settings.panelOpacityPercent / 100));
    };

    const showOverlay = (): void => {
        observer.disconnect();

        if (document.getElementById(overlayId)) return;
        if (!isPlaybackPage()) return;
        pausePlayback();

        const overlay = document.createElement("div");
        overlay.id = overlayId;
        applyAppearanceVariables(overlay);

        const artwork = document.createElement("div");
        artwork.className = "sleepguard-artwork";
        overlay.appendChild(artwork);

        const dim = document.createElement("div");
        dim.className = "sleepguard-dim";
        overlay.appendChild(dim);

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
        overflow: hidden;
        color: var(--sg-text-color);
        font-family: inherit;
        animation: sleepguard-fadein 220ms ease-out;
        background: var(--sg-background-color);
      }

      #${overlayId} .sleepguard-artwork {
        position: absolute;
        inset: -34px;
        z-index: 0;
        background-position: center;
        background-repeat: no-repeat;
        background-size: cover;
        filter: blur(var(--sg-artwork-blur));
        opacity: 0;
        transform: scale(1.04);
        transition: opacity 240ms ease-out;
      }

      #${overlayId}.sleepguard-has-image .sleepguard-artwork {
        opacity: 1;
      }

      #${overlayId} .sleepguard-dim {
        position: absolute;
        inset: 0;
        z-index: 0;
        background: rgba(0, 0, 0, var(--sg-dim-opacity));
        opacity: 0;
      }

      #${overlayId}.sleepguard-has-image .sleepguard-dim {
        opacity: 1;
      }

      #${overlayId} .sleepguard-panel {
        position: relative;
        z-index: 1;
        width: min(700px, calc(100vw - 32px));
        text-align: center;
        padding: 34px 26px;
        border: 1px solid rgba(255, 255, 255, 0.16);
        border-radius: 8px;
        background: rgba(10, 14, 22, var(--sg-panel-opacity));
        box-shadow: 0 24px 90px rgba(0, 0, 0, 0.42);
        backdrop-filter: blur(18px);
      }

      #${overlayId} .sleepguard-title {
        font-size: 18px;
        opacity: 0.76;
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
        transition: transform 120ms ease-out, filter 120ms ease-out;
      }

      #${overlayId} button:hover {
        filter: brightness(1.08);
        transform: translateY(-1px);
      }

      #${overlayId} .sleepguard-continue {
        background: var(--sg-primary-button-color);
        color: var(--sg-primary-button-text-color);
      }

      #${overlayId} .sleepguard-dismiss {
        background: var(--sg-secondary-button-color);
        color: var(--sg-secondary-button-text-color);
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
        void applyBackground(overlay);
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
