(function () {
    "use strict";

    function formatPoint(point) {
        var timestamp = point.getAttribute("data-chart-timestamp") || "";
        var date = timestamp.slice(0, 10);
        var close = point.getAttribute("data-chart-close") || "";
        return "Data: " + date + " · Cena zamknięcia: " + close;
    }

    function createController(container) {
        var svg = container.querySelector("[data-chart-svg]");
        var tooltip = container.querySelector("[data-chart-tooltip]");
        var points = Array.prototype.slice.call(container.querySelectorAll("[data-chart-point]"));

        if (!svg || !tooltip || points.length === 0) {
            return;
        }

        var activePoint = null;
        var pinned = false;

        function positionTooltip(point) {
            var containerRect = container.getBoundingClientRect();
            var pointRect = point.getBoundingClientRect();
            var tooltipWidth = tooltip.offsetWidth;
            var tooltipHeight = tooltip.offsetHeight;
            var left = pointRect.left - containerRect.left + (pointRect.width / 2) - (tooltipWidth / 2);
            var top = pointRect.top - containerRect.top - tooltipHeight - 8;
            var maxLeft = Math.max(8, containerRect.width - tooltipWidth - 8);

            if (top < 8) {
                top = pointRect.bottom - containerRect.top + 8;
            }

            tooltip.style.left = Math.max(8, Math.min(left, maxLeft)) + "px";
            tooltip.style.top = Math.max(8, top) + "px";
        }

        function clearActive() {
            if (activePoint) {
                activePoint.classList.remove("is-active");
                activePoint.removeAttribute("aria-describedby");
            }

            activePoint = null;
            tooltip.hidden = true;
            tooltip.textContent = "";
        }

        function showPoint(point, shouldPin) {
            if (activePoint && activePoint !== point) {
                activePoint.classList.remove("is-active");
                activePoint.removeAttribute("aria-describedby");
            }

            activePoint = point;
            pinned = shouldPin;
            point.classList.add("is-active");
            point.setAttribute("aria-describedby", tooltip.id || "");
            tooltip.textContent = formatPoint(point);
            tooltip.hidden = false;
            positionTooltip(point);
        }

        function dismissIfNotPinned() {
            if (!pinned) {
                clearActive();
            }
        }

        points.forEach(function (point) {
            point.addEventListener("pointerenter", function () {
                showPoint(point, false);
            });
            point.addEventListener("pointerleave", dismissIfNotPinned);
            point.addEventListener("focus", function () {
                showPoint(point, true);
            });
            point.addEventListener("blur", function () {
                pinned = false;
                window.setTimeout(function () {
                    if (document.activeElement !== point && !container.contains(document.activeElement)) {
                        clearActive();
                    }
                }, 0);
            });
            point.addEventListener("pointerdown", function () {
                showPoint(point, true);
            });
        });

        container.addEventListener("pointerleave", function () {
            if (!container.contains(document.activeElement)) {
                dismissIfNotPinned();
            }
        });
        container.addEventListener("pointerdown", function (event) {
            if (event.target === container || event.target === svg) {
                clearActive();
            }
        });
        container.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                pinned = false;
                clearActive();
            }
        });
        window.addEventListener("resize", function () {
            if (activePoint && !tooltip.hidden) {
                positionTooltip(activePoint);
            }
        });
    }

    function initialize() {
        document.querySelectorAll("[data-chart-container]").forEach(createController);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize);
    } else {
        initialize();
    }
})();
