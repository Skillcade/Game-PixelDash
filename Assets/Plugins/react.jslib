mergeInto(LibraryManager.library, {
    GameLoaded: function () {
        console.log("GameLoaded");
        window.dispatchReactUnityEvent("GameLoaded");
    },
    ConnectedToServer: function () {
        console.log("ConnectedToServer");
        window.dispatchReactUnityEvent("ConnectedToServer");
    },
});