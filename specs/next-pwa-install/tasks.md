# Tasks: Installable site (Android and iOS)

One task at a time, test first. Stop after each task. Update the `Session state` block in `spec.md` after closing each one.

- [x] **T01** `[A]` Write `HaveTickets.Api.Tests/PwaInstallTests.cs`.
  - **Covers**: REQ-TKT-016, REQ-TKT-017, REQ-TKT-018
  - **Done when**: the test fails on the current tree because the manifest, the icons and the tags are missing.
  - **Result**: three of the four tests failed: two with `FileNotFoundException` on
    `wwwroot/manifest.webmanifest`, the head-tag one with `should be ["manifest.webmanifest"] but was []`. The
    fourth pins a deploy step that already existed (`cp index.html 404.html`) and passed from the start.
- [x] **T02** `[A]` Render the install icons into `HaveTickets.WebUI/wwwroot/icons/`.
  - **Covers**: REQ-TKT-017, REQ-TKT-018
  - **Done when**: the four files exist at 192, 512, 512 and 180 px, the maskable and the Apple icon are opaque,
    and the mark of the maskable icon stays inside the safe zone.
  - **Result**: rendered with the command in `plan.md`. The maskable mark ends 35.5% of the width from the
    centre; the maskable and the Apple icon have no transparent pixel.
- [x] **T03** `[A]` Add `manifest.webmanifest` and the head tags in `index.html`.
  - **Covers**: REQ-TKT-016, REQ-TKT-018
  - **Done when**: `PwaInstallTests` passes and the commands of `ci.yml` pass.
  - **Result**: the build with `-warnaserror` is clean; 74 domain tests and 59 API tests pass (55 before, plus
    these four); the Vue client still builds with `npm ci` and `npm run build`.
- [x] **T04** `[A]` Publish as the deploy workflow does and check the published folder.
  - **Covers**: acceptance criteria
  - **Done when**: the install check reports no failure and Chrome reports no installability error.
  - **Result**: `dotnet publish` puts `manifest.webmanifest` and `icons/` in the published `wwwroot/`. With
    `404.html` copied from `index.html`, `verify-install.mjs` reports no failure for both documents. Headless
    Chrome reports no installability error on `/` and `/404.html`, nor on `/my-tickets` and `/movie/<id>` when a
    local server answers them with `404.html` and status 404, as GitHub Pages does; the manifest resolves to
    `/manifest.webmanifest` on every route.
- [x] **T05** `[A]` Add the requirements and the traceability rows to `docs/`.
  - **Covers**: REQ-TKT-016, REQ-TKT-017, REQ-TKT-018
  - **Done when**: `docs/10-requirements.md` and `docs/50-traceability.md` carry the three requirements in their
    existing format.
  - **Result**: a new "Installability Requirements" section and three rows; nothing renumbered.
- [ ] **T06** `[M]` Verify the live site after the deploy.
  - **Covers**: acceptance criteria
  - **Done when**: the live manifest answers 200 with a manifest media type and the owner has installed the
    site on Android and on iOS.
  - **Needs from owner**: merge the pull request, then install from Chrome on Android and from Safari on an
    iPhone and confirm icon, label and window.
- [x] **T07** `[H]` Decide the mark of the product.
  - **Covers**: edge cases (the favicon was the stock icon of the Blazor template)
  - **Done when**: the owner keeps that icon or chooses a HaveTickets mark; in the second case the four icons
    are rendered again with the command in `plan.md`.
  - **Result**: the owner chose an HT monogram on 2026-11-14. `wwwroot/favicon.svg` holds it, the page links
    it as its favicon, the four icons are rendered from it and the template icon is removed. The published
    folder passes the install check and Chrome reports no installability error with the new files.
