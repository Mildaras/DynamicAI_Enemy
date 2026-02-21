# Session-Based AI Learning System

## Overview
Each game session is completely independent and automatically managed:
- **Start game** → New clean session begins
- **Play & fight** → Actions logged in real-time
- **Press K** → AI adapts based on current session data
- **Close game** → Session saved with timestamp, logs cleared for next time

## File Structure

```
AppData/LocalLow/<Company>/<Game>/
├── Sessions/
│   ├── SessionIndex.txt              # List of all sessions
│   ├── Session_2026.02.21-14.54/     # Individual session folder
│   │   ├── SessionSummary.txt        # Session start/end times
│   │   ├── CombatStats.txt           # Detailed combat statistics
│   │   ├── WeightChanges.txt         # AI weight adaptations
│   │   └── ActionLog.json            # Raw action log (copy)
│   ├── Session_2026.02.21-15.32/
│   └── Session_2026.02.21-16.08/
└── ActionLog.json                     # Working log (cleared each session)
```

## Setup in Unity

### 1. Add SessionManager to Scene
- Create empty GameObject: `SessionManager`
- Add component: `SessionManager.cs`
- Configure in Inspector:
  - ✅ Clear Logs On Start (default: true)
  - ✅ Auto Save On Exit (default: true)

### 2. Add to CombatAnalytics GameObject
- SessionManager must exist in scene before CombatAnalytics

### 3. That's it!
The system now automatically:
- Creates new session folders
- Clears logs on start
- Saves everything on exit
- Maintains session index

## Session Files Explained

### SessionIndex.txt
Master list of all sessions:
```
2026-02-21 14:54:32 | Duration: 12.3min | Folder: Session_2026.02.21-14.54
2026-02-21 15:32:18 | Duration: 8.7min | Folder: Session_2026.02.21-15.32
```

### CombatStats.txt
Complete combat statistics from LogParser:
```
Combat Summary - Session 2026.02.21-14.54
Timestamp: 2026-02-21 15:06:45
===================================================

=== PLAYER PERFORMANCE ===
Melee: 19 swings, 7 hits (36.8%)
Ranged: 18 fired, 8 hits (44.4%)
...
```

### WeightChanges.txt
AI adaptation results:
```
Weight Adaptation - Session 2026.02.21-14.54
Timestamp: 2026-02-21 15:06:45
============================================================

=== WEIGHT CHANGES (9 modified) ===
  FastSpell: 1.00 → 0.71 (↓ 0.29, -29.5%)
  Attack: 10.00 → 8.10 (↓ 1.90, -19.0%)
...
```

### ActionLog.json
Raw timestamped action data (for debugging/analysis)

## Workflow

### Normal Play Session
1. Launch game
2. SessionManager auto-creates `Session_2026.02.21-14.54/`
3. Play normally, all actions logged
4. Press K to run adaptation (saves to session folder)
5. Close game → Session auto-saved, logs cleared

### Next Session
1. Launch game
2. SessionManager auto-creates `Session_2026.02.21-15.32/`
3. Logs start fresh (previous session preserved in its folder)
4. AI uses **learned weights** from previous sessions

## Finding Your Sessions

**Windows:**
```
C:\Users\<YourName>\AppData\LocalLow\<CompanyName>\<GameName>\Sessions\
```

The path is shown in Unity console when session starts:
```
[AI-CRITICAL] Session folder: C:\Users\...\Sessions\Session_2026.02.21-14.54
```

## Log Levels

Control verbosity in Inspector on `CombatAnalytics`:
- **None** (0) - Silent
- **Critical** (1) - Session info + weight changes only
- **Important** (2) - + Combat summary + player profile
- **Detailed** (3) - + Adapter decisions
- **Verbose** (4) - Everything

## Tips

- Sessions are named by start time, so you can correlate with testing notes
- SessionIndex.txt gives quick overview of all play sessions
- Each session is completely independent - safe to delete old ones
- AI learns cumulatively (weights persist via PlayerPrefs)
- Logs are session-specific (cleared each time)
