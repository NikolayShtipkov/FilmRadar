# Third-party notices

## TMDB

This product uses the TMDB API but is not endorsed or certified by TMDB.

Movie metadata and posters come from [TMDB](https://www.themoviedb.org/). Review the [API terms](https://www.themoviedb.org/api-terms-of-use) and [attribution guidelines](https://www.themoviedb.org/about/logos-attribution) before redistribution or public deployment. FilmRadar does not redistribute a bulk movie dataset.

`wwwroot/images/tmdb-logo.svg` is the unmodified TMDB logo by Travis Bell, obtained from [Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Tmdb.new.logo.svg), which identifies the original TMDB artwork as its source and lists [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). No changes to the artwork were made. TMDB retains its trademarks. This attribution applies to the logo, not to FilmRadar's source code.

## UI libraries

The local ASP.NET MVC template assets retain their upstream notices in `wwwroot/lib/`:

- Bootstrap, MIT license.
- jQuery, MIT license.
- jQuery Validation, MIT license.
- jQuery Validation Unobtrusive, MIT license.

The current interface loads Bootstrap CSS and FilmRadar's own CSS/JavaScript. It does not require a public CDN or JavaScript for core form submissions. Remaining template validation assets can support future client-side enhancements.

NuGet package licenses are supplied with each upstream package. Demo titles are synthetic fixtures, not TMDB movie records.
