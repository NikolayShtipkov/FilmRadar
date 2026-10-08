"""Build the Bulgarian defense slides. Optional tooling, not a web app dependency."""
import argparse
from pathlib import Path

from reportlab.lib.colors import HexColor
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--font', type=Path, default=Path('C:/Windows/Fonts/arial.ttf'))
parser.add_argument('--bold-font', type=Path, default=Path('C:/Windows/Fonts/arialbd.ttf'))
args = parser.parse_args()
pdfmetrics.registerFont(TTFont('FilmRadar', str(args.font)))
pdfmetrics.registerFont(TTFont('FilmRadarBold', str(args.bold_font)))
pdfmetrics.registerFontFamily('FilmRadar', normal='FilmRadar', bold='FilmRadarBold')

output = ROOT / 'output/pdf/FilmRadar-Presentation.pdf'
output.parent.mkdir(parents=True, exist_ok=True)
pdf = canvas.Canvas(str(output), pagesize=(960, 540))
pdf.setTitle('FilmRadar - Интелигентен филмов навигатор')
pdf.setAuthor('FilmRadar coursework')
background, foreground, accent, muted = [HexColor(c) for c in ['#11181c', '#f0f3ec', '#b4f4c9', '#adbcb9']]
body = ParagraphStyle('body', fontName='FilmRadar', fontSize=21, leading=31, textColor=foreground)
small = ParagraphStyle('small', parent=body, fontSize=15, leading=23, textColor=muted)

slides = [
    ('FilmRadar', [
        'Интелигентен филмов навигатор',
        'Курсова работа с ASP.NET Core MVC и .NET 10',
        'Личен списък и обясними препоръки за филми.',
    ], 'Демонстрация за 5-8 минути. Подробният сценарий е в docs/demo-script.md.'),
    ('Проблем и цел', [
        'Каталогът предлага много филми, но не пази личния контекст на избора.',
        'FilmRadar събира предпочитания, гледани филми и лични оценки.',
        'Потребителят вижда защо даден филм отговаря на вкуса му.',
    ], 'Резултатът 0-100 измерва съвпадение с правилата, а не обективно качество.'),
    ('Основни страници', [
        'Начало и каталог: търсене по заглавие или откриване с филтри.',
        'Детайли и личен списък: запазване, статус, бележка и оценка.',
        'Профил и препоръки: жанрове, период, рейтинг и настроение.',
        'За проекта: използвани технологии, алгоритъм и TMDB attribution.',
    ], 'Razor генерира HTML на сървъра. Основните форми работят без JavaScript.'),
    ('MVC архитектура', [
        '<b>Views</b> показват ViewModels и изпращат форми.',
        '<b>Controllers</b> проверяват входа и избират отговора.',
        '<b>Services</b> прилагат бизнес правилата и координират I/O.',
        '<b>EF Core / SQLite</b> пазят данните. <b>HttpClient</b> зарежда TMDB.',
    ], 'RecommendationService е async workflow. MockRecommendationEngine е чиста C# функция.'),
    ('Релационен модел', [
        '<b>UserProfile</b> пази името и началните критерии.',
        '<b>Genre</b> и <b>UserPreferredGenre</b> описват любимите жанрове.',
        '<b>SavedMovie</b> пази snapshot и личните данни.',
        '<b>SavedMovieGenre</b> свързва записаните филми с жанровете.',
    ], 'Миграции, foreign keys, unique index за profile + TMDB ID и check constraints.'),
    ('Лична оценка от 0 до 4', [
        '<b>0</b> За нищо не става &nbsp;&nbsp; <b>1</b> Зле &nbsp;&nbsp; <b>2</b> Става',
        '<b>3</b> Важи &nbsp;&nbsp; <b>4</b> Топ',
        '<b>null</b> означава „Не е оценен“. Нулата е реална оценка.',
        'Връщане към „За гледане“ изчиства оценката и датата след потвърждение.',
    ], '„Гледан“ има дата и незадължителна оценка. Любим и бележка са независими.'),
    ('TMDB и асинхронна обработка', [
        'Typed HttpClient използва Bearer token от User Secrets или environment.',
        '/search/movie търси заглавие. /discover/movie прилага филтри.',
        'async/await и CancellationToken преминават през всички I/O операции.',
        'Timeout, 401/403, 429 и невалиден JSON дават безопасни съобщения.',
    ], 'Източник: developer.themoviedb.org/reference/search-movie и /discover-movie'),
    ('Подготовка на препоръките', [
        'До 3 TMDB страници и максимум 60 уникални кандидата.',
        'Текущите жанрове ограничават Discover с OR условие.',
        'Една batch справка изключва гледаните и маркира запазените.',
        'Алгоритъмът подрежда кандидатите и връща до 10 резултата.',
    ], 'Няма отделна details заявка за всеки филм. Watchlist не получава scoring бонус.'),
    ('Mock AI формула', [
        'Любими жанрове <b>35</b> &nbsp;&nbsp; История на оценки <b>25</b>',
        'Настроение <b>15</b> &nbsp;&nbsp; TMDB <b>15</b> &nbsp;&nbsp; Период <b>10</b>',
        '<b>Score = 100 × sum(weight × value) / sum(active weights)</b>',
        'TMDB value = (rating / 10) × min(votes / 200, 1).',
    ], 'Активността е обща за заявката. Genre affinity идва от лични оценки / 4. Закръгляне само при показване.'),
    ('Обяснения и предвидимост', [
        'До четири положителни причини, подредени по принос.',
        'Жанр без история има affinity 0.5 без положително обяснение.',
        'При равни резултати: TMDB рейтинг, брой гласове, после ID.',
        'Еднакви входни данни дават еднакъв резултат.',
    ], 'Mock AI използва прозрачни правила. Приложението не обучава модел и не извиква LLM.'),
    ('Защита и устойчивост', [
        'Antiforgery за POST, Input ViewModels и server-side validation.',
        'Razor HTML encoding и параметризирани EF заявки.',
        'SQLite constraints защитават и директните записи.',
        'Профилът и личният списък работят без TMDB token.',
    ], 'Един локален профил без authentication. Приложението не е предназначено за публичен hosting.'),
    ('Тестове и демонстрация', [
        'xUnit: scoring fixtures, HTTP handler, SQLite и MVC integration тестове.',
        'Демо: жанрове, филм в списъка, оценка 0, любими и препоръки.',
        'Offline: шест измислени записа в отделна Development база.',
        'Преди защита: проверка на live token и визуален преглед в браузър.',
    ], 'Автоматизираните тестове не използват live TMDB. Резултатите и ограниченията са в docs/verification.md.'),
    ('Ограничения и развитие', [
        'Каталогът и новите препоръки зависят от TMDB и интернет.',
        'Snapshot данните не се обновяват автоматично.',
        'Следващи версии: Identity, повече профили, cache и реален AI.',
        'Текущият обхват остава подходящ за индивидуална курсова работа.',
    ], 'This product uses the TMDB API but is not endorsed or certified by TMDB.'),
]

for index, (title, paragraphs, footer) in enumerate(slides, start=1):
    pdf.setFillColor(background)
    pdf.rect(0, 0, 960, 540, fill=1, stroke=0)
    pdf.setFillColor(accent)
    pdf.setFont('FilmRadarBold', 48 if index == 1 else 34)
    pdf.drawString(56, 456, title)
    top = 385
    for content in paragraphs:
        paragraph = Paragraph(content, body)
        width, height = paragraph.wrap(848, 500)
        if top - height < 115:
            raise ValueError(f'Content exceeds slide {index}')
        paragraph.drawOn(pdf, 56, top - height)
        top -= height + 19
    paragraph = Paragraph(footer, small)
    width, height = paragraph.wrap(800, 70)
    paragraph.drawOn(pdf, 56, 43)
    pdf.setFont('FilmRadar', 12)
    pdf.setFillColor(muted)
    pdf.drawRightString(904, 28, f'{index:02d} / {len(slides):02d}')
    pdf.showPage()
pdf.save()
print(output)
