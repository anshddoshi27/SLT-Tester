// HTML5 drag-and-drop helpers for Blazor WASM (dataTransfer + drop zone)
window.sltExecutorDrag = {
    start(event, payload) {
        event.dataTransfer.setData('text/plain', payload);
        event.dataTransfer.effectAllowed = 'copy';
        this._payload = payload;
    },
    readPayload() {
        return this._payload || '';
    },
    clear() {
        this._payload = null;
    }
};
