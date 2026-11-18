//document.querySelector('.switch-container').addEventListener('click', function () {
//    this.classList.toggle('active');
//    if (this.classList.contains('active')) {
//        // переключено на доставку
//        // здесь можно изменить текст на "Доставка"
//    } else {
//        // переключено на самовывоз
//        // здесь можно изменить текст на "Самовывоз"
//    }
//});

function transliterate(str) {
    var rusToEng = {
        'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd', 'е': 'e', 'ё': 'yo',
        'ж': 'zh', 'з': 'z', 'и': 'i', 'й': 'y', 'к': 'k', 'л': 'l', 'м': 'm',
        'н': 'n', 'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't', 'у': 'u',
        'ф': 'f', 'х': 'h', 'ц': 'c', 'ч': 'ch', 'ш': 'sh', 'щ': 'sch', 'ъ': '',
        'ы': 'y', 'ь': '', 'э': 'e', 'ю': 'yu', 'я': 'ya',
        'А': 'A', 'Б': 'B', 'В': 'V', 'Г': 'G', 'Д': 'D', 'Е': 'E', 'Ё': 'Yo',
        'Ж': 'Zh', 'З': 'Z', 'И': 'I', 'Й': 'Y', 'К': 'K', 'Л': 'L', 'М': 'M',
        'Н': 'N', 'О': 'O', 'П': 'P', 'Р': 'R', 'С': 'S', 'Т': 'T', 'У': 'U',
        'Ф': 'F', 'Х': 'H', 'Ц': 'C', 'Ч': 'Ch', 'Ш': 'Sh', 'Щ': 'Sch', 'Ъ': '',
        'Ы': 'Y', 'Ь': '', 'Э': 'E', 'Ю': 'Yu', 'Я': 'Ya'
    };

    return str.replace(/[а-яА-ЯёЁ]/g, function (char) {
        return rusToEng[char] || char;
    });
}

document.addEventListener('DOMContentLoaded', function () {
    var categoryTitles = document.querySelectorAll('.product-selection-title');

    categoryTitles.forEach(function (title) {
        var russianTitle = title.textContent.trim();
        var transliteratedTitle = transliterate(russianTitle);
        title.id = transliteratedTitle.toLowerCase().replace(/\s+/g, '-');
    });
});

// Прокрутка списка фильтров мышью
var filtersScroll = document.getElementById('filters-scroll');

let isDown = false;
let startX;
let scrollLeft;

filtersScroll.addEventListener('mousedown', (e) => {
    isDown = true;
    filtersScroll.classList.add('active');
    startX = e.pageX - filtersScroll.offsetLeft;
    scrollLeft = filtersScroll.scrollLeft;
    e.preventDefault(); // Предотвращаем стандартное поведение ссылки
});

filtersScroll.addEventListener('mouseleave', () => {
    isDown = false;
    filtersScroll.classList.remove('active');
});

filtersScroll.addEventListener('mouseup', () => {
    isDown = false;
    filtersScroll.classList.remove('active');
});

filtersScroll.addEventListener('mousemove', (e) => {
    if (!isDown) return;
    e.preventDefault();
    const x = e.pageX - filtersScroll.offsetLeft;
    const walk = (x - startX)/* * 3*/; // Умножение для ускорения прокрутки
    filtersScroll.scrollLeft = scrollLeft - walk;
});



function scrollToCategory(category) {
    var element = document.getElementById(category);
    if (element) {
        var offset = element.getBoundingClientRect().top + window.pageYOffset - document.getElementById('filters-container').offsetHeight;
        window.scrollTo({
            top: offset,
            behavior: 'smooth'
        });

        // Удаляем класс "active" у всех категорий
        document.querySelectorAll('.category').forEach(link => {
            link.classList.remove('active');
        });

        // Добавляем класс "active" категории, к которой прокручиваем
        var categoryLink = document.querySelector(`.category[data-category="${category}"]`);
        if (categoryLink) {
            categoryLink.classList.add('active');

            //Добавил
            // Прокрутка контейнера фильтров до активной категории
            var categoryRect = categoryLink.getBoundingClientRect();
            var scrollAmount = categoryRect.left - filtersScroll.getBoundingClientRect().left - filtersScroll.clientWidth / 2 + categoryLink.clientWidth / 2;
            filtersScroll.scrollTo({
                left: filtersScroll.scrollLeft + scrollAmount,
                behavior: 'smooth'
            });
        }
    }
}

function highlightVisibleCategory() {
    var categories = document.querySelectorAll('.div-category');
    var visibleCategory = null;

    categories.forEach(function (category) {
        var sectionId = category.querySelector('h2').id;
        var categoryLink = document.querySelector('.category[data-category="' + sectionId + '"]');
        var rect = category.getBoundingClientRect();

        if (rect.top <= window.innerHeight / 2 && rect.bottom >= window.innerHeight / 2) {
            visibleCategory = categoryLink;
            return; // Прерываем выполнение forEach после нахождения видимой категории
        }
    });

    // Удаляем класс "active" у всех категорий
    document.querySelectorAll('.category').forEach(link => {
        link.classList.remove('active');
    });

    // Добавляем класс "active" только к текущей видимой категории, если она не активна
    if (visibleCategory) {
        visibleCategory.classList.add('active');

        //Добавил
        // Прокрутка контейнера фильтров до видимой категории
        var categoryRect = visibleCategory.getBoundingClientRect();
        var scrollAmount = categoryRect.left - filtersScroll.getBoundingClientRect().left - filtersScroll.clientWidth / 2 + visibleCategory.clientWidth / 2;
        filtersScroll.scrollTo({
            left: filtersScroll.scrollLeft + scrollAmount,
            behavior: 'smooth'
        });
    }
}

// Вызываем функцию для выделения текущей видимой категории при загрузке страницы и при каждой прокрутке
highlightVisibleCategory();
window.addEventListener('scroll', highlightVisibleCategory);

var filtersContainer = document.getElementById('filters-container');
var topOffset = filtersContainer.offsetTop; // Получаем верхний отступ блока от верха страницы

// Добавляем класс "sticky" к контейнеру фильтров при прокрутке вниз и убираем его при достижении верхней части страницы
window.addEventListener('scroll', function () {
    var scrollPosition = window.scrollY || window.pageYOffset; // Текущая позиция прокрутки

    if (scrollPosition >= topOffset) {
        filtersContainer.classList.add('sticky');
    } else {
        filtersContainer.classList.remove('sticky');
    }
});

// Создаем IntersectionObserver для отслеживания текущей видимой категории
var observerOptions = {
    root: null,
    rootMargin: '0px',
    threshold: 0.5
};

var observerCallback = function (entries, observer) {
    entries.forEach(function (entry) {
        var category = entry.target.closest('.div-selection-title');
        if (category) {
            var categoryId = category.querySelector('h2').id;
            var categoryLink = document.querySelector(`.category[data-category="${categoryId}"]`);
            if (entry.isIntersecting && categoryLink) {
                // Если плитка товара видна, добавляем класс "active" к соответствующей категории в фильтре
                if (!categoryLink.classList.contains('active')) {
                    categoryLink.classList.add('active');
                }
            } else {
                // В противном случае удаляем класс "active" только у категории, которая перестала быть видимой
                if (categoryLink.classList.contains('active')) {
                    categoryLink.classList.remove('active');
                }
            }
        }
    });
};

var observer = new IntersectionObserver(observerCallback, observerOptions);
var productTiles = document.querySelectorAll('.product-tile');
productTiles.forEach(function (tile) {
    observer.observe(tile);
});

function getScrollbarWidth() {
    var outer = document.createElement('div');
    outer.style.visibility = 'hidden';
    outer.style.overflow = 'scroll';
    document.body.appendChild(outer);

    var inner = document.createElement('div');
    outer.appendChild(inner);

    var scrollbarWidth = outer.offsetWidth - inner.offsetWidth;
    outer.parentNode.removeChild(outer);

    return scrollbarWidth;
}
