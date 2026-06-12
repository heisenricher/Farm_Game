# Farm Empire Tycoon - World Agriculture Edition

A clean, minimalistic, portrait-oriented business board game designed specifically for mobile play on Android, utilizing a light "Day Mode" aesthetic.

---

## 📱 Play Immediately on Android (No Install Required)

The mobile day-mode mockup is hosted on GitHub Pages and is fully playable directly in your phone's browser:

👉 **[Play Farm Empire Tycoon Mobile](https://heisenricher.github.io/Farm_Game/)**

*Tip: Open the link in Chrome or Safari on your phone, then tap "Add to Home Screen" to install it as a full-screen App!*

---

## 🎮 Game Features
* **16-Tile Portrait Perimeter Board**: Compact board optimized for vertical mobile screens.
* **Agriculture Business Theme**: Buy and upgrade crops (Rice, Wheat, Corn, Dairy, Fruits, Tea, Cotton) from empty lands into processing facilities and headquarters.
* **Minimalistic Day Mode**: Clean, bright white backgrounds with rich green and gold accents.
* **Offline AI Bots**: Choose the number of bot competitors and set their strategic difficulty levels.
* **Save/Load System**: Matches are persistently saved locally to your device.
* **Audio Cues**: Web Audio API synthetically generates dice rolling and transaction sounds.

---

## 🛠️ Unity 6 Project Architecture
The `Assets/_Project/` directory contains the complete C# scripts, ScriptableObject definitions, and assembly configurations for a premium, modular Unity 6 build:
* **Core Turn Engine**: Handles players, double rolls, passing start rewards, and audit reviews.
* **Decoupled Economy**: Rent calculations, monopoly doubles, even build rules, mortgages, and bankruptcies.
* **AI Personalities**: Easy, Normal, and Hard bot decision profiles.
* **Multiplayer Hooks**: Swappable networking layers (supporting Netcode for GameObjects, Lobby, and Relay).
* **Automated Tests**: Located in the `Tests/` directory to verify game loop transitions and economic rules.

For setup, linking, and local Android compilation instructions, see the **[Setup & Build Guide](Assets/_Project/Documentation/SetupGuide.md)**.

---

## ⚙️ Automated GitHub Actions builds (CI/CD)
This repository includes a GitHub Actions workflow that compiles the Unity project into an Android APK automatically:
1. Add your Unity license secrets (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`) to your GitHub Repository Secrets.
2. Push a release tag (e.g. `v1.0.0`) to trigger the build.
3. The APK will be automatically compiled and attached to the GitHub Release page for download!
