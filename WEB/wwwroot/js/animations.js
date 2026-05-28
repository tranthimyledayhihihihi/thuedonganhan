// ========================================
// JAVASCRIPT CHO HIỆU ỨNG ANIMATIONS
// ========================================

document.addEventListener('DOMContentLoaded', function () {

    // ========== ANIMATE ON SCROLL ==========
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.animate-on-scroll').forEach(el => {
        observer.observe(el);
    });

    // ========== PRODUCT CARD ANIMATIONS ==========
    const productCards = document.querySelectorAll('.product-card.animate-on-scroll, .product-card-modern.animate-on-scroll, .product-card-home.animate-on-scroll');

    productCards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;

        card.addEventListener('mousemove', function (e) {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            const centerX = rect.width / 2;
            const centerY = rect.height / 2;

            const rotateX = (y - centerY) / 20;
            const rotateY = (centerX - x) / 20;

            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateY(-8px)`;
        });

        card.addEventListener('mouseleave', function () {
            card.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) translateY(0)';
        });
    });

    // ========== BUTTON RIPPLE EFFECT ==========
    const buttons = document.querySelectorAll('.btn-primary, .btn-hero-primary, .btn-rent, .btn-view');

    buttons.forEach(button => {
        button.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = button.getBoundingClientRect();

            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';
            ripple.classList.add('ripple');

            button.appendChild(ripple);

            setTimeout(() => ripple.remove(), 600);
        });
    });

    // ========== SMOOTH SCROLL ==========
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // ========== NAVBAR SCROLL EFFECT ==========
    let lastScroll = 0;
    const navbar = document.querySelector('.modern-header');

    if (navbar) {
        window.addEventListener('scroll', function () {
            const currentScroll = window.pageYOffset;

            if (currentScroll > 100) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }

            if (currentScroll > lastScroll && currentScroll > 200) {
                navbar.classList.add('scroll-down');
                navbar.classList.remove('scroll-up');
            } else {
                navbar.classList.remove('scroll-down');
                navbar.classList.add('scroll-up');
            }

            lastScroll = currentScroll;
        });
    }

    // ========== SCROLL TO TOP BUTTON ==========
    const scrollTopBtn = document.querySelector('.scroll-to-top');

    if (scrollTopBtn) {
        window.addEventListener('scroll', function () {
            if (window.pageYOffset > 300) {
                scrollTopBtn.classList.add('visible');
            } else {
                scrollTopBtn.classList.remove('visible');
            }
        });

        scrollTopBtn.addEventListener('click', function () {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }

    // ========== COUNTER ANIMATION ==========
    function animateCounter(element, target, duration = 2000) {
        const start = 0;
        const increment = target / (duration / 16);
        let current = start;

        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                element.textContent = target.toLocaleString();
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(current).toLocaleString();
            }
        }, 16);
    }

    const counters = document.querySelectorAll('[data-counter]');
    const counterObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const target = parseInt(entry.target.dataset.counter);
                animateCounter(entry.target, target);
                counterObserver.unobserve(entry.target);
            }
        });
    }, { threshold: 0.5 });

    counters.forEach(counter => counterObserver.observe(counter));

    // ========== PARALLAX EFFECT ==========
    const parallaxElements = document.querySelectorAll('[data-parallax]');

    window.addEventListener('scroll', function () {
        parallaxElements.forEach(element => {
            const speed = element.dataset.parallax || 0.5;
            const yPos = -(window.pageYOffset * speed);
            element.style.transform = `translateY(${yPos}px)`;
        });
    });

    // ========== TYPING EFFECT ==========
    function typeWriter(element, text, speed = 100) {
        let i = 0;
        element.textContent = '';

        function type() {
            if (i < text.length) {
                element.textContent += text.charAt(i);
                i++;
                setTimeout(type, speed);
            }
        }

        type();
    }

    document.querySelectorAll('[data-typing]').forEach(element => {
        const text = element.textContent;
        const speed = parseInt(element.dataset.typingSpeed) || 100;

        const typingObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    typeWriter(entry.target, text, speed);
                    typingObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.5 });

        typingObserver.observe(element);
    });

    // ========== IMAGE LAZY LOADING WITH FADE ==========
    const images = document.querySelectorAll('img[data-src]');

    const imageObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.add('fade-in');
                imageObserver.unobserve(img);
            }
        });
    });

    images.forEach(img => imageObserver.observe(img));

    // ========== STAGGER ANIMATION ==========
    function staggerAnimation(selector, delay = 100) {
        const elements = document.querySelectorAll(selector);
        elements.forEach((el, index) => {
            if (!el.classList.contains('animate-on-scroll')) {
                el.style.animationDelay = `${index * delay}ms`;
                el.classList.add('animate-on-scroll');
            }
        });
    }

    // ========== CONFETTI EFFECT ==========
    function createConfetti() {
        const colors = ['#2563eb', '#7c3aed', '#ec4899', '#f59e0b'];
        const confettiCount = 50;

        for (let i = 0; i < confettiCount; i++) {
            const confetti = document.createElement('div');
            confetti.className = 'confetti';
            confetti.style.left = Math.random() * 100 + '%';
            confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
            confetti.style.animationDelay = Math.random() * 3 + 's';
            confetti.style.animationDuration = (Math.random() * 3 + 2) + 's';

            document.body.appendChild(confetti);

            setTimeout(() => confetti.remove(), 5000);
        }
    }

    window.triggerConfetti = createConfetti;

    // ========== TOAST NOTIFICATION ==========
    function showToast(message, type = 'success', duration = 3000) {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type} notification-enter`;
        toast.textContent = message;

        toast.style.cssText = `
            position: fixed;
            top: 100px;
            right: 20px;
            padding: 1rem 1.5rem;
            background: ${type === 'success' ? '#10b981' : '#ef4444'};
            color: white;
            border-radius: 12px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
            z-index: 9999;
            font-weight: 600;
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'slideInNotification 0.3s ease-out reverse';
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }

    // ✅ Gán vào window để các file khác có thể gọi được
    window.showToast = showToast;

    // ========== LOADING OVERLAY ==========
    function showLoading() {
        const overlay = document.createElement('div');
        overlay.id = 'loading-overlay';
        overlay.innerHTML = `
            <div class="spinner"></div>
            <p style="color: white; margin-top: 1rem; font-weight: 600;">Đang tải...</p>
        `;
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.7);
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            z-index: 99999;
            backdrop-filter: blur(5px);
        `;
        document.body.appendChild(overlay);
    }

    function hideLoading() {
        const overlay = document.getElementById('loading-overlay');
        if (overlay) {
            overlay.style.opacity = '0';
            setTimeout(() => overlay.remove(), 300);
        }
    }

    // ✅ Gán vào window để các file khác có thể gọi được
    window.showLoading = showLoading;
    window.hideLoading = hideLoading;

    // ========== FORM VALIDATION ANIMATION ==========
    const forms = document.querySelectorAll('form');

    forms.forEach(form => {
        const inputs = form.querySelectorAll('input, textarea, select');

        inputs.forEach(input => {
            input.addEventListener('invalid', function (e) {
                e.preventDefault();
                this.classList.add('shake');
                setTimeout(() => this.classList.remove('shake'), 500);
            });

            input.addEventListener('input', function () {
                if (this.validity.valid) {
                    this.style.borderColor = '#10b981';
                } else {
                    this.style.borderColor = '#ef4444';
                }
            });
        });
    });

    // ========== FAVORITE BUTTON ANIMATION ==========
    const favoriteButtons = document.querySelectorAll('.product-favorite, .btn-favorite');

    favoriteButtons.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            this.classList.toggle('active');
            this.classList.add('heartbeat');

            const icon = this.querySelector('i') || this;
            if (this.classList.contains('active')) {
                icon.textContent = '❤️';
                icon.style.color = '#ef4444';
            } else {
                icon.textContent = '♡';
                icon.style.color = '';
            }

            setTimeout(() => this.classList.remove('heartbeat'), 1500);
        });
    });

    // ========== SEARCH BAR ANIMATION ==========
    const searchInputs = document.querySelectorAll('.search-input, .search-input-home');

    searchInputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'scale(1.02)';
            this.parentElement.style.boxShadow = '0 10px 30px rgba(37, 99, 235, 0.2)';
        });

        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'scale(1)';
            this.parentElement.style.boxShadow = '';
        });
    });

    // ========== CATEGORY CARD ANIMATIONS ==========
    const categoryCards = document.querySelectorAll('.category-card-img');

    categoryCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(30px)';
        card.style.transition = 'all 0.6s ease';
        card.style.transitionDelay = `${index * 0.1}s`;

        const cardObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                    cardObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        cardObserver.observe(card);

        const img = card.querySelector('img');
        if (img) {
            card.addEventListener('mouseenter', function () {
                img.style.transform = 'scale(1.1)';
                img.style.transition = 'transform 0.5s ease';
            });

            card.addEventListener('mouseleave', function () {
                img.style.transform = 'scale(1)';
            });
        }

        const overlay = card.querySelector('.category-overlay');
        if (overlay) {
            card.addEventListener('mouseenter', function () {
                overlay.style.background = 'linear-gradient(to top, rgba(0,0,0,0.9) 0%, rgba(0,0,0,0.3) 100%)';
                overlay.style.transition = 'background 0.3s ease';
            });

            card.addEventListener('mouseleave', function () {
                overlay.style.background = '';
            });
        }
    });

    // ========== FOOTER ANIMATIONS ==========
    const footer = document.querySelector('.simple-footer');

    if (footer) {
        const footerColumns = footer.querySelectorAll('.simple-footer-column');

        const footerObserver = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    setTimeout(() => {
                        entry.target.style.opacity = '1';
                        entry.target.style.transform = 'translateY(0)';
                    }, index * 100);
                    footerObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        footerColumns.forEach((column) => {
            column.style.opacity = '0';
            column.style.transform = 'translateY(30px)';
            column.style.transition = 'all 0.6s ease';
            footerObserver.observe(column);
        });

        const socialLinks = footer.querySelectorAll('.simple-social-link');
        socialLinks.forEach(link => {
            link.addEventListener('mouseenter', function () {
                this.style.transform = 'translateY(-5px) scale(1.1)';
            });
            link.addEventListener('mouseleave', function () {
                this.style.transform = 'translateY(0) scale(1)';
            });
        });

        const footerLinks = footer.querySelectorAll('.simple-footer-links a');
        footerLinks.forEach(link => {
            link.addEventListener('mouseenter', function () {
                this.style.paddingLeft = '0.5rem';
                this.style.transition = 'all 0.3s ease';
            });
            link.addEventListener('mouseleave', function () {
                this.style.paddingLeft = '0';
            });
        });
    }

    // ========== INITIALIZE ==========
    console.log('🎨 Animations initialized!');

    // Add CSS for ripple effect
    const style = document.createElement('style');
    style.textContent = `
        .ripple {
            position: absolute;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.6);
            transform: scale(0);
            animation: ripple-animation 0.6s ease-out;
            pointer-events: none;
        }
        
        @keyframes ripple-animation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }
        
        .fade-in {
            animation: fadeIn 0.5s ease-in;
        }
    `;
    document.head.appendChild(style);

    // ✅ window.animationUtils đặt TRONG DOMContentLoaded
    //    để showToast, showLoading, hideLoading đã được khai báo
    window.animationUtils = {
        showToast,
        showLoading,
        hideLoading,
        triggerConfetti: createConfetti
    };

}); // ← Đóng DOMContentLoaded — KHÔNG có gì bên ngoài này
