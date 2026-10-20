(function () {
    "use strict";

    function formatPoint(point) {
        var timestamp = point.getAttribute("data-chart-timestamp") || "";
        var date = timestamp.slice(0, 10);
        var close = point.getAttribute("data-chart-close") || "";
        return "Data: " + date + " · Cena zamknięcia: " + close;
    }

    var RANGE_DAYS = {
        "1M": 31,
        "3M": 92,
        "6M": 183,
        "1Y": 365,
        "Max": null
    };

    // .NET "O" round-trip timestamps can carry up to 7 fractional-second
    // digits and may omit an explicit UTC offset. `new Date(...)` handles
    // neither case reliably across engines, so normalize to a 3-digit
    // fraction with an explicit offset (defaulting to UTC) before parsing.
    // Malformed input falls through to the native parser, which yields an
    // Invalid Date and preserves the existing fallback behavior.
    var TIMESTAMP_PATTERN = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2}:\d{2})(?:\.(\d+))?(Z|[+-]\d{2}:\d{2})?$/;

    function parseTimestamp(value) {
        var text = typeof value === "string" ? value : "";
        var match = TIMESTAMP_PATTERN.exec(text);

        if (!match) {
            return new Date(text);
        }

        var fraction = (match[3] || "").slice(0, 3);
        while (fraction.length < 3) {
            fraction += "0";
        }

        var offset = match[4] || "Z";
        return new Date(match[1] + "T" + match[2] + "." + fraction + offset);
    }

    function readPoint(point, index) {
        return {
            element: point,
            index: index,
            timestamp: parseTimestamp(point.getAttribute("data-chart-timestamp")),
            close: Number(point.getAttribute("data-chart-close") || "0")
        };
    }

    function selectPoints(points, range) {
        if (range === "Max" || points.length === 0) {
            return points;
        }

        var latest = points[points.length - 1].timestamp.getTime();
        var cutoff = latest - (RANGE_DAYS[range] * 24 * 60 * 60 * 1000);
        var selected = points.filter(function (point) {
            var time = point.timestamp.getTime();
            return isNaN(time) ? false : time >= cutoff;
        });

        return selected.length > 0 ? selected : points;
    }

    function createController(container) {
        var svg = container.querySelector("[data-chart-svg]");
        var tooltip = container.querySelector("[data-chart-tooltip]");
        var points = Array.prototype.slice.call(container.querySelectorAll("[data-chart-point]"));
        var line = container.querySelector("[data-chart-line]");
        var rangeButtons = Array.prototype.slice.call(container.querySelectorAll("[data-chart-range-option]"));

        if (!svg || !tooltip || points.length === 0) {
            return;
        }

        var pointRecords = points.map(readPoint);
        var chartLeft = Number(svg.getAttribute("data-chart-left") || "0");
        var chartRight = Number(svg.getAttribute("data-chart-right") || "0");
        var chartTop = Number(svg.getAttribute("data-chart-top") || "0");
        var chartBottom = Number(svg.getAttribute("data-chart-bottom") || "0");
        var chartWidth = Number(svg.getAttribute("data-chart-width") || "0");
        var chartHeight = Number(svg.getAttribute("data-chart-height") || "0");
        var plotWidth = chartWidth - chartLeft - chartRight;
        var plotHeight = chartHeight - chartTop - chartBottom;
        var activeRange = "1Y";

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

        function redraw(selectedPoints) {
            var selectedSet = new Set(selectedPoints.map(function (point) {
                return point.element;
            }));

            // Fail-safe: deselected circles are hidden, but also reset to the
            // origin so a stale `hidden` attribute (e.g. cleared elsewhere)
            // cannot leave a point rendered at an old position.
            pointRecords.forEach(function (point) {
                if (!selectedSet.has(point.element)) {
                    point.element.hidden = true;
                    point.element.setAttribute("cx", "0");
                    point.element.setAttribute("cy", "0");
                }
            });

            var minimum = Math.min.apply(null, selectedPoints.map(function (point) { return point.close; }));
            var maximum = Math.max.apply(null, selectedPoints.map(function (point) { return point.close; }));
            var range = maximum - minimum;
            var path = [];

            selectedPoints.forEach(function (point, selectedIndex) {
                var x = selectedPoints.length === 1
                    ? chartLeft + plotWidth / 2
                    : chartLeft + (selectedIndex * plotWidth / (selectedPoints.length - 1));
                var normalized = range === 0 ? 0.5 : (point.close - minimum) / range;
                var y = chartTop + ((1 - normalized) * plotHeight);

                point.element.hidden = false;
                point.element.setAttribute("cx", x.toFixed(2));
                point.element.setAttribute("cy", y.toFixed(2));
                path.push((selectedIndex === 0 ? "M" : "L") + x.toFixed(2) + "," + y.toFixed(2));
            });

            if (line) {
                line.hidden = selectedPoints.length < 2;
                line.setAttribute("d", path.join(" "));
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

        rangeButtons.forEach(function (button) {
            button.addEventListener("click", function () {
                activeRange = button.getAttribute("data-chart-range-option") || "1Y";
                rangeButtons.forEach(function (option) {
                    option.setAttribute("aria-pressed",
                        option === button ? "true" : "false");
                });
                clearActive();
                redraw(selectPoints(pointRecords, activeRange));
            });
        });

        redraw(selectPoints(pointRecords, activeRange));
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
