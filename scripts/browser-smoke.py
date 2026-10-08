"""Read-only desktop/mobile smoke checks against an already running FilmRadar demo."""
import argparse
from pathlib import Path
from urllib.parse import urlparse

from playwright.sync_api import sync_playwright

parser = argparse.ArgumentParser()
parser.add_argument('--base-url', default='http://localhost:5133')
parser.add_argument('--channel', default='msedge', help='msedge, chrome or chromium')
parser.add_argument('--output', type=Path, default=Path(__file__).resolve().parents[1] / 'docs/screenshots')
args = parser.parse_args()
if urlparse(args.base_url).hostname not in {'localhost', '127.0.0.1', '::1'}:
    raise SystemExit('This smoke script is intended for a local development server.')
args.output.mkdir(parents=True, exist_ok=True)

pages = [('/', 'home'), ('/Movies/Search', 'search'), ('/MyList', 'my-list'),
         ('/Profile/Edit', 'profile'), ('/Recommendations', 'recommendations'), ('/Home/About', 'about')]
with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel=args.channel, headless=True)
    for label, viewport in [('desktop', {'width': 1440, 'height': 1000}), ('mobile', {'width': 390, 'height': 844})]:
        page = browser.new_page(viewport=viewport)
        errors = []
        page.on('pageerror', lambda error: errors.append(str(error)))
        for route, name in pages:
            response = page.goto(args.base_url.rstrip('/') + route, wait_until='networkidle')
            assert response and response.status == 200, f'{route}: unexpected HTTP status'
            assert page.locator('h1').count() == 1, f'{route}: expected one main heading'
            assert page.evaluate('document.documentElement.scrollWidth <= innerWidth'), f'{route}: horizontal overflow'
            assert page.locator('img').evaluate_all('(images) => images.every(i => i.complete && i.naturalWidth > 0)'), f'{route}: broken image'
            page.screenshot(path=str(args.output / f'{name}-{label}.png'), full_page=True)
        page.goto(args.base_url.rstrip('/') + '/MyList', wait_until='networkidle')
        edit = page.get_by_role('link', name='Редактирай').first
        if edit.count():
            edit.click()
            page.wait_for_load_state('networkidle')
            assert page.locator('input[name="Input.PersonalRatingValue"]').count() == 6
            page.screenshot(path=str(args.output / f'rating-{label}.png'), full_page=True)
        assert not errors, errors
        page.close()
    browser.close()
print(f'Smoke checks passed. Screenshots: {args.output}')
