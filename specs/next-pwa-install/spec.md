# Specification: Installable site (Android and iOS)

Status: Implemented on `feat/pwa-install-tickets`, not merged

## Objective

A visitor on a phone can install `tickets.naindev.com` from the browser and open it like an app: its own icon
and name on the home screen and a window with no browser bar. Android Chrome and iOS Safari are the targets.

The same change lands in every public product site of the ecosystem (`*.naindev.com`). The portfolio
repository (`Nain9Dev/nain9dev.github.io`, `specs/next-pwa-install`) holds the reference spec and the audit of
2026-11-13; this spec covers the one site this repository publishes: the Blazor WebAssembly shell in
`HaveTickets.WebUI/wwwroot/`, which `.github/workflows/deploy.yml` publishes to GitHub Pages.

## Shared contract

Criteria as verified for the reference spec on 2026-11-13 (web.dev "install criteria", MDN "Making PWAs
installable", WebKit "Web Push for Web Apps on iOS and iPadOS"):

- Chrome on Android offers the install when the page is served over HTTPS and links a manifest with `name` or
  `short_name`, a 192 px and a 512 px icon, `start_url`, and `display` set to `standalone`, `fullscreen` or
  `minimal-ui`. A service worker is not a requirement.
- iOS opens the site as an app when the manifest `display` is `standalone`. It takes the icon from
  `apple-touch-icon` before the manifest icons and the label from `apple-mobile-web-app-title`.
- A maskable icon keeps its mark inside a centred circle whose radius is 40% of the icon width. Icons with
  purpose `any` and `maskable` are separate files.

Names approved by the owner on 2026-11-13: `short_name` is `HaveTickets`, the label under the icon (11
characters, within the 12 that no launcher truncates), and `name` is `HaveTickets`, what the install dialog
shows.

## Functional requirements (EARS)

- **REQ-TKT-016**: The site root shall serve `manifest.webmanifest` as valid JSON that declares `id`,
  `start_url` and `scope` as `./`, `display` as `standalone`, `name` and `short_name` as `HaveTickets`, `lang`
  as the language of the entry page (`en`), `dir` as `ltr`, the meta description of the entry page as
  `description`, and `theme_color` and `background_color` as `#0a0a0f`. It shall not declare
  `prefer_related_applications`.
- **REQ-TKT-017**: The manifest shall declare three PNG icons that exist under `icons/` at the declared size,
  one purpose per entry: 192 px and 512 px with purpose `any`, and 512 px with purpose `maskable`.
- **REQ-TKT-018**: Every HTML document the site serves (the entry page and the `404.html` single-page
  fallback, which the deploy workflow copies from it) shall link the manifest and a 180 px `apple-touch-icon`,
  and shall declare `mobile-web-app-capable` and `apple-mobile-web-app-capable` as `yes`,
  `apple-mobile-web-app-title` equal to the manifest `short_name`, `apple-mobile-web-app-status-bar-style` as
  `black`, and `theme-color` equal to the manifest `theme_color`. Both links shall resolve from the site root
  on every route.

## Edge cases and error handling

- GitHub Pages answers a deep route (`/my-tickets`, `/movie/<id>`) with `404.html`. The tags sit after
  `<base href="/">`, so the relative manifest and icon links resolve from the root there as well.
- `#0a0a0f` is the page background (`--bg-color` in `css/app.css`), the first colour the site paints. The page
  had no `theme-color` meta; it gets one with the same value, so the splash screen, the status bar and the
  page agree.
- Neither the page nor GitHub Pages sets a Content-Security-Policy, so nothing blocks a same-origin manifest.
- `dotnet publish` copies everything under `wwwroot/` to the published root and the deploy workflow uploads
  that folder whole: there is no copy list to extend for the manifest or the icons.
- Offline: the site registers no service worker, so an installed window without network shows the browser's
  own offline screen and no stale build can be served. `OfflineFallbackHandler` is unrelated: it is an HTTP
  handler inside the WebAssembly app that answers with sample data when no API is reachable, not a service
  worker.
- HaveTickets had no mark of its own: its favicon was the stock icon of the Blazor WebAssembly project
  template. The owner chose an HT monogram in the colours of the page on 2026-11-14; it is the favicon
  (`favicon.svg`) and the source of the install icons. A definitive logo replaces it by rendering the four
  files under `icons/` again with the command in `plan.md`; the manifest, the head tags and the test stay as
  they are.

## Out of scope

- A service worker, offline pages or any cache (owner decision of 2026-11-13: installable only, in this round).
- An in-page install button or iOS instructions; the browser's own install entry is the way in.
- Screenshots for the richer Android install dialog.
- A store package (Trusted Web Activity or a native shell).
- The Vue client in `HaveTickets.Client/`: CI builds it, but no workflow publishes it, so there is no second
  site to install.
- A definitive product logo: the monogram is a placeholder the owner approved.

## Acceptance criteria

- `dotnet test HaveTickets.Api.Tests/HaveTickets.Api.Tests.csproj -c Release --no-build`, the command
  `ci.yml` runs, passes with `PwaInstallTests`.
- The folder `dotnet publish` writes, with `404.html` copied as the deploy workflow does, passes the shared
  install check (manifest members, icon sizes, maskable safe zone, opaque Apple icon, head tags of both
  documents), and Chrome reports no installability error for it (DevTools protocol
  `Page.getInstallabilityErrors` on a local copy).
- After the deploy, the live manifest answers 200 with a manifest media type, and the owner installs the site
  on an Android phone and on an iPhone: the icon is not cropped, the label reads `HaveTickets` and the window
  opens with no browser bar.

## Session state

<!-- Session handoff: keep current. What is done, what is verified, what is next, what is blocked. -->
- Last updated: 2026-11-14, session pwa-install
- Done: T01 to T05 and T07. The site registered no service worker and shipped no manifest before this change.
- Verified: the commands of `ci.yml` pass locally (build with `-warnaserror`, 74 domain tests, 59 API tests,
  the Vue client build); the published folder passes the shared install check for `index.html` and
  `404.html`; Chrome reports no installability error on `/`, `/404.html` and two deep routes answered through
  the fallback.
- Next: T06 after the merge (live manifest media type, install on Android and iOS).
- Blocked: nothing. T06 needs the owner's phones.
