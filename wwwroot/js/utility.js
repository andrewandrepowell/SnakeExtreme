export function getWindowSize() {
    return {
        Width: window.innerWidth,
        Height: window.innerHeight
    };
};

export function registerServiceUpdates(dotnetHelper) {
    window.addEventListener('load', () => {
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight);    
    });
    window.addEventListener('resize', () => {
        // This is left here purely for debugging purposes.
        // Currently, the only way to see log messages on mobile is to use the alert box.
        // It's a bit hacky, but it gets the job done.
        //dotnetHelper.invokeMethodAsync('GetLog').then(data => {
        //    window.alert(data);
        //});
        dotnetHelper.invokeMethodAsync('ServiceWindowSizeUpdate', window.innerWidth, window.innerHeight);
    });
    window.document.addEventListener('touchstart', (event) => {
        let array = new Array(event.touches.length);              
        for (let i = 0; i < event.touches.length; i++) {
            let touch = event.touches[i];
            array[i] = {
                X: touch.pageX,
                Y: touch.pageY
            };            
        }        
        dotnetHelper.invokeMethodAsync('ServiceTouchStartUpdate', array);
    })
}