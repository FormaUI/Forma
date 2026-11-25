// A ZAP HTTP sender script recording each response of 500 or above that any part of ZAP receives, the active
// scanner's among them, as a line of its status, its method and its address in server-errors.txt.

const Files = Java.type('java.nio.file.Files');
const Paths = Java.type('java.nio.file.Paths');
const StandardCharsets = Java.type('java.nio.charset.StandardCharsets');
const StandardOpenOption = Java.type('java.nio.file.StandardOpenOption');

const record = Paths.get('/zap/wrk/server-errors.txt');

// Made when the script loads, so a record that is missing means the script never ran rather than that no server error
// occurred.
Files.writeString(record, '', StandardCharsets.UTF_8, StandardOpenOption.CREATE, StandardOpenOption.APPEND);

function sendingRequest(message, initiator, helper) {
}

function responseReceived(message, initiator, helper) {
    const status = message.getResponseHeader().getStatusCode();

    if (status < 500) {
        return;
    }

    const requestHeader = message.getRequestHeader();
    const line = `${status} ${requestHeader.getMethod()} ${requestHeader.getURI().toString()}\n`;

    Files.writeString(record, line, StandardCharsets.UTF_8, StandardOpenOption.CREATE, StandardOpenOption.APPEND);
}
