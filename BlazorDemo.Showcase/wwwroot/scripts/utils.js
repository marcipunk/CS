"use strict";

export function setThemeData(...themeData) {
    for (const { key, value } of themeData)
        setCookie(key, value);
}

export function getThemeData(key) {
    const value = getCookie(key);
    if (value)
        return value.trim();
    return "";
}

function setCookie(key, value) {
    const date = new Date();
    date.setFullYear(date.getFullYear() + 1);
    document.cookie = encodeURIComponent(key) + '=' + encodeURIComponent(value.toString()) + '; expires=' + date.toGMTString() + '; path=/';
}

function getCookie(key) {
    const keyValuePairs = document.cookie.split(';').map(entry => entry.trim().split('='));
    if (keyValuePairs.length > 0) {
        const entry = keyValuePairs.find(([k, v]) => decodeURIComponent(k) === key);
        if (entry)
            return decodeURIComponent(entry[1]);
    }
}

export async function copy(textToCopy) {
    if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(textToCopy);
    } else {
        const textArea = document.createElement("textarea");
        textArea.value = textToCopy;

        textArea.style.position = "absolute";
        textArea.style.left = "-999999px";

        document.body.prepend(textArea);
        textArea.select();

        try {
            document.execCommand('copy');
        } catch (error) {
            console.error(error);
        } finally {
            textArea.remove();
        }
    }
}

export function setBodyClass(className) {
    document.body.className = className;
}
