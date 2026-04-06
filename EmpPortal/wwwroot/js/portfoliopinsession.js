let targetPage = "";

const CORRECT_PIN = "2620";
const PIN_KEY = "PIN_VERIFIED";
const PIN_TIME_KEY = "PIN_TIME";
//const SESSION_TIMEOUT_MINUTES = 5;
const SESSION_TIMEOUT_MINUTES = 180;

function openPinModal(page) {
    targetPage = page;

    if (isPinSessionValid()) {
        redirectAfterPin();
        return;
    }

    document.getElementById("pinInput").value = "";
    document.getElementById("pinError").classList.add("d-none");

    const modal = new bootstrap.Modal(
        document.getElementById("pinModal")
    );
    modal.show();
}
function verifyPin() {
    const enteredPin = document.getElementById("pinInput").value;

    if (enteredPin === CORRECT_PIN) {
        setPinSession();
        redirectAfterPin();
    } else {
        document.getElementById("pinError").classList.remove("d-none");
    }
}
function redirectAfterPin() {
    if (targetPage === "products") {
        window.location.href = "/Home/Index";
    } else if (targetPage === "visitor") {
        window.location.href = "/Testpage/Index";
    }
}
function setPinSession() {
    sessionStorage.setItem(PIN_KEY, "true");
    sessionStorage.setItem(PIN_TIME_KEY, new Date().getTime());
}

function isPinSessionValid() {
    const verified = sessionStorage.getItem(PIN_KEY);
    const time = sessionStorage.getItem(PIN_TIME_KEY);

    if (!verified || !time) return false;

    const diff =
        (new Date().getTime() - parseInt(time)) / (1000 * 60);

    if (diff > SESSION_TIMEOUT_MINUTES) {
        clearPinSession();
        return false;
    }
    return true;
}

function clearPinSession() {
    sessionStorage.removeItem(PIN_KEY);
    sessionStorage.removeItem(PIN_TIME_KEY);
}
