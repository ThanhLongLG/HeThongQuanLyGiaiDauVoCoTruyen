(() => {
    "use strict";

    const closeAll = (except) => {
        document.querySelectorAll(".vct-custom-select.is-open").forEach((component) => {
            if (component !== except) {
                component.classList.remove("is-open");
                component.querySelector(".vct-custom-select__trigger")?.setAttribute("aria-expanded", "false");
            }
        });
    };

    const enhanceSelect = (select) => {
        if (
            select.dataset.vctSelectEnhanced === "true"
            || select.dataset.vctSelect === "false"
            || select.multiple
            || select.size > 1
        ) {
            return;
        }

        select.dataset.vctSelectEnhanced = "true";
        select.classList.add("vct-native-select");

        const component = document.createElement("div");
        component.className = "vct-custom-select";

        const trigger = document.createElement("button");
        trigger.type = "button";
        trigger.className = "vct-custom-select__trigger";
        trigger.setAttribute("aria-haspopup", "listbox");
        trigger.setAttribute("aria-expanded", "false");

        const value = document.createElement("span");
        value.className = "vct-custom-select__value";

        const chevron = document.createElement("span");
        chevron.className = "vct-custom-select__chevron";
        chevron.setAttribute("aria-hidden", "true");

        const optionsPanel = document.createElement("div");
        optionsPanel.className = "vct-custom-select__options";
        optionsPanel.setAttribute("role", "listbox");

        trigger.append(value, chevron);
        component.append(trigger, optionsPanel);
        select.insertAdjacentElement("afterend", component);

        const syncValue = () => {
            const selectedOption = select.options[select.selectedIndex];
            value.textContent = selectedOption?.textContent?.trim() || "Chọn một tùy chọn";
            value.classList.toggle("is-placeholder", !selectedOption?.value);

            optionsPanel.querySelectorAll(".vct-custom-select__option").forEach((optionButton) => {
                const selected = optionButton.dataset.value === select.value;
                optionButton.classList.toggle("is-selected", selected);
                optionButton.setAttribute("aria-selected", selected ? "true" : "false");
            });
        };

        const close = () => {
            component.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
        };

        const syncState = () => {
            trigger.disabled = select.disabled;
            component.classList.toggle("is-disabled", select.disabled);

            if (select.disabled) {
                close();
            }
        };

        const open = () => {
            if (select.disabled) {
                return;
            }
            closeAll(component);
            component.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
        };

        const chooseOption = (option) => {
            if (option.disabled) {
                return;
            }
            select.value = option.value;
            select.dispatchEvent(new Event("input", { bubbles: true }));
            select.dispatchEvent(new Event("change", { bubbles: true }));
            syncValue();
            close();
            trigger.focus();
        };

        const rebuildOptions = () => {
            optionsPanel.replaceChildren();

            Array.from(select.options).forEach((option) => {
                const optionButton = document.createElement("button");
                optionButton.type = "button";
                optionButton.className = "vct-custom-select__option";
                optionButton.dataset.value = option.value;
                optionButton.textContent = option.textContent?.trim() || "";
                optionButton.setAttribute("role", "option");
                optionButton.disabled = option.disabled;
                optionButton.addEventListener("click", () => chooseOption(option));
                optionsPanel.appendChild(optionButton);
            });

            syncValue();
            syncState();
        };

        trigger.addEventListener("click", () => {
            component.classList.contains("is-open") ? close() : open();
        });

        trigger.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                close();
                return;
            }

            if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
                return;
            }

            event.preventDefault();
            open();

            const availableOptions = Array.from(
                optionsPanel.querySelectorAll(".vct-custom-select__option:not(:disabled)")
            );
            const currentIndex = availableOptions.findIndex((option) => option.classList.contains("is-selected"));
            const direction = event.key === "ArrowDown" ? 1 : -1;
            const nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + direction + availableOptions.length) % availableOptions.length;
            availableOptions[nextIndex]?.focus();
        });

        optionsPanel.addEventListener("keydown", (event) => {
            const availableOptions = Array.from(
                optionsPanel.querySelectorAll(".vct-custom-select__option:not(:disabled)")
            );
            const currentIndex = availableOptions.indexOf(document.activeElement);

            if (event.key === "Escape") {
                close();
                trigger.focus();
            } else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                event.preventDefault();
                const direction = event.key === "ArrowDown" ? 1 : -1;
                const nextIndex = (currentIndex + direction + availableOptions.length) % availableOptions.length;
                availableOptions[nextIndex]?.focus();
            }
        });

        select.addEventListener("change", syncValue);
        select.addEventListener("invalid", (event) => {
            event.preventDefault();
            open();
            trigger.focus();
        });
        select.form?.addEventListener("reset", () => {
            window.requestAnimationFrame(syncValue);
        });

        new MutationObserver(rebuildOptions).observe(select, {
            childList: true,
            subtree: true,
            characterData: true,
            attributes: true,
            attributeFilter: ["disabled"]
        });

        document.addEventListener("click", (event) => {
            if (!component.contains(event.target)) {
                close();
            }
        });

        rebuildOptions();
    };

    const initialize = (root = document) => {
        if (root instanceof Element && root.matches("select")) {
            enhanceSelect(root);
        }

        root.querySelectorAll?.("select").forEach(enhanceSelect);
    };

    const start = () => {
        initialize();

        new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node instanceof Element) {
                        initialize(node);
                    }
                });
            });
        }).observe(document.body, {
            childList: true,
            subtree: true
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }
})();
