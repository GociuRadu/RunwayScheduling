# Glassmorphism UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the RunwayScheduling frontend from plain dark inline styles to a glassmorphism aesthetic with subtle animations, a new landing page, and an updated logo — without changing any functionality.

**Architecture:** CSS custom properties + utility classes added to `index.css` replace inline `S.card`/`S.input` style objects in JSX. The `tokens.ts` `C` object receives four new glass-specific color values; the `S` object is kept as-is. No new npm packages.

**Tech Stack:** React 18, TypeScript, Vite, plain CSS (no Tailwind), `react-router-dom`

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `src/frontend/src/index.css` | Modify | CSS variables, `.glass-*` utility classes, new `@keyframes` |
| `src/frontend/src/styles/tokens.ts` | Modify | Add `glassBg`, `glassBorder`, `glassBorderSelected`, `glowOrange` to `C` |
| `src/frontend/src/layout/AppShell.tsx` | Modify | Glass navbar (`glass-nav` class), logo text `sched`→`scheduling` |
| `src/frontend/src/components/Modal.tsx` | Modify | Apply `glass-modal` panel class |
| `src/frontend/src/layout/Login.tsx` | Modify | Glass modal panel + `modalIn` animation + `glass-input` |
| `src/frontend/src/pages/HomePage.tsx` | Modify | Full landing page with glow orb, gradient title, CTA buttons |
| `src/frontend/src/pages/AirportsPage.tsx` | Modify | Replace `S.card`/`S.cardAccent` with `glass-card`/`glass-card--selected`; staggered `fadeInUp` |
| `src/frontend/src/pages/ScenarioConfigPage.tsx` | Modify | Same card migration + `glass-card--selected` on context bar |

---

## Dev server

```bash
cd src/frontend
npm run dev   # http://localhost:5173
npm run lint  # must pass at end of each task
```

Visual verification after each task: open the browser and confirm the affected page looks correct.

---

## Task 1: CSS Foundation — variables, glass classes, animations

**Files:**
- Modify: `src/frontend/src/index.css`

- [ ] **Step 1: Add CSS custom properties** at the top of `index.css`, right after the existing reset block:

```css
/* ── Glass design system ──────────────────────────────── */
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

- [ ] **Step 2: Add utility classes** after the custom properties block:

```css
/* ── Glass utility classes ───────────────────────────── */
.glass-card {
  background: var(--glass-bg);
  border: 1px solid var(--glass-border);
  border-radius: 10px;
  backdrop-filter: var(--glass-blur);
  -webkit-backdrop-filter: var(--glass-blur);
  padding: 16px;
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
}
.glass-card:hover {
  border-color: var(--glass-border-hover);
  box-shadow: 0 0 20px var(--glow-orange);
}

.glass-card--selected {
  background: var(--glass-bg);
  border: 1px solid rgba(249, 115, 22, 0.3);
  border-radius: 10px;
  backdrop-filter: var(--glass-blur);
  -webkit-backdrop-filter: var(--glass-blur);
  padding: 16px;
  box-shadow: 0 0 16px var(--glow-orange);
  animation: glowPulse 3s ease-in-out infinite;
}

.glass-nav {
  background: rgba(8, 8, 8, 0.85);
  border-bottom: 1px solid var(--glass-border);
  backdrop-filter: var(--glass-nav-blur);
  -webkit-backdrop-filter: var(--glass-nav-blur);
}

.glass-input {
  width: 100%;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid var(--glass-border);
  background: rgba(255, 255, 255, 0.03);
  color: #ffffff;
  outline: none;
  font-size: 14px;
  box-sizing: border-box;
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
}
.glass-input:focus {
  border-color: var(--glass-border-hover);
  box-shadow: 0 0 10px var(--glow-orange);
}

.glass-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.75);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.glass-modal-panel {
  background: rgba(13, 13, 13, 0.92);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
  border: 1px solid var(--glass-border);
  border-radius: 12px;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.6), 0 0 40px rgba(249, 115, 22, 0.04);
  animation: modalIn var(--transition-med) ease forwards;
}

.glass-btn-primary {
  background: linear-gradient(135deg, #dc2626, #ea580c);
  color: white;
  border: none;
  border-radius: 6px;
  padding: 8px 16px;
  cursor: pointer;
  font-weight: 700;
  font-size: 13px;
  transition: filter var(--transition-fast), transform var(--transition-fast);
}
.glass-btn-primary:hover:not(:disabled) {
  filter: brightness(1.1);
  transform: translateY(-1px);
}
.glass-btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.glass-btn-ghost {
  background: rgba(255, 255, 255, 0.04);
  color: #888888;
  border: 1px solid var(--glass-border);
  border-radius: 6px;
  padding: 8px 16px;
  cursor: pointer;
  font-size: 13px;
  transition: border-color var(--transition-fast), color var(--transition-fast), transform var(--transition-fast);
}
.glass-btn-ghost:hover:not(:disabled) {
  border-color: var(--glass-border-hover);
  color: #ffffff;
  transform: translateY(-1px);
}
.glass-btn-ghost:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.glass-btn-danger {
  background: transparent;
  color: #dc2626;
  border: 1px solid rgba(220, 38, 38, 0.4);
  border-radius: 6px;
  padding: 8px 16px;
  cursor: pointer;
  font-size: 13px;
  transition: background var(--transition-fast), transform var(--transition-fast);
}
.glass-btn-danger:hover:not(:disabled) {
  background: rgba(220, 38, 38, 0.08);
  transform: translateY(-1px);
}
.glass-btn-danger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
```

- [ ] **Step 3: Add keyframe animations** after the existing `@keyframes toast-in` block:

```css
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes glowPulse {
  0%, 100% {
    box-shadow: 0 0 8px var(--glow-orange);
  }
  50% {
    box-shadow: 0 0 24px var(--glow-orange-strong);
  }
}

@keyframes modalIn {
  from {
    opacity: 0;
    transform: scale(0.96);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}
```

- [ ] **Step 4: Run lint**

```bash
cd src/frontend && npm run lint
```

Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/index.css
git commit -m "style: add glass CSS variables, utility classes, and animations"
```

---

## Task 2: Token additions

**Files:**
- Modify: `src/frontend/src/styles/tokens.ts`

- [ ] **Step 1: Add glass tokens to the `C` object** (after `activeGreen`):

```ts
// In the C object, add:
glassBg: "rgba(255,255,255,0.04)",
glassBorder: "rgba(255,255,255,0.08)",
glassBorderSelected: "rgba(249,115,22,0.3)",
glowOrange: "rgba(249,115,22,0.1)",
```

- [ ] **Step 2: Run lint**

```bash
cd src/frontend && npm run lint
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/styles/tokens.ts
git commit -m "style: add glass color tokens to C object"
```

---

## Task 3: AppShell — glass navbar + logo

**Files:**
- Modify: `src/frontend/src/layout/AppShell.tsx`

- [ ] **Step 1: Update the header element** — replace the `background: C.bg` inline style on the `<header>` with the `glass-nav` class. Change from:

```tsx
<header
  style={{
    width: "100%",
    height: "56px",
    borderBottom: `1px solid ${C.border}`,
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    padding: "0 24px",
    boxSizing: "border-box",
    gap: "16px",
    position: "sticky",
    top: 0,
    zIndex: 100,
    background: C.bg,
  }}
>
```

To:

```tsx
<header
  className="glass-nav"
  style={{
    width: "100%",
    height: "56px",
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    padding: "0 24px",
    boxSizing: "border-box",
    gap: "16px",
    position: "sticky",
    top: 0,
    zIndex: 100,
  }}
>
```

- [ ] **Step 2: Update the Logo component** — change the second span from `sched` to `scheduling`:

```tsx
// Before:
<span style={{ color: C.text, fontWeight: 800, fontSize: "14px", letterSpacing: "0.3px" }}>
  runway<span style={{ color: C.primary, fontWeight: 300 }}>sched</span>
</span>

// After:
<span style={{ color: C.text, fontWeight: 800, fontSize: "14px", letterSpacing: "0.3px" }}>
  runway<span style={{ color: C.primary, fontWeight: 300 }}>scheduling</span>
</span>
```

- [ ] **Step 3: Update active nav item style** — replace the bottom border indicator with an orange pill. Change the nav button style from:

```tsx
style={{
  background: "transparent",
  border: "none",
  color: active ? C.primary : C.textSub,
  fontSize: "13px",
  fontWeight: active ? 700 : 400,
  cursor: "pointer",
  padding: "6px 12px",
  borderRadius: "5px",
  borderBottom: active ? `2px solid ${C.primary}` : "2px solid transparent",
}}
```

To:

```tsx
style={{
  background: active ? "rgba(249,115,22,0.12)" : "transparent",
  border: active ? "1px solid rgba(249,115,22,0.3)" : "1px solid transparent",
  color: active ? C.primary : C.textSub,
  fontSize: "13px",
  fontWeight: active ? 700 : 400,
  cursor: "pointer",
  padding: "6px 14px",
  borderRadius: "20px",
  transition: "all 0.2s ease",
}}
```

- [ ] **Step 4: Update Login and Logout buttons** — replace their inline `style` props with class names:

```tsx
// Login button (shown when !isLoggedIn) — replace style prop with className:
<button
  onClick={() => setShowLogin(true)}
  className="glass-btn-primary"
  style={{ fontSize: "12px", padding: "7px 16px" }}
>
  Login
</button>

// Logout button (shown when isLoggedIn) — replace style prop with className:
<button
  onClick={handleLogout}
  className="glass-btn-danger"
  style={{ fontSize: "12px", padding: "7px 16px" }}
>
  Logout
</button>
```

- [ ] **Step 5: Start dev server and verify visually**

```bash
cd src/frontend && npm run dev
```

Open http://localhost:5173. Check: navbar has blur glass effect, logo shows "runwayscheduling", active nav item is an orange pill, Login button is orange gradient, Logout button is red ghost.

To test logged-in state: open browser DevTools → Application → Local Storage → add `token` key with any value, then refresh.

- [ ] **Step 6: Run lint**

```bash
npm run lint
```

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/layout/AppShell.tsx
git commit -m "style: glass navbar, logo text, active nav pill, glass Login/Logout buttons"
```

---

## Task 4: Modal component — glass panel

**Files:**
- Modify: `src/frontend/src/components/Modal.tsx`

- [ ] **Step 1: Replace backdrop and panel inline styles** with glass classes:

```tsx
export function Modal({ title, children, onClose }: ModalProps) {
  return (
    <div
      className="glass-modal-backdrop"
      onClick={onClose}
    >
      <div
        className="glass-modal-panel"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "520px",
          maxWidth: "calc(100vw - 24px)",
          padding: "20px",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: "16px",
          }}
        >
          <div style={{ fontSize: "15px", fontWeight: 800, color: C.text }}>{title}</div>
          <button
            onClick={onClose}
            style={{
              background: "transparent",
              border: "none",
              color: C.textMuted,
              fontSize: "16px",
              cursor: "pointer",
              lineHeight: 1,
            }}
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Run lint**

```bash
cd src/frontend && npm run lint
```

- [ ] **Step 3: Verify visually** — open /airports in dev server, click "+ Create airport" modal. Should have glass blur, subtle orange glow shadow, `modalIn` animation.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/components/Modal.tsx
git commit -m "style: apply glass-modal-panel to shared Modal component"
```

---

## Task 5: Login — glass modal redesign

**Files:**
- Modify: `src/frontend/src/layout/Login.tsx`

- [ ] **Step 1: Replace backdrop div style** with `glass-modal-backdrop` class:

```tsx
// Before: the outer div with position:fixed,inset:0,background:rgba(0,0,0,0.75)...
// After:
<div
  className="glass-modal-backdrop"
  onClick={onClose}
>
```

- [ ] **Step 2: Replace panel div style** with `glass-modal-panel` class + width inline style:

```tsx
// Before: the inner div with width:380px, background:C.bgModal, borderRadius:8px...
// After:
<div
  className="glass-modal-panel"
  onClick={(e) => e.stopPropagation()}
  style={{
    width: "380px",
    maxWidth: "calc(100vw - 32px)",
    padding: "28px",
    color: C.text,
  }}
>
```

- [ ] **Step 3: Replace input styles** — change both `<input>` elements from `style={S.input}` to `className="glass-input"`. Remove the `style` prop.

- [ ] **Step 4: Verify visually** — click Login in nav. Modal should have blur, `modalIn` scale animation, glass inputs with orange focus glow.

- [ ] **Step 5: Run lint**

```bash
cd src/frontend && npm run lint
```

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/layout/Login.tsx
git commit -m "style: glassmorphism login modal with blur and modalIn animation"
```

---

## Task 6: HomePage — landing page

**Files:**
- Modify: `src/frontend/src/pages/HomePage.tsx`

- [ ] **Step 1: Replace the empty component** with the full landing page:

```tsx
import { useNavigate } from "react-router-dom";
import { C } from "../styles/tokens";

export default function HomePage() {
  const navigate = useNavigate();
  const isLoggedIn = !!localStorage.getItem("token");

  return (
    <div
      style={{
        minHeight: "calc(100vh - 56px)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        position: "relative",
        overflow: "hidden",
      }}
    >
      {/* Glow orb */}
      <div
        style={{
          position: "absolute",
          top: "-60px",
          left: "50%",
          transform: "translateX(-50%)",
          width: "600px",
          height: "400px",
          background: "radial-gradient(ellipse, rgba(249,115,22,0.1) 0%, transparent 65%)",
          pointerEvents: "none",
        }}
      />

      <div style={{ textAlign: "center", position: "relative", maxWidth: "560px", padding: "0 24px" }}>
        {/* Badge */}
        <div
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "8px",
            background: "rgba(249,115,22,0.1)",
            border: "1px solid rgba(249,115,22,0.2)",
            borderRadius: "20px",
            padding: "5px 14px",
            marginBottom: "24px",
          }}
        >
          <span
            style={{
              width: "7px",
              height: "7px",
              borderRadius: "50%",
              background: C.primary,
              display: "inline-block",
              animation: "glowPulse 2s ease-in-out infinite",
            }}
          />
          <span style={{ color: C.primary, fontSize: "10px", fontWeight: 600, letterSpacing: "1.5px" }}>
            AIRPORT RUNWAY SCHEDULING
          </span>
        </div>

        {/* Title */}
        <h1
          style={{
            color: C.text,
            fontSize: "42px",
            fontWeight: 900,
            margin: "0 0 14px",
            lineHeight: 1.1,
          }}
        >
          Optimize every{" "}
          <span
            style={{
              background: "linear-gradient(90deg, #dc2626, #f97316)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            runway operation
          </span>
        </h1>

        {/* Subtitle */}
        <p
          style={{
            color: C.textSub,
            fontSize: "14px",
            margin: "0 0 32px",
            lineHeight: 1.6,
          }}
        >
          Greedy scheduling simulation for airport runway management.
          Configure scenarios, generate flights, and solve.
        </p>

        {/* CTA buttons */}
        {isLoggedIn ? (
          <div style={{ display: "flex", gap: "12px", justifyContent: "center" }}>
            <button className="glass-btn-primary" onClick={() => navigate("/airports")}>
              Airports →
            </button>
            <button className="glass-btn-ghost" onClick={() => navigate("/scenario-config")}>
              Scenarios
            </button>
          </div>
        ) : (
          <div style={{ display: "flex", gap: "12px", justifyContent: "center" }}>
            <button className="glass-btn-primary" style={{ padding: "11px 24px", fontSize: "14px" }} onClick={() => navigate("/airports")}>
              Get started →
            </button>
            <button className="glass-btn-ghost" style={{ padding: "11px 24px", fontSize: "14px" }} onClick={() => navigate("/airports")}>
              View airports
            </button>
          </div>
        )}

        {/* Feature pills */}
        <div style={{ display: "flex", gap: "10px", justifyContent: "center", marginTop: "36px", flexWrap: "wrap" }}>
          {["✈ Multi-runway", "⛅ Weather simulation", "⚡ Greedy solver"].map((pill) => (
            <span
              key={pill}
              style={{
                background: "rgba(255,255,255,0.03)",
                border: "1px solid rgba(255,255,255,0.07)",
                borderRadius: "8px",
                padding: "6px 14px",
                fontSize: "11px",
                color: C.textSub,
              }}
            >
              {pill}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Verify visually** — open http://localhost:5173. Check: glow orb visible, gradient title, CTA buttons, feature pills. Both logged-in and logged-out states.

- [ ] **Step 3: Run lint**

```bash
cd src/frontend && npm run lint
```

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/pages/HomePage.tsx
git commit -m "feat: implement landing page with glass design and glow orb"
```

---

## Task 7: AirportsPage — glass cards + fadeInUp

**Files:**
- Modify: `src/frontend/src/pages/AirportsPage.tsx`

- [ ] **Step 1: Replace the selected airport bar** — change `style={{ ...S.cardAccent, marginBottom: "20px" }}` to:

```tsx
<div className="glass-card--selected" style={{ marginBottom: "20px" }}>
```

- [ ] **Step 2: Replace airport list cards** — each airport card currently uses `style={{ ...S.card, borderColor: isSelected ? C.borderAccentRed : C.border }}`. Replace with:

```tsx
<div
  key={airport.id}
  className={isSelected ? "glass-card--selected" : "glass-card"}
  style={{ animationDelay: `${index * 50}ms`, animationFillMode: "both", animation: `fadeInUp 0.3s ease ${index * 50}ms both` }}
>
```

Note: the map callback needs the index parameter: `.map((airport, index) => {`

- [ ] **Step 3: Replace runway list cards** — each runway card currently uses `style={S.card}`. Replace with:

```tsx
<div
  key={runway.id}
  className="glass-card"
  style={{ animationDelay: `${index * 50}ms`, animation: `fadeInUp 0.3s ease ${index * 50}ms both` }}
>
```

Note: the map callback needs the index: `.map((runway, index) => {`

- [ ] **Step 4: Replace inner stat boxes** in the selected airport bar — the three stat boxes inside the grid use:
```tsx
style={{ background: C.bg, border: `1px solid ${C.border}`, borderRadius: "6px", padding: "8px 12px", textAlign: "center" }}
```
Replace with `className="glass-card"` and remove the `padding` override by adding `style={{ padding: "8px 12px", textAlign: "center" }}`.

- [ ] **Step 5: Replace button styles** — replace `style={S.primaryBtn}` with `className="glass-btn-primary"`, `style={S.secondaryBtn}` with `className="glass-btn-ghost"`, `style={S.dangerBtn}` with `className="glass-btn-danger"` throughout this file. Keep `disabled` props as-is.

- [ ] **Step 6: Replace modal form inputs** — inside the two Modal children (create airport and create runway), replace `style={S.input}` with `className="glass-input"` on all `<input>` and `<select>` elements.

- [ ] **Step 7: Verify visually** — open /airports. Check: cards fade in staggered, selected airport glows orange, hover glow on cards, buttons lift on hover.

- [ ] **Step 8: Run lint**

```bash
cd src/frontend && npm run lint
```

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/pages/AirportsPage.tsx
git commit -m "style: glassmorphism cards, fadeInUp stagger, glass buttons on AirportsPage"
```

---

## Task 8: ScenarioConfigPage — glass cards + modals

**Files:**
- Modify: `src/frontend/src/pages/ScenarioConfigPage.tsx`

- [ ] **Step 1: Replace context bar** — change `style={{ ...S.cardAccent, marginBottom: "20px" }}` to:

```tsx
<div className="glass-card--selected" style={{ marginBottom: "20px" }}>
```

- [ ] **Step 2: Replace scenario list cards** — each scenario card uses `style={{ ...S.card, borderColor: isSelected ? C.borderAccentRed : C.border }}`. Replace with:

```tsx
<div
  key={s.id}
  className={isSelected ? "glass-card--selected" : "glass-card"}
  style={{ animation: `fadeInUp 0.3s ease ${index * 60}ms both` }}
>
```

Note: map callback needs index: `.map((s, index) => {`

- [ ] **Step 3: Replace stat sub-tiles** — the small stat boxes inside each scenario card use `style={{ background: C.bg, border: \`1px solid ${C.border}\`, borderRadius: "5px", padding: "5px 8px" }}`. Replace with `className="glass-card"` and `style={{ padding: "5px 8px" }}`.

- [ ] **Step 4: Replace inner stat boxes in context bar** — same pattern as AirportsPage step 4.

- [ ] **Step 5: Replace button styles** — same replacements as AirportsPage step 5, throughout this file including the `LargeModal` close button and all action buttons in the context bar.

- [ ] **Step 6: Replace form inputs** in the create scenario Modal — replace all `style={S.input}` on `<input>` and `<select>` elements with `className="glass-input"`.

- [ ] **Step 7: Apply glass-modal-panel to LargeModal** — the `LargeModal` component defined in this file has its own panel div. Replace its inline styles:

```tsx
// Before:
style={{ width: "1200px", maxWidth: "95vw", maxHeight: "88vh", overflow: "hidden", borderRadius: "8px", padding: "20px", background: C.bgModal, border: `1px solid ${C.border}`, display: "flex", flexDirection: "column", gap: "14px" }}

// After: add className and keep layout styles only
className="glass-modal-panel"
style={{ width: "1200px", maxWidth: "95vw", maxHeight: "88vh", overflow: "hidden", padding: "20px", display: "flex", flexDirection: "column", gap: "14px" }}
```

Also update the backdrop div:
```tsx
// Before: position:fixed,inset:0,background:rgba(0,0,0,0.75)...
// After:
className="glass-modal-backdrop"
style={{ padding: "20px" }}  // keep the padding
```

- [ ] **Step 8: Verify visually** — open /scenario-config. Check: scenario cards stagger in, selected card glows, stat tiles are glass, flights/weather modals use glass panel.

- [ ] **Step 9: Run lint**

```bash
cd src/frontend && npm run lint
```

- [ ] **Step 10: Commit**

```bash
git add src/frontend/src/pages/ScenarioConfigPage.tsx
git commit -m "style: glassmorphism cards, glass modals, fadeInUp on ScenarioConfigPage"
```

---

## Final verification

- [ ] Run `npm run lint` — must be clean
- [ ] Run `npm run build` — must succeed with no errors

```bash
cd src/frontend
npm run lint
npm run build
```

- [ ] Walk through all pages manually: Home, Airports (create airport, create runway, select airport), Scenarios (create scenario, generate flights, view flights modal)
- [ ] Confirm no functional regressions (CRUD operations still work as before)
