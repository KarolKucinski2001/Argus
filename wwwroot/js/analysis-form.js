(() => {
    const form = document.querySelector("[data-analysis-form]");

    if (!form) {
        return;
    }

    const submitButton = form.querySelector("[data-submit-button]");
    const pendingStatus = form.querySelector("[data-pending-status]");

    if (!submitButton) {
        return;
    }

    form.addEventListener("submit", (event) => {
        if (form.dataset.pending === "true") {
            event.preventDefault();
            return;
        }

        form.dataset.pending = "true";
        form.setAttribute("aria-busy", "true");
        submitButton.disabled = true;
        submitButton.textContent = "Przygotowywanie analizy...";

        if (pendingStatus) {
            pendingStatus.hidden = false;
        }
    });
})();
