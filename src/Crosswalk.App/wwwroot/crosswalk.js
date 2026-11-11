// Crosswalk JS interop
window.crosswalk = {
    focusElement: function (selector) {
        const el = document.querySelector(selector);
        if (el) el.focus();
    },
    scrollToTop: function () {
        window.scrollTo({ top: 0 });
    },
    getScrollPosition: function () {
        return window.scrollY;
    },
    setScrollPosition: function (pos) {
        window.scrollTo({ top: pos });
    },
    registerSlashFocus: function () {
        document.addEventListener('keydown', function (e) {
            if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey) {
                var active = document.activeElement;
                var tag = active ? active.tagName : '';
                if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
                e.preventDefault();
                var search = document.getElementById('search-input');
                if (search) search.focus();
            }
        });
    },
    _drawerRef: null,
    _drawerHandler: null,
    registerDrawerKeys: function (dotNetRef) {
        this._drawerRef = dotNetRef;
        if (this._drawerHandler) {
            document.removeEventListener('keydown', this._drawerHandler);
        }
        this._drawerHandler = function (e) {
            if (e.key === 'Escape' || e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
                var active = document.activeElement;
                var tag = active ? active.tagName : '';
                if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
                e.preventDefault();
                if (window.crosswalk._drawerRef) {
                    window.crosswalk._drawerRef.invokeMethodAsync('OnKeyFromJs', e.key);
                }
            }
        };
        document.addEventListener('keydown', this._drawerHandler);
    },
    unregisterDrawerKeys: function () {
        if (this._drawerHandler) {
            document.removeEventListener('keydown', this._drawerHandler);
            this._drawerHandler = null;
        }
        this._drawerRef = null;
    }
};
