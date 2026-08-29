# AR Service Assistant

An augmented-reality service-assistance application for **Microsoft HoloLens 2**, developed as the practical component of a bachelor's / scientific thesis. The app lets a technician ask a spoken question, and receive contextual help — either from an **AI assistant** or from a **remote human expert** — rendered as a floating image panel with spoken audio inside the AR view.

> ℹ️ **This README was created with the support of AI** (based on the actual project files) — not exclusively AI-generated — and was reviewed by the author. Treat version numbers, paths, and steps as a starting point and adjust if the project changes.

---

## Project background & provenance

This project **grew out of the official [Mixed Reality Toolkit 3 (MRTK3)](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity) `MRTKDevTemplate` sample**. The sample scenes, sample scripts, profiles, and shared assets that ship with that template are still present in the repository so that the project stays reproducible and buildable.

**The author's own contribution** lives in clearly separated locations:

| Contribution | Location |
| --- | --- |
| Application C# code | `AR.Service.Assistant/Assets/Scripts/MyScripts/` |
| — Assistance system (AI / Human modes) | `MyScripts/AssistanceSystem/` |
| — Server communication (WebSocket / TCP / video) | `MyScripts/Communication/` |
| — Speech (dictation, keywords, text-to-speech) | `MyScripts/SpeechHandler/` |
| — Tutorial flow | `MyScripts/Tutorial/` |
| — UI, dialogs, localization | `MyScripts/UiScripts/` |
| — Runtime object placement / markers | `MyScripts/RuntimeObjects/` |
| — Logging | `MyScripts/Logging/` |
| Application scenes | `Assets/Scenes/AiAssistanceScene.unity`, `HiAssistanceScene.unity`, `MyTutorial.unity`, `DialogExample.unity` |

Everything else under `Assets/` (the various `*Example*` scenes, the flat scripts directly in `Assets/Scripts/`, `Data Binding Example/`, `UX Theming Example/`, `Example Assets/`, profiles, and standard assets) originates from the MRTK3 sample template.

---

## Features

- **AI assistance mode** – captures the HoloLens camera image, records a spoken question via dictation, sends both to a messaging server, and displays the returned image + audio answer as a floating panel.
- **Human assistance mode** – routes the same request to a remote human expert.
- **Speech interaction** – dictation, keyword recognition, and text-to-speech.
- **Real-time communication** – WebSocket and TCP clients, plus camera/video streaming to a companion server.
- **In-app tutorial** – guided onboarding flow.
- **Localization** and configurable in-app dialogs.

---

## Tech stack

| Component | Version / Detail |
| --- | --- |
| Unity Editor | **2021.3.45f2** (LTS) |
| Framework | MRTK3 (Mixed Reality Toolkit 3, `4.0.0-pre.1` core) |
| XR runtime | OpenXR (Microsoft Mixed Reality OpenXR 1.10.1) |
| Target device | HoloLens 2 (Universal Windows Platform, ARM64) |
| WebSocket | [NativeWebSocket](https://github.com/endel/NativeWebSocket) |
| JSON | Newtonsoft.Json |

---

## Prerequisites

1. **Unity Hub** and **Unity 2021.3.45f2** (install this exact version).
   - When installing, add the **Universal Windows Platform (UWP) Build Support** module (required for HoloLens 2 deployment).
   - The project also references AR Foundation / ARCore packages; add **Android Build Support** only if you intend to test on Android.
   - Developed and tested with **Unity 2021.3.45f2 LTS**. Newer Unity versions (e.g. Unity 6.x LTS) are **not supported out of the box**: nothing in the project hard-locks the editor version, but upgrading would require bumping the pinned XR packages (AR Foundation, OpenXR, XR Interaction Toolkit, Input System) to their Unity 6 generation and verifying MRTK3 compatibility, which is not guaranteed.
2. **Visual Studio 2022** with the **Universal Windows Platform development** and **Game development with Unity** workloads (needed to build and deploy the generated UWP solution to the HoloLens).
3. A **HoloLens 2** device (or the HoloLens 2 Emulator) for on-device testing.
4. A running **messaging server** that the app talks to over WebSocket (this back end is *not* part of this repository — see [Configuration](#configuration)).
5. **Git** with the ability to handle this repository's size.

---

## Installation

> ℹ️ The steps below are specific to this project. For the general toolchain setup (installing Unity, the Mixed Reality development tools, MRTK3, and deploying to a HoloLens 2), always refer to the official vendor guides linked under [Further reading](#further-reading--official-setup-guides) — they are the authoritative and up-to-date source.

```bash
# 1. Clone the repository
git clone <REPOSITORY_URL>
cd Bachelor-AR.Service.Assistant
```

2. **Open the project in Unity Hub**
   - Click **Add → Add project from disk**.
   - Select the **`AR.Service.Assistant`** subfolder (the folder that contains `Assets/`, `Packages/`, and `ProjectSettings/`) — *not* the repository root.
   - Open it with Unity **2021.3.45f2**.

3. **Let Unity resolve packages.**
   On first open, the Package Manager restores all dependencies automatically from `Packages/manifest.json`. This includes the local MRTK `.tgz` packages under `Packages/MixedReality/` and the Git-based NativeWebSocket package. The first import can take several minutes.

4. **Open a scene.** The author's scenes serve different roles:
   - **`AiAssistanceScene.unity`** and **`HiAssistanceScene.unity`** provide the actual assistance functionality — the AI assistant and the human-expert support respectively. These are the main application scenes.
   - **`MyTutorial.unity`** is the in-app onboarding **tutorial**, not a support scene.
   - **`DialogExample.unity`** is also included in the repository but is **not active in the Build Settings** (listed as disabled), so it is not part of the build.

   The assistance and tutorial scenes are already registered and enabled in **File → Build Settings**.

### Running in the Editor

Press **Play**. Camera capture falls back to `WebCamTexture` in the Editor, so a webcam lets you exercise the AI-assistance flow without a HoloLens. Configure the server URL first (see below).

### Building & deploying to HoloLens 2

1. **File → Build Settings** → select **Universal Windows Platform** → **Switch Platform**.
2. Set **Architecture** to **ARM64** and **Build Type** to **D3D Project**.
3. Click **Build** and choose an output folder.
4. Open the generated `.sln` in **Visual Studio 2022**.
5. Set the configuration to **Release / ARM64** and the target to **Device** (USB) or **Remote Machine** (Wi-Fi).
6. **Deploy** to the HoloLens 2.

### Further reading — official setup guides

For anything beyond the project-specific steps above, follow the official manufacturer documentation, which is kept current by the respective vendors:

- **Install the Unity Editor & Unity Hub** — Unity: <https://docs.unity3d.com/Manual/GettingStartedInstallingUnity.html>
- **Install the Mixed Reality tools** (Visual Studio workloads, Windows SDK, HoloLens 2 Emulator) — Microsoft: <https://learn.microsoft.com/windows/mixed-reality/develop/install-the-tools>
- **MRTK3 documentation & setup** — Microsoft: <https://learn.microsoft.com/windows/mixed-reality/mrtk-unity/mrtk3-overview/>
- **Mixed Reality OpenXR plugin** — Microsoft: <https://learn.microsoft.com/windows/mixed-reality/develop/unity/new-openxr-getting-started>
- **Build & deploy to HoloLens 2 with Visual Studio** — Microsoft: <https://learn.microsoft.com/windows/mixed-reality/develop/advanced-concepts/using-visual-studio>
- **HoloLens 2 device documentation** — Microsoft: <https://learn.microsoft.com/hololens/>

> Note: exact menu names and steps can differ between tool versions. If a step here diverges from the vendor guide, trust the vendor guide for the toolchain and keep this README only for the project-specific configuration.

---

## Configuration

> ⚠️ **Required to run the app:** before starting the software, you **must** set the **IP address and port of your messaging server**. The default values in the project are placeholders — without adjusting them to your own server, the app cannot connect and the assistance features will not work.

The app communicates with an external messaging/streaming server. The endpoints are set as serialized fields on components in the scene (default values are placeholders and must be changed to your environment):

| Component | Field | Example default |
| --- | --- | --- |
| `AssistanceManager` (`MyScripts/AssistanceSystem`) | `serverUrl` | `ws://192.168.1.100:40002` |
| `WebSocketClient` (`MyScripts/Communication`) | `serverUrl` | `ws://169.254.89.233:40000` |

Select the corresponding GameObject in the scene and edit these values in the Inspector to match your server's IP address and port.

---

## Project structure

```
Bachelor-AR.Service.Assistant/        # Git repository root
├── README.md                         # this file
└── AR.Service.Assistant/             # Unity project
    ├── Assets/
    │   ├── Scripts/
    │   │   ├── MyScripts/             # ← author's own code
    │   │   └── *.cs, EyeTracking/     # MRTK3 sample scripts
    │   ├── Scenes/                    # app scenes + MRTK sample scenes
    │   ├── Profiles/, Prefabs/, ...   # MRTK template / shared assets
    │   ├── Data Binding Example/      # MRTK sample content
    │   └── UX Theming Example/        # MRTK sample content
    ├── Packages/
    │   ├── manifest.json              # package dependencies
    │   └── MixedReality/              # local MRTK3 .tgz packages
    └── ProjectSettings/
```

---

## License / attribution

This project builds upon the **Mixed Reality Toolkit 3** and its `MRTKDevTemplate` sample, which are licensed under their respective terms (see the `NOTICE` file and the MRTK repository).
