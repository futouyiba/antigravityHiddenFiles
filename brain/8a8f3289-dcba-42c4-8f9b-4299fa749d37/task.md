# Task: Update TargetGDD with Bite Perception optimization

- [ ] Analyze documents <!-- id: 0 -->
    - [x] Read `documents\【预研】优化-中鱼后的迅速感知.md` <!-- id: 1 -->
    - [x] Read `documents\TargetGDD.md` <!-- id: 2 -->
- [ ] Update `documents\TargetGDD.md` <!-- id: 3 -->
    - [x] Copy content from `【预研】优化-中鱼后的迅速感知.md` <!-- id: 4 -->
    - [x] Add program trigger rules for `FX_fish_bite_mood_linger` and `FX_fish_bite_instant` based on user description <!-- id: 5 -->
    - [x] Verify changes <!-- id: 6 -->
- [ ] Refine TargetGDD with curve and overlap rules <!-- id: 7 -->
    - [x] Update curve type to `easeInOutCubic` <!-- id: 8 -->
    - [x] Add re-trigger/overlay rule for `FX_fish_bite_mood_linger` <!-- id: 9 -->
    - [x] Add mathematical formula for `easeInOutCubic` as fallback <!-- id: 10 -->
    - [x] Generate `easeInOutCubic` curve image <!-- id: 11 -->
    - [x] Embed image into `TargetGDD.md` <!-- id: 12 -->
    - [x] Update formula to decay from start value (0.7) to 0 <!-- id: 13 -->
    - [x] Regenerate curve image with new parameters <!-- id: 14 -->
    - [x] Add `moodLingerDuration` variable (default 10s) and update formula <!-- id: 15 -->
    - [x] Rename "constant bite mood value" and define default variables <!-- id: 16 -->
    - [x] Clarify re-trigger logic (stop current tween, restart from peak) <!-- id: 17 -->
