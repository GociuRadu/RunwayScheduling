# Glassmorphism UI Redesign — Design Spec

**Date:** 2026-03-25
**Project:** RunwayScheduling Frontend
**Status:** Approved

---

## Overview

Refactor the existing React + TypeScript frontend from plain dark cards with inline styles to a futuristic glassmorphism aesthetic. The goal is improved visual quality and responsiveness (desktop-only, 1024px+) without breaking existing functionality.

---

## Design Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Visual style | Glassmorphism | User selected: frosted-glass HUD look |
| Responsive target | Desktop only (1024px+) | User requirement |
| Animations | Subtle (hover glow, fadeInUp, glowPulse) | User selected |
| Homepage | Landing page with logo + nav buttons | User requirement |
| Logo text | `runwayscheduling` (full word) | User correction |
| Implementation approach | CSS Variables + glass layer (Option A) | Non-breaking, clean architecture |

---

## Architecture

### Approach: CSS Variables + Glass Layer

All glassmorphism styling is implemented via CSS custom properties in `index.css`. Inline JS styles in `tokens.ts` are kept for color values used in JS logic (button states, conditional colors). Card/nav/input components migrate to CSS classes.

**No new dependencies required.**

---

## CSS Design System (`index.css`)

### Custom Properties
```css
:root {
  --glass-bg: rgba(255, 255, 255, 0.04);
  --glass-border: rgba(255, 255, 255, 0.08);
  --glass-border-hover: rgba(249, 115, 22, 0.3);
  --glass-blur: blur(12px);
  --glass-nav-blur: blur(20px);
  --glow-orange: rgba(249, 115, 22, 0.1);
  --glow-orange-strong: rgba(249, 115, 22, 0.2);
  --transition-fast: 0.2s ease;
  --transition-med: 0.3s ease;
}
```

### Utility Classes
- `.glass-card` — frosted card with hover glow
- `.glass-card--selected` — selected state with orange border + pulse glow
- `.glass-nav` — sticky navbar with backdrop blur
- `.glass-input` — frosted input field
- `.glass-modal` — modal backdrop + panel
- `.glass-btn-primary` — gradient orange button with hover lift
- `.glass-btn-ghost` — translucent ghost button
- `.glass-btn-danger` — red ghost button

### Animations
```css
@keyframes fadeInUp       /* cards on load: opacity 0→1, translateY 8px→0 */
@keyframes glowPulse      /* selected card: box-shadow pulses orange */
@keyframes modalIn        /* modal open: scale 0.96→1 + fadeIn */
@keyframes slideInRight   /* toast: translateX 110%→0 (already exists, keep) */
@keyframes skeletonPulse  /* loading (already exists, keep) */
```

Staggered `fadeInUp` on card lists: each card gets `animation-delay: calc(index * 50ms)` via inline style.

---

## Token Additions (`tokens.ts`)

Add glass-specific values for use in JSX where inline styles are still needed (e.g., conditional border colors):

```ts
glassBg: "rgba(255,255,255,0.04)",
glassBorder: "rgba(255,255,255,0.08)",
glassBorderSelected: "rgba(249,115,22,0.3)",
glowOrange: "rgba(249,115,22,0.1)",
```

---

## Page Designs

### AppShell (Navbar)
- Class: `.glass-nav` — `backdrop-filter: blur(20px)`, `background: rgba(8,8,8,0.8)`, `border-bottom: 1px solid rgba(255,255,255,0.06)`
- Logo: full text `runwayscheduling` — `runway` bold white, `scheduling` light orange
- Active nav item: orange pill badge instead of bottom border
- Login button: `.glass-btn-primary`
- Logout button: `.glass-btn-danger`

### HomePage (new)
- Full-screen landing section centered vertically
- Background: radial glow orb centered top (orange, very subtle)
- Badge: `● AIRPORT RUNWAY SCHEDULING` — orange pill with pulsing dot
- Title: "Optimize every **runway operation**" — gradient text orange→red on highlight
- Subtitle: description text in `textMuted`
- CTA buttons: "Get started →" (primary) + "View airports" (ghost)
- Feature pills row: `✈ Multi-runway`, `⛅ Weather simulation`, `⚡ Greedy solver`
- Logged-out: shows Sign In CTA. Logged-in: shows quick nav to Airports/Scenarios.

### AirportsPage
- Page header unchanged (label + h1 + action buttons)
- Selected airport bar: `.glass-card--selected` with orange glow
- Airport list: `.glass-card` with `fadeInUp` stagger, hover glow
- Runway list: `.glass-card` with hover glow
- Two-column grid preserved (1.1fr 1fr)
- Modals: `.glass-modal`

### ScenarioConfigPage
- Context bar: `.glass-card--selected`
- Scenario cards: `.glass-card` with `fadeInUp` stagger
- Stat sub-tiles inside cards: `.glass-card` nested (smaller padding)
- LargeModal: `.glass-modal` full width
- Create form modal: `.glass-modal`

### Login Modal
- Backdrop: `rgba(0,0,0,0.75)` + `fadeIn`
- Panel: `backdrop-filter: blur(24px)`, `border: 1px solid rgba(255,255,255,0.08)`, `border-radius: 12px`, subtle orange glow shadow
- Inputs: `.glass-input`
- Animation: `modalIn` (scale 0.96→1 + opacity 0→1)

---

## Files Changed

| File | Type | Change |
|------|------|--------|
| `src/frontend/src/index.css` | Modify | Add CSS variables, glass utility classes, animations |
| `src/frontend/src/styles/tokens.ts` | Modify | Add glass color tokens |
| `src/frontend/src/layout/AppShell.tsx` | Modify | Glass nav, new logo text, class names |
| `src/frontend/src/layout/Login.tsx` | Modify | Glass modal redesign |
| `src/frontend/src/pages/HomePage.tsx` | Modify | Full landing page implementation |
| `src/frontend/src/pages/AirportsPage.tsx` | Modify | Glass cards, fadeInUp stagger |
| `src/frontend/src/pages/ScenarioConfigPage.tsx` | Modify | Glass cards, glass modals |

---

## Out of Scope

- Mobile/tablet responsiveness
- New routes or API changes
- Any backend changes
- Adding new npm packages
- ContactPage (minor, not in main flow)
