# Farm Empire Tycoon - Unity Android Setup and Build Guide

This guide provides step-by-step instructions on setting up the **Farm Empire Tycoon** project in Unity 6, configuring touch-friendly components, setting up a minimalistic light theme (Day Mode), and building for Android.

---

## 1. Project Initialization & Platform Setup

1. Open **Unity Hub** and create a new project using **Unity 6 (6000.0.x)** with the **3D (URP)** template.
2. Select the destination folder: `c:\Users\Srira\Desktop\Mahilan\Antigravity\Business Game\`.
3. Once the project opens, navigate to **File > Build Settings** (or **Build Profiles**).
4. Select **Android** in the platform list and click **Switch Platform**.
5. Navigate to **Window > Package Manager** and install:
   - **Netcode for GameObjects** (multiplayer syncing)
   - **TextMeshPro** (import Essentials when prompted)

---

## 2. Directory Structure

Ensure the directory structure matches the planned configuration under `Assets/`:
- `Assets/_Project/`
  - `Scripts/` (Already created with all core game scripts)
  - `Prefabs/` (For Player tokens and mobile screen overlays)
  - `Scenes/` (Create `01_MainMenu` and `02_Game` in portrait aspect ratio)
  - `Data/` (For storing your ScriptableObject instances)

---

## 3. Creating ScriptableObject Assets (16-Tile Layout)

1. Right-click in the `Assets/_Project/Data/` folder.
2. Select **Create > Farm Empire > Property Data** to create property tiles. Create 10 property instances:
   - **Rice Valley** (Sector: RiceValley, Cost: $60, Base Rent: $4, etc.)
   - **Wheat Plain** (Sector: RiceValley, Cost: $80, Base Rent: $6, etc.)
   - **Corn Belt** (Sector: WheatPlains, Cost: $120, Base Rent: $8, etc.)
   - **Dairy Ranch** (Sector: WheatPlains, Cost: $140, Base Rent: $10, etc.)
   - **Fruit Orchard** (Sector: CornBelt, Cost: $180, Base Rent: $14, etc.)
   - **Coffee Hill** (Sector: CornBelt, Cost: $200, Base Rent: $16, etc.)
   - **Tea Garden** (Sector: DairyRanch, Cost: $240, Base Rent: $20, etc.)
   - **Cotton Field** (Sector: DairyRanch, Cost: $260, Base Rent: $22, etc.)
   - **Sugar Estate** (Sector: FruitOrchard, Cost: $300, Base Rent: $26, etc.)
   - **Agritech Park** (Sector: FruitOrchard, Cost: $350, Base Rent: $35, etc.)
3. Create Special/Event tiles by selecting **Create > Farm Empire > Event Tile Data**:
   - **Agri HQ** (TileType: Start)
   - **Tariff Tax** (TileType: Tax, fixedValue: 150)
   - **Audit Hold** (TileType: RegulatoryReview)
   - **Subsidy** (TileType: Subsidy, fixedValue: 150)
   - **Audit Office** (TileType: GoToRegulatoryReview)
   - **Market Event** (TileType: MarketFluctuation)

---

## 4. Hierarchy Linkage & GameManager Setup

1. In your `02_Game` scene, create a new Empty GameObject and name it **_Managers**.
2. Add the following scripts to **_Managers**:
   - `GameManager`
   - `BoardManager`
   - `DiceManager`
   - `EconomyManager`
   - `TradeManager`
   - `AIManager`
   - `SaveLoadManager`
   - `UnityServicesNetcode` (Add `UNITY_SERVICES_ENABLED` to scripting define symbols if using)
3. Inspect `BoardManager` and populate the `boardTiles` array with your 16 ScriptableObject instances in order (0 to 15).

---

## 5. Mobile Day Mode UI Setup

1. Create a canvas (`GameObject > UI > Canvas`) in the `02_Game` scene.
2. In the **Canvas Scaler** component on your Canvas, configure:
   - **Ui Scale Mode**: `Scale With Screen Size`
   - **Reference Resolution**: `1080` x `1920` (Standard Portrait)
   - **Screen Match Mode**: `Match Width Or Height`
   - **Match**: `0.5`
3. Design a minimalistic, clean light theme (Day Mode):
   - Light gray background panel (`#F5F7FA`)
   - Pure white card backgrounds (`#FFFFFF`)
   - Sage green highlights (`#2E7D32`)
   - Warm matte gold borders (`#C5A059`)
   - Dark slate text (`#2C3E50`)
4. Attach the `UIManager` script to your Canvas and drag/drop your portrait panel UI structures, scrollable console text log elements, and operations buttons.

---

## 6. Build Instructions (Android APK)

1. Open **File > Build Settings**.
2. Add your scenes (`01_MainMenu`, `02_Game`).
3. Set Target Platform to **Android**.
4. In **Project Settings > Player > Resolution and Presentation**:
   - Set **Default Orientation** to **Portrait**.
5. Click **Build** (or **Build And Run** if an Android test device is connected via USB debugging).
6. Install the generated `.apk` file to start playing your minimalistic Day Mode farm empire tycoon on mobile!
