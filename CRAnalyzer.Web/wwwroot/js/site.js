// ============================================================
// CR Analyzer — site.js
// ============================================================

(function () {
    'use strict';

    // ---------- Theme Toggle ----------
    const THEME_KEY = 'cra-theme';

    function getTheme() {
        return localStorage.getItem(THEME_KEY) ||
            (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        const icon = document.getElementById('themeIcon');
        if (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
        }
        localStorage.setItem(THEME_KEY, theme);
    }

    // Apply on load
    applyTheme(getTheme());

    document.addEventListener('DOMContentLoaded', function () {
        const toggle = document.getElementById('themeToggle');
        if (toggle) {
            toggle.addEventListener('click', () => {
                const current = document.documentElement.getAttribute('data-theme') || 'dark';
                applyTheme(current === 'dark' ? 'light' : 'dark');
            });
        }

        // ---------- Active Nav Link ----------
        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('.nav-link').forEach(link => {
            const href = link.getAttribute('href') || '';
            if (href !== '/' && path.startsWith(href.toLowerCase())) {
                link.classList.add('active');
            } else if (href === '/' && path === '/') {
                link.classList.add('active');
            }
        });

        // ---------- File Upload Zones ----------
        document.querySelectorAll('.file-upload-zone').forEach(zone => {
            const input = zone.querySelector('input[type="file"]');
            const filenameEl = zone.querySelector('.upload-filename');

            if (input) {
                input.addEventListener('change', () => {
                    if (input.files.length > 0 && filenameEl) {
                        filenameEl.textContent = input.files[0].name;
                        zone.style.borderColor = 'var(--color-accent)';
                    }
                });

                zone.addEventListener('dragover', (e) => {
                    e.preventDefault();
                    zone.classList.add('dragover');
                });

                zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));

                zone.addEventListener('drop', (e) => {
                    e.preventDefault();
                    zone.classList.remove('dragover');
                    if (e.dataTransfer.files.length > 0) {
                        input.files = e.dataTransfer.files;
                        if (filenameEl) filenameEl.textContent = e.dataTransfer.files[0].name;
                        zone.style.borderColor = 'var(--color-accent)';
                    }
                });
            }
        });

        // ---------- Loading Overlay on Form Submit ----------
        const analysisForm = document.getElementById('analysisForm');
        const overlay = document.getElementById('loadingOverlay');

        if (analysisForm && overlay) {
            analysisForm.addEventListener('submit', () => {
                overlay.classList.add('show');
                startLoadingMessages();
            });
        }

        // ---------- Custom Tabs ----------
        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const target = btn.dataset.tab;
                const container = btn.closest('[data-tabs]') || document.querySelector('[data-tabs]');

                document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
                document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));

                btn.classList.add('active');
                const pane = document.getElementById(target);
                if (pane) pane.classList.add('active');
            });
        });

        // ---------- Copy to Clipboard ----------
        document.querySelectorAll('[data-copy]').forEach(btn => {
            btn.addEventListener('click', () => {
                const target = document.getElementById(btn.dataset.copy);
                if (!target) return;

                navigator.clipboard.writeText(target.textContent).then(() => {
                    const original = btn.innerHTML;
                    btn.innerHTML = '<i class="bi bi-check2 me-1"></i>Copied!';
                    btn.style.color = 'var(--color-success)';
                    setTimeout(() => {
                        btn.innerHTML = original;
                        btn.style.color = '';
                    }, 2000);
                });
            });
        });

        // ---------- Count-up Animation ----------
        document.querySelectorAll('[data-countup]').forEach(el => {
            const target = parseInt(el.textContent, 10);
            if (isNaN(target) || target === 0) return;

            let start = 0;
            const duration = 800;
            const step = duration / target;
            const timer = setInterval(() => {
                start++;
                el.textContent = start;
                if (start >= target) clearInterval(timer);
            }, step);
        });
    });

    // ---------- Loading Messages ----------
    const loadingMessages = [
        { text: 'Parsing your document...', sub: 'Extracting text content' },
        { text: 'Scanning project structure...', sub: 'Walking directory tree' },
        { text: 'Sending to GPT-4o...', sub: 'AI is analyzing your change request' },
        { text: 'Processing AI response...', sub: 'Building impact analysis' },
        { text: 'Almost done...', sub: 'Saving results to database' }
    ];

    function startLoadingMessages() {
        const textEl = document.getElementById('loadingText');
        const subEl = document.getElementById('loadingSub');
        if (!textEl || !subEl) return;

        let i = 0;
        const interval = setInterval(() => {
            if (i < loadingMessages.length) {
                textEl.textContent = loadingMessages[i].text;
                subEl.textContent = loadingMessages[i].sub;
                i++;
            } else {
                clearInterval(interval);
            }
        }, 3500);
    }

})();
