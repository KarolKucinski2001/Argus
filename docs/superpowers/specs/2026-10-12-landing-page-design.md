# Argus Landing Page Design

## Goal

Improve the existing public landing page so a beginner understands Argus's
educational value before signing in. The page should show enough of the product
to establish trust without presenting a score as investment advice.

## Scope

This change covers the public landing page and the shared HTML shell only. It
does not implement market analysis, persistence, new API endpoints, or a
frontend framework.

## User experience

The page uses a dark, premium analytics direction selected during brainstorming.
The hero is a responsive two-column layout:

- Left: the headline “Zrozum rynek, zanim wybierzesz aktywa”, a short
  educational description, and the primary CTA “Poznaj sposób działania”.
- Right: a static, explicitly labelled example of an asset ranking with
  Opportunity Scores, contributing factors, and data provenance.

Below the hero, the page explains the workflow in three steps:

1. Choose assets.
2. Review current and historical data.
3. Understand the factors behind the result.

The page includes a visible educational-not-advice boundary and a secondary
sign-in/create-account link. Example values are illustrative and must not look
like live recommendations.

## Visual contract

`wwwroot/css/site.css` is the single source for design tokens and shared
landing-page styles. It defines semantic colors, typography, spacing, borders,
focus treatment, buttons, cards, and responsive breakpoints. Views must use
semantic classes rather than literal colors or one-off layout values.

`Pages/Shared/_Layout.cshtml` links the stylesheet and preserves the existing
Razor Pages routing. `Pages/Index.cshtml` owns only the landing-page structure
and content.

The design must support:

- desktop and one narrow mobile layout,
- visible keyboard focus,
- readable contrast,
- hover and reduced-motion behavior,
- a clear visual distinction between illustrative data and live analysis.

## States and behavior

The landing page is static. Loading, error, and empty data states do not apply
because it does not fetch analysis data. Interactive elements are links, so
their destinations must remain valid:

- the primary CTA scrolls to the workflow explanation,
- the account link points to `/account/signin`,
- the logo returns to `/`.

## Validation

Validate the Razor application with the existing build and authentication
contract test commands. Perform a manual visual check at desktop and mobile
widths, including keyboard navigation and both primary links. Add or update a
focused UI/screenshot check only if the repository's existing test setup
supports it without introducing a new frontend testing stack.

## Out of scope

- real market-data calls,
- scoring implementation,
- AI summaries,
- account or authentication changes,
- Tailwind, shadcn, or another UI framework,
- a full product rebrand,
- dashboard or analysis-page work.
