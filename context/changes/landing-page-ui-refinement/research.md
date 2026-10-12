---
date: 2026-10-12T16:01:03+02:00
researcher: GitHub Copilot
git_commit: 0ab4ff22f2ebba1abc68d5469cb212a6a6086e29
branch: master
repository: Argus
topic: "Audit and scope the landing-page UI refinement"
tags: [research, codebase, landing-page, responsive-ui, branding]
status: complete
last_updated: 2026-10-12
last_updated_by: GitHub Copilot
---

# Research: Audit and scope the landing-page UI refinement

**Date**: 2026-10-12T16:01:03+02:00  
**Researcher**: GitHub Copilot  
**Git Commit**: 0ab4ff22f2ebba1abc68d5469cb212a6a6086e29  
**Branch**: master  
**Repository**: Argus

## Research Question

How should the existing Argus landing page be refined to resolve the reported
responsive clipping, missing brand mark, weak hero hierarchy, text-only process
section, and unverified mobile states while preserving the existing visual
contract and educational-not-investment-advice boundary?

## Summary

The inspected landing page uses Razor markup in `Pages/Index.cshtml` and a
single token source in `wwwroot/css/site.css`; the view scan found zero
hex/rgb/hsl/oklch color literals or Tailwind-style arbitrary values in
`Pages/Index.cshtml`. The repository has no shared view-component directory, so
the change should extend the existing BEM-style classes rather than introduce a
second component system (`wwwroot/css/site.css:1-27`,
`Pages/Index.cshtml:1-99`).

The reported clipping is consistent with the current layout contract: the hero
combines a `34rem` minimum height, `5rem` block padding, a large tightly
constrained heading, and a preview card with multiple rows
(`wwwroot/css/site.css:92-119`, `wwwroot/css/site.css:179-193`). The inspected
stylesheet has one structural breakpoint at `48rem`; below it, the flex rows
inside the preview have no wrapping rule and the body has a `20rem` minimum
width (`wwwroot/css/site.css:38`, `wwwroot/css/site.css:187-193`,
`wwwroot/css/site.css:313-325`). A browser measurement at the reported
viewport sizes remains an open validation item.

The supplied archive recommends `argus-logo-horizontal.svg` for a web header,
but that file uses deep navy for both its mark and wordmark. The current header
uses the dark background token `--color-bg: #0d1726`, so the supplied
horizontal asset is not suitable without a light-background header or a
lightened variant. The archive also contains `argus-symbol-white.svg`, which
has a `256 256` viewBox and white artwork and is suitable as a decorative mark
beside the existing text brand; the final visual choice should be confirmed by
the screenshot gate (`Pages/Shared/_Layout.cshtml:7-11`,
`wwwroot/css/site.css:1-13`,
`C:\Users\KUCINK01\AppData\Local\Temp\argus-logo-assets-research\argus_logo_assets\README.txt`).

## Detailed Findings

### Responsive layout and hierarchy

- The hero declares `min-height: 34rem` and `padding-block: var(--space-7)`,
  while the heading can reach `5.5rem` with a `12ch` maximum width. On the
  inspected path, this combination can make the hero taller than a typical
  first viewport once the lede, actions, and preview are included
  (`wwwroot/css/site.css:92-119`).
- The two-column layout is enabled by one `@media (min-width: 48rem)` rule,
  where the preview column has `minmax(22rem, 0.85fr)`. At widths near that
  breakpoint, the preview's minimum and the `3rem` column gap leave less room
  for the copy column; the workflow grid also uses `repeat(3, 1fr)`
  (`wwwroot/css/site.css:313-321`).
- The preview header, analysis rows, and factor rows use flex layout without a
  wrapping declaration in the inspected rules. The long Polish labels can
  therefore compete for horizontal space at narrow widths
  (`wwwroot/css/site.css:187-193`).
- The mobile path keeps the desktop hero padding and gap because the inspected
  mobile-specific rules do not override them below `48rem`; the CTA container
  wraps, but the individual links do not receive a full-width mobile treatment
  (`wwwroot/css/site.css:129-135`, `wwwroot/css/site.css:313-325`).
- The heading and preview card both carry strong visual weight: the heading uses
  a large scale and tight tracking, while the card uses a large shadow and
  prominent score values. This supports the reported hierarchy conflict, but
  the relative result needs screenshot verification (`wwwroot/css/site.css:115-119`,
  `wwwroot/css/site.css:179-185`, `wwwroot/css/site.css:236-300`).

### Branding and static assets

- The current header brand is a text-only link, so the supplied logo is not
  rendered on the landing page (`Pages/Shared/_Layout.cshtml:7-11`).
- The repository's inspected `wwwroot` contains the stylesheet but no image
  directory or favicon. Static assets are mapped through
  `app.MapStaticAssets()` (`Program.cs:107`).
- The archive README recommends the horizontal SVG for a web header and the
  white symbol for dark backgrounds. The horizontal SVG's `viewBox` is
  `0 0 720 220`; its mark and wordmark use `#0A2540`, while
  `argus-symbol-white.svg` uses `viewBox="0 0 256 256"` and white artwork
  (`README.txt`, `argus-logo-horizontal.svg`, `argus-symbol-white.svg`).
- Because the current header uses a dark background token, the bounded
  recommendation is to add the white symbol beside the accessible text
  `Argus`, mark the image decorative with `alt=""`, and give it explicit
  dimensions. A full horizontal light-on-dark logo would require a new
  approved asset variant rather than recoloring the SVG in the view
  (`wwwroot/css/site.css:1-13`, `Pages/Shared/_Layout.cshtml:11`).

### Visual proof and UI contract

- The workflow section currently contains three text-only `article` cards and
  no result visualization (`Pages/Index.cshtml:64-87`). A compact, clearly
  illustrative score/result panel can address the value-proof charge while
  keeping the existing `Dane ilustracyjne` distinction in the preview
  (`Pages/Index.cshtml:27-30`, `Pages/Index.cshtml:55-58`).
- `wwwroot/css/site.css:1-27` is the inspected source for color, spacing,
  radius, width, and typography tokens. The landing-page design specification
  also requires semantic classes, desktop plus one narrow mobile layout, visible
  focus, readable contrast, reduced-motion behavior, and illustrative data
  distinct from live analysis (`docs/superpowers/specs/2026-10-12-landing-page-design.md:37-62`).
- No shared component directory was found under `Pages/Shared`; the current
  view uses BEM-style classes directly. The change should therefore keep the
  existing Razor/CSS approach and should not introduce a frontend framework or
  a second design system (`Pages/Index.cshtml:8-99`,
  `docs/superpowers/specs/2026-10-12-landing-page-design.md:54-58`).
- The existing stylesheet covers hover and focus-visible for links and buttons,
  but the inspected landing path has no disabled controls and no loading,
  error, or empty data flow. Those states can be recorded as N/A for this
  static public page; default, hover, focus-visible, desktop, and narrow mobile
  remain applicable (`wwwroot/css/site.css:49-53`, `wwwroot/css/site.css:88`,
  `wwwroot/css/site.css:152-163`, `Pages/Index.cshtml:1-99`).

## Charges

### 1. Responsive hero and preview can exceed the intended viewport

- **Evidence**: the hero combines a `34rem` minimum, `5rem` block padding, a
  large heading, and a multi-row preview (`wwwroot/css/site.css:92-119`,
  `wwwroot/css/site.css:179-193`).
- **Impact**: at common desktop heights, the user may not see the complete
  landing-page entry point without scrolling; at narrow widths, the same
  vertical budget delays the preview.
- **Scope**: reduce the hero's viewport pressure and validate at the reported
  desktop and mobile dimensions; do not remove the educational preview.

### 2. Narrow preview rows have no explicit wrapping contract

- **Evidence**: `.analysis-card__header`, `.analysis-row`, and `.factor-row`
  are flex rows without a wrapping rule, while the page retains a `20rem`
  minimum width (`wwwroot/css/site.css:38`, `wwwroot/css/site.css:187-193`).
- **Impact**: long Polish labels and score content can be clipped or force
  horizontal scrolling on small screens.
- **Scope**: add narrow-screen rules for wrapping, shrinking, and minimum
  widths, then verify at one narrow mobile width and one slightly wider mobile
  width.

### 3. Header lacks the supplied Argus mark

- **Evidence**: the header renders only the text brand (`Pages/Shared/_Layout.cshtml:11`);
  the archive provides a white symbol specifically for dark backgrounds
  (`argus-symbol-white.svg`, `README.txt`).
- **Impact**: the first screen lacks the prepared visual identity and reads as
  an unfinished prototype.
- **Scope**: add the selected asset under `wwwroot`, keep an accessible text
  name, and verify focus, contrast, and sizing in the header.

### 4. Hero message and preview card compete for attention

- **Evidence**: the heading uses a large scale and narrow measure while the
  preview card uses a strong shadow and large score values
  (`wwwroot/css/site.css:115-119`, `wwwroot/css/site.css:179-185`,
  `wwwroot/css/site.css:236-300`).
- **Impact**: the user can split attention between the product promise and the
  illustrative result instead of immediately understanding the primary message.
- **Scope**: establish a clear primary hierarchy through scale, spacing, and
  preview emphasis without hiding the evidence that explains product value.

### 5. Process section lacks visual evidence of its output

- **Evidence**: the workflow section contains three text-only cards
  (`Pages/Index.cshtml:64-87`), while the hero preview already defines
  illustrative scores and factors (`Pages/Index.cshtml:23-62`).
- **Impact**: the user learns the sequence but does not see how the sequence
  produces an understandable result.
- **Scope**: add one lightweight, tokenized result visualization or connect the
  workflow to the existing illustrative ranking; preserve the educational
  disclaimer and explicit illustrative labeling.

## Code References

- `Pages/Index.cshtml:9-62` - Hero copy, CTAs, illustrative ranking, scores, and factors.
- `Pages/Index.cshtml:64-87` - Text-only three-step workflow section.
- `Pages/Index.cshtml:89-99` - Educational boundary card.
- `Pages/Shared/_Layout.cshtml:7-11` - Stylesheet link and text-only header brand.
- `wwwroot/css/site.css:1-27` - Color, spacing, radius, width, and typography tokens.
- `wwwroot/css/site.css:38-53` - Body minimum width and focus-visible contract.
- `wwwroot/css/site.css:92-193` - Hero, buttons, card, and flex-row layout rules.
- `wwwroot/css/site.css:313-325` - Responsive breakpoint and reduced-motion rules.
- `Program.cs:107` - Static asset mapping.
- `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs:14-41` - Existing public landing and CSS-serving contract tests.
- `docs/superpowers/specs/2026-10-12-landing-page-design.md:37-72` - Existing design and validation constraints.

## Architecture Insights

The landing page is a server-rendered Razor view with a shared layout and a
single ordinary CSS token source. The inspected implementation has no
component-library directory and no frontend screenshot runner, so the least
disruptive path is to extend the existing BEM-style classes, copy only the
required SVG into `wwwroot`, and use the existing contract-test project plus a
manual desktop/mobile screenshot gate.

The page is public and static. The inspected markup contains illustrative
market data and an educational boundary, not a live authenticated analysis
flow; the refinement should not turn the preview into a claim about current
market data (`Pages/Index.cshtml:23-62`, `Pages/Index.cshtml:89-99`).

## Historical Context (from prior changes)

- `docs/superpowers/specs/2026-10-12-landing-page-design.md:37-72` - The
  existing landing-page design established the midnight analytics direction,
  semantic token usage, accessible focus, reduced motion, illustrative data
  labeling, and manual desktop/mobile validation.
- `docs/superpowers/plans/2026-10-12-landing-page.md` - The prior
  implementation plan established the current Razor/CSS approach and excluded
  adding a frontend framework or screenshot-testing stack for the first version.
- `context/changes/bootstrap-verification/verification.md` and
  `context/changes/deployment/deployment-plan.md` - The inspected active change
  history contains bootstrap and deployment material; no prior landing-page
  research artifact was found.

## Related Research

No related `research.md` artifact was found under the inspected active change
folders or archive path.

## Open Questions

- Which exact desktop and mobile viewport dimensions will be used for the
  screenshot gate?
- Should the header use the white symbol plus text, or should a light-on-dark
  horizontal wordmark variant be approved before implementation?
- Should the process value proof reuse the existing ranking preview or introduce
  a second compact visualization?
- The reported clipping needs browser confirmation at the target viewport
  sizes; the source audit identifies likely causes but does not measure rendered
  pixels.
