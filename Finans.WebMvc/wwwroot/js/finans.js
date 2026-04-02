/* ========================================
   FINANS YÖNETİM SİSTEMİ - Utility JS
   ======================================== */

// Sidebar active link
document.addEventListener('DOMContentLoaded', function () {
    const currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.sidebar-nav a').forEach(function (link) {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase()) && href !== '/') {
            link.classList.add('active');
        } else if (href === '/' && currentPath === '/') {
            link.classList.add('active');
        }
    });

    // Mobile sidebar toggle
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('show');
        });
    }

    // Auto-hide alerts
    document.querySelectorAll('.alert-auto-hide').forEach(function (alert) {
        setTimeout(function () {
            alert.style.transition = 'opacity .5s';
            alert.style.opacity = '0';
            setTimeout(function () { alert.remove(); }, 500);
        }, 5000);
    });
});

// Select all / deselect all
function initCheckAll(checkAllId, checkClass) {
    var checkAll = document.getElementById(checkAllId);
    if (!checkAll) return;
    checkAll.addEventListener('change', function () {
        document.querySelectorAll('.' + checkClass).forEach(function (cb) {
            cb.checked = checkAll.checked;
        });
        updateSelectionCount(checkClass);
    });
    document.querySelectorAll('.' + checkClass).forEach(function (cb) {
        cb.addEventListener('change', function () {
            updateSelectionCount(checkClass);
        });
    });
}

function updateSelectionCount(checkClass) {
    var count = document.querySelectorAll('.' + checkClass + ':checked').length;
    var el = document.getElementById('selectionCount');
    if (el) el.textContent = count + ' kayıt seçildi';
}

// Confirm action
function confirmAction(message) {
    return confirm(message || 'Bu işlemi onaylıyor musunuz?');
}

// Format number (Turkish locale)
function formatNumber(n, decimals) {
    decimals = decimals !== undefined ? decimals : 2;
    return parseFloat(n).toLocaleString('tr-TR', {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    });
}

// Format date (Turkish)
function formatDate(dateStr) {
    if (!dateStr) return '-';
    var d = new Date(dateStr);
    return d.toLocaleDateString('tr-TR');
}

// POST link helper (for sidebar links that need POST)
document.addEventListener('click', function (e) {
    var target = e.target.closest('[data-method="post"]');
    if (target) {
        e.preventDefault();
        var href = target.getAttribute('href');
        if (!href) return;
        var form = document.createElement('form');
        form.method = 'POST';
        form.action = href;
        // CSRF token
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            var input = document.createElement('input');
            input.type = 'hidden';
            input.name = '__RequestVerificationToken';
            input.value = token.value;
            form.appendChild(input);
        }
        document.body.appendChild(form);
        form.submit();
    }
});

// Toast notification helper
function showToast(message, type) {
    type = type || 'info';
    var toast = document.createElement('div');
    toast.className = 'alert alert-' + type + ' alert-auto-hide';
    toast.style.cssText = 'position:fixed;top:70px;right:20px;z-index:9999;min-width:300px;box-shadow:0 4px 12px rgba(0,0,0,.15);';
    toast.innerHTML = '<i class="bi bi-info-circle"></i> ' + message;
    document.body.appendChild(toast);
    setTimeout(function () {
        toast.style.transition = 'opacity .5s';
        toast.style.opacity = '0';
        setTimeout(function () { toast.remove(); }, 500);
    }, 4000);
}
