export function getWindowSize() {
    return {
        Width: window.innerWidth,
        Height: window.innerHeight
    };
};

export function registerServiceWindowSizeUpdate(dotnetHelper) {
    window.addEventListener('load', () => {
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight);    
    });
    window.addEventListener('resize', () => {
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight);
    });    
}