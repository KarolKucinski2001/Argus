// Focused, deterministic check for `selectPoints` in
// wwwroot/js/analysis-chart.js. The repository has no browser/DOM test
// runner, so this script extracts the pure, DOM-free slice of the module
// (RANGE_DAYS + selectPoints, verbatim from the real file) and exercises it
// directly with `node`. Run with:
//   node tests\js\analysis-chart.selectPoints.test.js
"use strict";

var assert = require("assert");
var fs = require("fs");
var path = require("path");
var vm = require("vm");

var scriptPath = path.join(__dirname, "..", "..", "wwwroot", "js", "analysis-chart.js");
var source = fs.readFileSync(scriptPath, "utf8");

var startMarker = "var RANGE_DAYS = {";
var endMarker = "function createController";
var startIndex = source.indexOf(startMarker);
var endIndex = source.indexOf(endMarker);

assert.ok(startIndex !== -1, "expected to find RANGE_DAYS in analysis-chart.js");
assert.ok(endIndex !== -1, "expected to find createController in analysis-chart.js");

// Pull the verbatim RANGE_DAYS + selectPoints definitions out of the real
// file (rather than re-implementing the logic here) so this test fails the
// moment the shipped behavior changes.
var extracted = source.slice(startIndex, endIndex) + "\nmodule.exports = { selectPoints: selectPoints };";

var sandbox = { module: { exports: {} } };
vm.createContext(sandbox);
vm.runInContext(extracted, sandbox, { filename: "analysis-chart-selectPoints-slice.js" });

var selectPoints = sandbox.module.exports.selectPoints;
assert.strictEqual(typeof selectPoints, "function", "selectPoints must be extracted as a function");

function point(isoTimestamp, close) {
    return { timestamp: new Date(isoTimestamp), close: close };
}

function invalidPoint(close) {
    return { timestamp: new Date("not-a-real-timestamp"), close: close };
}

(function malformedFinalTimestampStillFiltersByRealLatest() {
    var oldest = point("2024-01-01T00:00:00Z", 10);
    var withinRange = point("2024-12-20T00:00:00Z", 20);
    var validLatest = point("2024-12-31T00:00:00Z", 30);
    var malformedFinal = invalidPoint(40);

    var points = [oldest, withinRange, validLatest, malformedFinal];
    var selected = selectPoints(points, "1M");

    assert.deepStrictEqual(
        selected,
        [withinRange, validLatest],
        "a malformed final timestamp must not force a full-history fallback for a valid latest point"
    );
})();

(function allMalformedTimestampsFallBackToCompleteHistory() {
    var points = [invalidPoint(1), invalidPoint(2), invalidPoint(3)];
    var selected = selectPoints(points, "1M");

    assert.deepStrictEqual(
        selected,
        points,
        "when no point has a valid timestamp, selectPoints must fall back to the complete history"
    );
})();

(function maxRangeReturnsEveryPointUnfiltered() {
    var points = [point("2024-01-01T00:00:00Z", 10), invalidPoint(20)];
    var selected = selectPoints(points, "Max");

    assert.deepStrictEqual(selected, points, "Max range must bypass filtering entirely");
})();

(function shortHistoryFallsBackWhenNothingSatisfiesTheCutoff() {
    var onlyOldPoint = [point("2020-01-01T00:00:00Z", 10)];
    var selected = selectPoints(onlyOldPoint, "1M");

    assert.deepStrictEqual(
        selected,
        onlyOldPoint,
        "a short history with nothing inside the cutoff must still render instead of an empty chart"
    );
})();

console.log("analysis-chart.selectPoints.test.js: all assertions passed");
