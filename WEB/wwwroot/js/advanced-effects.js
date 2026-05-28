// ========================================
// ADVANCED JAVASCRIPT EFFECTS
// Hiệu ứng nâng cao cho UTE Rent
// ========================================

(function() {
    'use strict';

    // ========== PARTICLE BACKGROUND ==========
    class ParticleBackground {
        constructor(containerId) {
            this.container = document.getElementById(containerId);
            if (!this.container) return;
            
            this.canvas = document.createElement('canvas');
            this.ctx = this.canvas.getContext('2d');
            this.particles = [];
            this.particleCount = 50;
            
            this.init();
        }

        init() {
            this.canvas.style.position = 'absolute';
            this.canvas.style.top = '0';
            this.canvas.style.left = '0';
            this.canvas.style.width = '100%';
            this.canvas.style.height = '100%';
            this.canvas.style.pointerEvents = 'none';
            this.canvas.style.zIndex = '0';
            
            this.container.style.position = 'relative';
            this.container.insertBefore(this.canvas, this.container.firstChild);
            
            this.resize();
            this.createParticles();
            this.animate();
            
            window.addEventListener('resize', () => this.resize());
        }

        resize() {
            this.canvas.width = this.container.offsetWidth;
            this.canvas.height = this.container.offsetHeight;
        }

        createParticles() {
            for (let i = 0; i < this.particleCount; i++) {
                this.particles.push({
                    x: Math.random() * this.canvas.width,
                    y: Math.random() * this.canvas.height,
                    radius: Math.random() * 3 + 1,
                    vx: (Math.random() - 0.5) * 0.5,
                    vy: (Math.random() - 0.5) * 0.5,
                    opacity: Math.random() * 0.5 + 0.2
                });
            }
        }

        animate() {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
            
            this.particles.forEach(particle => {
                particle.x += particle.vx;
                particle.y += particle.vy;
                
                if (particle.x < 0 || particle.x > this.canvas.width) particle.vx *= -1;
                if (particle.y < 0 || particle.y > this.canvas.height) particle.vy *= -1;
                
                this.ctx.beginPath();
                this.ctx.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
                this.ctx.fillStyle = `rgba(37, 99, 235, ${particle.opacity})`;
                this.ctx.fill();
            });
            
            // Draw connections
            this.particles.forEach((p1, i) => {
                this.particles.slice(i + 1).forEach(p2 => {
                    const dx = p1.x - p2.x;
                    const dy = p1.y - p2.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    
                    if (distance < 100) {
                        this.ctx.beginPath();
                        this.ctx.moveTo(p1.x, p1.y);
                        this.ctx.lineTo(p2.x, p2.y);
                        this.ctx.strokeStyle = `rgba(37, 99, 235, ${0.2 * (1 - distance / 100)})`;
                        this.ctx.lineWidth = 1;
                        this.ctx.stroke();
                    }
                });
            });
            
            requestAnimationFrame(() => this.animate());
        }
    }

    // ========== MOUSE TRAIL EFFECT ==========
    class MouseTrail {
        constructor() {
            this.trail = [];
            this.maxTrail = 20;
            this.init();
        }

        init() {
            document.addEventListener('mousemove', (e) => {
                this.trail.push({
                    x: e.clientX,
                    y: e.clientY,
                    timestamp: Date.now()
                });
                
                if (this.trail.length > this.maxTrail) {
                    this.trail.shift();
                }
                
                this.draw();
            });
        }

        draw() {
            // Remove old trails
            document.querySelectorAll('.mouse-trail-dot').forEach(dot => {
                const age = Date.now() - parseInt(dot.dataset.timestamp);
                if (age > 1000) {
                    dot.remove();
                }
            });
            
            // Add new trail dot
            if (this.trail.length > 0) {
                const latest = this.trail[this.trail.length - 1];
                const dot = document.createElement('div');
                dot.className = 'mouse-trail-dot';
                dot.dataset.timestamp = latest.timestamp;
                dot.style.cssText = `
                    position: fixed;
                    left: ${latest.x}px;
                    top: ${latest.y}px;
                    width: 8px;
                    height: 8px;
                    background: rgba(37, 99, 235, 0.6);
                    border-radius: 50%;
                    pointer-events: none;
                    z-index: 9999;
                    animation: trailFade 1s ease-out forwards;
                `;
                document.body.appendChild(dot);
            }
        }
    }

    // ========== TYPING ANIMATION ==========
    class TypeWriter {
        constructor(element, texts, speed = 100, deleteSpeed = 50, pauseTime = 2000) {
            this.element = element;
            this.texts = texts;
            this.speed = speed;
            this.deleteSpeed = deleteSpeed;
            this.pauseTime = pauseTime;
            this.textIndex = 0;
            this.charIndex = 0;
            this.isDeleting = false;
            
            this.type();
        }

        type() {
            const currentText = this.texts[this.textIndex];
            
            if (this.isDeleting) {
                this.element.textContent = currentText.substring(0, this.charIndex - 1);
                this.charIndex--;
            } else {
                this.element.textContent = currentText.substring(0, this.charIndex + 1);
                this.charIndex++;
            }
            
            let timeout = this.isDeleting ? this.deleteSpeed : this.speed;
            
            if (!this.isDeleting && this.charIndex === currentText.length) {
                timeout = this.pauseTime;
                this.isDeleting = true;
            } else if (this.isDeleting && this.charIndex === 0) {
                this.isDeleting = false;
                this.textIndex = (this.textIndex + 1) % this.texts.length;
            }
            
            setTimeout(() => this.type(), timeout);
        }
    }

    // ========== IMAGE ZOOM ON HOVER ==========
    function initImageZoom() {
        document.querySelectorAll('.product-card-image img, .product-img').forEach(img => {
            // Đảm bảo ảnh đã load xong
            if (img.complete) {
                setupZoom(img);
            } else {
                img.addEventListener('load', function() {
                    setupZoom(this);
                });
            }
        });
        
        function setupZoom(img) {
            img.addEventListener('mouseenter', function() {
                this.style.transition = 'transform 0.5s ease';
                this.style.transform = 'scale(1.1)';
            });
            
            img.addEventListener('mouseleave', function() {
                this.style.transform = 'scale(1)';
            });
        }
    }

    // ========== PARALLAX SCROLL ==========
    function initParallax() {
        const parallaxElements = document.querySelectorAll('[data-parallax]');
        
        window.addEventListener('scroll', () => {
            parallaxElements.forEach(element => {
                const speed = parseFloat(element.dataset.parallax) || 0.5;
                const yPos = -(window.pageYOffset * speed);
                element.style.transform = `translateY(${yPos}px)`;
            });
        });
    }

    // ========== TILT EFFECT ==========
    function initTiltEffect() {
        // CHỈ áp dụng cho elements có class tilt-effect
        // KHÔNG tự động áp dụng cho tất cả product-card
        document.querySelectorAll('.tilt-effect').forEach(card => {
            card.addEventListener('mousemove', function(e) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                const centerX = rect.width / 2;
                const centerY = rect.height / 2;
                
                const rotateX = (y - centerY) / 10;
                const rotateY = (centerX - x) / 10;
                
                this.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.05, 1.05, 1.05)`;
            });
            
            card.addEventListener('mouseleave', function() {
                this.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) scale3d(1, 1, 1)';
            });
        });
    }

    // ========== MAGNETIC BUTTONS ==========
    function initMagneticButtons() {
        document.querySelectorAll('.btn-primary, .btn-hero-primary, .magnetic-btn').forEach(btn => {
            btn.addEventListener('mousemove', function(e) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left - rect.width / 2;
                const y = e.clientY - rect.top - rect.height / 2;
                
                this.style.transform = `translate(${x * 0.3}px, ${y * 0.3}px)`;
            });
            
            btn.addEventListener('mouseleave', function() {
                this.style.transform = 'translate(0, 0)';
            });
        });
    }

    // ========== SCROLL REVEAL ==========
    function initScrollReveal() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        });

        document.querySelectorAll('.reveal-on-scroll').forEach(el => {
            el.style.opacity = '0';
            el.style.transform = 'translateY(50px)';
            el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
            observer.observe(el);
        });
    }

    // ========== NUMBER COUNTER ==========
    function animateCounter(element) {
        const target = parseInt(element.dataset.count);
        const duration = parseInt(element.dataset.duration) || 2000;
        const start = 0;
        const increment = target / (duration / 16);
        let current = start;
        
        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                element.textContent = target.toLocaleString('vi-VN');
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(current).toLocaleString('vi-VN');
            }
        }, 16);
    }

    // ========== CURSOR FOLLOWER ==========
    class CursorFollower {
        constructor() {
            this.cursor = document.createElement('div');
            this.cursor.className = 'custom-cursor';
            this.cursor.style.cssText = `
                position: fixed;
                width: 20px;
                height: 20px;
                border: 2px solid #2563eb;
                border-radius: 50%;
                pointer-events: none;
                z-index: 99999;
                transition: transform 0.2s ease;
                mix-blend-mode: difference;
            `;
            document.body.appendChild(this.cursor);
            
            this.init();
        }

        init() {
            document.addEventListener('mousemove', (e) => {
                this.cursor.style.left = e.clientX - 10 + 'px';
                this.cursor.style.top = e.clientY - 10 + 'px';
            });
            
            document.querySelectorAll('a, button, .clickable').forEach(el => {
                el.addEventListener('mouseenter', () => {
                    this.cursor.style.transform = 'scale(2)';
                    this.cursor.style.borderColor = '#7c3aed';
                });
                
                el.addEventListener('mouseleave', () => {
                    this.cursor.style.transform = 'scale(1)';
                    this.cursor.style.borderColor = '#2563eb';
                });
            });
        }
    }

    // ========== SMOOTH SCROLL ==========
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function(e) {
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
    }

    // ========== LAZY LOAD IMAGES ==========
    function initLazyLoad() {
        // CHỈ lazy load những ảnh có attribute data-src
        // Không ảnh hưởng đến ảnh bình thường
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    if (img.dataset.src) {
                        img.src = img.dataset.src;
                        img.classList.add('loaded');
                    }
                    imageObserver.unobserve(img);
                }
            });
        });

        // CHỈ observe ảnh có data-src, không ảnh hưởng ảnh khác
        document.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // ========== PROGRESS BAR ==========
    function initProgressBar() {
        const progressBar = document.createElement('div');
        progressBar.className = 'scroll-progress';
        progressBar.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            height: 3px;
            background: linear-gradient(90deg, #2563eb 0%, #7c3aed 100%);
            z-index: 99999;
            transition: width 0.1s ease;
        `;
        document.body.appendChild(progressBar);
        
        window.addEventListener('scroll', () => {
            const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
            const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            const scrolled = (winScroll / height) * 100;
            progressBar.style.width = scrolled + '%';
        });
    }

    // ========== INITIALIZE ALL ==========
    document.addEventListener('DOMContentLoaded', function() {
        console.log('🚀 Advanced effects loading...');
        
        // Initialize effects
        initImageZoom();
        initParallax();
        initTiltEffect();
        initMagneticButtons();
        initScrollReveal();
        initSmoothScroll();
        initLazyLoad();
        initProgressBar();
        
        // Initialize particle background on hero sections
        if (document.querySelector('.hero-section')) {
            new ParticleBackground('hero-section');
        }
        
        // Initialize typing effect
        const typingElement = document.querySelector('[data-typewriter]');
        if (typingElement) {
            const texts = JSON.parse(typingElement.dataset.texts || '["UTE Rent"]');
            new TypeWriter(typingElement, texts);
        }
        
        // Initialize counters
        const counterObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateCounter(entry.target);
                    counterObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.5 });

        document.querySelectorAll('[data-count]').forEach(counter => {
            counterObserver.observe(counter);
        });
        
        // Optional: Custom cursor (uncomment if needed)
        // new CursorFollower();
        
        // Optional: Mouse trail (uncomment if needed)
        // new MouseTrail();
        
        console.log('✨ Advanced effects initialized!');
    });

    // Add CSS for revealed elements
    const style = document.createElement('style');
    style.textContent = `
        .revealed {
            opacity: 1 !important;
            transform: translateY(0) !important;
        }
        
        @keyframes trailFade {
            to {
                opacity: 0;
                transform: scale(0);
            }
        }
        
        .loaded {
            animation: fadeIn 0.5s ease-in;
        }
    `;
    document.head.appendChild(style);

    // Export to window
    window.advancedEffects = {
        ParticleBackground,
        TypeWriter,
        CursorFollower,
        MouseTrail
    };
})();
